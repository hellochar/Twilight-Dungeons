import type { Actor } from './Actor';

export enum EquipmentSlot {
  Weapon,
  Head,
  Body,
  Feet,
  Accessory,
}

/**
 * Stub for Equipment — will be fully implemented in Session 7.
 */
export class Equipment {
  private slots = new Map<EquipmentSlot, any>();
  private actor: Actor;

  constructor(actor: Actor) {
    this.actor = actor;
  }

  get(slot: EquipmentSlot): any | null {
    return this.slots.get(slot) ?? null;
  }

  set(slot: EquipmentSlot, item: any): void {
    this.slots.set(slot, item);
  }

  remove(slot: EquipmentSlot): any | null {
    const item = this.slots.get(slot) ?? null;
    this.slots.delete(slot);
    return item;
  }

  *[Symbol.iterator](): IterableIterator<any> {
    yield* this.slots.values();
  }
}
