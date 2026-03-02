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
import { BODY_MOVE_HANDLER, type IBodyMoveHandler } from '../Body';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  type IAttackDamageTakenModifier,
} from '../../core/Modifiers';
import { ArmoredStatus } from '../statuses/ArmoredStatus';
import { BarkmealStatus } from '../statuses/BarkmealStatus';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Actor } from '../Actor';

// ─── ItemThickBranch ───

/**
 * Weapon [3,3] with 2 durability.
 * Port of C# ItemThickBranch from Frizzlefen.cs.
 */
export class ItemThickBranch extends EquippableItem implements IWeapon, IDurable {
  readonly [WEAPON_TAG] = true as const;
  readonly [DURABLE_TAG] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Weapon;
  }

  get maxDurability(): number {
    return 2;
  }

  get attackSpread(): [number, number] {
    return [3, 3];
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }
}

// ─── ItemPlatedArmor ───

/**
 * Armor with scaling damage block: first hit blocks 1, then 2, then 3, then 4.
 * Port of C# ItemPlatedArmor from Frizzlefen.cs.
 */
export class ItemPlatedArmor
  extends EquippableItem
  implements IDurable, IAttackDamageTakenModifier
{
  readonly [DURABLE_TAG] = true as const;
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Armor;
  }

  get maxDurability(): number {
    return 4;
  }

  private get damageBlock(): number {
    return (this.maxDurability + 1) - this.durability;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  modify(input: any): any {
    if (typeof input === 'number') {
      if (input > 0) {
        reduceDurability(this);
        return input - this.damageBlock;
      }
      return input;
    }
    return input;
  }

  getStats(): string {
    return `Blocks ${this.damageBlock} damage. Each time this blocks damage, increase damage blocked by 1.`;
  }
}

// ─── ItemBarkmeal ───

/**
 * Eat to heal 2 HP and gain BarkmealStatus (+4 max HP).
 * Port of C# ItemBarkmeal from Frizzlefen.cs.
 */
export class ItemBarkmeal extends Item implements IEdible {
  readonly [EDIBLE_TAG] = true as const;

  eat(actor: Actor): void {
    actor.statuses.add(new BarkmealStatus());
    actor.heal(2);
    this.Destroy();
  }

  getStats(): string {
    return 'Heal 2 HP and permanently gain +4 max HP.';
  }
}

// ─── ItemStompinBoots ───

/**
 * Footwear. On move to tile with grass: kill grass + gain Armored if not already Armored.
 * Port of C# ItemStompinBoots from Frizzlefen.cs.
 */
export class ItemStompinBoots extends EquippableItem implements IBodyMoveHandler {
  readonly [BODY_MOVE_HANDLER] = true as const;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Footwear;
  }

  handleMove(_newPos: Vector2Int, _oldPos: Vector2Int): void {
    const grass = this.player.grass;
    if (grass && !this.player.statuses.findOfType(ArmoredStatus)) {
      grass.kill(this.player);
      this.player.statuses.add(new ArmoredStatus());
    }
  }

  getStats(): string {
    return 'Everlasting.\nWhen you walk on to a Grass, Kill it and gain Armored. This only happens if you are not Armored.';
  }
}
