import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { MoveToTargetTask } from '../tasks/MoveToTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { JumpToTargetTask } from '../tasks/JumpToTargetTask';
import { TelegraphedTask } from '../tasks/TelegraphedTask';
import { WaitTask } from '../tasks/WaitTask';
import { GenericBaseAction } from '../BaseAction';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';

/**
 * Jumps next to you. When hurt, eats a nearby Grass to heal to full HP.
 * Port of C# Hopper.cs.
 */
export class Hopper extends AIActor {
  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = 3;
    this._baseMaxHp = 3;
  }

  baseAttackDamage(): [number, number] {
    return [1, 1];
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;

    if (this._hp !== this.maxHp) {
      // Hurt — try to eat grass
      if (this.grass != null) {
        return new TelegraphedTask(this, 1, new GenericBaseAction(this, () => this.eatGrass()));
      } else {
        // Search for nearby grass
        const nearbyGrassTile = this.floor!
          .breadthFirstSearch(this.pos, t => t.canBeOccupied() && this.distanceTo(t.pos) < 5)
          .find(t => t.grass != null);
        if (nearbyGrassTile) {
          return new MoveToTargetTask(this, nearbyGrassTile.pos);
        } else {
          return new MoveRandomlyTask(this);
        }
      }
    } else {
      // Full HP — chase and attack
      if (this.canTargetPlayer()) {
        if (this.isNextTo(player)) {
          return new AttackTask(this, player);
        } else {
          const jumpTile = this.floor!
            .getAdjacentTiles(player.pos)
            .filter(t => t.canBeOccupied())
            .sort((a, b) => a.distanceTo(this.pos) - b.distanceTo(this.pos))
            [0];
          if (jumpTile) {
            return new JumpToTargetTask(this, jumpTile.pos);
          } else {
            return new WaitTask(this, 1);
          }
        }
      } else {
        return new MoveRandomlyTask(this);
      }
    }
  }

  private eatGrass(): void {
    if (this.grass != null) {
      this.heal(this.maxHp - this._hp);
      this.grass.kill(this);
    }
  }
}

entityRegistry.register('Hopper', Hopper);
