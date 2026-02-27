import { Item, STACKABLE_TAG, USABLE_TAG, type IStackable, type IUsable } from '../Item';
import type { Actor } from '../Actor';

/**
 * Consumable healing item. Heals 2 HP per use.
 * Port of C# ItemRedberry.cs.
 */
export class ItemRedberry extends Item implements IStackable, IUsable {
  readonly [STACKABLE_TAG] = true as const;
  readonly [USABLE_TAG] = true as const;

  private _stacks: number;

  get stacksMax(): number {
    return 10;
  }

  get stacks(): number {
    return this._stacks;
  }

  set stacks(value: number) {
    if (value < 0) {
      throw new Error(`Setting negative stack! ${this} to ${value}`);
    }
    this._stacks = value;
    if (this._stacks === 0) {
      this.Destroy();
    }
  }

  constructor(stacks = 1) {
    super();
    this._stacks = stacks;
  }

  use(actor: Actor): void {
    actor.heal(2);
    this.stacks--;
  }

  getStats(): string {
    return 'Heals 2 HP.';
  }
}
