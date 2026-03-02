import { EventEmitter } from '../core/EventEmitter';
import { GameModelRef } from './GameModelRef';
import type { EquipmentSlot } from './Equipment';
import type { Inventory } from './Inventory';
import type { Actor } from './Actor';
import type { Entity } from './Entity';
import type { Player } from './Player';
import type { Soil } from './Tile';

// ─── Symbol tags for item interfaces ───

export const WEAPON_TAG = Symbol.for('IWeapon');
export const DURABLE_TAG = Symbol.for('IDurable');
export const STACKABLE_TAG = Symbol.for('IStackable');
export const USABLE_TAG = Symbol.for('IUsable');
export const EDIBLE_TAG = Symbol.for('IEdible');
export const PLANTABLE_TAG = Symbol.for('IPlantable');
export const STICKY_TAG = Symbol.for('ISticky');
export const TARGETED_ACTION_TAG = Symbol.for('ITargetedAction');

// ─── Item interfaces ───

export interface IWeapon {
  readonly [WEAPON_TAG]: true;
  readonly attackSpread: [number, number];
}

export interface IDurable {
  readonly [DURABLE_TAG]: true;
  durability: number;
  readonly maxDurability: number;
}

export function reduceDurability(durable: IDurable): void {
  durable.durability--;
  if (durable.durability <= 0 && durable instanceof Item) {
    durable.Destroy();
  }
}

export function increaseDurability(durable: IDurable, amount = 1): void {
  durable.durability = Math.min(durable.durability + amount, durable.maxDurability);
}

export interface IStackable {
  readonly [STACKABLE_TAG]: true;
  stacks: number;
  readonly stacksMax: number;
}

export interface IConditionallyStackable extends IStackable {
  canStackWith(other: IConditionallyStackable): boolean;
}

/** Merge as many stacks as possible from other into this. Returns true if other is now empty. */
export function mergeStacks<T extends IStackable>(target: T, source: T): boolean {
  const spaceLeft = target.stacksMax - target.stacks;
  const stacksToAdd = Math.max(0, Math.min(source.stacks, spaceLeft));
  target.stacks += stacksToAdd;
  source.stacks -= stacksToAdd;
  return source.stacks === 0;
}

export interface IUsable {
  readonly [USABLE_TAG]: true;
  use(actor: Actor): void;
}

export interface IEdible {
  readonly [EDIBLE_TAG]: true;
  eat(actor: Actor): void;
}

export interface IPlantable {
  readonly [PLANTABLE_TAG]: true;
  plant(actor: Actor, soil: Soil): void;
}

/** Marker: cannot unequip or destroy once equipped. */
export interface ISticky {
  readonly [STICKY_TAG]: true;
}

/** Item that requires player to select a target before use (e.g. place on tile, charm enemy). */
export interface ITargetedAction {
  readonly [TARGETED_ACTION_TAG]: true;
  readonly targetedActionName: string;
  targets(player: Player): Entity[];
  performTargetedAction(player: Player, target: Entity): void;
}

// ─── Item base class ───

/**
 * Base class for all items.
 * NOT an Entity — items live inside Inventory/Equipment.
 * ItemOnGround wraps an Item as an Entity for floor placement.
 * Port of C# Item.cs.
 */
export class Item {
  private _inventory: Inventory | null = null;

  get inventory(): Inventory | null {
    return this._inventory;
  }

  set inventory(value: Inventory | null) {
    this._inventory = value;
  }

  readonly onDestroyed = new EventEmitter<[]>();

  get displayName(): string {
    // Strip "Item" prefix and add spaces before capitals
    const name = this.constructor.name;
    const stripped = name.startsWith('Item') ? name.substring(4) : name;
    return stripped.replace(/([A-Z])/g, ' $1').trim();
  }

  getStats(): string {
    return '';
  }

  getStatsFull(): string {
    let text = this.getStats() + '\n';
    if (WEAPON_TAG in this) {
      const w = this as unknown as IWeapon;
      const [min, max] = w.attackSpread;
      text += min === max ? `Damage: ${min}. ` : `Damage: ${min}-${max}. `;
    }
    if (DURABLE_TAG in this) {
      const d = this as unknown as IDurable;
      text += `Durability: ${d.durability}/${d.maxDurability}.`;
    }
    return text.trim();
  }

  Destroy(): void {
    if (this.inventory != null) {
      this.onDestroyed.emit();
      this.inventory.onItemDestroyed.emit(this);
      this.inventory.removeItem(this);
    }
  }

  Drop(actor: Actor): void {
    if (this.inventory != null && actor.floor) {
      this.inventory.tryDropItem(actor.floor, actor.pos, this);
    }
  }

  getAvailableMethods(): string[] {
    const methods: string[] = ['Drop'];
    if (EDIBLE_TAG in this) methods.push('Eat');
    if (USABLE_TAG in this) methods.push('Use');
    if (TARGETED_ACTION_TAG in this) {
      methods.push((this as unknown as ITargetedAction).targetedActionName);
    }
    return methods;
  }
}

// ─── EquippableItem ───

/**
 * Item that can be equipped in a specific slot.
 * Port of C# EquippableItem.cs.
 */
export abstract class EquippableItem extends Item {
  abstract get slot(): EquipmentSlot;

  protected get player() {
    return GameModelRef.main.player;
  }

  get isEquipped(): boolean {
    return this.inventory != null;
  }

  Equip(actor: Actor): void {
    (actor as any).equipment?.addItem(this);
  }

  Unequip(actor: Actor): void {
    (actor as any).inventory?.addItem(this);
  }

  /** Called when placed into equipment. Override for custom behavior. */
  OnEquipped(): void {}

  /** Called when removed from equipment. Override for custom behavior. */
  OnUnequipped(): void {}

  getAvailableMethods(): string[] {
    const methods = super.getAvailableMethods();
    // Check if in inventory (can equip) or in equipment (can unequip)
    const player = GameModelRef.mainOrNull?.player;
    if (player) {
      if (player.inventory.hasItem(this)) {
        methods.push('Equip');
      } else if (player.equipment.hasItem(this)) {
        methods.push('Unequip');
      }
      // Sticky items can't be unequipped/dropped/destroyed
      if (STICKY_TAG in this) {
        const idx = methods.indexOf('Unequip');
        if (idx >= 0) methods.splice(idx, 1);
        if (player.equipment.hasItem(this)) {
          const dropIdx = methods.indexOf('Drop');
          if (dropIdx >= 0) methods.splice(dropIdx, 1);
        }
      }
    }
    return methods;
  }
}
