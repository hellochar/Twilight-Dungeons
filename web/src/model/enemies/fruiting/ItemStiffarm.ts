import { EquippableItem, DURABLE_TAG, WEAPON_TAG, STICKY_TAG, type IDurable, type IWeapon, type ISticky } from '../../Item';
import { ATTACK_DAMAGE_TAKEN_MOD, type IAttackDamageTakenModifier } from '../../../core/Modifiers';
import { EquipmentSlot } from '../../Equipment';

/**
 * Weapon infection. +1 attack damage taken.
 * Port of C# ItemStiffarm from FruitingBody.cs.
 */
export class ItemStiffarm
  extends EquippableItem
  implements IDurable, IWeapon, IAttackDamageTakenModifier, ISticky
{
  readonly [DURABLE_TAG] = true as const;
  readonly [WEAPON_TAG] = true as const;
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;
  readonly [STICKY_TAG] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Weapon;
  }

  get maxDurability(): number {
    return 15;
  }

  get attackSpread(): [number, number] {
    return [2, 3];
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  modify(input: any): any {
    return input + 1;
  }

  getStats(): string {
    return "You're infected with Stiffarm!\nYou take +1 damage from attacks.";
  }
}
