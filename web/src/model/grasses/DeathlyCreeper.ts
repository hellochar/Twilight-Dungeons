import { Grass } from './Grass';
import { Ground } from '../Tile';
import { GameModelRef } from '../GameModelRef';
import { MyRandom } from '../../core/MyRandom';
import { entityRegistry } from '../../generator/entityRegistry';
import type { ISteppable } from '../Floor';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';

let timeLastChecked = -1;

/**
 * Spreads to adjacent Ground tiles every 4 turns.
 * If all Ground tiles are covered, kills all bodies.
 * Port of C# DeathlyCreeper.cs.
 */
export class DeathlyCreeper extends Grass implements ISteppable {
  timeNextAction: number;
  get turnPriority(): number { return 50; }

  get displayName(): string { return 'Black Creeper'; }

  static canOccupy(tile: Tile): boolean {
    return tile.canBeOccupied() && tile instanceof Ground && !(tile.grass instanceof DeathlyCreeper);
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.timeNextAction = this.timeCreated + 1;
  }

  step(): number {
    if (timeLastChecked !== GameModelRef.main.time) {
      this.checkWin();
    }

    if (this.age % 4 === 3) {
      const neighbors = this.floor!.getCardinalNeighbors(this.pos)
        .filter(t => DeathlyCreeper.canOccupy(t));
      const target = MyRandom.Pick(neighbors);
      if (target) {
        this.onNoteworthyAction();
        this.floor!.put(new DeathlyCreeper(target.pos));
      }
    }
    return 1;
  }

  private checkWin(): void {
    timeLastChecked = GameModelRef.main.time;
    // Check if all Ground tiles are covered by DeathlyCreeper
    let allCovered = true;
    for (const tile of this.floor!.enumerateFloor()) {
      const t = this.floor!.tiles.get(tile);
      if (t instanceof Ground && !(t.grass instanceof DeathlyCreeper)) {
        allCovered = false;
        break;
      }
    }
    if (allCovered) {
      GameModelRef.main.enqueuEvent(() => {
        const bodies = [...this.floor!.bodies];
        for (const body of bodies) {
          body.kill(this);
        }
      });
    }
  }
}

entityRegistry.register('DeathlyCreeper', DeathlyCreeper);
