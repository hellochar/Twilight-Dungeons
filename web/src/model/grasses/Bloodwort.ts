import { Grass } from './Grass';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { Ground } from '../Tile';
import { GameModelRef } from '../GameModelRef';
import { StrengthStatus } from '../statuses/StrengthStatus';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';

/**
 * When the player walks over the Bloodwort, destroy it and gain 2 strength.
 * Port of C# Bloodwort.cs.
 */
export class Bloodwort extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground;
  }

  handleActorEnter(who: any): void {
    const player = GameModelRef.main.player;
    if (who === player) {
      who.statuses.add(new StrengthStatus(2));
      this.kill(who);
    }
  }
}

entityRegistry.register('Bloodwort', Bloodwort);
