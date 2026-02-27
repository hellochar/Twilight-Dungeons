import { EventEmitter } from '../core/EventEmitter';
import {
  Item,
  STACKABLE_TAG,
  mergeStacks,
  type IStackable,
  type IConditionallyStackable,
} from './Item';
import { ItemOnGround } from './ItemOnGround';
import type { Entity } from './Entity';
import type { Floor } from './Floor';
import type { Vector2Int } from '../core/Vector2Int';

/**
 * Fixed-size item storage with stacking support.
 * Port of C# Inventory.cs (158 lines).
 */
export class Inventory {
  protected items: (Item | null)[];

  readonly onItemAdded = new EventEmitter<[Item, Entity | null]>();
  readonly onItemRemoved = new EventEmitter<[Item]>();
  readonly onItemDestroyed = new EventEmitter<[Item]>();

  get capacity(): number {
    return this.items.length;
  }

  constructor(capacity: number) {
    this.items = new Array<Item | null>(capacity).fill(null);
  }

  /** Get item at slot index. Override in Equipment for weapon fallback. */
  getAt(index: number): Item | null {
    return this.items[index] ?? null;
  }

  get isFull(): boolean {
    return this.getFirstFreeSlot() === null;
  }

  /**
   * Add an item to a specific slot. Handles IStackable merging.
   * Returns true if the item was successfully added.
   */
  addItemAtSlot(item: Item, slot: number, source: Entity | null = null): boolean {
    // Try to merge with existing stacks of the same type
    if (STACKABLE_TAG in item) {
      const stackable = item as unknown as IStackable;
      for (const existing of this.itemsNonNull()) {
        if (existing.constructor !== item.constructor) continue;
        // Check conditional stacking
        if ('canStackWith' in item && 'canStackWith' in existing) {
          const c1 = item as unknown as IConditionallyStackable;
          const c2 = existing as unknown as IConditionallyStackable;
          if (!c1.canStackWith(c2)) continue;
        }
        const isConsumed = mergeStacks(
          existing as unknown as IStackable,
          stackable
        );
        if (isConsumed) {
          item.Destroy();
          this.handleItemAdded(existing, source);
          return true;
        }
      }
    }

    if (slot === -1) return false;

    // Swap logic: if item is in another inventory, swap
    const otherInventory = item.inventory;
    if (otherInventory != null) {
      const otherSlot = otherInventory.indexOf(item);
      otherInventory.removeItem(item);
      const itemToSwap = this.items[slot];
      if (itemToSwap != null) {
        this.removeItem(itemToSwap);
        otherInventory.addItemAtSlot(itemToSwap, otherSlot);
      }
    }

    this.items[slot] = item;
    item.inventory = this;
    this.handleItemAdded(item, source);
    return true;
  }

  /** Add an item to the first free slot. */
  addItem(item: Item, source: Entity | null = null): boolean {
    const slot = this.getFirstFreeSlot();
    if (!(STACKABLE_TAG in item) && slot === null) {
      return false;
    }
    return this.addItemAtSlot(item, slot ?? -1, source);
  }

  protected handleItemAdded(item: Item, source: Entity | null): void {
    this.onItemAdded.emit(item, source);
  }

  protected handleItemRemoved(item: Item): void {
    this.onItemRemoved.emit(item);
  }

  removeItem(item: Item): boolean {
    const slot = this.items.indexOf(item);
    if (slot < 0) return false;
    this.items[slot] = null;
    item.inventory = null;
    this.handleItemRemoved(item);
    return true;
  }

  tryDropItem(floor: Floor, pos: Vector2Int, item: Item): void {
    this.removeItem(item);
    const itemOnGround = new ItemOnGround(pos, item, pos);
    floor.put(itemOnGround);
  }

  tryDropAllItems(floor: Floor, pos: Vector2Int): void {
    for (const item of this.itemsNonNull()) {
      this.tryDropItem(floor, pos, item);
    }
  }

  hasItem(item: Item): boolean {
    return this.items.includes(item);
  }

  indexOf(item: Item): number {
    return this.items.indexOf(item);
  }

  /** Iterate over non-null items. */
  *itemsNonNull(): IterableIterator<Item> {
    for (const item of this.items) {
      if (item != null) yield item;
    }
  }

  private getFirstFreeSlot(): number | null {
    const idx = this.items.indexOf(null);
    return idx === -1 ? null : idx;
  }

  *[Symbol.iterator](): IterableIterator<Item | null> {
    yield* this.items;
  }
}
