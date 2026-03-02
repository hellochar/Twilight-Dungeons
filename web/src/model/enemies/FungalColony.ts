import { Boss } from './Boss';
import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { WaitTask } from '../tasks/WaitTask';
import { GenericTask } from '../tasks/GenericTask';
import { TelegraphedTask } from '../tasks/TelegraphedTask';
import { ExplodeTask } from '../tasks/ExplodeTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { MoveToTargetTask } from '../tasks/MoveToTargetTask';
import { GenericBaseAction } from '../BaseAction';
import {
  TAKE_ANY_DAMAGE_HANDLER,
  type ITakeAnyDamageHandler,
} from '../Body';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  type IAttackDamageTakenModifier,
} from '../../core/Modifiers';
import { Faction, type IDeathHandler, type INoTurnDelay } from '../../core/types';
import { Ground, FungalWall } from '../Tile';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { MyRandom } from '../../core/MyRandom';
import { SurprisedStatus } from '../tasks/SleepTask';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Entity } from '../Entity';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

// ─── FungalColony ───

/**
 * Boss that blocks 1 attack damage and spawns a FungalSentinel when attacked.
 * Every 12 turns, summons a FungalBreeder and teleports to a random FungalWall.
 * Port of C# FungalColony from FungalColony.cs.
 */
export class FungalColony extends Boss implements IAttackDamageTakenModifier {
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 48;
    this.faction = Faction.Enemy;
    this.clearTasks();
  }

  handleDeath(source: Entity): void {
    super.handleDeath(source);
    // Kill all breeders and sentinels
    const floor = this.floor;
    if (!floor) return;
    const minions = floor.bodies.where(
      (b) => b instanceof FungalBreeder || b instanceof FungalSentinel
    );
    for (const b of [...minions]) {
      b.kill(this);
    }
  }

  private needsWait = true;

  protected getNextTask(): ActorTask {
    if (this.needsWait) {
      this.needsWait = false;
      return new WaitTask(this, 12);
    } else {
      return new GenericTask(this, () => this.summonFungalBreeder());
    }
  }

  private summonFungalBreeder(): void {
    const player = GameModelRef.main.player;
    if (player.isDead) {
      this.needsWait = true;
      return;
    }
    const floor = this.floor;
    if (!floor) return;

    const fungalWalls: FungalWall[] = [];
    for (const t of floor.tiles) {
      if (t instanceof FungalWall) fungalWalls.push(t);
    }

    // Prefer walls greater than distance 3 from player
    const farWalls = fungalWalls.filter((t) => t.distanceTo(player) > 3);
    const nextTile = MyRandom.Pick(farWalls.length > 0 ? farWalls : fungalWalls);

    if (nextTile) {
      this.needsWait = true;
      const oldPos = this.pos;
      // Remove the wall so it's occupiable
      floor.put(new Ground(nextTile.pos));
      this.pos = nextTile.pos;
      const breeder = new FungalBreeder(oldPos);
      floor.put(breeder);
      breeder.clearTasks();
      breeder.statuses.add(new SurprisedStatus());
    }
  }

  modify(input: any): any {
    // Spawn FungalSentinel at this position when taking attack damage
    this.floor?.put(new FungalSentinel(this.pos));
    return input - 1;
  }
}

// ─── FungalBreeder ───

/**
 * Summons a FungalSentinel every 7 turns.
 * Does not move or attack.
 * Port of C# FungalBreeder from FungalColony.cs.
 */
export class FungalBreeder extends AIActor {
  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 8;
    this.faction = Faction.Enemy;
  }

  private needsWait = false;

  protected getNextTask(): ActorTask {
    if (this.needsWait) {
      this.needsWait = false;
      return new WaitTask(this, 6);
    } else {
      this.needsWait = true;
      return new TelegraphedTask(
        this,
        1,
        new GenericBaseAction(this, () => this.summonFungalSentinel()),
      );
    }
  }

  private summonFungalSentinel(): void {
    const sentinel = new FungalSentinel(this.pos);
    sentinel.statuses.add(new SurprisedStatus());
    sentinel.timeNextAction += 1;
    this.floor?.put(sentinel);
  }
}

// ─── FungalSentinel ───

/**
 * Explodes at melee range, dealing 2 damage to adjacent creatures.
 * Leaves a FungalWall on death.
 * Port of C# FungalSentinel from FungalColony.cs.
 */
export class FungalSentinel extends AIActor implements ITakeAnyDamageHandler, IDeathHandler, INoTurnDelay {
  readonly [TAKE_ANY_DAMAGE_HANDLER] = true as const;
  readonly [DEATH_HANDLER] = true as const;
  readonly noTurnDelay = true as const;

  get turnPriority(): number {
    return this.task instanceof ExplodeTask ? 49 : 50;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 3;
    this.faction = Faction.Enemy;
    this.clearTasks();
  }

  handleDeath(source: Entity): void {
    if (this.tile instanceof Ground) {
      this.floor?.put(new FungalWall(this.pos));
    }
    super.handleDeath(source);
  }

  explode(): void {
    const floor = this.floor;
    if (!floor) return;
    for (const actor of floor.adjacentActors(this.pos)) {
      if (actor !== this) {
        (actor as any).takeDamage(2, this);
      }
    }
    this.killSelf();
  }

  private shouldExplode = false;

  protected getNextTask(): ActorTask {
    if (!this.canTargetPlayer()) {
      return new MoveRandomlyTask(this);
    }

    if (this.shouldExplode) {
      return new GenericTask(this, () => this.explode());
    }

    const player = GameModelRef.main.player;
    if (this.isNextTo(player)) {
      this.shouldExplode = true;
      return new ExplodeTask(this);
    } else {
      // Move toward player — find closest adjacent occupiable tile
      const floor = this.floor!;
      const adjacent = floor
        .getAdjacentTiles(this.pos)
        .filter((t) => t.canBeOccupied() || t === this.tile)
        .sort((a, b) => a.distanceTo(player) - b.distanceTo(player));

      const best = adjacent[0];
      if (!best || best === this.tile) {
        return new WaitTask(this, 1);
      }
      return new MoveToTargetTask(this, best.pos);
    }
  }

  handleTakeAnyDamage(_damage: number): void {
    if (!this.shouldExplode) {
      this.shouldExplode = true;
      this.setTasks(new ExplodeTask(this));
    }
  }
}

entityRegistry.register('FungalColony', FungalColony);
entityRegistry.register('FungalBreeder', FungalBreeder);
entityRegistry.register('FungalSentinel', FungalSentinel);
