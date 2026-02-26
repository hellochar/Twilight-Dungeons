import { ActorTask, TaskStage } from '../ActorTask';
import { WaitBaseAction, StruggleBaseAction, type BaseAction } from '../BaseAction';
import { Status } from '../StatusList';
import { GameModelRef } from '../GameModelRef';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  BASE_ACTION_MOD,
  type IAttackDamageTakenModifier,
  type IBaseActionModifier,
} from '../../core/Modifiers';
import { TAKE_ANY_DAMAGE_HANDLER, type ITakeAnyDamageHandler } from '../Body';
import type { Actor } from '../Actor';

export class SleepTask extends ActorTask {
  private done = false;
  private maxTurns: number | null;
  readonly isDeepSleep: boolean;
  wakeUpNextTurn = false;

  // Modifier/handler symbols
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true;
  readonly [TAKE_ANY_DAMAGE_HANDLER] = true;

  get whenToCheckIsDone(): TaskStage {
    return TaskStage.After;
  }

  get isPlayerOverridable(): boolean {
    return this.isDeepSleep;
  }

  constructor(actor: Actor, maxTurns: number | null = null, isDeepSleep = false) {
    super(actor);
    this.maxTurns = maxTurns;
    this.isDeepSleep = isDeepSleep;
  }

  /** IAttackDamageTakenModifier — doubles attack damage while sleeping */
  modify(input: number): number {
    return input * 2;
  }

  /** ITakeAnyDamageHandler — wake up when hurt */
  handleTakeAnyDamage(damage: number): void {
    if (damage > 0) {
      this.actor.statuses.add(new SurprisedStatus());
      this.actor.goToNextTask();
    }
  }

  protected shouldWakeUp(): boolean {
    if (this.wakeUpNextTurn) return true;
    if (this.maxTurns !== null && this.maxTurns <= 0) return true;
    if (this.isDeepSleep) return false;
    return this.actor.isVisible && this.actor.canTargetPlayer();
  }

  protected getNextActionImpl(): BaseAction {
    if (this.maxTurns !== null) this.maxTurns--;
    if (this.shouldWakeUp()) {
      this.done = true;
      this.actor.statuses.add(new SurprisedStatus());
    }
    return new WaitBaseAction(this.actor);
  }

  isDone(): boolean {
    return this.done;
  }
}

export class SurprisedStatus extends Status {
  readonly [BASE_ACTION_MOD] = true;
  private removeNext = false;

  get isDebuff(): boolean {
    return true;
  }

  modify(input: BaseAction): BaseAction {
    if (this.removeNext) {
      this.remove();
      return input;
    }
    const player = GameModelRef.mainOrNull?.player;
    if (this.actor === player) {
      this.remove();
      return new StruggleBaseAction(input.actor);
    }
    this.removeNext = true;
    return new WaitBaseAction(input.actor);
  }

  consume(_other: Status): boolean {
    return true;
  }

  private actor: any;
  private removeFn: (() => void) | null = null;

  start(actor: any): void {
    this.actor = actor;
  }

  remove(): void {
    if (this.actor) {
      this.actor.statuses?.remove(this);
    }
  }
}
