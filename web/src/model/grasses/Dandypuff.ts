import { Grass } from './Grass';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { Ground } from '../Tile';
import { GameModelRef } from '../GameModelRef';
import { DandyStatus } from '../statuses/DandyStatus';
import { AIActor } from '../enemies/AIActor';
import { BODY_MOVE_HANDLER, type IBodyMoveHandler } from '../Body';
import { Faction } from '../../core/types';
import { MoveToTargetTask } from '../tasks/MoveToTargetTask';
import { WaitTask } from '../tasks/WaitTask';
import { MyRandom } from '../../core/MyRandom';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';
import type { ActorTask } from '../ActorTask';

/**
 * Walk over to gain DandyStatus. Destroyed on enter.
 * Port of C# Dandypuff from Dandypuff.cs.
 */
export class Dandypuff extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground;
  }

  constructor(pos: Vector2Int) {
    super(pos);
  }

  handleActorEnter(actor: any): void {
    const player = GameModelRef.main.player;
    if (actor === player) {
      actor.statuses.add(new DandyStatus(1));
      this.kill(actor);
    }
  }
}

/**
 * Neutral enemy that wanders and leaves Dandypuffs behind on move.
 * Port of C# Dandyslug from Dandypuff.cs.
 */
export class Dandyslug extends AIActor implements IBodyMoveHandler {
  readonly [BODY_MOVE_HANDLER] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
    this.hp = this.baseMaxHp = 3;
    this.faction = Faction.Neutral;
    this.clearTasks();
  }

  baseAttackDamage(): [number, number] {
    return [2, 2];
  }

  protected getNextTask(): ActorTask {
    const tiles = [...this.floor.enumerateCircle(this.pos, 5)]
      .map(p => this.floor.tiles.get(p))
      .filter(t => t != null && t.canBeOccupied());
    const target = MyRandom.Pick(tiles);
    if (target) {
      return new MoveToTargetTask(this, target.pos);
    }
    return new WaitTask(this, 1);
  }

  handleMove(newPos: Vector2Int, oldPos: Vector2Int): void {
    if (this.floor) {
      this.floor.put(new Dandypuff(oldPos));
    }
  }
}

entityRegistry.register('Dandypuff', Dandypuff);
entityRegistry.register('Dandyslug', Dandyslug);
