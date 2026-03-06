import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { RunAwayTask } from '../tasks/RunAwayTask';
import { WaitTask } from '../tasks/WaitTask';
import { TelegraphedTask } from '../tasks/TelegraphedTask';
import { ActionCosts, GenericBaseAction } from '../BaseAction';
import { ActionType, Faction } from '../../core/types';
import type { IDeathHandler } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { MyRandom } from '../../core/MyRandom';
import { entityRegistry } from '../../generator/entityRegistry';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * Fast enemy that flees when another Jackal dies nearby.
 * Port of C# Jackal.cs.
 */
export class Jackal extends AIActor implements IDeathHandler {
  readonly [DEATH_HANDLER] = true as const;

  private fastStepsRemaining = 2;
  private wasAdjacentAtTurnStart = false;

  get sprintReady(): boolean {
    return this.fastStepsRemaining > 0;
  }

  protected get actionCosts(): ActionCosts {
    return new ActionCosts([
      [ActionType.MOVE, this.fastStepsRemaining > 0 ? 0.5 : 1],
      [ActionType.ATTACK, 1],
      [ActionType.WAIT, 1],
      [ActionType.GENERIC, 1],
    ]);
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.faction = Faction.Enemy;
    this.hp = this._baseMaxHp = 1;
  }

  baseAttackDamage(): [number, number] {
    return [1, 1];
  }

  step(): number {
    if (this.fastStepsRemaining === 2) {
      this.wasAdjacentAtTurnStart = this.isNextTo(GameModelRef.main.player);
    }
    const cost = super.step();
    if (cost <= 0.5) {
      this.fastStepsRemaining--;
    } else {
      this.fastStepsRemaining = 2;
    }
    return cost;
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;
    if (this.canTargetPlayer()) {
      if (this.isNextTo(player)) {
        if (this.sprintReady && !this.wasAdjacentAtTurnStart) {
          return new WaitTask(this, 1);
        }
        return new AttackTask(this, player);
      }
      return new ChaseTargetTask(this, player);
    }
    return new MoveRandomlyTask(this);
  }

  handleDeath(_source: any): void {
    const floor = this.floor;
    if (!floor) return;
    const myPos = this.pos;

    // Alert nearby jackals/jackal bosses within radius 7 that can see this jackal
    const toAlert: AIActor[] = [];
    for (const pos of floor.enumerateCircle(myPos, 7)) {
      const body = floor.bodies.get(pos);
      if (body && (body instanceof Jackal || body instanceof JackalBoss)) {
        if (body !== this && floor.testVisibility(myPos, body.pos)) {
          toAlert.push(body as AIActor);
        }
      }
    }

    GameModelRef.main.enqueuEvent(() => {
      for (const jackal of toAlert) {
        (jackal as any).setTasks(new RunAwayTask(jackal, myPos, 6));
      }
    });
  }
}

/**
 * Boss that summons jackals when none remain.
 * Port of C# JackalBoss (extends Boss → AIActor).
 */
export class JackalBoss extends AIActor implements IDeathHandler {
  readonly [DEATH_HANDLER] = true as const;

  get turnPriority(): number {
    return super.turnPriority - 1;
  }

  private cooldown = 0;
  private numToSummon = 2;

  constructor(pos: Vector2Int) {
    super(pos);
    this.faction = Faction.Enemy;
    this.hp = this._baseMaxHp = 16;
    this.clearTasks();
  }

  baseAttackDamage(): [number, number] {
    return [2, 3];
  }

  step(): number {
    const dt = super.step();
    if (this.cooldown > 0) {
      this.cooldown -= dt;
    }
    return dt;
  }

  handleDeath(_source: any): void {
    // Kill all jackals on the map when the boss dies
    const jackals = this.allJackals();
    for (const j of jackals) {
      j.kill(this);
    }
  }

  private allJackals(): Jackal[] {
    if (!this.floor) return [];
    return this.floor.bodies.where(b => b instanceof Jackal) as Jackal[];
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;
    const shouldCast = this.cooldown <= 0 && this.allJackals().length < 1;

    if (shouldCast) {
      return new TelegraphedTask(
        this,
        1,
        new GenericBaseAction(this, () => this.summonJackals()),
      );
    }

    if (this.canTargetPlayer()) {
      if (this.isNextTo(player)) {
        return new AttackTask(this, player);
      }
      const chase = new ChaseTargetTask(this, player);
      chase.maxMoves = 1;
      return chase;
    }
    return new MoveRandomlyTask(this);
  }

  private summonJackals(): void {
    this.cooldown = 9;
    const perimeter = [...this.floor!.enumeratePerimeter(1)];
    MyRandom.Shuffle(perimeter);
    for (let i = 0; i < this.numToSummon && i < perimeter.length; i++) {
      this.floor!.put(new Jackal(perimeter[i]));
    }
    this.numToSummon++;
  }
}

entityRegistry.register('Jackal', Jackal);
entityRegistry.register('JackalBoss', JackalBoss);
