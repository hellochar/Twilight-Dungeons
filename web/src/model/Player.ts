import { Actor, ATTACK_HANDLER, DEAL_ATTACK_DAMAGE_HANDLER, ACTION_PERFORMED_HANDLER, STATUS_ADDED_HANDLER, type IAttackHandler, type IDealAttackDamageHandler, type IActionPerformedHandler } from './Actor';
import { Body, BODY_MOVE_HANDLER, TAKE_ANY_DAMAGE_HANDLER, type IBodyMoveHandler, type ITakeAnyDamageHandler } from './Body';
import { Vector2Int } from '../core/Vector2Int';
import { Faction } from '../core/types';
import { collectModifiers } from '../core/Modifiers';
import { GameModelRef } from './GameModelRef';
import { Inventory } from './Inventory';
import { Equipment, EquipmentSlot } from './Equipment';
import { WEAPON_TAG, DURABLE_TAG, type IWeapon, type IDurable, reduceDurability } from './Item';
import { ItemHands } from './items/ItemHands';
import { EventEmitter } from '../core/EventEmitter';
import type { BaseAction } from './BaseAction';
import type { Entity } from './Entity';

const KILL_HANDLER = Symbol.for('IKillEntityHandler');
const DEATH_HANDLER = Symbol.for('IDeathHandler');
const CAMOUFLAGE = Symbol.for('IPlayerCamouflage');

/**
 * The player character.
 * Port of C# Player.cs.
 */
export class Player extends Actor {
  readonly inventory: Inventory;
  readonly equipment: Equipment;
  readonly hands: ItemHands;
  readonly onChangeWater = new EventEmitter<[number]>();

  get turnPriority(): number {
    return 10;
  }

  get isCamouflaged(): boolean {
    return collectModifiers<any>(this, CAMOUFLAGE).length > 0;
  }

  get myModifiers(): Iterable<object | null | undefined> {
    return [...super.myModifiers, ...this.equipment];
  }

  // Handler symbols — these mark Player as implementing these interfaces
  readonly [BODY_MOVE_HANDLER] = true;
  readonly [TAKE_ANY_DAMAGE_HANDLER] = true;
  readonly [ATTACK_HANDLER] = true;
  readonly [DEAL_ATTACK_DAMAGE_HANDLER] = true;
  readonly [ACTION_PERFORMED_HANDLER] = true;
  readonly [STATUS_ADDED_HANDLER] = true;
  readonly [DEATH_HANDLER] = true;
  readonly [KILL_HANDLER] = true;

  constructor(pos: Vector2Int) {
    super(pos);
    this.faction = Faction.Ally;
    this.inventory = new Inventory(10);
    this.equipment = new Equipment(this);
    this.hands = new ItemHands(this);
    this._hp = this._baseMaxHp = 12;
  }

  /** IDeathHandler */
  handleDeath(_source: Entity): void {
    GameModelRef.main.enqueuEvent(() => {
      // GameOver handled by GameModel
    });
  }

  /** IBodyMoveHandler */
  handleMove(_newPos: Vector2Int, _oldPos: Vector2Int): void {
    this.floor?.recomputeVisibility();
  }

  /** ITakeAnyDamageHandler */
  handleTakeAnyDamage(_damage: number): void {
    // Cancel path following on damage
    if (this.task && this.task.constructor.name === 'FollowPathTask') {
      this.clearTasks();
    }
  }

  /** IDealAttackDamageHandler */
  handleDealAttackDamage(_damage: number, _target: Body): void {
    // stats tracking
  }

  /** IKillEntityHandler */
  onKill(_entity: Entity): void {
    // stats tracking
  }

  /** IActionPerformedHandler */
  handleActionPerformed(finalAction: BaseAction, initialAction: BaseAction): void {
    if (finalAction !== initialAction) {
      this.clearTasks();
    }
  }

  /** IAttackHandler */
  onAttack(_damage: number, _target: Body): void {
    const item = this.equipment.get(EquipmentSlot.Weapon);
    if (item && DURABLE_TAG in item) {
      GameModelRef.main.enqueuEvent(() => reduceDurability(item as unknown as IDurable));
    }
    if (this.task && this.task.constructor.name === 'FollowPathTask') {
      this.task = null;
    }
  }

  baseAttackDamage(): [number, number] {
    const item = this.equipment.get(EquipmentSlot.Weapon);
    if (item && WEAPON_TAG in item) {
      return (item as unknown as IWeapon).attackSpread;
    }
    return [1, 1];
  }

  protected onMoveFailed(_target: Vector2Int): void {
    this.clearTasks();
  }

  protected handleEnterFloor(): void {
    this.floor?.recomputeVisibility();
    if (this.floor) {
      this.floor.timePlayerEntered = GameModelRef.main?.time ?? 0;
    }
  }

  replenish(): void {
    this._hp = this.maxHp;
    const debuffs = this.statuses.list.filter(s => s.isDebuff);
    for (const d of debuffs) {
      this.statuses.remove(d);
    }
  }

  getVisibleActors(faction: Faction): Actor[] {
    if (!this.floor) return [];
    const result: Actor[] = [];
    for (const b of this.floor.bodies) {
      if ('faction' in b && (b as Actor).isVisible && ((b as Actor).faction & faction) !== 0) {
        result.push(b as Actor);
      }
    }
    return result;
  }

  isInCombat(): boolean {
    return this.getVisibleActors(Faction.Enemy).length > 0;
  }
}
