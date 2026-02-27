import { Grass } from './Grass';
import { WebbedStatus, isActorWebNice } from '../statuses/WebbedStatus';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { Ground } from '../Tile';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';
import type { Actor } from '../Actor';

/**
 * Web grass placed by Spiders. Applies WebbedStatus on enter.
 * Port of C# Web class from Spider.cs.
 */
export class Web extends Grass implements IActorEnterHandler {
  readonly _isWeb = true;
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground && !(tile.grass && '_isWeb' in tile.grass);
  }

  /** Check if actor is "nice" to webs (Spider or wearing SpiderSandals). */
  static isActorNice(actor: any): boolean {
    return isActorWebNice(actor);
  }

  /** Actor property: whoever is standing on this grass's tile. */
  private get actor(): Actor | null {
    return this.floor?.bodies.get(this.pos) as Actor | null;
  }

  protected handleEnterFloor(): void {
    const standing = this.actor;
    if (standing) {
      standing.statuses.add(new WebbedStatus());
    }
  }

  protected handleLeaveFloor(): void {
    const standing = this.actor;
    if (standing) {
      standing.statuses.removeOfType(WebbedStatus);
    }
  }

  handleActorEnter(actor: any): void {
    (actor as Actor).statuses.add(new WebbedStatus());
    this.onNoteworthyAction();
  }
}

entityRegistry.register('Web', Web);
