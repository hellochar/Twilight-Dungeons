import {
  Item,
  EquippableItem,
  WEAPON_TAG,
  DURABLE_TAG,
  STACKABLE_TAG,
  EDIBLE_TAG,
  reduceDurability,
  type IWeapon,
  type IDurable,
  type IStackable,
  type IEdible,
} from '../Item';
import { EquipmentSlot } from '../Equipment';
import { GameModelRef } from '../GameModelRef';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  type IAttackDamageTakenModifier,
} from '../../core/Modifiers';
import {
  BODY_TAKE_ATTACK_DAMAGE_HANDLER,
  type IBodyTakeAttackDamageHandler,
} from '../Body';
import { ATTACK_HANDLER, type IAttackHandler } from '../Actor';
import { ConstrictedStatus } from '../statuses/ConstrictedStatus';
import { HeartyVeggieStatus } from '../statuses/HeartyVeggieStatus';
import { Grass } from '../grasses/Grass';
import { entityRegistry } from '../../generator/entityRegistry';
import {
  PseudoRandomDistribution,
  CfromP,
} from '../../core/PseudoRandomDistribution';
import { Faction } from '../../core/types';
import type { ISteppable } from '../Floor';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Actor } from '../Actor';
import type { Body } from '../Body';

// ─── ItemThicket ───

/**
 * Armor that constricts enemies who attack you.
 * Port of C# ItemThicket from StoutShrub.cs.
 */
export class ItemThicket
  extends EquippableItem
  implements IStackable, IBodyTakeAttackDamageHandler
{
  readonly [STACKABLE_TAG] = true as const;
  readonly [BODY_TAKE_ATTACK_DAMAGE_HANDLER] = true as const;

  readonly stacksMax = 100;
  private _stacks: number;

  get stacks(): number {
    return this._stacks;
  }

  set stacks(value: number) {
    if (value < 0) throw new Error('Setting negative stack! ' + this + ' to ' + value);
    this._stacks = value;
    if (this._stacks === 0) {
      this.Destroy();
    }
  }

  get slot(): EquipmentSlot {
    return EquipmentSlot.Armor;
  }

  constructor(stacks: number) {
    super();
    this._stacks = stacks;
  }

  handleTakeAttackDamage(_damage: number, _hp: number, source: any): void {
    if (source.faction !== Faction.Ally) {
      source.statuses.add(new ConstrictedStatus(null, 6));
      this.stacks--;
    }
  }

  getStats(): string {
    return 'Constrict enemies who attack you for 6 turns.';
  }
}

// ─── ItemPrickler ───

/**
 * Weapon that spawns PricklyGrowth on attacked tile.
 * Port of C# ItemPrickler from StoutShrub.cs.
 */
export class ItemPrickler
  extends EquippableItem
  implements IWeapon, IStackable, IAttackHandler
{
  readonly [WEAPON_TAG] = true as const;
  readonly [STACKABLE_TAG] = true as const;
  readonly [ATTACK_HANDLER] = true as const;

  readonly attackSpread: [number, number] = [1, 2];
  readonly stacksMax = 100;
  private _stacks: number;

  get stacks(): number {
    return this._stacks;
  }

  set stacks(value: number) {
    if (value < 0) throw new Error('Setting negative stack! ' + this + ' to ' + value);
    this._stacks = value;
    if (this._stacks === 0) {
      this.Destroy();
    }
  }

  get slot(): EquipmentSlot {
    return EquipmentSlot.Weapon;
  }

  constructor(stacks = 1) {
    super();
    this._stacks = stacks;
  }

  onAttack(_damage: number, target: Body): void {
    if ('faction' in target && target.floor) {
      target.floor.put(new PricklyGrowth(target.pos));
    }
  }

  getStats(): string {
    return 'Leaves a Prickly Growth on the attacked Creature\'s tile, which deals 3 attack damage to the Creature standing over it next turn.';
  }
}

// ─── PricklyGrowth ───

/**
 * Grass that deals 3 attack damage next turn to the actor standing on it, then self-destructs.
 * Port of C# PricklyGrowth from StoutShrub.cs.
 */
export class PricklyGrowth extends Grass implements ISteppable {
  timeNextAction: number;
  get turnPriority(): number { return 11; }

  private get actor(): any {
    return this.floor?.bodies.get(this.pos) ?? null;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.timeNextAction = (GameModelRef.mainOrNull?.time ?? 0) + 1;
  }

  step(): number {
    this.onNoteworthyAction();
    const body = this.actor;
    if (body) {
      body.takeAttackDamage(3, GameModelRef.main.player);
    }
    this.killSelf();
    return 3;
  }
}

entityRegistry.register('PricklyGrowth', PricklyGrowth);

// ─── ItemStoutShield ───

const prdC50 = CfromP(0.5);

/**
 * Offhand shield with 50% PRD-based chance to block all attack damage.
 * Port of C# ItemStoutShield from StoutShrub.cs.
 */
export class ItemStoutShield
  extends EquippableItem
  implements IDurable, IAttackDamageTakenModifier
{
  readonly [DURABLE_TAG] = true as const;
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;

  durability: number;

  get maxDurability(): number {
    return 20;
  }

  get slot(): EquipmentSlot {
    return EquipmentSlot.Offhand;
  }

  private prd: PseudoRandomDistribution;

  constructor() {
    super();
    this.durability = this.maxDurability;
    this.prd = new PseudoRandomDistribution(prdC50);
  }

  modify(input: any): any {
    if (typeof input === 'number' && input > 0) {
      // invert the test so the very first turn is actually ~70%, but successive blocks are unlikely
      if (!this.prd.test()) {
        reduceDurability(this);
        return 0;
      }
    }
    return input;
  }

  getStats(): string {
    return '50% chance to block all damage from an attack.';
  }
}

// ─── ItemHeartyVeggie ───

/**
 * Edible item that grants HeartyVeggieStatus.
 * Port of C# ItemHeartyVeggie from StoutShrub.cs.
 */
export class ItemHeartyVeggie extends Item implements IEdible {
  readonly [EDIBLE_TAG] = true as const;

  eat(actor: Actor): void {
    actor.statuses.add(new HeartyVeggieStatus(4));
    this.Destroy();
  }

  getStats(): string {
    return 'Heal 4 HP over 100 turns.\nWill not tick down if you\'re at full HP.';
  }
}
