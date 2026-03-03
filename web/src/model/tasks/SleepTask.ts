import { ActorTask, TaskStage } from '../ActorTask';
import { BaseAction, WaitBaseAction, StruggleBaseAction } from '../BaseAction';
import { Status } from '../Status';
import { GameModelRef } from '../GameModelRef';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  BASE_ACTION_MOD,
} from '../../core/Modifiers';
import { TAKE_ANY_DAMAGE_HANDLER } from '../Body';
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

  /** Handles both STEP_MOD (inherited) and BASE_ACTION_MOD calls. */
  modify(input: any): any {
    if (!(input instanceof BaseAction)) {
      // STEP_MOD call — delegate to super (calls Step())
      return super.modify(input);
    }
    // BASE_ACTION_MOD call
    if (this.removeNext) {
      this.Remove();
      return input;
    }
    const player = GameModelRef.mainOrNull?.player;
    if (this.actor === player) {
      this.Remove();
      return new StruggleBaseAction(input.actor);
    }
    this.removeNext = true;
    return new WaitBaseAction(input.actor);
  }

  Consume(_other: Status): boolean {
    return true;
  }
}
