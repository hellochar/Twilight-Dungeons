import { Grass } from './Grass';
import { GuardedStatus } from '../statuses/GuardedStatus';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { Ground } from '../Tile';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';

/**
 * Blocks up to 5 attack damage for the creature standing on it.
 * Port of C# Guardleaf.cs.
 */
export class Guardleaf extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  readonly renderLayer = 'above-entity' as const;
  guardLeft = 5;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground;
  }

  /** Actor standing on this grass's tile. */
  private get actor(): any {
    return this.floor?.bodies.get(this.pos) ?? null;
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

  removeGuard(reduction: number): void {
    this.guardLeft -= reduction;
    if (this.guardLeft <= 0) {
      GameModelRef.main.enqueuEvent(() => this.killSelf());
    }
  }
}

entityRegistry.register('Guardleaf', Guardleaf);
