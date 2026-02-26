import { ActorTask, TaskStage } from '../ActorTask';
import { MoveBaseAction, WaitBaseAction, type BaseAction } from '../BaseAction';
import { Vector2Int } from '../../core/Vector2Int';
import type { Actor } from '../Actor';

export class RunAwayTask extends ActorTask {
  private fearPoint: Vector2Int;
  readonly turns: number;
  turnsRemaining: number;
  hasSurpriseTurn: boolean;

  get whenToCheckIsDone(): TaskStage {
    return TaskStage.After;
  }

  constructor(actor: Actor, fearPoint: Vector2Int, turns: number, hasSurpriseTurn = true) {
    super(actor);
    this.fearPoint = fearPoint;
    this.turns = turns;
    this.turnsRemaining = turns;
    this.hasSurpriseTurn = hasSurpriseTurn;
  }

  protected getNextActionImpl(): BaseAction {
    if (this.hasSurpriseTurn) {
      this.hasSurpriseTurn = false;
      return new WaitBaseAction(this.actor);
    }
    this.turnsRemaining--;
    const tiles = this.actor.floor?.getAdjacentTiles(this.actor.pos).filter(t => t.canBeOccupiedBy(this.actor)) ?? [];
    if (tiles.length > 0) {
      const furthest = tiles.reduce((best, t) =>
        Vector2Int.distance(this.fearPoint, t.pos) > Vector2Int.distance(this.fearPoint, best.pos) ? t : best,
      );
      return new MoveBaseAction(this.actor, furthest.pos);
    }
    return new WaitBaseAction(this.actor);
  }

  isDone(): boolean {
    return this.turnsRemaining <= 0;
  }
}
