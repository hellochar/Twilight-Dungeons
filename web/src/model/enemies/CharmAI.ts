import { AI } from './AIActor';
import type { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { MoveNextToTargetTask } from '../tasks/MoveNextToTargetTask';
import { ChaseDynamicTargetTask } from '../tasks/ChaseDynamicTargetTask';
import { CharmedStatus } from '../statuses/CharmedStatus';
import { Faction } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import type { Actor } from '../Actor';

/**
 * AI override for charmed actors. Makes them follow the player
 * and attack nearby enemies.
 * Port of C# CharmAI from ItemCharmBerry.cs.
 */
export class CharmAI extends AI {
  private actor: AIActor;

  constructor(actor: AIActor) {
    super();
    this.actor = actor;
  }

  start(): void {
    this.actor.statuses.add(new CharmedStatus());
    this.actor.faction = Faction.Ally;
  }

  getNextTask(): ActorTask {
    const target = this.targetDecider();
    if (target == null) {
      return new MoveNextToTargetTask(this.actor, GameModelRef.main.player.pos);
    }
    if (this.actor.isNextTo(target)) {
      return new AttackTask(this.actor, target);
    }
    return new ChaseDynamicTargetTask(this.actor, () => this.targetDecider()!);
  }

  private targetDecider(): Actor | null {
    const player = GameModelRef.main.player;
    const visibleEnemies = player.getVisibleActors(Faction.Enemy) as Actor[];
    if (visibleEnemies.length === 0) return null;

    // Sort by diamond distance to player
    const withDist = visibleEnemies.map(a => ({
      actor: a,
      playerDist: Vector2Int.manhattanDistance(a.pos, player.pos),
    }));
    withDist.sort((a, b) => a.playerDist - b.playerDist);

    const closestDist = withDist[0].playerDist;
    // Consider targets tied for closest distance to player
    const tied = withDist.filter(e => e.playerDist === closestDist);
    // Among those, pick the one closest to the charmed actor
    tied.sort((a, b) =>
      this.actor.distanceTo(a.actor.pos) - this.actor.distanceTo(b.actor.pos)
    );
    return tied[0].actor;
  }
}
