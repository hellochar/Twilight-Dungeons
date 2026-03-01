import { Item, STACKABLE_TAG, EDIBLE_TAG, type IStackable, type IEdible } from '../Item';
import type { Actor } from '../Actor';
import { StatusWild } from '../statuses/StatusWild';

/**
 * Stackable edible. Eating grants StatusWild (2x move speed for 15 turns).
 * Port of C# ItemWildwoodLeaf.cs.
 */
export class ItemWildwoodLeaf extends Item implements IStackable, IEdible {
  readonly [STACKABLE_TAG] = true as const;
  readonly [EDIBLE_TAG] = true as const;

  readonly stacksMax = 10;
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
    actor.statuses.add(new StatusWild());
    this.stacks--;
  }

  getStats(): string {
    return 'Apply the Wild status for 15 turns, doubling your movespeed.';
  }
}
