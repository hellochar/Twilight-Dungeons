import { Grass } from './Grass';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { Ground } from '../Tile';
import { GameModelRef } from '../GameModelRef';
import { SurprisedStatus } from '../tasks/SleepTask';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';

/**
 * Paired teleport grass. Walking into one teleports you near the partner.
 * Enemies get SurprisedStatus on teleport.
 * Port of C# Tunnelroot.cs.
 */
export class Tunnelroot extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  private partner: Tunnelroot | null = null;
  private isOpen = true;

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground;
  }

  constructor(pos: Vector2Int) {
    super(pos);
  }

  partnerWith(other: Tunnelroot): void {
    other.partner = this;
    this.partner = other;
  }

  protected handleLeaveFloor(): void {
    if (this.partner && this.partner.floor) {
      const partner = this.partner;
      GameModelRef.main.enqueuEvent(() => {
        partner.floor?.remove(partner);
      });
    }
  }

  handleActorEnter(who: any): void {
    if (!this.partner || this.partner.isDead || !this.isOpen) return;

    // Check partner tile is unoccupied
    const partnerBody = this.floor!.bodies.get(this.partner.pos);
    if (partnerBody) return;

    // Find nearest open tile near partner via BFS (skip partner pos itself)
    let destTile: Tile | null = null;
    let first = true;
    for (const t of this.floor!.breadthFirstSearch(this.partner.pos)) {
      if (first) { first = false; continue; } // Skip(1) — skip partner pos
      if (t.canBeOccupied() && !(t.grass instanceof Tunnelroot)) {
        destTile = t;
        break;
      }
    }

    if (destTile) {
      this.onNoteworthyAction();
      this.partner.onNoteworthyAction();
      who.pos = destTile.pos;
      if (who !== GameModelRef.main.player) {
        who.statuses.add(new SurprisedStatus());
      }
    }
  }
}

entityRegistry.register('Tunnelroot', Tunnelroot);
