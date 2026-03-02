import { EquippableItem, DURABLE_TAG, reduceDurability, type IDurable } from '../Item';
import { EquipmentSlot } from '../Equipment';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  type IAttackDamageTakenModifier,
} from '../../core/Modifiers';

/**
 * Offhand shield that blocks 2 damage per hit.
 * Port of C# ItemBarkShield from Frizzlefen.cs.
 */
export class ItemBarkShield
  extends EquippableItem
  implements IDurable, IAttackDamageTakenModifier
{
  readonly [DURABLE_TAG] = true as const;
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Offhand;
  }

  get maxDurability(): number {
    return 6;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  modify(input: any): any {
    if (typeof input === 'number') {
      reduceDurability(this);
      return Math.max(0, input - 2);
    }
    return input;
  }

  getStats(): string {
    return 'Blocks 2 damage per hit.';
  }
}
