import { ATTACK_DAMAGE_TAKEN_MOD, BASE_ACTION_MOD, type IAttackDamageTakenModifier, type IBaseActionModifier } from '../../core/Modifiers';
import { Status } from '../Status';
import { BaseAction, StruggleBaseAction } from '../BaseAction';
import { ActionType, type IDeathHandler } from '../../core/types';
import { GameModelRef } from '../GameModelRef';
import type { Entity } from '../Entity';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/** Duck-type check: is the actor's grass a Web? */
function getWeb(actor: any): any {
  const grass = actor?.grass;
  return grass && '_isWeb' in grass ? grass : null;
}

/**
 * Check if actor is "nice" to webs (Spider or wearing SpiderSandals).
 * Exported so Web grass can use it too.
 */
export function isActorWebNice(actor: any): boolean {
  if (!actor) return false;
  // Spider check via constructor name (avoids importing Spider)
  if (actor.constructor?.name === 'Spider') return true;
  // SpiderSandals check via equipment footwear slot
  if (actor.equipment) {
    const footwear = actor.equipment.get?.(4); // EquipmentSlot.Footwear = 4
    if (footwear?.constructor?.name === 'ItemSpiderSandals') return true;
  }
  return false;
}

/**
 * Applied by Web grass. Blocks next movement (turns it into a Struggle),
 * and increases damage taken by 1. Removes when actor leaves the Web.
 * Port of C# WebbedStatus from Spider.cs.
 */
export class WebbedStatus extends Status implements IAttackDamageTakenModifier, IBaseActionModifier, IDeathHandler {
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;
  readonly [BASE_ACTION_MOD] = true as const;
  readonly [DEATH_HANDLER] = true as const;

  get isDebuff(): boolean {
    return !isActorWebNice(this.actor);
  }

  blocksMovement(): boolean {
    return !isActorWebNice(this.actor);
  }

  private get web(): any {
    return getWeb(this.actor);
  }

  End(): void {
    this.web?.kill(this.actor);
  }

  Step(): void {
    if (!this.web) {
      this.Remove();
    }
  }

  Consume(_other: Status): boolean {
    return true;
  }

  /** Handles STEP_MOD (inherited), ATTACK_DAMAGE_TAKEN_MOD, and BASE_ACTION_MOD. */
  modify(input: any): any {
    if (typeof input === 'number') {
      // ATTACK_DAMAGE_TAKEN_MOD: +1 damage taken
      const web = this.web;
      if (!web) return input;
      if (isActorWebNice(web.actor)) return input;
      this.Remove();
      return input + 1;
    }
    if (input instanceof BaseAction) {
      // BASE_ACTION_MOD
      const web = this.web;
      if (!web) {
        this.Remove();
        return input;
      }
      if (isActorWebNice(web.actor)) return input;
      if (input.type === ActionType.MOVE) {
        this.Remove();
        return new StruggleBaseAction(input.actor);
      }
      return input;
    }
    // STEP_MOD
    return super.modify(input);
  }

  handleDeath(_source: Entity): void {
    if (!isActorWebNice(this.actor)) {
      const web = this.web;
      if (web) {
        GameModelRef.main.enqueuEvent(() => {
          web.kill(this.actor);
        });
      }
    }
  }
}
