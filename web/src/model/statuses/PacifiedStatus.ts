import { Status } from '../Status';
import {
  BASE_ACTION_MOD,
  type IBaseActionModifier,
} from '../../core/Modifiers';
import { ActionType } from '../../core/types';
import { StruggleBaseAction } from '../BaseAction';

/**
 * Cannot attack while standing on an open Violet.
 * Removes itself when the actor leaves the Violet.
 * Port of C# PacifiedStatus.
 */
export class PacifiedStatus extends Status implements IBaseActionModifier {
  readonly [BASE_ACTION_MOD] = true as const;

  Consume(_other: Status): boolean {
    return true;
  }

  modify(input: any): any {
    if (typeof input === 'object' && input !== null && 'type' in input) {
      // BaseAction path
      if (input.type === ActionType.ATTACK) {
        return new StruggleBaseAction(input.actor);
      }
      return input;
    }
    // STEP_MOD path — check if still on open Violet
    this.Step();
    return input;
  }

  Step(): void {
    const grass = this.actor?.grass;
    const shouldKeep = grass && grass.constructor.name === 'Violets' && (grass as any).isOpen;
    if (!shouldKeep) {
      this.Remove();
    }
  }
}
