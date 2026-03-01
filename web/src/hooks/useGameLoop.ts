import { useCallback, useEffect, useRef, useState } from 'react';
import { Application, TextureSource } from 'pixi.js';
import { GameModel } from '../model/GameModel';
import { Player } from '../model/Player';
import { Floor } from '../model/Floor';
import { Vector2Int } from '../core/Vector2Int';
import { Faction } from '../core/types';
import { Camera, SpriteManager, GameRenderer, AnimationPlayer } from '../renderer';
import { InputHandler, type PlayerIntent } from '../input/InputHandler';
import { FollowPathTask } from '../model/tasks/FollowPathTask';
import { AttackTask } from '../model/tasks/AttackTask';
import { WaitTask } from '../model/tasks/WaitTask';
import { MoveNextToTargetTask } from '../model/tasks/MoveNextToTargetTask';
import { Item, EquippableItem, STACKABLE_TAG, DURABLE_TAG, EDIBLE_TAG, USABLE_TAG, type IStackable, type IDurable, type IEdible, type IUsable } from '../model/Item';
import { StackingStatus } from '../model/Status';
import { ON_TOP_ACTION_HANDLER, type IOnTopActionHandler } from '../core/types';

// ─── Snapshot types for React consumption ───

export interface ItemSnapshot {
  displayName: string;
  spriteName: string;
  category: string;
  index: number;
  stacks: number | null;
  durability: number | null;
  maxDurability: number | null;
  methods: string[];
  statsFull: string;
}

export interface StatusSnapshot {
  displayName: string;
  className: string;
  stacks: number | null;
  isDebuff: boolean;
}

export interface GameOverInfo {
  won: boolean;
  turnsTaken: number;
  killedBy: string | null;
  enemiesDefeated: number;
  damageDealt: number;
  damageTaken: number;
}

export interface OnTopActionSnapshot {
  name: string;
  spriteName: string;
}

export interface GameState {
  hp: number;
  maxHp: number;
  depth: number;
  turn: number;
  enemyCount: number;
  isPlayerDead: boolean;
  isCleared: boolean;
  inventoryItems: (ItemSnapshot | null)[];
  equipmentItems: (ItemSnapshot | null)[];
  statuses: StatusSnapshot[];
  gameOver: GameOverInfo | null;
  onTopAction: OnTopActionSnapshot | null;
}

const EMPTY_STATE: GameState = {
  hp: 0, maxHp: 0, depth: 0, turn: 0, enemyCount: 0,
  isPlayerDead: false, isCleared: false,
  inventoryItems: [], equipmentItems: [],
  statuses: [], gameOver: null, onTopAction: null,
};

function getItemCategory(item: Item): string {
  if (item instanceof EquippableItem) {
    const slotNames = ['Headwear', 'Weapon', 'Armor', 'Offhand', 'Footwear'];
    return slotNames[item.slot] ?? 'Equipment';
  }
  if (EDIBLE_TAG in item) return 'Food';
  return 'Item';
}

function snapshotItem(item: Item | null, index: number): ItemSnapshot | null {
  if (!item) return null;
  return {
    displayName: item.displayName,
    spriteName: item.constructor.name.startsWith('Item')
      ? item.constructor.name.substring(4).toLowerCase()
      : item.constructor.name.toLowerCase(),
    category: getItemCategory(item),
    index,
    stacks: STACKABLE_TAG in item ? (item as unknown as IStackable).stacks : null,
    durability: DURABLE_TAG in item ? (item as unknown as IDurable).durability : null,
    maxDurability: DURABLE_TAG in item ? (item as unknown as IDurable).maxDurability : null,
    methods: item.getAvailableMethods(),
    statsFull: item.getStatsFull(),
  };
}

/**
 * Main game loop hook. Creates model, renderer, input; orchestrates turn cycle.
 * Returns a ref to attach to the canvas container div + reactive game state.
 */
