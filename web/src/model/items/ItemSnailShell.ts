import { Item, STACKABLE_TAG, type IStackable } from '../Item';

/**
 * Stackable item. Can be thrown for 3 damage (targeting UI not yet implemented).
 * Port of C# ItemSnailShell from Snail.cs.
 */
export class ItemSnailShell extends Item implements IStackable {
  readonly [STACKABLE_TAG] = true as const;

  readonly stacksMax = 3;
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

  constructor(stacks: number) {
    super();
    this._stacks = stacks;
  }
}
