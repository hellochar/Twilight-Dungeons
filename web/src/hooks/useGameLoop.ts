import { useCallback, useEffect, useRef, useState } from 'react';
import { Application, TextureSource } from 'pixi.js';
import { GameModel } from '../model/GameModel';
import { Player } from '../model/Player';
import { Floor } from '../model/Floor';
import { Vector2Int } from '../core/Vector2Int';
import { Faction } from '../core/types';
import { Camera, SpriteManager, GameRenderer, AnimationPlayer } from '../renderer';
import { InputHandler, type PlayerIntent, type TileContextEvent } from '../input/InputHandler';
import { SoundManager } from '../audio/SoundManager';
import { Boss } from '../model/enemies/Boss';
import { WaitBaseAction, GenericBaseAction } from '../model/BaseAction';
import type { EntityInfoData } from '../ui/EntityInfoPopup';
import type { GameEvent } from '../renderer/AnimationPlayer';
import { Body } from '../model/Body';
import { FollowPathTask } from '../model/tasks/FollowPathTask';
import { AttackTask } from '../model/tasks/AttackTask';
import { WaitTask } from '../model/tasks/WaitTask';
import { MoveNextToTargetTask } from '../model/tasks/MoveNextToTargetTask';
import { Item, EquippableItem, STACKABLE_TAG, DURABLE_TAG, EDIBLE_TAG, USABLE_TAG, TARGETED_ACTION_TAG, type IStackable, type IDurable, type IEdible, type IUsable, type ITargetedAction } from '../model/Item';
import type { Entity } from '../model/Entity';
import { StackingStatus } from '../model/Status';
import { ON_TOP_ACTION_HANDLER, type IOnTopActionHandler } from '../core/types';
import { loadDebugState } from '../debug/DebugPanel';

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

/** Internal targeting data stored in a ref (accessed by processIntent without re-render deps). */
interface TargetingInfo {
  source: 'inventory' | 'equipment';
  slotIndex: number;
  actionName: string;
  targetMap: Map<string, Entity>; // Vector2Int.key() → target entity
}

/** Exported targeting state for UI consumption. */
export interface TargetingState {
  actionName: string;
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
  dateSeed: string;
}

