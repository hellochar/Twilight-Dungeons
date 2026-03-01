import { Grass } from './Grass';
import { ON_TOP_ACTION_HANDLER, type IOnTopActionHandler } from '../../core/types';
import { Ground, type IBlocksVision } from '../Tile';
import { GameModelRef } from '../GameModelRef';
import { GenericPlayerTask } from '../tasks/GenericTask';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';

/**
 * Blocks vision, but creatures can still walk over it.
 * You can cut it down when standing over it.
 * Port of C# Fern.cs.
 */
export class Fern extends Grass implements IBlocksVision, IOnTopActionHandler {
  readonly blocksVision = true as const;
  readonly [ON_TOP_ACTION_HANDLER] = true as const;
  readonly onTopActionName = 'Cut';

  constructor(pos: Vector2Int) {
    super(pos);
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground && (tile.body == null || 'faction' in tile.body);
  }

  handleOnTopAction(): void {
    const player = GameModelRef.main.player;
    player.setTasks(new GenericPlayerTask(player, () => this.cutSelfAndAdjacent(player)));
  }

  /** Cut this fern and all adjacent ferns. */
  cutSelfAndAdjacent(player: any): void {
    const floor = this.floor;
    if (!floor) return;
    const adjacentFerns = floor
      .getAdjacentTiles(this.pos)
      .map(t => t.grass)
      .filter((g): g is Fern => g instanceof Fern);
    for (const fern of adjacentFerns) {
      fern.kill(player);
    }
  }
}

entityRegistry.register('Fern', Fern);
