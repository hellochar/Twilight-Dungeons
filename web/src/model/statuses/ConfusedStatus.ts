import { StackingStatus, StackingMode } from '../Status';
import {
  BASE_ACTION_MOD,
  type IBaseActionModifier,
} from '../../core/Modifiers';
import { ActionType } from '../../core/types';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';

/**
 * Forces random movement for the next N actions.
 * Port of C# ConfusedStatus.
 */
export class ConfusedStatus extends StackingStatus implements IBaseActionModifier {
  readonly [BASE_ACTION_MOD] = true as const;

  get stackingMode(): StackingMode {
    return StackingMode.Max;
  }

  get isDebuff(): boolean {
    return true;
  }

  constructor(stacks: number) {
    super(stacks);
  }

  modify(input: any): any {
    if (typeof input === 'object' && input !== null && 'type' in input) {
      // BaseAction path
      this.stacks--;
      if (input.type === ActionType.MOVE || input.type === ActionType.ATTACK) {
        return MoveRandomlyTask.getRandomMove(input.actor);
      }
      return input;
    }
    // STEP_MOD path
    return super.modify(input);
  }
}
