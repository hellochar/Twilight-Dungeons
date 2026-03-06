import { Grass } from './Grass';
import { Ground } from '../Tile';
import { PoisonedStatus } from '../statuses/PoisonedStatus';
import { entityRegistry } from '../../generator/entityRegistry';
import { Actor } from '../Actor';
import type { ISteppable } from '../Floor';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';

/**
 * Applies Poison to the creature standing over it every turn.
 * Gradually turns adjacent Grass into Poisonmoss.
 * Dies if surrounded by Walls and/or other Poisonmoss.
 * Port of C# Poisonmoss.cs.
 */
export class Poisonmoss extends Grass implements ISteppable {
  timeNextAction: number;
  get turnPriority(): number { return 50; }

  constructor(pos: Vector2Int) {
    super(pos);
    this.timeNextAction = this.timeCreated + 1;
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground;
  }

  /** Actor standing on this grass's tile (null if body is not an Actor). */
  private get actor(): Actor | null {
    const body = this.floor?.bodies.get(this.pos);
    return body instanceof Actor ? body : null;
  }

  step(): number {
    const floor = this.floor;
    if (!floor) return 1;

    if (this.actor != null) {
      this.onNoteworthyAction();
      this.actor.statuses.add(new PoisonedStatus(1));
    }

    // Die if entirely surrounded by walls/poisonmoss (all 9 tiles)
    const adjacent = floor.getAdjacentTiles(this.pos);
    const blocked = adjacent.filter(
      t => (t.grass instanceof Poisonmoss) || t.basePathfindingWeight() === 0
    );
    if (blocked.length === adjacent.length) {
      this.killSelf();
      return 1;
    }

    // Every 6 turns, try to spread into adjacent non-poisonmoss grass
    if (this.age % 6 === 5) {
      const candidates = adjacent.filter(
        t => Poisonmoss.canOccupy(t) && t.grass != null && !(t.grass instanceof Poisonmoss)
      );
      if (candidates.length > 0) {
        const target = candidates[Math.floor(Math.random() * candidates.length)];
        this.onNoteworthyAction();
        (target.grass as any)?.kill(this);
        floor.put(new Poisonmoss(target.pos));
      }
    }

    return 1;
  }
}

entityRegistry.register('Poisonmoss', Poisonmoss);
