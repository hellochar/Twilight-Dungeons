import { DoOnceTask } from '../ActorTask';
import { MoveBaseAction, WaitBaseAction, type BaseAction } from '../BaseAction';
import { MyRandom } from '../../core/MyRandom';
import type { Actor } from '../Actor';
import type { Tile } from '../Tile';

export class MoveRandomlyTask extends DoOnceTask {
  private predicate: ((t: Tile) => boolean) | null;

  constructor(actor: Actor, predicate: ((t: Tile) => boolean) | null = null) {
    super(actor);
    this.predicate = predicate;
  }

  protected getNextActionImpl(): BaseAction {
    return MoveRandomlyTask.getRandomMove(this.actor, this.predicate);
  }

  static getRandomMove(actor: Actor, predicate: ((t: Tile) => boolean) | null = null): BaseAction {
    let tiles = actor.floor?.getAdjacentTiles(actor.pos).filter(t => t.canBeOccupiedBy(actor)) ?? [];
    if (predicate) {
      tiles = tiles.filter(predicate);
    }
    if (tiles.length > 0) {
      return new MoveBaseAction(actor, MyRandom.Pick(tiles).pos);
    }
    return new WaitBaseAction(actor);
  }
}
