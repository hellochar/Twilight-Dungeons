import { AIActor } from './AIActor';
import { Actor } from '../Actor';
import { ActorTask } from '../ActorTask';
import { WaitTask } from '../tasks/WaitTask';
import { TelegraphedTask } from '../tasks/TelegraphedTask';
import { GenericBaseAction } from '../BaseAction';
import { Faction } from '../../core/types';
import type { IDeathHandler } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { MyRandom } from '../../core/MyRandom';
import { Status } from '../Status';
import { ArmoredStatus } from '../statuses/ArmoredStatus';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Entity } from '../Entity';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * Neutral 1 HP entity. On first action, telegraphs duplication to adjacent tile.
 * After duplicating, waits forever. On death, applies EaterStatus to adjacent actors.
 * Port of C# Shielder.cs.
 */
export class Shielder extends AIActor implements IDeathHandler {
  readonly [DEATH_HANDLER] = true;

  private bDuplicated = false;

  constructor(pos: Vector2Int) {
    super(pos);
    this.faction = Faction.Neutral;
    this._hp = this._baseMaxHp = 1;
    this.clearTasks();
  }

  baseAttackDamage(): [number, number] {
    return [0, 0];
  }

  protected getNextTask(): ActorTask {
    if (this.bDuplicated) {
      return new WaitTask(this, 9999);
    }
    return new TelegraphedTask(this, 1, new GenericBaseAction(this, () => this.duplicate()));
  }

  private duplicate(): void {
    this.bDuplicated = true;
    const floor = this.floor;
    if (!floor) return;

    const candidates = floor.getAdjacentTiles(this.pos)
      .filter(t => t.canBeOccupied())
      .sort((a, b) => Vector2Int.distance(a.pos, this.pos) - Vector2Int.distance(b.pos, this.pos));

    if (candidates.length > 0) {
      floor.put(new Shielder(candidates[0].pos));
    } else {
      this.statuses.add(new DeathlyStatus());
    }
  }

  handleDeath(_source: Entity | null): void {
    const floor = this.floor;
    if (!floor) return;
    for (const body of floor.adjacentBodies(this.pos)) {
      if (body instanceof Actor) {
        body.statuses.add(new EaterStatus());
      }
    }
  }
}

/**
 * Spreads to adjacent Shielders each turn and deals 1 self-damage.
 * Port of C# DeathlyStatus.
 */
export class DeathlyStatus extends Status {
  Consume(_other: Status): boolean {
    return true;
  }

  Step(): void {
    const actor = this.actor;
    if (!actor || !actor.floor) return;

    // Spread to adjacent Shielders
    for (const body of actor.floor.adjacentBodies(actor.pos)) {
      if (body instanceof Shielder) {
        (body as Actor).statuses.add(new DeathlyStatus());
      }
    }
    actor.takeDamage(1, actor);
  }
}

/**
 * 5% chance each turn to add ArmoredStatus, spawn a Shielder, and remove self.
 * Port of C# EaterStatus.
 */
export class EaterStatus extends Status {
  Consume(_other: Status): boolean {
    return false;
  }

  Step(): void {
    const actor = this.actor;
    if (!actor || !actor.floor) return;

    if (MyRandom.value < 0.05) {
      actor.statuses.add(new ArmoredStatus());
      actor.floor.put(new Shielder(actor.pos));
      this.Remove();
    }
  }
}

entityRegistry.register('Shielder', Shielder);
