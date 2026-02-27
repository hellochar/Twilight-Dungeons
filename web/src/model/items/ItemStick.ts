import {
  EquippableItem,
  WEAPON_TAG,
  DURABLE_TAG,
  reduceDurability,
  type IWeapon,
  type IDurable,
} from '../Item';
import { EquipmentSlot } from '../Equipment';

/**
 * A basic weapon with limited durability.
 * Port of C# ItemStick.cs.
 */
export class ItemStick extends EquippableItem implements IWeapon, IDurable {
  readonly [WEAPON_TAG] = true as const;
  readonly [DURABLE_TAG] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Weapon;
  }

  get maxDurability(): number {
    return 3;
  }

  get attackSpread(): [number, number] {
    return [2, 2];
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  reduceDurability(): void {
    reduceDurability(this);
  }
}
