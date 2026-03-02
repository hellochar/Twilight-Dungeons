import { MAX_HP_MOD, type IMaxHPModifier } from '../../core/Modifiers';
import { StackingStatus } from '../Status';

/**
 * +stacks max HP.
 * Port of C# BarkmealStatus from Frizzlefen.cs.
 */
export class BarkmealStatus extends StackingStatus implements IMaxHPModifier {
  readonly [MAX_HP_MOD] = true as const;

  constructor(stacks = 4) {
    super(stacks);
  }

  modify(input: any): any {
    if (typeof input === 'number') {
      return input + this.stacks;
    }
    return super.modify(input);
  }
}
