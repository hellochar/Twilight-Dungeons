import { Inventory } from './Inventory';
import { EquippableItem, Item, STICKY_TAG } from './Item';
import { CannotPerformActionException } from './BaseAction';
import type { Entity } from './Entity';
import type { Player } from './Player';

/**
 * Equipment slots matching C# EquipmentSlot enum ordering.
 */
export enum EquipmentSlot {
  Headwear = 0,
  Weapon = 1,
  Armor = 2,
  Offhand = 3,
  Footwear = 4,
}

/**
 * Specialized inventory where only EquippableItems can be added.
 * Each slot corresponds to an EquipmentSlot.
 * Port of C# Equipment.cs (58 lines).
 */
export class Equipment extends Inventory {
  readonly player: Player;

  constructor(player: Player) {
    super(5);
    this.player = player;
  }

  /** Get item at slot. Weapon slot returns Player.hands if empty. */
  override getAt(index: number): Item | null {
    if (index === EquipmentSlot.Weapon) {
      return super.getAt(index) ?? this.player.hands;
    }
    return super.getAt(index);
  }

  /** Get item by equipment slot enum. */
  get(slot: EquipmentSlot): Item | null {
    return this.getAt(slot);
  }

  /** Only accepts EquippableItems; places in correct slot. */
  override addItem(item: Item, source: Entity | null = null): boolean {
    if (item instanceof EquippableItem) {
      const slot = item.slot as number;
      // Check for sticky items blocking the slot
      const existing = super.getAt(slot);
      if (existing && STICKY_TAG in existing) {
        throw new CannotPerformActionException(
          existing.displayName + ' is stuck to your body!'
        );
      }
      return this.addItemAtSlot(item, slot, source);
    }
    return false;
  }

  protected override handleItemAdded(item: Item, source: Entity | null): void {
    super.handleItemAdded(item, source);
    if (item instanceof EquippableItem) {
      item.OnEquipped();
    }
  }

  protected override handleItemRemoved(item: Item): void {
    super.handleItemRemoved(item);
    if (item instanceof EquippableItem) {
      item.OnUnequipped();
    }
  }

  /** Iterate over equipped items (non-null). */
  *[Symbol.iterator](): IterableIterator<Item | null> {
    for (let i = 0; i < this.capacity; i++) {
      const item = this.getAt(i);
      if (item != null) yield item;
    }
  }
}
