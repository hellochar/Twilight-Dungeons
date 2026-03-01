import { Grass } from './Grass';
import { Faction, ON_TOP_ACTION_HANDLER, type IOnTopActionHandler } from '../../core/types';
import { Ground } from '../Tile';
import { VulnerableStatus } from '../statuses/VulnerableStatus';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';

/**
 * You may pop the Redcap, applying Vulnerable (7 turns) to adjacent enemies.
 * Port of C# Redcap.cs.
 */
export class Redcap extends Grass implements IOnTopActionHandler {
  readonly [ON_TOP_ACTION_HANDLER] = true as const;
  readonly onTopActionName = 'Pop';

  constructor(pos: Vector2Int) {
    super(pos);
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground;
  }

  handleOnTopAction(): void {
    this.pop(GameModelRef.main.player);
  }

  pop(who: any): void {
    this.onNoteworthyAction();
    for (const actor of this.floor!.adjacentActors(this.pos)) {
      if ((actor as any).faction === Faction.Enemy) {
        (actor as any).statuses.add(new VulnerableStatus(7));
      }
    }
    this.kill(who);
  }
}

entityRegistry.register('Redcap', Redcap);
