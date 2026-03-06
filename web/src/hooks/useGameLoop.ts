import { useCallback, useEffect, useRef, useState } from 'react';
import { Application, TextureSource } from 'pixi.js';
import { GameModel, type Difficulty, generateBasicGame, generateMediumGame, generateComplexGame } from '../model/GameModel';
import { Player } from '../model/Player';
import { Floor } from '../model/Floor';
import { Vector2Int } from '../core/Vector2Int';
import { Faction } from '../core/types';
import { Camera, SpriteManager, GameRenderer, AnimationPlayer, isMobile } from '../renderer';
import { InputHandler, type PlayerIntent/*, type TileContextEvent*/ } from '../input/InputHandler';
import { soundManager } from '../audio/SoundManager';
import gsap from 'gsap';
import { WATER_SFX_VOLUME, MOVE_LERP_MS, TIME_GAP_DELAY } from '../constants';
// FUTURE: hover entity → draw line to card. Re-enable these + restore EntityInfoPanel in App.tsx
// import type { EntityInfoData } from '../ui/EntityInfoPanel';
// import { Body } from '../model/Body';
import { Boss } from '../model/enemies/Boss';
import { WaitBaseAction, GenericBaseAction } from '../model/BaseAction';
import type { GameEvent } from '../renderer/AnimationPlayer';
import { FollowPathTask } from '../model/tasks/FollowPathTask';
import { AttackTask } from '../model/tasks/AttackTask';
import { WaitTask } from '../model/tasks/WaitTask';
import { MoveNextToTargetTask } from '../model/tasks/MoveNextToTargetTask';
import { Item, EquippableItem, STACKABLE_TAG, DURABLE_TAG, EDIBLE_TAG, USABLE_TAG, TARGETED_ACTION_TAG, type IStackable, type IDurable, type IEdible, type IUsable, type ITargetedAction } from '../model/Item';
import type { Entity } from '../model/Entity';
import { StackingStatus } from '../model/Status';
import { ON_TOP_ACTION_HANDLER, type IOnTopActionHandler } from '../core/types';
import { loadDebugState } from '../debug/DebugPanel';
import { getLocalScore } from '../services/ScoreService';
import { trackSessionStart, trackGameOver, trackRetry } from '../services/AnalyticsService';
import type { PlayStats } from '../model/GameModel';

const DAY_ONE = '2026-02-04';

