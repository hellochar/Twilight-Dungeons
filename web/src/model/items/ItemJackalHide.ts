import { EquippableItem, DURABLE_TAG, reduceDurability, type IDurable } from '../Item';
import { EquipmentSlot } from '../Equipment';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  type IAttackDamageTakenModifier,
} from '../../core/Modifiers';

/**
 * Armor that blocks 1 damage per hit.
 * Port of C# ItemJackalHide from Jackal.cs.
 */
export class ItemJackalHide
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

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  modify(input: any): any {
    if (typeof input === 'number') {
      reduceDurability(this);
      return Math.max(0, input - 1);
    }
    return input;
  }

  getStats(): string {
    return 'Blocks 1 damage per hit.';
  }
}
