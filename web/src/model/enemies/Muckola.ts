import { SimpleStatusApplicationEnemy } from './SimpleStatusApplicationEnemy';
import { Muck } from '../grasses/Muck';
import { Ground } from '../Tile';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { MyRandom } from '../../core/MyRandom';
import { entityRegistry } from '../../generator/entityRegistry';

/**
 * Every other turn, places a Muck next to the Player if visible.
 * Port of C# Muckola.
 */
export class Muckola extends SimpleStatusApplicationEnemy {
  get cooldown(): number { return 0; }

  constructor(pos: Vector2Int) {
    super(pos);
  }

  doTask(): void {
    const player = GameModelRef.main.player;
    const groundTiles = this.floor!.getAdjacentTiles(player.pos)
      .filter(t => t instanceof Ground && t.grass == null);
    if (groundTiles.length > 0) {
      const tile = MyRandom.Pick(groundTiles);
      this.floor!.put(new Muck(tile.pos));
    }
  }
}

entityRegistry.register('Muckola', Muckola);
