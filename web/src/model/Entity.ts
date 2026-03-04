import {
  type IModifierProvider,
  collectModifiers,
} from '../core/Modifiers';
import { Vector2Int } from '../core/Vector2Int';
import { TileVisibility, type IDeathHandler, type IKillEntityHandler } from '../core/types';
import { GameModelRef } from './GameModelRef';
import type { Floor } from './Floor';
import type { Tile } from './Tile';

// Symbols for death/kill handler collection
const DEATH_HANDLER = Symbol.for('IDeathHandler');
const KILL_HANDLER = Symbol.for('IKillEntityHandler');

let nextGuid = 0;

/**
 * Base class for all game entities.
 * Port of C# Entity.cs.
 */
export abstract class Entity implements IModifierProvider {
  readonly guid: string = `e-${nextGuid++}`;
  readonly timedEvents = new Set<TimedEvent>();
  private _isDead = false;
  private _floor: Floor | null = null;
  private _timeCreated: number = 0;

  abstract get pos(): Vector2Int;
  abstract set pos(value: Vector2Int);

  get isDead(): boolean {
    return this._isDead;
  }

  get floor(): Floor | null {
    return this._floor;
  }

  get timeCreated(): number {
    return this._timeCreated;
  }

  get age(): number {
    return (GameModelRef.mainOrNull?.time ?? 0) - this._timeCreated;
  }

  get tile(): Tile {
    return this._floor!.tiles.get(this.pos)!;
  }

  get grass(): Entity | null {
    return this._floor?.grasses.get(this.pos) ?? null;
  }

  get item(): Entity | null {
    return this._floor?.items.get(this.pos) ?? null;
  }

  get body(): Entity | null {
    return this._floor?.bodies.get(this.pos) ?? null;
  }

  get trigger(): Entity | null {
    return this._floor?.triggers.get(this.pos) ?? null;
  }

  get displayName(): string {
    return this.constructor.name.replace(/([A-Z])/g, ' $1').trim();
  }

  get description(): string {
    return '';
  }

  get isVisible(): boolean {
    if (this._isDead || !this._floor) return false;
    return this.tile.visibility === TileVisibility.Visible;
  }

  get myModifiers(): Iterable<object | null | undefined> {
    return [this as object, ...this.nonserializedModifiers];
  }

  nonserializedModifiers: object[] = [];

  constructor() {
    this._timeCreated = GameModelRef.mainOrNull?.time ?? 0;
  }

  /** Only call from Floor to internally update this Entity's floor pointer. */
  setFloor(floor: Floor | null): void {
    if (this._floor) {
      this.handleLeaveFloor();
    }
    this._floor = floor;
    if (this._floor) {
      this.handleEnterFloor();
    }
  }

  protected handleEnterFloor(): void {}
  protected handleLeaveFloor(): void {}

  distanceTo(other: Vector2Int | Entity): number {
    const target = other instanceof Entity ? other.pos : other;
    return Vector2Int.distance(this.pos, target);
  }

  isNextTo(other: Vector2Int | Entity): boolean {
    const target = other instanceof Entity ? other.pos : other;
    return (
      Math.abs(this.pos.x - target.x) <= 1 &&
      Math.abs(this.pos.y - target.y) <= 1
    );
  }

  toString(): string {
    const dead = this._isDead ? ' Dead' : '';
    const noFloor = !this._floor ? ' floor=null' : '';
    return `${this.constructor.name}@(${this.pos.x}, ${this.pos.y})${dead}${noFloor}`.trim();
  }

  killSelf(): void {
    this.kill(this);
  }

  kill(source: Entity): void {
    if (!this._isDead) {
      this._isDead = true;
      this.onDeath(source);
      this._floor?.remove(this);
    } else {
      console.warn('Calling kill() on already dead Entity! Ignoring');
    }
  }

  addTimedEvent(time: number, action: () => void): TimedEvent {
    const model = GameModelRef.main;
    const evt = new TimedEvent(this, model.time + time, action);
    this.timedEvents.add(evt);
    model.timedEvents.register(evt);
    return evt;
  }

  private onDeath(source: Entity): void {
    for (const handler of collectModifiers<IDeathHandler>(this, DEATH_HANDLER)) {
      handler.handleDeath(source);
    }
    for (const handler of collectModifiers<IKillEntityHandler>(source, KILL_HANDLER)) {
      handler.onKill(this);
    }
  }

  forceSetTimeCreated(time: number): void {
    this._timeCreated = time;
  }
}

export class TimedEvent {
  readonly time: number;
  readonly action: () => void;
  readonly owner: Entity;

  constructor(owner: Entity, time: number, action: () => void) {
    this.owner = owner;
    this.time = time;
    this.action = action;
  }

  get isInvalid(): boolean {
    return this.owner.floor !== GameModelRef.main.currentFloor;
  }

  unregisterFromOwner(): void {
    this.owner.timedEvents.delete(this);
  }
}

export class NoSpaceException extends Error {
  constructor(message = 'No space available') {
    super(message);
    this.name = 'NoSpaceException';
  }
}
