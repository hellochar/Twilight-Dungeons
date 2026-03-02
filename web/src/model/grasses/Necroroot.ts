import { Grass } from './Grass';
import { AIActor } from '../enemies/AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { ChaseDynamicTargetTask } from '../tasks/ChaseDynamicTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { ACTION_PERFORMED_HANDLER, type IActionPerformedHandler } from '../Actor';
import { Vector2Int } from '../../core/Vector2Int';
import { Faction, type IDeathHandler } from '../../core/types';
import { GameModelRef } from '../GameModelRef';
import { Ground } from '../Tile';
import { entityRegistry } from '../../generator/entityRegistry';
import type { BaseAction } from '../BaseAction';
import type { Tile } from '../Tile';
import type { Actor } from '../Actor';
import type { Entity } from '../Entity';
import type { Body } from '../Body';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * Internal body modifier for Necroroot. When an actor dies on the grass, triggers zombie spawn.
 */
class NecrorootBodyModifier implements IDeathHandler {
  readonly [DEATH_HANDLER] = true as const;
  private owner: Necroroot;

  constructor(owner: Necroroot) {
    this.owner = owner;
  }

  handleDeath(_source: Entity): void {
    this.owner.handleBodyDied();
  }
}

/**
 * Spawn a Zombie of any creature that dies on the Necroroot.
 * Port of C# Necroroot.cs.
 */
export class Necroroot extends Grass {
  corpse: Actor | null = null;

  private _bodyModifier: NecrorootBodyModifier;

  get bodyModifier(): object | null {
    return this._bodyModifier;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this._bodyModifier = new NecrorootBodyModifier(this);
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground && tile.grass == null;
  }

  handleBodyDied(): void {
    const actor = this.floor?.bodies.get(this.pos) as Actor | null;
    if (this.corpse == null && actor != null && !(actor instanceof Zombie)) {
      this.corpse = actor;
      this.addTimedEvent(3.01, () => this.createZombie());
    }
  }

  private createZombie(): void {
    if (this.floor && this.corpse) {
      this.floor.put(new Zombie(this.pos, this.corpse));
      this.killSelf();
    }
  }
}

/**
 * Zombie spawned from Necroroot. Attacks nearby non-Zombies.
 * Loses 1 HP per action when not standing on Necroroot.
 * Port of C# Zombie from Necroroot.cs.
 */
export class Zombie extends AIActor implements IActionPerformedHandler {
  readonly [ACTION_PERFORMED_HANDLER] = true as const;
  readonly baseActor: Actor;

  get displayName(): string {
    return `Zombie ${this.baseActor.displayName}`;
  }

  constructor(pos: Vector2Int, baseActor: Actor) {
    super(pos);
    this.baseActor = baseActor;
    this._hp = this._baseMaxHp = baseActor.maxHp;
    this.faction = Faction.Enemy;
    this.clearTasks();
  }

  baseAttackDamage(): [number, number] {
    return this.baseActor.baseAttackDamage();
  }

  handleActionPerformed(_finalAction: BaseAction, _initialAction: BaseAction): void {
    if (!(this.grass instanceof Necroroot)) {
      this.takeDamage(1, this);
    }
  }

  protected getNextTask(): ActorTask {
    const target = this.selectTarget();
    if (!target) {
      return new MoveRandomlyTask(this);
    }
    if (this.isNextTo(target)) {
      return new AttackTask(this, target as any);
    }
    return new ChaseDynamicTargetTask(this, () => this.selectTarget());
  }

  private selectTarget(): Body | null {
    const floor = this.floor;
    if (!floor) return null;

    const bodies = floor.bodiesInCircle(this.pos, 7) as Body[];
    const visible = bodies.filter(
      b => floor.testVisibility(this.pos, b.pos) && !(b instanceof Zombie)
    );

    if (visible.length === 0) return null;
    return visible.reduce((best, b) =>
      this.distanceTo(b) < this.distanceTo(best) ? b : best
    );
  }
}

entityRegistry.register('Necroroot', Necroroot);
entityRegistry.register('Zombie', Zombie);
