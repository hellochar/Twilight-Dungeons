import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { Rubble } from './Destructible';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { ActionType } from '../../core/types';
import { ActionCosts } from '../BaseAction';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  type IAttackDamageTakenModifier,
} from '../../core/Modifiers';
import { BODY_MOVE_HANDLER, type IBodyMoveHandler } from '../Body';
import { entityRegistry } from '../../generator/entityRegistry';

/**
 * Slow, tanky. Blocks 1 attack damage. Leaves Rubble behind when moving.
 * Port of C# Golem.cs.
 */
export class Golem extends AIActor implements IBodyMoveHandler, IAttackDamageTakenModifier {
  readonly [BODY_MOVE_HANDLER] = true as const;
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 6;
  }

  protected get actionCosts(): ActionCosts {
    const costs = ActionCosts.default();
    costs.set(ActionType.ATTACK, 2);
    costs.set(ActionType.MOVE, 2);
    return costs;
  }

  baseAttackDamage(): [number, number] {
    return [3, 4];
  }

  handleMove(_newPos: Vector2Int, oldPos: Vector2Int): void {
    this.floor!.put(new Rubble(oldPos));
  }

  /** Blocks 1 attack damage. */
  modify(input: number): number {
    return input - 1;
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;
    if (this.canTargetPlayer()) {
      if (this.isNextTo(player)) {
        return new AttackTask(this, player);
      }
      return new ChaseTargetTask(this, player);
    }
    return new MoveRandomlyTask(this);
  }
}

entityRegistry.register('Golem', Golem);