const EMPTY_STATE: GameState = {
  hp: 0, maxHp: 0, depth: 0, turn: 0, enemyCount: 0,
  isPlayerDead: false, isCleared: false,
  inventoryItems: [], equipmentItems: [],
  statuses: [], gameOver: null, onTopAction: null,
  dateSeed: '',
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
  const targetingRef = useRef<TargetingInfo | null>(null);
  const [targetingState, setTargetingState] = useState<TargetingState | null>(null);
  const proposedTargetRef = useRef<Vector2Int | null>(null);
  const [debugNotice, setDebugNotice] = useState<string | null>(null);
  const [entityInfo, setEntityInfo] = useState<EntityInfoData | null>(null);

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

    // Game over info — only set when model.gameOver() has been called
    const gameOver: GameOverInfo | null = model.gameOverInfo ? { ...model.gameOverInfo } : null;

    // On-top action (hide when player is dead)
    let onTopAction: OnTopActionSnapshot | null = null;
    if (!player.isDead) {
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
      dateSeed: model.dateSeed,
    };
  }, []);

  const syncAndUpdate = useCallback(() => {
    rendererRef.current?.syncToModel();
    setGameState(readState());
  }, [readState]);

  const cancelTargeting = useCallback(() => {
    targetingRef.current = null;
    setTargetingState(null);
    rendererRef.current?.clearTargetHighlights();
  }, []);

  const clearProposed = useCallback(() => {
    proposedTargetRef.current = null;
    rendererRef.current?.clearProposedPath();
  }, []);

  /** Build EntityInfoData from whatever entity/tile is at a given tile position. */
  const handleContextMenu = useCallback((event: TileContextEvent) => {
    const model = modelRef.current;
    if (!model) return;
    const floor = model.currentFloor;
    const { tilePos, screenX, screenY } = event;

    // Priority: body > grass > item on ground > tile
    const body = floor.bodies.get(tilePos);
    if (body) {
      const b = body as any as Body;
      setEntityInfo({
        name: body.displayName,
        typeName: body.constructor.name,
        hp: b.hp,
        maxHp: b.maxHp,
        x: screenX,
        y: screenY,
      });
      return;
    }

    const grass = floor.grasses.get(tilePos);
    if (grass) {
      setEntityInfo({
        name: grass.displayName,
        typeName: grass.constructor.name,
        x: screenX,
        y: screenY,
      });
      return;
    }

    const item = floor.items.get(tilePos);
    if (item) {
      // ItemOnGround wraps a held item — show that item's info
      const heldItem = (item as any).heldItem;
      if (heldItem) {
        setEntityInfo({
          name: heldItem.displayName,
          typeName: heldItem.constructor.name,
          stats: heldItem.getStatsFull?.() ?? '',
          x: screenX,
          y: screenY,
        });
      } else {
        setEntityInfo({
          name: item.displayName,
          typeName: item.constructor.name,
          x: screenX,
          y: screenY,
        });
      }
      return;
    }

    const tile = floor.tiles.get(tilePos);
    if (tile) {
      setEntityInfo({
        name: tile.displayName,
        typeName: tile.constructor.name,
        x: screenX,
        y: screenY,
      });
    }
  }, []);

  const beginTargeting = useCallback((source: 'inventory' | 'equipment', slotIndex: number) => {
    const model = modelRef.current;
    const renderer = rendererRef.current;
    if (!model || !renderer) return;

    const container = source === 'inventory' ? model.player.inventory : model.player.equipment;
    const item = container.getAt(slotIndex);
    if (!item || !(TARGETED_ACTION_TAG in item)) return;

    const targeted = item as unknown as ITargetedAction;
    const targets = targeted.targets(model.player);
    if (targets.length === 0) return;

    const targetMap = new Map<string, Entity>();
    const positions: Vector2Int[] = [];
    for (const t of targets) {
      const key = Vector2Int.key(t.pos);
      targetMap.set(key, t);
      positions.push(t.pos);
    }

    targetingRef.current = { source, slotIndex, actionName: targeted.targetedActionName, targetMap };
    setTargetingState({ actionName: targeted.targetedActionName });
    renderer.showTargetHighlights(positions);
  }, []);

  // Skip-all flag: any input during animation loop sets this to skip all delays
  const skipAllRef = useRef(false);

  /** Async incremental step loop: step one entity at a time with stagger delays. */
  const stepAndAnimate = useCallback(async () => {
    const model = modelRef.current;
    const renderer = rendererRef.current;
    const animator = animatorRef.current;
    const input = inputRef.current;
    if (!model || !renderer || !animator || !input) return;

    processingRef.current = true;
    input.setEnabled(false);
    skipAllRef.current = false;

    // Register skip-all handler after one frame so the triggering keypress doesn't count
    const skipHandler = () => { skipAllRef.current = true; animator.skip(); };
    const skipRAF = requestAnimationFrame(() => {
      window.addEventListener('keydown', skipHandler);
      window.addEventListener('pointerdown', skipHandler);
    });

    try {
      model.turnManager.beginStepSession();

      while (true) {
        const result = model.turnManager.stepOneEntity();

        if (result.done) {
          // Timed events (e.g. poison tickDamage) may have fired during this step.
          // Play their animation events before exiting.
          const doneEvents = model.consumeAnimationEvents();
          if (doneEvents.length > 0 && !skipAllRef.current) {
            await animator.playBatch(doneEvents);
          }
          break;
        }

        // Time-gap delay (matches Unity GAME_TIME_TO_SECONDS_WAIT_SCALE = 0.2)
        if (!result.isFirstStep && result.timeGap > 0 && !result.shouldSpeedThrough && !skipAllRef.current) {
          await delay(result.timeGap * 200);
        }

        // Consume and play animation events for this step
        const events: GameEvent[] = model.consumeAnimationEvents();
        if (events.length > 0) {
          // Mark attack-animated guids so lerp doesn't interfere with bump-and-return.
          // Moves are NOT marked — they're driven by lerpPositions (matching Unity).
          const animatedGuids = new Set<string>();
          for (const ev of events) {
            if (ev.type === 'attack' || ev.type === 'attackGround' || ev.type === 'jump' || ev.type === 'struggle') {
              animatedGuids.add(ev.entityGuid);
              renderer.animatingGuids.add(ev.entityGuid);
            }
          }

          if (skipAllRef.current) {
            // Skip: just sync positions immediately
            for (const guid of animatedGuids) {
              renderer.animatingGuids.delete(guid);
            }
          } else {
            try {
              await animator.playBatch(events);
            } finally {
              for (const guid of animatedGuids) {
                renderer.animatingGuids.delete(guid);
              }
            }
          }
        }

        // Sync after each step so subsequent animations see updated positions
        renderer.syncToModel();

        // Stagger delay between visible enemy turns (Unity JUICE_STAGGER_SECONDS = 0.02)
        if (result.shouldStagger && !skipAllRef.current && !result.shouldSpeedThrough) {
          await delay(20);
        }
      }
    } catch (e) {
      console.error('Error during step/animation loop:', e);
    } finally {
      cancelAnimationFrame(skipRAF);
      window.removeEventListener('keydown', skipHandler);
      window.removeEventListener('pointerdown', skipHandler);

      try {
        renderer.syncToModel();
      } catch (e) {
        console.error('Error syncing renderer after step loop:', e);
      }
      setGameState(readState());

      processingRef.current = false;
      input.setEnabled(true);
    }
  }, [readState]);

  /** Process a player intent: assign task to player, step model, animate, sync. */
  const processIntent = useCallback(async (intent: PlayerIntent) => {
    const model = modelRef.current;
    if (!model) return;
    if (processingRef.current) return;
    if (model.player.isDead) return;

    // ─── Targeting mode intercept ───
    const targeting = targetingRef.current;
    if (targeting) {
      if (intent.type === 'click') {
        const key = Vector2Int.key(intent.tilePos);
        const target = targeting.targetMap.get(key);
        if (target) {
          // Valid target selected — execute the targeted action
          cancelTargeting();
          const container = targeting.source === 'inventory'
            ? model.player.inventory : model.player.equipment;
          const item = container.getAt(targeting.slotIndex);
          if (item && TARGETED_ACTION_TAG in item) {
            (item as unknown as ITargetedAction).performTargetedAction(model.player, target);
            // If performTargetedAction set player tasks (e.g. ChaseTargetTask), step normally.
            // Otherwise treat as a single-turn action.
            if (!model.player.task) {
              model.player.setTasks(new WaitTask(model.player, 1));
            }
            await stepAndAnimate();
          }
        } else {
          cancelTargeting();
        }
      } else {
        // move / wait / cancel all cancel targeting
        cancelTargeting();
      }
      return;
    }

    const player = model.player;
    const floor = model.currentFloor;

    // ─── Two-click path preview (in combat only) ───
    // First click: show path dots + reticle. Second click on same tile: execute.
    if (intent.type === 'click') {
      const { tilePos } = intent;
      const isInCombat = floor.depth > 0 && !floor.isCleared;
      const isAdjacent = Vector2Int.chebyshevDistance(player.pos, tilePos) === 1;
      if (isInCombat && !Vector2Int.equals(tilePos, player.pos) && !isAdjacent) {
        if (proposedTargetRef.current && Vector2Int.equals(proposedTargetRef.current, tilePos)) {
          // Second click on same tile — clear dots visually, keep ref for post-execution refresh
          rendererRef.current?.clearProposedPath();
          // fall through to execute
        } else {
          // First click — compute task, show path preview if it's a FollowPathTask
          const previewTask = resolveIntent(intent, player, floor);
          if (previewTask instanceof FollowPathTask) {
            clearProposed();
            proposedTargetRef.current = previewTask.target;
            rendererRef.current?.showProposedPath(previewTask.target, [...previewTask.path]);
            return; // don't execute yet
          }
          // No valid path — clear proposed and bail
          clearProposed();
          return;
        }
      } else {
        clearProposed();
      }
    } else {
      clearProposed();
    }

    const task = resolveIntent(intent, player, floor);
    if (!task) { clearProposed(); return; }

    player.setTasks(task);
    await stepAndAnimate();

    // After execution, refresh proposed path so the player can keep clicking the same target
    const pendingTarget = proposedTargetRef.current;
    if (pendingTarget) {
      const newPath = floor.findPath(player.pos, pendingTarget, false, player);
      if (!player.isDead && !floor.isCleared && newPath.length > 0) {
        rendererRef.current?.showProposedPath(pendingTarget, newPath);
      } else {
        clearProposed();
      }
    }
  }, [stepAndAnimate, cancelTargeting, clearProposed]);

  /** Execute an item action from the inventory/equipment UI. */
  const executeItemAction = useCallback(async (
    source: 'inventory' | 'equipment',
    slotIndex: number,
    action: string,
  ) => {
    const model = modelRef.current;
    if (!model || processingRef.current || model.player.isDead) return;

    const player = model.player;
    const container = source === 'inventory' ? player.inventory : player.equipment;
    const item = container.getAt(slotIndex);
    if (!item || item.constructor.name === 'ItemHands') return;

    // Check if this is a targeted action (e.g. "Place", "Charm")
    if (TARGETED_ACTION_TAG in item && (item as unknown as ITargetedAction).targetedActionName === action) {
      beginTargeting(source, slotIndex);
      return;
    }

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
      default: {
        // Custom action (e.g. "Germinate", "Refine") — call method by camelCase name
        const methodName = action[0].toLowerCase() + action.slice(1);
        if (typeof (item as any)[methodName] === 'function') {
          (item as any)[methodName](player);
          break;
        }
        return;
      }
    }

    // Drop/Eat/Use/custom actions cost a turn — only Equip/Unequip are free
    const costsTurn = !['Equip', 'Unequip'].includes(action);
    if (costsTurn) {
      player.setTasks(new WaitTask(player, 1));
      await stepAndAnimate();
    } else {
      rendererRef.current?.syncToModel();
      setGameState(readState());
    }
  }, [readState, stepAndAnimate, beginTargeting]);

  /** Execute the on-top action at the player's current position. */
  const executeOnTopAction = useCallback(async () => {
    const model = modelRef.current;
    if (!model || processingRef.current || model.player.isDead) return;

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

  // Dev-only: R to regen same depth with new seed, +/= to go deeper, - to go shallower
  useEffect(() => {
    if (!import.meta.env.DEV) return;

    const onKey = (e: KeyboardEvent) => {
      const renderer = rendererRef.current;
      if (!renderer) return;
      const currentDepth = modelRef.current?.currentFloor.depth ?? 0;

      let depth: number;
      if (e.key === 'r' || e.key === 'R') {
        depth = currentDepth;
      } else if (e.key === '+' || e.key === '=') {
        depth = Math.min(27, currentDepth + 1);
      } else if (e.key === '-') {
        depth = Math.max(0, currentDepth - 1);
      } else {
        return;
      }

      const seed = String(Date.now());
      const newModel = GameModel.createDailyGame(seed, depth);
      newModel.consumeAnimationEvents();
      modelRef.current = newModel;
      renderer.setFloor(newModel.currentFloor);
      renderer.syncToModel();
      renderer.camera.resize(renderer.app.screen.width, renderer.app.screen.height, newModel.currentFloor.width, newModel.currentFloor.height);
      setGameState(readState());
      setDebugNotice(`depth ${depth} (seed …${seed.slice(-5)})`);
      setTimeout(() => setDebugNotice(null), 3000);
    };

    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [readState]);

  /** Make the player wait one turn — mirrors Unity WaitButtonController.HandleWaitPressed(). */
  const executeWait = useCallback(async () => {
    const model = modelRef.current;
    if (!model || processingRef.current || model.player.isDead) return;
    model.player.setTasks(new WaitTask(model.player, 1));
    await stepAndAnimate();
  }, [stepAndAnimate]);

  /** Reset game (play again). */
  const resetGame = useCallback(() => {
    const renderer = rendererRef.current;
    if (!renderer) return;
    clearProposed();

    const debugState = import.meta.env.DEV ? loadDebugState() : {};
    const customSeed = debugState.seed?.trim() || undefined;
    const customDepth = debugState.depth ? parseInt(debugState.depth, 10) : undefined;
    const depthArg = customDepth != null && customDepth >= 0 && customDepth <= 27 ? customDepth : undefined;

    const newModel = GameModel.createDailyGame(customSeed, depthArg);
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
  }, [readState, clearProposed]);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    let input: InputHandler | null = null;
    let resizeHandler: (() => void) | null = null;
    let destroyed = false;
    let appReady = false;
    let pixiApp: Application | null = null;
    const audioUnsubs: (() => void)[] = [];

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

      const debugState = import.meta.env.DEV ? loadDebugState() : {};
      const customSeed = debugState.seed?.trim() || undefined;
      const customDepth = debugState.depth ? parseInt(debugState.depth, 10) : undefined;
      const depthArg = customDepth != null && customDepth >= 0 && customDepth <= 27 ? customDepth : undefined;

      const model = GameModel.createDailyGame(customSeed, depthArg);
      model.consumeAnimationEvents();
      modelRef.current = model;

      if (import.meta.env.DEV && (customSeed || depthArg != null)) {
        const parts: string[] = [];
        if (customSeed) parts.push(`seed "${customSeed}"`);
        if (depthArg != null) parts.push(`depth ${depthArg}`);
        setDebugNotice(`Loaded with ${parts.join(', ')}`);
        setTimeout(() => setDebugNotice(null), 4000);
      }

      const sound = new SoundManager();
      await sound.loadSFX();
      if (destroyed) { app.destroy(true, { children: true }); return; }
      // Music loads in background — game starts without waiting for large music files.
      // setMusic() no-ops gracefully if buffers aren't ready yet.
      sound.loadMusic().catch(console.warn);

      const camera = new Camera();
      const renderer = new GameRenderer(app, camera, sprites);
      rendererRef.current = renderer;

      const animator = new AnimationPlayer(renderer, camera, model.player.guid, sound);
      animatorRef.current = animator;

      // Background music — boss floor plays boss track, all others play normal
      const updateMusic = (floor: Floor) => {
        const hasBoss = [...floor.bodies].some(b => b instanceof Boss);
        sound.setMusic(hasBoss ? 'boss' : 'normal');
      };
      updateMusic(model.currentFloor);

      // Per-player audio subscriptions (item pickup, equip, status, actions)
      const p = model.player;
      audioUnsubs.push(
        p.inventory.onItemAdded.on(() => sound.play('playerPickupItem')),
        p.equipment.onItemAdded.on(() => sound.play('playerEquip')),
        p.equipment.onItemRemoved.on(() => sound.play('playerEquip')),
        p.equipment.onItemDestroyed.on(() => setTimeout(() => sound.play('playerEquipmentBreak'), 250)),
        p.onChangeWater.on(delta => { if (Math.abs(delta) > 1) sound.play('playerChangeWater', 0.2); }),
        p.statuses.onAdded.on(s => { if (s.isDebuff) sound.play('playerGetDebuff'); }),
        p.afterActionPerformed.on(a => {
          if (a instanceof WaitBaseAction) sound.play('playerWait');
          else if (a instanceof GenericBaseAction) sound.play('playerGeneric');
        }),
        model.currentFloor.onEntityRemoved.on(e => {
          if (e instanceof Boss) sound.setMusic('none');
        }),
      );

      renderer.setFloor(model.currentFloor);
      renderer.syncToModel();

      // Register ticker for per-frame updates (runs every frame)
      app.ticker.add((ticker) => {
        const dt = ticker.deltaTime / 60;
        renderer.lerpPositions(dt);
        renderer.updateTelegraphEffects(dt);
        renderer.updateEntityAnimations(dt);
      });

      input = new InputHandler(camera, app.canvas);
      inputRef.current = input;
      input.onIntent.on(processIntent);
      input.onContextMenu.on(handleContextMenu);
      input.attach();

      resizeHandler = () => {
        const w = container!.clientWidth;
        const h = container!.clientHeight;
        app.renderer.resize(w, h);
        const currentModel = modelRef.current ?? model;
        camera.resize(w, h, currentModel.currentFloor.width, currentModel.currentFloor.height);
        renderer.rebuildAll();
      };
      window.addEventListener('resize', resizeHandler);

      setGameState(readState());
      setReady(true);
    }

    init();

    return () => {
      destroyed = true;
      for (const unsub of audioUnsubs) unsub();
      input?.detach();
      if (resizeHandler) window.removeEventListener('resize', resizeHandler);
      if (pixiApp && appReady) {
        pixiApp.destroy(true, { children: true });
      }
      modelRef.current = null;
      rendererRef.current = null;
      animatorRef.current = null;
      inputRef.current = null;
    };
  }, [processIntent, readState, handleContextMenu]);

  return { containerRef, gameState, ready, executeItemAction, executeOnTopAction, resetGame, targetingState, cancelTargeting, syncAndUpdate, modelRef, rendererRef, debugNotice, entityInfo, setEntityInfo };
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

    case 'cancel':
      return null;
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

function delay(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms));
}
