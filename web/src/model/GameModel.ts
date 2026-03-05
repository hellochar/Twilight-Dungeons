import { Player } from './Player';
import { Floor } from './Floor';
import { Ground, Wall } from './Tile';
import { Blob } from './enemies/Blob';
import { Bird } from './enemies/Bird';
import { Snake } from './enemies/Snake';
// Register all entities in entityRegistry
import './registerAll';
import { TurnManager } from './TurnManager';
import { TimedEvent } from './Entity';
import { EventEmitter } from '../core/EventEmitter';
import { GameModelRef, type IGameModelRef } from './GameModelRef';
import { Vector2Int } from '../core/Vector2Int';
import { MyRandom } from '../core/MyRandom';
import { FloorGenerator } from '../generator/FloorGenerator';
import type { GameEvent } from '../renderer/AnimationPlayer';

// ─── Depth selection ───

/** djb2 hash of a string → 32-bit integer */
function djb2(str: string): number {
  let hash = 5381;
  for (let i = 0; i < str.length; i++) {
    hash = ((hash << 5) + hash + str.charCodeAt(i)) | 0;
  }
  return hash >>> 0;
}

export type Difficulty = 'basic' | 'medium' | 'complex';

export const NEXT_DIFFICULTY: Record<Difficulty, Difficulty | null> = {
  basic: 'medium',
  medium: 'complex',
  complex: null,
};

export const DIFFICULTY_LABEL: Record<Difficulty, string> = {
  basic: 'Basic',
  medium: 'Medium',
  complex: 'Complex',
};

/**
 * Select a floor depth for the daily puzzle.
 * Easy to swap out — currently returns a seeded random depth 1-26.
 * Future options: weekly difficulty curve, monthly progression, etc.
 */
export function selectDepth(seed: number): number {
  // Use the seed to pick a depth — avoid depth 0 (tutorial) and 27 (end floor)
  const rng = seed >>> 0;
  return 1 + (rng % 26);
}

/** Generate today's basic-difficulty game (depths 1–9, earlyGame encounters). */
export function generateBasicGame(dateSeed?: string): GameModel {
  const seed = djb2(dateSeed ?? _todayString());
  return GameModel.createDailyGame(dateSeed, 3, 3);
}

/** Generate today's medium-difficulty game (depths 10–18, everything encounters). */
export function generateMediumGame(dateSeed?: string): GameModel {
  const seed = djb2(dateSeed ?? _todayString());
  return GameModel.createDailyGame(dateSeed, 5, 4);
}

/** Generate today's complex-difficulty game (depths 19–26, midGame encounters). */
export function generateComplexGame(dateSeed?: string): GameModel {
  const seed = djb2(dateSeed ?? _todayString());
  return GameModel.createDailyGame(dateSeed, 7, 5);
}

function _todayString(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
}

/**
 * Manages timed events — a priority queue of future actions.
 */
export class TimedEventManager {
  private events: TimedEvent[] = [];

  register(evt: TimedEvent): void {
    this.events.push(evt);
    this.events.sort((a, b) => a.time - b.time);
  }

  unregister(evt: TimedEvent): void {
    const idx = this.events.indexOf(evt);
    if (idx !== -1) this.events.splice(idx, 1);
    evt.unregisterFromOwner();
  }

  /** Returns the next event without removing it, or null if none. */
  next(): TimedEvent | null {
    return this.events[0] ?? null;
  }

  clear(): void {
    this.events.length = 0;
  }
}

export interface PlayStats {
  won: boolean;
  turnsTaken: number;
  killedBy: string | null;
  enemiesDefeated: number;
  damageDealt: number;
  damageTaken: number;
}

function createPlayStats(): PlayStats {
  return {
    won: false,
    turnsTaken: 0,
    killedBy: null,
    enemiesDefeated: 0,
    damageDealt: 0,
    damageTaken: 0,
  };
}

/**
 * Core game state — simplified for daily puzzle (single floor).
 * Port of C# GameModel.cs.
 */
export class GameModel implements IGameModelRef {
  static main: GameModel;

  player!: Player;
  floor!: Floor;
  time = 0;
  stats: PlayStats;
  /** Snapshot of stats at the moment game ended (win or loss). Null until gameOver() is called. */
  gameOverInfo: PlayStats | null = null;
  readonly timedEvents = new TimedEventManager();

  private _turnManager: TurnManager | null = null;
  private eventQueue: (() => void)[] = [];
  private _animationEvents: GameEvent[] = [];

  readonly onGameOver = new EventEmitter<[PlayStats]>();
  readonly onFloorCleared = new EventEmitter<[Floor]>();

  /** Debug: the seed string used for generation (date or custom). */
  dateSeed = '';
  /** Debug: the depth selected for this game. */
  generatedDepth = 0;

