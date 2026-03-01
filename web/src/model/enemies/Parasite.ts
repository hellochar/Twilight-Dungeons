import { AIActor } from './AIActor';
import { Body } from '../Body';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { ActionCosts } from '../BaseAction';
import { ActionType, Faction } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { MyRandom } from '../../core/MyRandom';
import { DEAL_ATTACK_DAMAGE_HANDLER, type IDealAttackDamageHandler } from '../Actor';
import { SurprisedStatus } from '../tasks/SleepTask';
import { ParasiteStatus } from '../statuses/ParasiteStatus';
import { entityRegistry } from '../../generator/entityRegistry';

/**
 * Fast enemy that applies ParasiteStatus on hit and dies.
 * Attacks anything adjacent, not just the player.
 * Port of C# Parasite.cs.
 */
export class Parasite extends AIActor implements IDealAttackDamageHandler {
  readonly [DEAL_ATTACK_DAMAGE_HANDLER] = true as const;

  protected get actionCosts(): ActionCosts {
    return new ActionCosts([
      [ActionType.MOVE, 0.5],
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

  handleDealAttackDamage(damage: number, target: Body): void {
    if (damage > 0 && target instanceof Body && 'statuses' in target && !(target instanceof Parasite)) {
      (target as any).statuses.add(new ParasiteStatus());
      this.killSelf();
    }
  }

  protected getNextTask(): ActorTask {
    const target = this.selectTarget();
    if (target) {
      return new AttackTask(this, target);
    }
    return new MoveRandomlyTask(this);
  }

  private selectTarget(): Body | null {
    if (!this.floor) return null;
    const adjacent = this.floor.adjacentActors(this.pos)
      .filter(a => !(a instanceof Parasite)) as Body[];

    // Remove player if can't target
    const player = GameModelRef.main.player;
    const filtered = this.canTargetPlayer()
      ? adjacent
      : adjacent.filter(a => a !== player);

    if (filtered.length === 0) return null;
    return MyRandom.Pick(filtered);
  }
}

/**
 * Hatches into two Parasites after 3 turns.
 * Port of C# ParasiteEgg (extends Body, not AIActor).
 */
export class ParasiteEgg extends Body {
  /** Mark as enemy so it counts toward floor clear check */
  readonly faction = Faction.Enemy;

  constructor(pos: Vector2Int) {
    super(pos);
    this.hp = this._baseMaxHp = 3;
  }

  /**
   * Override enterFloor to schedule hatch.
   * addTimedEvent requires GameModelRef.main, which is only available after
   * the entity is placed on a floor.
   */
  protected handleEnterFloor(): void {
    super.handleEnterFloor();
    this.addTimedEvent(3, () => this.hatch());
  }

  private hatch(): void {
    if (this.isDead) return;
    const floor = this.floor;
    if (!floor) return;
    const spawnPos = this.pos;
    this.killSelf();
    for (let i = 0; i < 2; i++) {
      const p = new Parasite(spawnPos);
      p.clearTasks();
      p.statuses.add(new SurprisedStatus());
      floor.put(p);
    }
  }
}

entityRegistry.register('Parasite', Parasite);
entityRegistry.register('ParasiteEgg', ParasiteEgg);
