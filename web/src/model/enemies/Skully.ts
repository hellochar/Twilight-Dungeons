import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { ACTOR_KILLED_HANDLER } from '../Actor';
import { Grass } from '../grasses/Grass';
import { Ground } from '../Tile';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { entityRegistry } from '../../generator/entityRegistry';
import type { ISteppable } from '../Floor';
import type { Actor } from '../Actor';

/**
 * Chases you. On death, spawns Muck. Muck regenerates into a Skully after 3 turns.
 * Port of C# Skully.cs.
 */
export class Skully extends AIActor {
  readonly [ACTOR_KILLED_HANDLER] = true as const;
  /** Set by Muck.step() so the renderer plays an unsquish spawn instead of GrowAtStart. */
  squishSpawn = false;

  protected override get deathEventType(): string { return 'squishDeath'; }

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 1;
  }

  baseAttackDamage(): [number, number] {
    return [1, 1];
  }

  onKilled(_a: Actor): void {
    const floor = this.floor!;
    const pos = this.pos;
    // Synchronous: Muck appears in the same animation batch as the squishDeath.
    const candidates = floor.breadthFirstSearch(pos, tile => tile instanceof Ground);
    const muckSpot = candidates.find(t => !(floor.grasses.get(t.pos) instanceof Muck));
    if (muckSpot) {
      floor.put(new Muck(muckSpot.pos));
    }
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;
    if (this.canTargetPlayer()) {
      if (this.isNextTo(player)) {
        return new AttackTask(this, player);
      }
      return new ChaseTargetTask(this, player);
    }
    return new MoveRandomlyTask(this);
  }
}

/**
 * Grass that regenerates into a Skully after 3 turns.
 * Step on it to remove it.
 * Port of C# Muck.
 */
export class Muck extends Grass implements ISteppable, IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;
  timeNextAction: number;
  turnsElapsed = 0;

  get turnPriority(): number {
    return 40;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.timeNextAction = this.timeCreated + 1;
  }

  handleActorEnter(who: any): void {
    if (who.constructor.name === 'Player') {
      this.kill(who);
    }
  }

  step(): number {
    this.onNoteworthyAction();
    this.turnsElapsed++;
    if (this.turnsElapsed >= 3) {
      GameModelRef.mainOrNull?.emitAnimation({ type: 'quickDeath', entityGuid: this.guid });
      const s = new Skully(this.pos);
      s.squishSpawn = true;
      s.clearTasks();
      s.timeNextAction += 1;
      this.floor!.put(s);
      this.killSelf();
    }
    return 1;
  }

  catchUpStep(_lastTime: number, _currentTime: number): void {}
}

entityRegistry.register('Skully', Skully);
entityRegistry.register('Muck', Muck);
