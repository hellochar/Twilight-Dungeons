import { Grass } from './Grass';
import { GuardedStatus } from '../statuses/GuardedStatus';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { Ground } from '../Tile';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';
import { Actor } from '../Actor';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';

/**
 * Blocks the next attack for the creature standing on it.
 * Port of C# Guardleaf.cs.
 */
export class Guardleaf extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  readonly renderLayer = 'above-entity' as const;
  // guardLeft = 5;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground;
  }

  /** Actor standing on this grass's tile (null if body is not an Actor). */
  private get actor(): Actor | null {
    const body = this.floor?.bodies.get(this.pos);
    return body instanceof Actor ? body : null;
  }

  protected handleEnterFloor(): void {
    const standing = this.actor;
    if (standing?.statuses) {
      standing.statuses.add(new GuardedStatus());
    }
  }

  protected handleLeaveFloor(): void {
    const standing = this.actor;
    if (standing?.statuses) {
      standing.statuses.removeOfType(GuardedStatus);
    }
  }

  handleActorEnter(who: any): void {
    who.statuses.add(new GuardedStatus());
    this.onNoteworthyAction();
  }

  removeGuard(): void {
    GameModelRef.main.enqueuEvent(() => this.killSelf());
  }
}

entityRegistry.register('Guardleaf', Guardleaf);