function localTodayStr(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

/** Reads ?date= from URL, validates it, strips it if invalid, returns it or undefined. */
function getUrlDateParam(): string | undefined {
  const raw = new URLSearchParams(window.location.search).get('date');
  if (!raw) return undefined;
  if (!/^\d{4}-\d{2}-\d{2}$/.test(raw) || raw < DAY_ONE || raw > localTodayStr()) {
    const p = new URLSearchParams(window.location.search);
    p.delete('date');
    const qs = p.toString();
    window.history.replaceState(null, '', window.location.pathname + (qs ? `?${qs}` : ''));
    return undefined;
  }
  return raw;
}

/** Reads ?difficulty= from URL, defaults to 'basic' if absent or invalid. */
function getUrlDifficulty(): Difficulty {
  const raw = new URLSearchParams(window.location.search).get('difficulty');
  if (raw === 'medium' || raw === 'complex') return raw;
  return 'basic';
}

function createGameForDifficulty(difficulty: Difficulty, dateSeed?: string): GameModel {
  if (difficulty === 'medium') return generateMediumGame(dateSeed);
  if (difficulty === 'complex') return generateComplexGame(dateSeed);
  return generateBasicGame(dateSeed);
}

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

export interface EntityCardData {
  displayName: string;
  typeName: string;
  hp?: number;
  maxHp?: number;
  /** Base attack damage range [min, max]. Only set for Actors with non-zero damage. */
  attackDamage?: [number, number];
  pos: { x: number; y: number };
}

export interface GameState {
  hp: number;
  maxHp: number;
  depth: number;
  turn: number;
  enemyCount: number;
  isPlayerDead: boolean;
  isCleared: boolean;
  clearedOnTurn: number | null;
  inventoryItems: (ItemSnapshot | null)[];
  equipmentItems: (ItemSnapshot | null)[];
  statuses: StatusSnapshot[];
  gameOver: GameOverInfo | null;
  onTopAction: OnTopActionSnapshot | null;
  dateSeed: string;
  playerPos: { x: number; y: number };
  floorBodies: EntityCardData[];
  floorGrasses: EntityCardData[];
  difficulty: Difficulty;
}

const EMPTY_STATE: GameState = {
  hp: 0, maxHp: 0, depth: 0, turn: 0, enemyCount: 0,
  isPlayerDead: false, isCleared: false, clearedOnTurn: null,
  inventoryItems: [], equipmentItems: [],
  statuses: [], gameOver: null, onTopAction: null,
  dateSeed: '', playerPos: { x: 0, y: 0 }, floorBodies: [], floorGrasses: [],
  difficulty: 'basic',
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
  const difficulty = useRef<Difficulty>(getUrlDifficulty()).current;
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
  const [hoveredTilePos, setHoveredTilePos] = useState<{ x: number; y: number } | null>(null);
  const gameStartTimeRef = useRef<number>(Date.now());
  const retryCountRef = useRef<number>(0);
  const lastGameOverRef = useRef<PlayStats | null>(null);
  // const [entityInfo, setEntityInfo] = useState<EntityInfoData | null>(null);

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

    const floorBodies: EntityCardData[] = [];
    for (const body of floor.bodies) {
      if (body === player) continue;
      const b = body as any;
      const dmg: [number, number] | undefined = typeof b.baseAttackDamage === 'function' ? b.baseAttackDamage() : undefined;
      const attackDamage = dmg && (dmg[0] !== 0 || dmg[1] !== 0) ? dmg : undefined;
      floorBodies.push({ displayName: body.displayName, typeName: body.constructor.name, hp: b.hp, maxHp: b.maxHp, attackDamage, pos: { x: body.pos.x, y: body.pos.y } });
    }

    const floorGrasses: EntityCardData[] = [];
    for (const grass of floor.grasses) {
      floorGrasses.push({ displayName: grass.displayName, typeName: grass.constructor.name, pos: { x: grass.pos.x, y: grass.pos.y } });
    }

    return {
      hp: player.hp,
      maxHp: player.maxHp,
      depth: floor.depth,
      turn: Math.floor(model.time),
      enemyCount: countEnemies(floor),
      isPlayerDead: player.isDead,
      isCleared: floor.isCleared,
      clearedOnTurn: floor.clearedOnTurn,
      inventoryItems,
      equipmentItems,
      statuses,
      gameOver,
      onTopAction,
      dateSeed: model.dateSeed,
      playerPos: { x: player.pos.x, y: player.pos.y },
      floorBodies,
      floorGrasses,
      difficulty: difficulty,
    };
  }, []);

  // /** Build EntityInfoData from entity/tile at a given tile position (hover/click inspect). */
  // const handleTileInspect = useCallback((event: TileContextEvent) => {
  //   const model = modelRef.current;
  //   if (!model) return;
  //   const floor = model.currentFloor;
  //   const { tilePos } = event;
  //   const body = floor.bodies.get(tilePos);
  //   if (body) {
  //     const b = body as any as Body;
  //     setEntityInfo({ name: body.displayName, typeName: body.constructor.name, hp: b.hp, maxHp: b.maxHp });
  //     return;
  //   }
  //   const grass = floor.grasses.get(tilePos);
  //   if (grass) { setEntityInfo({ name: grass.displayName, typeName: grass.constructor.name }); return; }
  //   const item = floor.items.get(tilePos);
  //   if (item) {
  //     const heldItem = (item as any).heldItem;
  //     if (heldItem) setEntityInfo({ name: heldItem.displayName, typeName: heldItem.constructor.name, stats: heldItem.getStatsFull?.() ?? '' });
  //     else setEntityInfo({ name: item.displayName, typeName: item.constructor.name });
  //     return;
  //   }
  //   const tile = floor.tiles.get(tilePos);
  //   if (tile) setEntityInfo({ name: tile.displayName, typeName: tile.constructor.name });
  // }, []);

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

  /** Show path/reticle preview for a tile on mobile (used by both tap and drag). */
  const updateMobilePreview = useCallback((tilePos: Vector2Int) => {
    const model = modelRef.current;
    if (!model || !isMobile()) return;
    const player = model.player;
    const floor = model.currentFloor;
    if (player.isDead || Vector2Int.equals(tilePos, player.pos)) { clearProposed(); return; }

    const intent: PlayerIntent = { type: 'click', tilePos };
    const previewTask = resolveIntent(intent, player, floor);
    if (previewTask instanceof FollowPathTask) {
      clearProposed();
      proposedTargetRef.current = previewTask.target;
      rendererRef.current?.showProposedPath(previewTask.target, [...previewTask.path]);
      return;
    }
    // Adjacent tile — show reticle only
    const isAdjacent = Vector2Int.chebyshevDistance(player.pos, tilePos) === 1;
    if (isAdjacent) {
      clearProposed();
      proposedTargetRef.current = tilePos;
      rendererRef.current?.showProposedPath(tilePos, [tilePos]);
      return;
    }
    clearProposed();
  }, [clearProposed]);

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
    // Note: pointerdown intentionally excluded — clicks show path preview instead of skipping.
    const skipHandler = () => { skipAllRef.current = true; animator.skip(); };
    const skipRAF = requestAnimationFrame(() => {
      window.addEventListener('keydown', skipHandler);
    });

    try {
      model.turnManager.beginStepSession();

      // Ensure move lerps complete before the next entity steps. The delay must happen
      // BEFORE stepOneEntity() because the step may kill a moving entity, and lerpPositions
      // skips dead entities (freezing the sprite mid-walk).
      // MOVE_LERP_MS imported from constants.ts (derived from MOVE_SPEED)
      let prevBatchHadMoves = false;
      let prevStaggerMs = 0;

      while (true) {
        if (prevBatchHadMoves && !skipAllRef.current) {
          const extra = Math.max(0, MOVE_LERP_MS - prevStaggerMs);
          if (extra > 0) await delay(extra);
        }

        const result = model.turnManager.stepOneEntity();

        if (result.done) {
          // Timed events (e.g. poison tickDamage) may have fired during this step.
          // Play their animation events before exiting.
          const doneEvents = model.consumeAnimationEvents();
          if (doneEvents.length > 0 && !skipAllRef.current) {
            renderer.addNewBodySprites();
            await animator.playBatch(doneEvents);
          }
          break;
        }

        // Time-gap delay (matches Unity GAME_TIME_TO_SECONDS_WAIT_SCALE = 0.2)
        const timeGapDelayMs = result.timeGap * TIME_GAP_DELAY;
        if (!result.isFirstStep && result.timeGap > 0 && !result.shouldSpeedThrough && !skipAllRef.current) {
          await delay(timeGapDelayMs);
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
              renderer.addNewBodySprites();
              await animator.playBatch(events);
            } finally {
              for (const guid of animatedGuids) {
                renderer.animatingGuids.delete(guid);
              }
            }
          }
        }

        prevBatchHadMoves = events.some(e => e.type === 'move');

        // Sync after each step so subsequent animations see updated positions
        renderer.syncToModel();

        // If game just ended, show UI immediately without animating remaining entities
        if (model.gameOverInfo) break;

        // Stagger delay between visible enemy turns (Unity JUICE_STAGGER_SECONDS = 0.02)
        const willStagger = result.shouldStagger && !skipAllRef.current && !result.shouldSpeedThrough;
        prevStaggerMs = willStagger ? 20 : 0;
        if (willStagger) {
          await delay(20);
        }
      }
    } catch (e) {
      console.error('Error during step/animation loop:', e);
    } finally {
      cancelAnimationFrame(skipRAF);
      window.removeEventListener('keydown', skipHandler);

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
    if (model.player.isDead) return;

    // ─── During animation: clicks show path preview only, no execution ───
    if (processingRef.current) {
      if (intent.type !== 'click') return;
      const { tilePos } = intent;
      const { player, currentFloor: floor } = model;
      if (Vector2Int.equals(tilePos, player.pos)) { clearProposed(); return; }
      const previewTask = resolveIntent(intent, player, floor);
      if (previewTask instanceof FollowPathTask) {
        proposedTargetRef.current = previewTask.target;
        rendererRef.current?.showProposedPath(previewTask.target, [...previewTask.path]);
      } else if (previewTask) {
        // Adjacent attack / move-next-to: show a single-tile path as reticle
        proposedTargetRef.current = tilePos;
        rendererRef.current?.showProposedPath(tilePos, [tilePos]);
      } else {
        clearProposed();
      }
      return;
    }

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

    // ─── Two-click path/action preview ───
    // Desktop: non-adjacent tiles in combat only. Mobile: all non-self tiles.
    // First click: show path dots + reticle (+ info popup on mobile). Second click on same tile: execute.
    if (intent.type === 'click') {
      const { tilePos } = intent;
      const isInCombat = floor.depth > 0 && !floor.isCleared;
      const isAdjacent = Vector2Int.chebyshevDistance(player.pos, tilePos) === 1;
      const needsConfirm = isMobile()
        ? !Vector2Int.equals(tilePos, player.pos)
        : (isInCombat && !Vector2Int.equals(tilePos, player.pos) && !isAdjacent);
      if (needsConfirm) {
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
          // Adjacent or no valid path on mobile — show reticle only for first tap
          if (isMobile() && isAdjacent) {
            clearProposed();
            proposedTargetRef.current = tilePos;
            rendererRef.current?.showProposedPath(tilePos, [tilePos]);
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
      if (!player.isDead && !floor.isCleared) {
        const newPath = floor.findPath(player.pos, pendingTarget, false, player);
        if (newPath.length > 0) {
          rendererRef.current?.showProposedPath(pendingTarget, newPath);
        } else {
          // Target may be an enemy (pathfinding can't enter occupied tile) — show reticle for attack
          const bodyAtTarget = floor.bodies.get(pendingTarget);
          if (bodyAtTarget && bodyAtTarget !== player && 'hp' in bodyAtTarget) {
            rendererRef.current?.showProposedPath(pendingTarget, [pendingTarget]);
          } else {
            clearProposed();
          }
        }
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
      // Play any animation events emitted directly by the handler (e.g. Llaora disperse poof).
      const events = model.consumeAnimationEvents();
      const animator = animatorRef.current;
      if (events.length > 0 && animator) {
        processingRef.current = true;
        await animator.playBatch(events);
        processingRef.current = false;
      }
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

      // '<' / '>' — shift date seed by one day (bypasses validation limits)
      if (e.key === '<' || e.key === '>') {
        const currentSeed = modelRef.current?.dateSeed ?? localTodayStr();
        const d = new Date(currentSeed + 'T00:00:00');
        d.setDate(d.getDate() + (e.key === '>' ? 1 : -1));
        const newDateSeed = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
        const newModel = createGameForDifficulty(difficulty, newDateSeed);
        newModel.consumeAnimationEvents();
        modelRef.current = newModel;
        renderer.setFloor(newModel.currentFloor);
        renderer.syncToModel();
        renderer.camera.resize(renderer.app.screen.width, renderer.app.screen.height, newModel.currentFloor.width, newModel.currentFloor.height, isMobile() ? -0.5 : 0.5);
        setGameState(readState());
        setDebugNotice(`date ${newDateSeed}`);
        setTimeout(() => setDebugNotice(null), 3000);
        // Update URL to match
        const p = new URLSearchParams(window.location.search);
        p.set('date', newDateSeed);
        window.history.replaceState(null, '', `${window.location.pathname}?${p.toString()}`);
        return;
      }

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
      renderer.camera.resize(renderer.app.screen.width, renderer.app.screen.height, newModel.currentFloor.width, newModel.currentFloor.height, isMobile() ? -0.5 : 0.5);
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

  /** Load a specific date seed into the game. */
  const playDate = useCallback((dateSeed: string) => {
    const renderer = rendererRef.current;
    if (!renderer) return;
    clearProposed();
    const newModel = GameModel.createDailyGame(dateSeed);
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

  /** Reset game (play again / today's puzzle). */
  const resetGame = useCallback(() => {
    if (lastGameOverRef.current) {
      retryCountRef.current += 1;
      trackRetry(retryCountRef.current, lastGameOverRef.current);
    }
    gameStartTimeRef.current = Date.now();

    const urlDate = getUrlDateParam();
    const debugState = import.meta.env.DEV ? loadDebugState() : {};
    const customSeed = urlDate || debugState.seed?.trim() || undefined;
    const customDepth = debugState.depth ? parseInt(debugState.depth, 10) : undefined;
    const depthArg = customDepth != null && customDepth >= 0 && customDepth <= 27 ? customDepth : undefined;

    const renderer = rendererRef.current;
    if (!renderer) return;
    clearProposed();

    const newModel = depthArg != null
      ? GameModel.createDailyGame(customSeed, depthArg)
      : createGameForDifficulty(difficulty, customSeed);
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

  /** Navigate to a different date/difficulty via full page reload. */
  const navigateToGame = useCallback((dateSeed: string, diff: Difficulty) => {
    window.location.assign(`?date=${encodeURIComponent(dateSeed)}&difficulty=${encodeURIComponent(diff)}`);
  }, []);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    let input: InputHandler | null = null;
    let resizeHandler: (() => void) | null = null;
    let resizeTimer: ReturnType<typeof setTimeout> | null = null;
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

      const urlDate = getUrlDateParam();
      const debugState = import.meta.env.DEV ? loadDebugState() : {};
      const customSeed = urlDate || debugState.seed?.trim() || undefined;
      const customDepth = debugState.depth ? parseInt(debugState.depth, 10) : undefined;
      const depthArg = customDepth != null && customDepth >= 0 && customDepth <= 27 ? customDepth : undefined;

      const model = depthArg != null
        ? GameModel.createDailyGame(customSeed, depthArg)
        : createGameForDifficulty(difficulty, customSeed);
      model.consumeAnimationEvents();
      modelRef.current = model;

      if (import.meta.env.DEV && (customSeed || depthArg != null)) {
        const parts: string[] = [];
        if (customSeed) parts.push(`seed "${customSeed}"`);
        if (depthArg != null) parts.push(`depth ${depthArg}`);
        setDebugNotice(`Loaded with ${parts.join(', ')}`);
        setTimeout(() => setDebugNotice(null), 4000);
      }

      const sound = soundManager;
      sound.init();
      await sound.loadSFX();
      if (destroyed) { app.destroy(true, { children: true }); return; }
      // Music loads in background. After loading, pending setMusic() calls are replayed.
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
        p.onChangeWater.on(delta => { if (Math.abs(delta) > 1) sound.play('playerChangeWater', WATER_SFX_VOLUME); }),
        p.statuses.onAdded.on(s => { if (s.isDebuff) sound.play('playerGetDebuff'); }),
        p.afterActionPerformed.on(a => {
          if (a instanceof WaitBaseAction) sound.play('playerWait');
          else if (a instanceof GenericBaseAction) sound.play('playerGeneric');
        }),
        model.currentFloor.onEntityRemoved.on(e => {
          if (e instanceof Boss) sound.setMusic('none');
        }),
      );

      // Analytics: track game over
      model.onGameOver.on((stats) => {
        lastGameOverRef.current = stats;
        trackGameOver(stats, difficulty, model.dateSeed, Date.now() - gameStartTimeRef.current, retryCountRef.current);
      });

      renderer.setFloor(model.currentFloor);
      renderer.syncToModel();

      // Sync GSAP ticker with PixiJS so animations update in the same frame as rendering
      gsap.ticker.remove(gsap.updateRoot);
      // Register ticker for per-frame updates (runs every frame)
      app.ticker.add((ticker) => {
        const dt = ticker.deltaTime / 60;
        gsap.updateRoot(gsap.ticker.time + dt);
        renderer.lerpPositions(dt);
        renderer.syncHpLabelPositions();
        renderer.updateTelegraphEffects(dt);
        renderer.updateExplodeEffects(dt);
        renderer.updateEntityAnimations(dt);
      });

      input = new InputHandler(camera, app.canvas);
      inputRef.current = input;
      input.onIntent.on(processIntent);
      // input.onContextMenu.on(handleTileInspect);
      input.onTileHover.on((ev) => {
        setHoveredTilePos({ x: ev.tilePos.x, y: ev.tilePos.y });
        updateMobilePreview(ev.tilePos);
      });
      input.attach();

      resizeHandler = () => {
        if (resizeTimer) clearTimeout(resizeTimer);
        resizeTimer = setTimeout(() => {
          const w = container!.clientWidth;
          const h = container!.clientHeight;
          app.renderer.resize(w, h);
          const currentModel = modelRef.current ?? model;
          camera.resize(w, h, currentModel.currentFloor.width, currentModel.currentFloor.height, isMobile() ? -0.5 : 0.5);
          renderer.rebuildAll();
          renderer.syncToModel();
        }, 150);
      };
      window.addEventListener('resize', resizeHandler);

      setGameState(readState());
      setReady(true);
      trackSessionStart(!!getLocalScore(`${localTodayStr()}-${difficulty}`), difficulty);
    }

    init();

    return () => {
      destroyed = true;
      for (const unsub of audioUnsubs) unsub();
      input?.detach();
      if (resizeHandler) {
        window.removeEventListener('resize', resizeHandler);
        if (resizeTimer) clearTimeout(resizeTimer);
      }
      if (pixiApp && appReady) {
        pixiApp.destroy(true, { children: true });
      }
      modelRef.current = null;
      rendererRef.current = null;
      animatorRef.current = null;
      inputRef.current = null;
    };
  }, [processIntent, readState, updateMobilePreview/*, handleTileInspect*/]);

  const clearHoveredTile = useCallback(() => {
    setHoveredTilePos(null);
    clearProposed();
  }, [clearProposed]);

  return { containerRef, gameState, ready, executeItemAction, executeOnTopAction, executeWait, resetGame, playDate, navigateToGame, targetingState, cancelTargeting, syncAndUpdate, modelRef, rendererRef, debugNotice, hoveredTilePos, clearHoveredTile/*, entityInfo, setEntityInfo*/ };
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
