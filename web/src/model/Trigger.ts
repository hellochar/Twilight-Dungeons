import { Entity } from './Entity';
import { Vector2Int } from '../core/Vector2Int';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../core/types';
import type { Actor } from './Actor';

/**
 * Immobile trigger entity placed on tiles. Fires callback when an actor enters.
 * No _isBody/_isGrass/_isItem markers → routed to Floor.triggers grid.
 * Port of C# Trigger from HangingVines.cs.
 */
export class Trigger extends Entity implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  private _pos: Vector2Int;
  readonly action: (who: Actor) => void;

  get pos(): Vector2Int {
    return this._pos;
  }

  set pos(_value: Vector2Int) {
    // Triggers cannot move
  }

  constructor(pos: Vector2Int, action: (who: Actor) => void) {
    super();
    this._pos = pos;
    this.action = action;
  }

  handleActorEnter(who: Actor): void {
    this.action(who);
  }
}
