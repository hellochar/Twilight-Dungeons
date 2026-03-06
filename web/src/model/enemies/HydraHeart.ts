import { AIActor } from './AIActor';
import { Body } from '../Body';
import { ActorTask } from '../ActorTask';
import { WaitTask } from '../tasks/WaitTask';
import { TelegraphedTask } from '../tasks/TelegraphedTask';
import { AttackGroundTask } from '../tasks/AttackGroundTask';
import { GenericBaseAction, WaitBaseAction, ActionCosts } from '../BaseAction';
import { BASE_ACTION_MOD, type IBaseActionModifier } from '../../core/Modifiers';
import { ActionType, Faction } from '../../core/types';
import type { IDeathHandler } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { MyRandom } from '../../core/MyRandom';
import { SurprisedStatus } from '../tasks/SleepTask';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Entity } from '../Entity';
import type { Tile } from '../Tile';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * Spawns HydraHeads every 4 turns within range 3. Max 6 heads.
 * Cannot move or attack. On death, kills all heads.
 * Port of C# HydraHeart.cs.
 */
export class HydraHeart extends AIActor implements IBaseActionModifier, IDeathHandler {
  readonly [BASE_ACTION_MOD] = true as const;
  readonly [DEATH_HANDLER] = true;
  override get isStationary() { return true; }

  static readonly spawnRange = 3;

  private heads: HydraHead[] = [];
  private needsWait = false;

  static isTarget(b: Body): boolean {
    return !(b instanceof HydraHead) && !(b instanceof HydraHeart) && !b.isDead;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.faction = Faction.Enemy;
    this._hp = this._baseMaxHp = 5;
  }

  baseAttackDamage(): [number, number] {
    return [0, 0];
  }

  protected getNextTask(): ActorTask {
    if (this.needsWait) {
      this.needsWait = false;
      return new WaitTask(this, 2);
    }
    // Clean up dead heads
    this.heads = this.heads.filter(h => !h.isDead);
    if (this.heads.length < 5) {
      this.needsWait = true;
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, () => this.spawnHydraHead()));
    }
    return new WaitTask(this, 1);
  }

  private spawnHydraHead(): void {
    const floor = this.floor;
    if (!floor) return;

    let spawnTile: Tile | null = null;

    // Find nearest target in range
    const bodiesInRange = floor.bodiesInCircle(this.pos, HydraHeart.spawnRange)
      .filter((b): b is Body => b instanceof Body && HydraHeart.isTarget(b));
    bodiesInRange.sort((a, b) => a.distanceTo(this.pos) - b.distanceTo(this.pos));
    const nearestEnemy = bodiesInRange[0] ?? null;

    if (nearestEnemy) {
      const adjacentTiles = floor.getAdjacentTiles(nearestEnemy.pos)
        .filter(t => this.canSpawnHydraHeadAt(t));
      if (adjacentTiles.length > 0) {
        spawnTile = MyRandom.Pick(adjacentTiles);
      }
    }

    if (!spawnTile) {
      const candidates: Tile[] = [];
      for (const p of floor.enumerateCircle(this.pos, HydraHeart.spawnRange)) {
        const t = floor.tiles.get(p);
        if (t && this.canSpawnHydraHeadAt(t)) {
          candidates.push(t);
        }
      }
      if (candidates.length > 0) {
        spawnTile = MyRandom.Pick(candidates);
      }
    }

    if (spawnTile) {
      const head = new HydraHead(spawnTile.pos);
      head.clearTasks();
      head.statuses.add(new SurprisedStatus());
      floor.put(head);
      this.heads.push(head);
    }
  }

  private canSpawnHydraHeadAt(t: Tile): boolean {
    return t.canBeOccupied() && this.floor!.testVisibility(this.pos, t.pos);
  }

  handleDeath(source: Entity | null): void {
    const heads = [...this.heads];
    GameModelRef.main.enqueuEvent(() => {
      for (const head of heads) {
        if (!head.isDead) {
          head.kill(source ?? this);
        }
      }
    });
  }

  modify(input: any): any {
    if (input != null && typeof input === 'object' && 'type' in input) {
      const type = input.type;
      if (type === ActionType.MOVE || type === ActionType.ATTACK) {
        return new WaitBaseAction(this);
      }
    }
    return input;
  }
}

/**
 * Stationary head that attacks random adjacent non-Hydra targets.
 * Attack cost = 3. Cannot move.
 * Port of C# HydraHead.cs.
 */
export class HydraHead extends AIActor implements IBaseActionModifier {
  readonly [BASE_ACTION_MOD] = true as const;
  override get isStationary() { return true; }

  get turnPriority(): number {
    return this.task?.constructor.name === 'AttackGroundTask' ? 90 : super.turnPriority;
  }

  protected get actionCosts(): ActionCosts {
    return new ActionCosts([
      [ActionType.ATTACK, 1],
      [ActionType.GENERIC, 1],
      [ActionType.MOVE, 1],
      [ActionType.WAIT, 1],
    ]);
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.faction = Faction.Enemy;
    this._hp = this._baseMaxHp = 1;
  }

  baseAttackDamage(): [number, number] {
    return [1, 1];
  }

  protected getNextTask(): ActorTask {
    const floor = this.floor;
    if (!floor) return new WaitTask(this, 1);

    const targets = floor.adjacentBodies(this.pos)
      .filter((b): b is Body => b instanceof Body && HydraHeart.isTarget(b));

    if (targets.length > 0) {
      const target = MyRandom.Pick(targets);
      return new AttackGroundTask(this, target.pos, 1);
    }
    return new WaitTask(this, 1);
  }

  modify(input: any): any {
    if (input != null && typeof input === 'object' && 'type' in input) {
      if (input.type === ActionType.MOVE) {
        return new WaitBaseAction(this);
      }
    }
    return input;
  }
}

entityRegistry.register('HydraHeart', HydraHeart);
entityRegistry.register('HydraHead', HydraHead);
