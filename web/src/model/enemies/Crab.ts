import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { MoveToTargetTask } from '../tasks/MoveToTargetTask';
import { WaitTask } from '../tasks/WaitTask';
import { Vector2Int } from '../../core/Vector2Int';
import { Faction } from '../../core/types';
import { MyRandom } from '../../core/MyRandom';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Body } from '../Body';

/**
 * Neutral creature. Only moves horizontally. Attacks anything in its path.
 * Port of C# Crab.cs.
 */
export class Crab extends AIActor {
  get turnPriority(): number {
    return 40;
  }

  dx: number;
  onDirectionChanged: (() => void) | null = null;

  constructor(pos: Vector2Int) {
    super(pos);
    this.dx = MyRandom.value < 0.5 ? -1 : 1;
    this.faction = Faction.Neutral;
    this.hp = this._baseMaxHp = 5;
  }

  baseAttackDamage(): [number, number] {
    return [2, 2];
  }

  protected getNextTask(): ActorTask {
    const nextPos = Vector2Int.add(this.pos, new Vector2Int(this.dx, 0));
    const nextTile = this.floor?.tiles.get(nextPos);

    if (!nextTile || nextTile.basePathfindingWeight() === 0 || (nextTile.body instanceof Crab)) {
      // Can't walk there; change directions
      this.dx *= -1;
      this.onDirectionChanged?.();
      return new WaitTask(this, 1);
    } else {
      if (nextTile.body == null) {
        return new MoveToTargetTask(this, nextPos);
      } else {
        // Something's blocking the way; attack it
        return new AttackTask(this, nextTile.body as Body);
      }
    }
  }
}

entityRegistry.register('Crab', Crab);
