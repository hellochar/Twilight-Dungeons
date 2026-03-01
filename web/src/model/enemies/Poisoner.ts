import { SimpleStatusApplicationEnemy } from './SimpleStatusApplicationEnemy';
import { PoisonedStatus } from '../statuses/PoisonedStatus';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';

/**
 * Every other turn, applies Poisoned to the Player if visible.
 * Port of C# Poisoner.
 */
export class Poisoner extends SimpleStatusApplicationEnemy {
  get cooldown(): number { return 1; }

  constructor(pos: Vector2Int) {
    super(pos);
  }

  doTask(): void {
    GameModelRef.main.player.statuses.add(new PoisonedStatus(1));
  }
}

entityRegistry.register('Poisoner', Poisoner);
