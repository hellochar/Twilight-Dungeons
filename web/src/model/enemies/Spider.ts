import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { MoveToTargetTask } from '../tasks/MoveToTargetTask';
import { WaitTask } from '../tasks/WaitTask';
import { TelegraphedTask } from '../tasks/TelegraphedTask';
import { GenericBaseAction } from '../BaseAction';
import { PoisonedStatus } from '../statuses/PoisonedStatus';
import { Web } from '../grasses/Web';
import { DEAL_ATTACK_DAMAGE_HANDLER, type IDealAttackDamageHandler } from '../Actor';
import { Vector2Int } from '../../core/Vector2Int';
import { MyRandom } from '../../core/MyRandom';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Body } from '../Body';
import type { Tile } from '../Tile';

/**
 * Spins webs, deals no damage but applies poison.
 * Port of C# Spider.cs.
 */
export class Spider extends AIActor implements IDealAttackDamageHandler {
  readonly [DEAL_ATTACK_DAMAGE_HANDLER] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
    this.hp = this._baseMaxHp = 4;
  }

  baseAttackDamage(): [number, number] {
    return [0, 0];
  }

  handleDealAttackDamage(_dmg: number, target: Body): void {
    if ((target as any).statuses) {
      (target as any).statuses.add(new PoisonedStatus(1));
    }
  }

  private putWeb(): void {
    if (Web.canOccupy(this.tile)) {
      this.floor!.put(new Web(this.pos));
    }
  }

  protected getNextTask(): ActorTask {
    // Place web if possible
    if (Web.canOccupy(this.tile)) {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, () => this.putWeb()));
    }

    // Attack adjacent player
    const player = GameModelRef.main.player;
    if (this.canTargetPlayer() && this.isNextTo(player)) {
      return new AttackTask(this, player);
    }

    // Move to adjacent tile, preferring non-webbed ones
    const adjacent = this.floor!.getAdjacentTiles(this.pos).filter((t: Tile) => t.canBeOccupied());
    const nonWebbed = adjacent.filter((t: Tile) => Web.canOccupy(t));
    const webbed = adjacent.filter((t: Tile) => !Web.canOccupy(t));

    if (nonWebbed.length > 0) {
      const target = MyRandom.Pick(nonWebbed);
      return new MoveToTargetTask(this, target.pos);
    } else if (webbed.length > 0) {
      const target = MyRandom.Pick(webbed);
      return new MoveToTargetTask(this, target.pos);
    }
    return new WaitTask(this, 1);
  }
}

entityRegistry.register('Spider', Spider);
