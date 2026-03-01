import { Entity } from './Entity';
import { Vector2Int } from '../core/Vector2Int';
import {
  CollisionLayer,
} from '../core/types';
import {
  collectModifiers,
  processModifiers,
  ATTACK_DAMAGE_TAKEN_MOD,
  ANY_DAMAGE_TAKEN_MOD,
  MOVEMENT_LAYER_MOD,
  type IAttackDamageTakenModifier,
  type IAnyDamageTakenModifier,
  type IMovementLayerModifier,
} from '../core/Modifiers';
import { EventEmitter } from '../core/EventEmitter';
import { GameModelRef } from './GameModelRef';

// ─── Handler symbols ───
export const BODY_MOVE_HANDLER = Symbol.for('IBodyMoveHandler');
export const BODY_TAKE_ATTACK_DAMAGE_HANDLER = Symbol.for('IBodyTakeAttackDamageHandler');
export const TAKE_ANY_DAMAGE_HANDLER = Symbol.for('ITakeAnyDamageHandler');
export const HEAL_HANDLER = Symbol.for('IHealHandler');

export interface IBodyMoveHandler {
  handleMove(newPos: Vector2Int, oldPos: Vector2Int): void;
}

export interface IBodyTakeAttackDamageHandler {
  handleTakeAttackDamage(damage: number, hp: number, source: any): void;
}

export interface ITakeAnyDamageHandler {
  handleTakeAnyDamage(damage: number): void;
}

export interface IHealHandler {
  handleHeal(amount: number): void;
}

/**
 * Entity with HP, position movement, and damage pipeline.
 * Port of C# Body.cs.
 */
export class Body extends Entity {
  private _pos: Vector2Int;
  protected _hp: number = 8;
  protected _baseMaxHp: number = 8;

  readonly onMaxHPAdded = new EventEmitter();

  get hp(): number {
    return this._hp;
  }
  protected set hp(value: number) {
    this._hp = value;
  }

  get baseMaxHp(): number {
    return this._baseMaxHp;
  }
  protected set baseMaxHp(value: number) {
    this._baseMaxHp = value;
  }

  get maxHp(): number {
    return this._baseMaxHp;
  }

  /** Override in subclasses for innate movement type (e.g. Flying for Bat). */
  get baseMovementLayer(): CollisionLayer {
    return CollisionLayer.Walking;
  }

  /** Computed from baseMovementLayer + modifiers. */
  get movementLayer(): CollisionLayer {
    const modifiers = collectModifiers<IMovementLayerModifier>(this, MOVEMENT_LAYER_MOD);
    return processModifiers(modifiers, this.baseMovementLayer);
  }

  get pos(): Vector2Int {
    return this._pos;
  }

  set pos(value: Vector2Int) {
    if (!this.floor) {
      this._pos = value;
      return;
    }

    const tile = this.floor.tiles.get(value);
    if (tile && tile.canBeOccupiedBy(this)) {
      const oldTile = this.floor.tiles.get(this._pos)!;
      oldTile.bodyLeft(this);
      const oldPos = this._pos;
      this._pos = value;
      this.floor.bodyMoved();
      GameModelRef.mainOrNull?.emitAnimation({ type: 'move', entityGuid: this.guid, from: oldPos, to: value });
      this.onMove(value, oldPos);
      const newTile = this.floor.tiles.get(this._pos)!;
      newTile.bodyEntered(this);
    } else {
      this.onMoveFailed(value);
    }
  }

  get myModifiers(): Iterable<object | null | undefined> {
    const grassMod = this.grass && 'bodyModifier' in this.grass ? (this.grass as any).bodyModifier : null;
    return [...super.myModifiers, grassMod];
  }

  /** Mark as body for Floor type discrimination */
  readonly _isBody = true;

  constructor(pos: Vector2Int) {
    super();
    this._pos = pos;
  }

  protected handleLeaveFloor(): void {
    this.tile.bodyLeft(this);
  }

  protected handleEnterFloor(): void {
    this.tile.bodyEntered(this);
  }

  heal(amount: number): number {
    if (amount <= 0) return 0;
    amount = Math.min(amount, this.maxHp - this._hp);
    this._hp += amount;
    if (amount > 0) {
      GameModelRef.mainOrNull?.emitAnimation({ type: 'heal', entityGuid: this.guid, to: this._pos, amount });
    }
    this.onHealEvent(amount);
    return amount;
  }

  addMaxHP(amount: number): void {
    this._baseMaxHp += amount;
    this.onMaxHPAdded.emit();
  }

  attacked(damage: number, source: any): void {
    this.takeAttackDamage(damage, source);
  }

  takeAttackDamage(damage: number, source: any): void {
    if (this.isDead) return;
    const mods = collectModifiers<IAttackDamageTakenModifier>(this, ATTACK_DAMAGE_TAKEN_MOD);
    damage = processModifiers(mods, damage);
    damage = Math.max(damage, 0);
    source.onDealAttackDamage(damage, this);
    this.onTakeAttackDamage(damage, this._hp, source);
    this.takeDamage(damage, source);
  }

  takeDamage(damage: number, source: Entity): void {
    if (this.isDead) return;
    const mods = collectModifiers<IAnyDamageTakenModifier>(this, ANY_DAMAGE_TAKEN_MOD);
    damage = processModifiers(mods, damage);
    damage = Math.max(damage, 0);
    this.onTakeAnyDamageEvent(damage);
    if (damage > 0) {
      GameModelRef.mainOrNull?.emitAnimation({ type: 'damage', entityGuid: this.guid, to: this._pos, amount: damage });
    }
    this._hp -= damage;
    if (this._hp <= 0) {
      this.kill(source);
    }
  }

  kill(source: Entity): void {
    if (!this.isDead) {
      this._hp = Math.max(this._hp, 0);
      GameModelRef.mainOrNull?.emitAnimation({ type: 'death', entityGuid: this.guid, to: this._pos });
      super.kill(source);
    }
  }

  setHPDirect(hp: number): void {
    this._hp = hp;
  }

  protected onMoveFailed(_wantedPos: Vector2Int): void {}

  private onMove(newPos: Vector2Int, oldPos: Vector2Int): void {
    for (const handler of collectModifiers<IBodyMoveHandler>(this, BODY_MOVE_HANDLER)) {
      handler.handleMove(newPos, oldPos);
    }
  }

  private onTakeAttackDamage(dmg: number, hp: number, source: any): void {
    for (const handler of collectModifiers<IBodyTakeAttackDamageHandler>(this, BODY_TAKE_ATTACK_DAMAGE_HANDLER)) {
      handler.handleTakeAttackDamage(dmg, hp, source);
    }
  }

  private onTakeAnyDamageEvent(dmg: number): void {
    for (const handler of collectModifiers<ITakeAnyDamageHandler>(this, TAKE_ANY_DAMAGE_HANDLER)) {
      handler.handleTakeAnyDamage(dmg);
    }
  }

  private onHealEvent(amount: number): void {
    for (const handler of collectModifiers<IHealHandler>(this, HEAL_HANDLER)) {
      handler.handleHeal(amount);
    }
  }
}