  get turnManager(): TurnManager {
    if (!this._turnManager) {
      this._turnManager = new TurnManager(this);
    }
    return this._turnManager;
  }

  get currentFloor(): Floor {
    return this.floor;
  }

  constructor() {
    this.stats = createPlayStats();
  }

  /**
   * Initialize a daily puzzle game.
   * Floor and player should be set up by DailyPuzzle, then this is called.
   */
  static createAndSetMain(floor: Floor, playerPos: Vector2Int): GameModel {
    const model = new GameModel();
    GameModelRef.main = model;
    GameModel.main = model;

    model.floor = floor;
    model.player = new Player(playerPos);
    floor.put(model.player);

    model.stepUntilPlayerChoiceImmediate();
    return model;
  }

  /**
   * Create a daily puzzle game from a date seed.
   * Uses FloorGenerator to produce a procedurally generated floor.
   */
  static createDailyGame(dateSeed?: string, depthOverride?: number, playerHp?: number): GameModel {
    const _d = new Date();
    const dateStr = dateSeed ?? `${_d.getFullYear()}-${String(_d.getMonth()+1).padStart(2,'0')}-${String(_d.getDate()).padStart(2,'0')}`;
    const seed = djb2(dateStr);

    // Generate floor seeds from master seed
    MyRandom.setSeed(seed);
    const floorSeeds: number[] = [];
    for (let i = 0; i < 28; i++) {
      floorSeeds.push(MyRandom.Range(0, 0x7fffffff));
    }

    const depth = depthOverride ?? selectDepth(seed);
    const generator = new FloorGenerator(floorSeeds);
    // Clear GameModelRef before generating the floor so all entities created during
    // generation get timeCreated=0. Without this, on resetGame the old model (with
    // time > 0) is still set, causing enemies to start with timeNextAction = old_time
    // and the player getting that many free turns before any enemy can act.
    GameModelRef.main = null;
    const floor = generator.generateCaveFloor(depth);

    const model = GameModel.createAndSetMain(floor, floor.startPos);
    if (playerHp !== undefined) {
      model.player.hp = model.player.baseMaxHp = playerHp;
    }
    model.dateSeed = dateStr;
    model.generatedDepth = depth;
    return model;
  }

  /** Quick test setup: small hardcoded floor with enemies */
  static createTestGame(width = 14, height = 10): GameModel {
    const model = new GameModel();
    GameModelRef.main = model;
    GameModel.main = model;

    const floor = new Floor(0, width, height);

    // Fill with walls, then carve ground
    for (let x = 0; x < width; x++) {
      for (let y = 0; y < height; y++) {
        const pos = new Vector2Int(x, y);
        if (x === 0 || y === 0 || x === width - 1 || y === height - 1) {
          floor.put(new Wall(pos));
        } else {
          floor.put(new Ground(pos));
        }
      }
    }

    model.floor = floor;
    const playerPos = new Vector2Int(2, Math.floor(height / 2));
    model.player = new Player(playerPos);
    floor.put(model.player);

    // Place test enemies
    floor.put(new Blob(new Vector2Int(8, 5)));
    floor.put(new Bird(new Vector2Int(10, 3)));
    floor.put(new Snake(new Vector2Int(6, 7)));

    model.stepUntilPlayerChoiceImmediate();
    return model;
  }

  // ─── IGameModelRef implementation ───

  enqueuEvent(action: () => void): void {
    this.eventQueue.push(action);
  }

  drainEventQueue(): void {
    const maxGenerations = 32;
    for (let gen = 0; gen < maxGenerations; gen++) {
      const queue = [...this.eventQueue];
      this.eventQueue.length = 0;

      for (const action of queue) {
        action();
      }

      if (this.eventQueue.length === 0) return;
    }
    throw new Error('Reached max event queue generations!');
  }

  // ─── Animation events ───

  emitAnimation(event: object): void {
    this._animationEvents.push(event as GameEvent);
  }

  consumeAnimationEvents(): GameEvent[] {
    const events = this._animationEvents;
    this._animationEvents = [];
    return events;
  }

  // ─── Turn management ───

  stepUntilPlayerChoiceImmediate(): void {
    this.turnManager.stepUntilPlayerChoice();
  }

  stepUntilPlayerChoice(): void {
    this.turnManager.stepUntilPlayerChoice();
  }

  getAllEntitiesInPlay() {
    return this.currentFloor.steppableEntities;
  }

  // ─── Game state ───

  gameOver(won: boolean, deathSource?: { displayName: string }): void {
    if (this.gameOverInfo) return; // already ended
    this.stats.won = won;
    this.stats.turnsTaken = Math.floor(this.time);
    this.stats.killedBy = deathSource?.displayName ?? null;
    this.gameOverInfo = { ...this.stats };
    this.onGameOver.emit(this.gameOverInfo);
  }

  floorCleared(floor: Floor): void {
    this.onFloorCleared.emit(floor);
  }
}
