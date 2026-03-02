import {
  Item,
  EquippableItem,
  WEAPON_TAG,
  DURABLE_TAG,
  EDIBLE_TAG,
  reduceDurability,
  type IWeapon,
  type IDurable,
  type IEdible,
} from '../Item';
import { EquipmentSlot } from '../Equipment';
import { TAKE_ANY_DAMAGE_HANDLER, HEAL_HANDLER, type ITakeAnyDamageHandler, type IHealHandler } from '../Body';
import { MAX_HP_MOD, type IMaxHPModifier } from '../../core/Modifiers';
import { StrengthStatus } from '../statuses/StrengthStatus';
import { RecoveringStatus } from '../statuses/RecoveringStatus';
import { ArmoredStatus } from '../statuses/ArmoredStatus';
import type { Actor } from '../Actor';

// ─── ItemFlowerBuds ───

/**
 * Eat to heal 1 HP and gain 2 stacks of Strength.
 * Port of C# ItemFlowerBuds from ChangErsWillow.cs.
 */
export class ItemFlowerBuds extends Item implements IDurable, IEdible {
  readonly [DURABLE_TAG] = true as const;
  readonly [EDIBLE_TAG] = true as const;

  durability: number;

  get maxDurability(): number {
    return 3;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  eat(actor: Actor): void {
    actor.heal(1);
    actor.statuses.add(new StrengthStatus(2));
    reduceDurability(this);
  }

  getStats(): string {
    return 'Eat to heal 1 HP and get 2 stacks of Strength.';
  }
}

// ─── ItemCatkin ───

/**
 * Headwear. On damage: add RecoveringStatus, reduce durability.
 * Port of C# ItemCatkin from ChangErsWillow.cs.
 */
export class ItemCatkin
  extends EquippableItem
  implements IDurable, ITakeAnyDamageHandler
{
  readonly [DURABLE_TAG] = true as const;
  readonly [TAKE_ANY_DAMAGE_HANDLER] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Headwear;
  }

  get maxDurability(): number {
    return 3;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  handleTakeAnyDamage(damage: number): void {
    if (damage > 0) {
      this.player.statuses.add(new RecoveringStatus(damage));
      reduceDurability(this);
    }
  }

  getStats(): string {
    return 'When you take damage, heal an equivalent amount after 25 turns.';
  }
}

// ─── ItemHardenedSap ───

/**
 * Armor. +4 max HP. On heal: add ArmoredStatus stacks.
 * Port of C# ItemHardenedSap from ChangErsWillow.cs.
 */
export class ItemHardenedSap
  extends EquippableItem
  implements IDurable, IHealHandler, IMaxHPModifier
{
  readonly [DURABLE_TAG] = true as const;
  readonly [HEAL_HANDLER] = true as const;
  readonly [MAX_HP_MOD] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Armor;
  }

  get maxDurability(): number {
    return 5;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  handleHeal(amount: number): void {
    if (amount > 0) {
      this.player.statuses.add(new ArmoredStatus(amount));
      reduceDurability(this);
    }
  }

  modify(input: any): any {
    if (typeof input === 'number') {
      return input + 4;
    }
    return input;
  }

  getStats(): string {
    return '+4 Max HP.\nWhen you heal, gain that many stacks of the Armored Status.';
  }
}

// ─── ItemCrescentVengeance ───

/**
 * Weapon [3,5]. Removes Armored stacks instead of losing durability if possible.
 * Port of C# ItemCrescentVengeance from ChangErsWillow.cs.
 */
export class ItemCrescentVengeance
  extends EquippableItem
  implements IWeapon, IDurable
{
  readonly [WEAPON_TAG] = true as const;
  readonly [DURABLE_TAG] = true as const;

  private _durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Weapon;
  }

  get maxDurability(): number {
    return 10;
  }

  get attackSpread(): [number, number] {
    return [3, 5];
  }

  get durability(): number {
    return this._durability;
  }

  set durability(value: number) {
    if (value >= this._durability) {
      this._durability = value;
      return;
    }
    const status = this.player?.statuses.findOfType(ArmoredStatus);
    if (!status) {
      this._durability = value;
      return;
    }

    let amountToLose = this._durability - value;
    const stacksLost = Math.min(amountToLose, status.stacks);
    status.stacks -= stacksLost;
    amountToLose -= stacksLost;

    this._durability -= amountToLose;
  }

  constructor() {
    super();
    this._durability = this.maxDurability;
  }

  getStats(): string {
    return 'If possible, Crescent Vengeance removes a stack of the Armored Status rather than lose durability.';
  }
}
