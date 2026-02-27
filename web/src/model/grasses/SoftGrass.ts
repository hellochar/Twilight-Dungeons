import { Grass } from './Grass';
import { SoftGrassStatus } from '../statuses/SoftGrassStatus';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';

/**
 * Moving twice on SoftGrass grants the player a free move.
 * Port of C# SoftGrass.cs.
 */
export class SoftGrass extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  handleActorEnter(who: any): void {
    const player = GameModelRef.mainOrNull?.player;
    if (who === player) {
      player!.statuses.add(new SoftGrassStatus(1));
      this.onNoteworthyAction();
    }
  }
}

entityRegistry.register('SoftGrass', SoftGrass);
