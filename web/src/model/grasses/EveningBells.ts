import { Grass } from './Grass';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { Ground } from '../Tile';
import { GameModelRef } from '../GameModelRef';
import { SleepTask } from '../tasks/SleepTask';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';

/**
 * Any non-player creature walking into the Evening Bells falls into
 * deep sleep for 3 turns. This consumes the Evening Bells.
 * Port of C# EveningBells.cs.
 */
export class EveningBells extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;
  readonly angle: number;

  constructor(pos: Vector2Int, angle = 0) {
    super(pos);
    this.angle = angle;
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground && tile.canBeOccupied();
  }

  handleActorEnter(actor: any): void {
    if (actor !== GameModelRef.main.player) {
      actor.setTasks(new SleepTask(actor, 3, true));
      GameModelRef.main.enqueuEvent(() => this.kill(actor));
    }
  }
}

entityRegistry.register('EveningBells', EveningBells);