export function useGameLoop() {
  const containerRef = useRef<HTMLDivElement>(null);
  const [gameState, setGameState] = useState<GameState>(EMPTY_STATE);
  const [ready, setReady] = useState(false);

  // Refs for mutable objects that persist across renders
  const modelRef = useRef<GameModel | null>(null);
  const rendererRef = useRef<GameRenderer | null>(null);
  const animatorRef = useRef<AnimationPlayer | null>(null);
  const inputRef = useRef<InputHandler | null>(null);
  const processingRef = useRef(false);

  const readState = useCallback((): GameState => {
    const model = modelRef.current;
    if (!model) return EMPTY_STATE;
    const player = model.player;
    const floor = model.currentFloor;

    // Inventory snapshot
    const inventoryItems: (ItemSnapshot | null)[] = [];
    for (let i = 0; i < player.inventory.capacity; i++) {
      inventoryItems.push(snapshotItem(player.inventory.getAt(i), i));
    }

    // Equipment snapshot (filter out ItemHands fallback)
    const equipmentItems: (ItemSnapshot | null)[] = [];
    for (let i = 0; i < player.equipment.capacity; i++) {
      const raw = player.equipment.getAt(i);
      const item = raw?.constructor.name === 'ItemHands' ? null : raw;
      equipmentItems.push(snapshotItem(item, i));
    }

    // Status snapshot
    const statuses: StatusSnapshot[] = player.statuses.list.map(s => ({
      displayName: s.displayName,
      className: s.constructor.name,
      stacks: s instanceof StackingStatus ? s.stacks : null,
      isDebuff: s.isDebuff,
    }));

    // Game over info
    const isOver = player.isDead || floor.isCleared;
    const gameOver: GameOverInfo | null = isOver ? { ...model.stats } : null;

    // On-top action
    let onTopAction: OnTopActionSnapshot | null = null;
    if (!isOver) {
      const handler = getOnTopActionHandler(floor, player.pos);
      if (handler) {
        onTopAction = {
          name: handler.onTopActionName,
          spriteName: handler.displayName.toLowerCase(),
        };
      }
    }

    return {
      hp: player.hp,
      maxHp: player.maxHp,
      depth: floor.depth,
      turn: Math.floor(model.time),
      enemyCount: countEnemies(floor),
      isPlayerDead: player.isDead,
      isCleared: floor.isCleared,
      inventoryItems,
      equipmentItems,
      statuses,
      gameOver,
      onTopAction,
    };
  }, []);

  const syncAndUpdate = useCallback(() => {
    rendererRef.current?.syncToModel();
    setGameState(readState());
  }, [readState]);

  /** Step model, play animations, sync renderer. */
  const stepAndAnimate = useCallback(async () => {
    const model = modelRef.current;
    const renderer = rendererRef.current;
    const animator = animatorRef.current;
    const input = inputRef.current;
    if (!model || !renderer || !animator || !input) return;

    processingRef.current = true;
    input.setEnabled(false);

    model.turnManager.stepUntilPlayerChoice();

    const events = model.consumeAnimationEvents();
    if (events.length > 0) {
      let skipRegistered = false;
      const skipHandler = () => { if (skipRegistered) animator.skip(); };
      requestAnimationFrame(() => {
        skipRegistered = true;
        window.addEventListener('keydown', skipHandler, { once: true });
        window.addEventListener('pointerdown', skipHandler, { once: true });
      });
      try {
        await animator.play(events);
      } finally {
        window.removeEventListener('keydown', skipHandler);
        window.removeEventListener('pointerdown', skipHandler);
      }
    }

    renderer.syncToModel();
    setGameState(readState());

    processingRef.current = false;
    input.setEnabled(true);
  }, [readState]);

  /** Process a player intent: assign task to player, step model, animate, sync. */
  const processIntent = useCallback(async (intent: PlayerIntent) => {
    const model = modelRef.current;
    if (!model) return;
    if (processingRef.current) return;
    if (model.player.isDead || model.currentFloor.isCleared) return;

    const player = model.player;
    const floor = model.currentFloor;

    const task = resolveIntent(intent, player, floor);
    if (!task) return;

    player.setTasks(task);
    await stepAndAnimate();
  }, [stepAndAnimate]);

  /** Execute an item action from the inventory/equipment UI. */
  const executeItemAction = useCallback(async (
    source: 'inventory' | 'equipment',
    slotIndex: number,
    action: string,
  ) => {
    const model = modelRef.current;
    if (!model || processingRef.current || model.player.isDead || model.currentFloor.isCleared) return;

    const player = model.player;
    const container = source === 'inventory' ? player.inventory : player.equipment;
    const item = container.getAt(slotIndex);
    if (!item || item.constructor.name === 'ItemHands') return;

    switch (action) {
      case 'Drop':
        item.Drop(player);
        break;
      case 'Eat':
        if (EDIBLE_TAG in item) (item as unknown as IEdible).eat(player);
        break;
      case 'Use':
        if (USABLE_TAG in item) (item as unknown as IUsable).use(player);
        break;
      case 'Equip':
        if ('Equip' in item) (item as any).Equip(player);
        break;
      case 'Unequip':
        if ('Unequip' in item) (item as any).Unequip(player);
        break;
      default:
        return;
    }

    // Drop/Eat/Use cost a turn — step the model
    const costsTurn = ['Drop', 'Eat', 'Use'].includes(action);
    if (costsTurn) {
      player.setTasks(new WaitTask(player, 1));
      await stepAndAnimate();
    } else {
      rendererRef.current?.syncToModel();
      setGameState(readState());
    }
  }, [readState, stepAndAnimate]);

  /** Execute the on-top action at the player's current position. */
  const executeOnTopAction = useCallback(async () => {
    const model = modelRef.current;
    if (!model || processingRef.current || model.player.isDead || model.currentFloor.isCleared) return;

    const handler = getOnTopActionHandler(model.currentFloor, model.player.pos);
    if (!handler) return;

    handler.handleOnTopAction();

    // If the handler set a player task (e.g. Fern's Cut via GenericPlayerTask), step the model
    if (model.player.task) {
      await stepAndAnimate();
    } else {
      syncAndUpdate();
    }
  }, [stepAndAnimate, syncAndUpdate]);

  /** Reset game (play again). */
  const resetGame = useCallback(() => {
    const renderer = rendererRef.current;
    if (!renderer) return;

    const newModel = GameModel.createDailyGame();
    newModel.consumeAnimationEvents();
    modelRef.current = newModel;

    renderer.setFloor(newModel.currentFloor);
    renderer.syncToModel();
    renderer.camera.resize(
      renderer.app.screen.width,
      renderer.app.screen.height,
      newModel.currentFloor.width,
      newModel.currentFloor.height,
    );
    setGameState(readState());
  }, [readState]);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    let input: InputHandler | null = null;
    let resizeHandler: (() => void) | null = null;
    let keyHandler: ((e: KeyboardEvent) => void) | null = null;
    let destroyed = false;
    let appReady = false;
    let pixiApp: Application | null = null;

    async function init() {
      if (destroyed) return;

      TextureSource.defaultOptions.scaleMode = 'nearest';

      const app = new Application();
      pixiApp = app;
      await app.init({
        width: container!.clientWidth,
        height: container!.clientHeight,
        backgroundColor: 0x111118,
      });
      if (destroyed) { app.destroy(true, { children: true }); return; }
      appReady = true;
      container!.appendChild(app.canvas);

      const sprites = new SpriteManager();
      await sprites.load();
      if (destroyed) { app.destroy(true, { children: true }); return; }

      const model = GameModel.createDailyGame();
      model.consumeAnimationEvents();
      modelRef.current = model;

      const camera = new Camera();
      const renderer = new GameRenderer(app, camera, sprites);
      rendererRef.current = renderer;

      const animator = new AnimationPlayer(renderer, camera);
      animatorRef.current = animator;

      renderer.setFloor(model.currentFloor);
      renderer.syncToModel();

      input = new InputHandler(camera, app.canvas);
      inputRef.current = input;
      input.onIntent.on(processIntent);
      input.attach();

      resizeHandler = () => {
        camera.resize(app.screen.width, app.screen.height, model.currentFloor.width, model.currentFloor.height);
        renderer.rebuildAll();
      };
      window.addEventListener('resize', resizeHandler);

      if (import.meta.env.DEV) {
        keyHandler = (e: KeyboardEvent) => {
          if (e.key === 'r' || e.key === 'R') {
            if (processingRef.current) return;
            const newModel = GameModel.createDailyGame(String(Date.now()));
            newModel.consumeAnimationEvents();
            modelRef.current = newModel;
            renderer.setFloor(newModel.currentFloor);
            renderer.syncToModel();
            camera.resize(app.screen.width, app.screen.height, newModel.currentFloor.width, newModel.currentFloor.height);
            setGameState(readState());
          }
        };
        window.addEventListener('keydown', keyHandler);
      }

      setGameState(readState());
      setReady(true);
    }

    init();

    return () => {
      destroyed = true;
      input?.detach();
      if (resizeHandler) window.removeEventListener('resize', resizeHandler);
      if (keyHandler) window.removeEventListener('keydown', keyHandler);
      if (pixiApp && appReady) {
        pixiApp.destroy(true, { children: true });
      }
      modelRef.current = null;
      rendererRef.current = null;
      animatorRef.current = null;
      inputRef.current = null;
    };
  }, [processIntent, readState]);

  return { containerRef, gameState, ready, executeItemAction, executeOnTopAction, resetGame };
}

