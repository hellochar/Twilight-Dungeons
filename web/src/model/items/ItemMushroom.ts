import { Item, STACKABLE_TAG, EDIBLE_TAG, type IStackable, type IEdible } from '../Item';
import type { Actor } from '../Actor';
import { PumpedUpStatus } from '../statuses/PumpedUpStatus';

/**
 * Stackable edible. Eating all stacks grants PumpedUpStatus (2x attack speed for N attacks).
 * Port of C# ItemMushroom.cs.
 */
export class ItemMushroom extends Item implements IStackable, IEdible {
  readonly [STACKABLE_TAG] = true as const;
  readonly [EDIBLE_TAG] = true as const;

  readonly stacksMax = 100;
  private _stacks: number;

  get stacks(): number {
    return this._stacks;
  }

  set stacks(value: number) {
    if (value < 0) throw new Error('Setting negative stack! ' + this + ' to ' + value);
    this._stacks = value;
    if (this._stacks === 0) {
      this.Destroy();
    }
  }

  constructor(stacks = 1) {
    super();
    this._stacks = stacks;
  }

  eat(actor: Actor): void {
    actor.statuses.add(new PumpedUpStatus(this.stacks));
    this.stacks = 0;
  }

  getStats(): string {
    return `Eat all to make your next ${this.stacks} attacks twice as fast.`;
  }
}
