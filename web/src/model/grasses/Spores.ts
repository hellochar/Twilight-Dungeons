import { Grass } from './Grass';
import { AIActor } from '../enemies/AIActor';
import { Status } from '../Status';
import { ActorTask } from '../ActorTask';
import { WaitTask } from '../tasks/WaitTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { TelegraphedTask } from '../tasks/TelegraphedTask';
import { GenericBaseAction, type BaseAction } from '../BaseAction';
import { ATTACK_DAMAGE_MOD, type IAttackDamageModifier } from '../../core/Modifiers';
import { ACTION_PERFORMED_HANDLER, ACTOR_KILLED_HANDLER, type IActionPerformedHandler } from '../Actor';
import { ACTOR_ENTER_HANDLER, ActionType, Faction, type IActorEnterHandler } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { MyRandom } from '../../core/MyRandom';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Tile } from '../Tile';
import type { Actor } from '../Actor';

/**
 * Releases three SporeBloats when any non-SporeBloat creature steps on it.
 * Port of C# Spores.cs.
 */
export class Spores extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  handleActorEnter(actor: any): void {
    if (!(actor instanceof SporeBloat)) {
      this.activate();
      this.kill(actor);
    }
  }

  activate(): void {
    const floor = this.floor!;
    const freeSpots = floor.getAdjacentTiles(this.pos).filter(t => t.canBeOccupied());
    MyRandom.Shuffle(freeSpots);
    for (const tile of freeSpots.slice(0, 3)) {
      floor.put(new SporeBloat(tile.pos));
    }
  }
}

/**
 * Pops after three turns, applying SporedStatus to adjacent non-SporeBloat actors.
 * Port of C# SporeBloat from Spores.cs.
 */
export class SporeBloat extends AIActor {
  get turnPriority(): number {
    return 25;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 1;
    this.faction = Faction.Neutral;
    this.setTasks(
      new WaitTask(this, 1),
      new MoveRandomlyTask(this).onlyCheckBefore(),
      new WaitTask(this, 1).onlyCheckBefore(),
      this.explodeTask(),
    );
  }

  private explodeTask(): ActorTask {
    return new TelegraphedTask(this, 1, new GenericBaseAction(this, () => this.explode()));
  }

  protected getNextTask(): ActorTask {
    return this.explodeTask();
  }

  private explode(): void {
    const floor = this.floor;
    this.killSelf();

    if (floor) {
      for (const actor of floor.adjacentActors(this.pos)) {
        if (!(actor instanceof SporeBloat)) {
          (actor as Actor).statuses.add(new SporedStatus());
        }
      }
    }
  }
}

/**
 * Deal 0 attack damage. Moving removes. On death, spawn Spores at position.
 * Port of C# SporedStatus from Spores.cs.
 */
export class SporedStatus extends Status implements IAttackDamageModifier, IActionPerformedHandler {
  readonly [ATTACK_DAMAGE_MOD] = true as const;
  readonly [ACTION_PERFORMED_HANDLER] = true as const;

  private static readonly ACTOR_KILLED = Symbol.for('IActorKilledHandler');
  readonly [SporedStatus.ACTOR_KILLED] = true as const;

  get isDebuff(): boolean {
    return true;
  }

  Consume(_other: Status): boolean {
    return true;
  }

  /** On death, spawn Spores at position. */
  onKilled(a: Actor): void {
    if (a.floor) {
      a.floor.put(new Spores(a.pos));
    }
  }

  modify(input: any): any {
    // ATTACK_DAMAGE_MOD: force 0 damage
    if (typeof input === 'number') {
      return 0;
    }
    // STEP_MOD: inherited Step()
    return super.modify(input);
  }

  handleActionPerformed(finalAction: BaseAction, _initialAction: BaseAction): void {
    if (finalAction.type === ActionType.MOVE) {
      this.Remove();
    }
  }
}

entityRegistry.register('Spores', Spores);
entityRegistry.register('SporeBloat', SporeBloat);
