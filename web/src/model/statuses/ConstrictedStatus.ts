import { BASE_ACTION_MOD, type IBaseActionModifier } from '../../core/Modifiers';
import { StackingMode, StackingStatus } from '../Status';
import { BODY_MOVE_HANDLER, type IBodyMoveHandler } from '../Body';
import { ActionType, type IDeathHandler } from '../../core/types';
import { BaseAction, StruggleBaseAction } from '../BaseAction';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Entity } from '../Entity';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * Blocks movement and attacks — actor must struggle free.
 * Linked to a HangingVines owner; signals back on end/death.
 * Port of C# ConstrictedStatus from HangingVines.cs.
 */
export class ConstrictedStatus extends StackingStatus implements IBaseActionModifier, IBodyMoveHandler, IDeathHandler {
  readonly [BASE_ACTION_MOD] = true as const;
  readonly [BODY_MOVE_HANDLER] = true as const;
  readonly [DEATH_HANDLER] = true as const;

  private owner: { constrictedStatusEnded(): void; constrictedCreatureDied(): void } | null;

  get isDebuff(): boolean {
    return true;
  }

  get stackingMode(): StackingMode {
    return StackingMode.Max;
  }

  constructor(owner: { constrictedStatusEnded(): void; constrictedCreatureDied(): void } | null, stacks = 3) {
    super(stacks);
    this.owner = owner;
  }

  End(): void {
    this.owner?.constrictedStatusEnded();
  }

  handleDeath(_source: Entity): void {
    this.owner?.constrictedCreatureDied();
  }

  handleMove(newPos: Vector2Int, oldPos: Vector2Int): void {
    if (newPos.x !== oldPos.x || newPos.y !== oldPos.y) {
      this.Remove();
    }
  }

  modify(input: any): any {
    if (input instanceof BaseAction) {
      if (input.type === ActionType.MOVE || input.type === ActionType.ATTACK) {
        this.stacks--;
        if (this.stacks <= 0) {
          return input;
        } else {
          return new StruggleBaseAction(input.actor);
        }
      }
      return input;
    }
    return super.modify(input);
  }
}