/** Translate a PlayerIntent into an ActorTask for the player. */
function resolveIntent(
  intent: PlayerIntent,
  player: Player,
  floor: Floor,
): import('../model/ActorTask').ActorTask | null {
  switch (intent.type) {
    case 'wait':
      return new WaitTask(player, 1);

    case 'move': {
      const target = Vector2Int.add(player.pos, intent.direction);
      const bodyAtTarget = floor.bodies.get(target);
      if (bodyAtTarget && bodyAtTarget !== player && 'hp' in bodyAtTarget) {
        return new AttackTask(player, bodyAtTarget as any);
      }
      const tile = floor.tiles.get(target);
      if (tile && tile.canBeOccupiedBy(player)) {
        return new FollowPathTask(player, target, [target]);
      }
      return null;
    }

    case 'click': {
      const { tilePos } = intent;
      if (Vector2Int.equals(tilePos, player.pos)) {
        return new WaitTask(player, 1);
      }
      if (Vector2Int.chebyshevDistance(player.pos, tilePos) === 1) {
        const bodyAtTarget = floor.bodies.get(tilePos);
        if (bodyAtTarget && bodyAtTarget !== player && 'hp' in bodyAtTarget) {
          return new AttackTask(player, bodyAtTarget as any);
        }
      }
      const bodyAtTarget = floor.bodies.get(tilePos);
      if (bodyAtTarget && bodyAtTarget !== player && 'hp' in bodyAtTarget) {
        return new MoveNextToTargetTask(player, bodyAtTarget.pos);
      }
      const path = floor.findPath(player.pos, tilePos, false, player);
      if (path.length > 0) {
        return new FollowPathTask(player, tilePos, path);
      }
      return null;
    }
  }
}

/** Scan entities at pos in layer order: item → grass → tile. Returns first IOnTopActionHandler. */
function getOnTopActionHandler(floor: Floor, pos: Vector2Int): (IOnTopActionHandler & { displayName: string }) | null {
  const tile = floor.tiles.get(pos);
  if (!tile) return null;
  // Check item layer
  const item = floor.items.get(pos);
  if (item && ON_TOP_ACTION_HANDLER in item) return item as any;
  // Check grass layer
  const grass = floor.grasses.get(pos);
  if (grass && ON_TOP_ACTION_HANDLER in grass) return grass as any;
  // Check tile itself
  if (ON_TOP_ACTION_HANDLER in tile) return tile as any;
  return null;
}

function countEnemies(floor: Floor): number {
  let count = 0;
  for (const body of floor.bodies) {
    if ('faction' in body && (body as any).faction === Faction.Enemy) {
      count++;
    }
  }
  return count;
}
