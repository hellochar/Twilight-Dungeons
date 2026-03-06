import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { TelegraphedTask } from '../tasks/TelegraphedTask';
import { GenericBaseAction } from '../BaseAction';
import { Grass } from '../grasses/Grass';
import { Ground, Wall, type Tile } from '../Tile';
import { ACTOR_ENTER_HANDLER, Faction, type IActorEnterHandler } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Actor } from '../Actor';

/**
 * Chases and attacks its target until it dies, then burrows back underground.
 * Port of C# Scuttler.cs.
 */
export class Scuttler extends AIActor {
  get turnPriority(): number { return 21; }

  target: Actor | null = null;
  private hasAttacked = false;

  constructor(pos: Vector2Int) {
    super(pos);
    this.faction = Faction.Neutral;
    this._hp = 1;
    this._baseMaxHp = 1;
  }

  baseAttackDamage(): [number, number] {
    return [1, 1];
  }

  static canOccupy(t: Tile): boolean {
    return t.canBeOccupied() && t instanceof Ground &&
      t.floor!.getCardinalNeighbors(t.pos).some(n => n instanceof Wall);
  }

  targetting(who: Actor): this {
    this.target = who;
    return this;
  }

  private becomeGrass(): void {
    this.floor!.put(new ScuttlerUnderground(this.pos));
    // Don't kill, just remove
    this.floor!.remove(this);
  }

  protected getNextTask(): ActorTask {
    if (this.hasAttacked || this.target == null || this.target.isDead) {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, () => this.becomeGrass()));
    }
    const player = GameModelRef.main.player;
    if (this.target === player && !this.canTargetPlayer()) {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, () => this.becomeGrass()));
    }

    if (this.isNextTo(this.target)) {
      this.hasAttacked = true;
      return new AttackTask(this, this.target);
    } else {
      return new ChaseTargetTask(this, this.target);
    }
  }
}

/**
 * Something lies in wait here. Anything that walks over it becomes targeted.
 * Port of C# ScuttlerUnderground.
 */
export class ScuttlerUnderground extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  get displayName(): string { return 'Burrowed Scuttler'; }

  constructor(pos: Vector2Int) {
    super(pos);
  }

  static canOccupy(tile: Tile): boolean {
    return tile.canBeOccupied() && tile instanceof Ground;
  }

  handleActorEnter(who: Actor): void {
    if (!(who instanceof Scuttler) && this.floor && !this.isDead) {
      this.floor.put(new Scuttler(this.pos).targetting(who));
      this.kill(who);
    }
  }
}

entityRegistry.register('Scuttler', Scuttler);
entityRegistry.register('ScuttlerUnderground', ScuttlerUnderground);
