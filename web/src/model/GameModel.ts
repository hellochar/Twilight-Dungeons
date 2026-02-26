import { Player } from './Player';
import { Floor } from './Floor';
import { Ground, Wall } from './Tile';
import { TurnManager } from './TurnManager';
import { TimedEvent } from './Entity';
import { EventEmitter } from '../core/EventEmitter';
import { GameModelRef, type IGameModelRef } from './GameModelRef';
import { Vector2Int } from '../core/Vector2Int';

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
  readonly timedEvents = new TimedEventManager();

  private _turnManager: TurnManager | null = null;
  private eventQueue: (() => void)[] = [];

  readonly onGameOver = new EventEmitter<[PlayStats]>();
  readonly onFloorCleared = new EventEmitter<[Floor]>();

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

  /** Quick test setup: small hardcoded floor */
  static createTestGame(width = 10, height = 8): GameModel {
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
    const playerPos = new Vector2Int(1, Math.floor(height / 2));
    model.player = new Player(playerPos);
    floor.put(model.player);

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
    this.stats.won = won;
    this.stats.turnsTaken = Math.floor(this.time);
    this.stats.killedBy = deathSource?.displayName ?? null;
    this.onGameOver.emit(this.stats);
  }

  floorCleared(floor: Floor): void {
    this.onFloorCleared.emit(floor);
  }
}
