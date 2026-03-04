import { ActionType } from '../core/types';
import { Vector2Int } from '../core/Vector2Int';
import type { Actor } from './Actor';
import type { Body } from './Body';
import { GameModelRef } from './GameModelRef';

export class CannotPerformActionException extends Error {
  constructor(message = 'Cannot perform action') {
    super(message);
    this.name = 'CannotPerformActionException';
  }
}

export abstract class BaseAction {
  readonly actor: Actor;
  abstract get type(): ActionType;

  constructor(actor: Actor) {
    this.actor = actor;
  }

  abstract perform(): void;
}

export class MoveBaseAction extends BaseAction {
  get type(): ActionType {
    return ActionType.MOVE;
  }
  readonly targetPos: Vector2Int;

  constructor(actor: Actor, pos: Vector2Int) {
    super(actor);
    this.targetPos = pos;
  }

  perform(): void {
    if (!this.actor.isNextTo(this.targetPos)) return;
    this.actor.pos = this.targetPos;
  }
}

export class AttackBaseAction extends BaseAction {
  get type(): ActionType {
    return ActionType.ATTACK;
  }
  readonly target: Body;
  readonly allowReach: boolean;

  constructor(actor: Actor, target: Body, allowReach = false) {
    super(actor);
    console.assert(target != null, 'attacking a null target');
    this.target = target;
    this.allowReach = allowReach;
  }

  perform(): void {
    if (this.actor.isNextTo(this.target) || this.allowReach) {
      this.actor.attack(this.target);
    } else {
      throw new CannotPerformActionException('Cannot reach target!');
    }
  }
}

export class AttackGroundBaseAction extends BaseAction {
  get type(): ActionType {
    return ActionType.ATTACK;
  }
  readonly targetPosition: Vector2Int;

  constructor(actor: Actor, targetPosition: Vector2Int) {
    super(actor);
    this.targetPosition = targetPosition;
  }

  perform(): void {
    this.actor.attackGround(this.targetPosition);
    GameModelRef.mainOrNull?.emitAnimation({ type: 'attackGroundHit', entityGuid: this.actor.guid, to: this.targetPosition });
  }
}

export class WaitBaseAction extends BaseAction {
  private _type: ActionType;
  get type(): ActionType {
    return this._type;
  }

  constructor(actor: Actor, type: ActionType = ActionType.WAIT) {
    super(actor);
    this._type = type;
  }

  perform(): void {
    if (this.actor === GameModelRef.mainOrNull?.player) {
      GameModelRef.mainOrNull?.emitAnimation({ type: 'wait', entityGuid: this.actor.guid, from: this.actor.pos });
    }
  }
}

export class GenericBaseAction extends BaseAction {
  private _type: ActionType;
  get type(): ActionType {
    return this._type;
  }
  readonly action: () => void;

  constructor(actor: Actor, action: () => void, type: ActionType = ActionType.GENERIC) {
    super(actor);
    this._type = type;
    this.action = action;
  }

  perform(): void {
    this.action();
    GameModelRef.mainOrNull?.emitAnimation({ type: 'pulse', entityGuid: this.actor.guid });
  }
}

export class JumpBaseAction extends BaseAction {
  get type(): ActionType {
    return ActionType.MOVE;
  }
  readonly targetPos: Vector2Int;

  constructor(actor: Actor, pos: Vector2Int) {
    super(actor);
    this.targetPos = pos;
  }

  perform(): void {
    this.actor.isJumping = true;
    this.actor.pos = this.targetPos;
    this.actor.isJumping = false;
  }
}

export class StruggleBaseAction extends BaseAction {
  get type(): ActionType {
    return ActionType.MOVE;
  }

  constructor(actor: Actor) {
    super(actor);
  }

  perform(): void {
    GameModelRef.mainOrNull?.emitAnimation({ type: 'struggle', entityGuid: this.actor.guid, from: this.actor.pos });
  }
}

/** Map of ActionType → cost (in turns). Mutable for modifier processing. */
export class ActionCosts extends Map<ActionType, number> {
  constructor(init?: Iterable<[ActionType, number]>) {
    super(init);
  }

  copy(): ActionCosts {
    return new ActionCosts(this);
  }

  static default(): ActionCosts {
    return new ActionCosts([
      [ActionType.ATTACK, 1],
      [ActionType.GENERIC, 1],
      [ActionType.MOVE, 1],
      [ActionType.WAIT, 1],
    ]);
  }
}
