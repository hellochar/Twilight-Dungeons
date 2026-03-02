import { Boss } from './Boss';
import { Blob, MiniBlob } from './Blob';
import { BlobSlime } from '../grasses/BlobSlime';
import { ActorTask } from '../ActorTask';
import { AttackGroundTask } from '../tasks/AttackGroundTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import {
  TAKE_ANY_DAMAGE_HANDLER,
  BODY_MOVE_HANDLER,
  type ITakeAnyDamageHandler,
  type IBodyMoveHandler,
} from '../Body';
import { Faction } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { MyRandom } from '../../core/MyRandom';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Entity } from '../Entity';

/**
 * Spawns a Blob or MiniBlob upon taking damage. Leaves a trail of BlobSlime.
 * Port of C# Blobmother.cs.
 */
export class Blobmother extends Boss implements ITakeAnyDamageHandler, IBodyMoveHandler {
  readonly [TAKE_ANY_DAMAGE_HANDLER] = true as const;
  readonly [BODY_MOVE_HANDLER] = true as const;

  get turnPriority(): number {
    return this.task?.constructor.name === 'AttackGroundTask' ? 90 : super.turnPriority + 1;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 28;
    this.faction = Faction.Enemy;
    this.clearTasks();
  }

  handleDeath(source: Entity): void {
    super.handleDeath(source);
    // Kill all blobs and blob slime on the floor
    const floor = this.floor;
    if (!floor) return;
    const blobs = floor.bodies.where(
      (b) => b.constructor.name === 'Blob' || b.constructor.name === 'MiniBlob'
    );
    const slimes: Entity[] = [];
    for (const g of floor.grasses) {
      if (g instanceof BlobSlime) slimes.push(g);
    }
    for (const b of [...blobs, ...slimes]) {
      b.kill(this);
    }
  }

  handleTakeAnyDamage(damage: number): void {
    if (damage > 0) {
      const blob = MyRandom.value < 0.5 ? new Blob(this.pos) : new MiniBlob(this.pos);
      this.floor?.put(blob);
    }
  }

  baseAttackDamage(): [number, number] {
    return [3, 4];
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;
    if (this.canTargetPlayer()) {
      if (this.isNextTo(player)) {
        return new AttackGroundTask(this, player.pos, 1);
      } else {
        return new ChaseTargetTask(this, player);
      }
    }
    return new MoveRandomlyTask(this);
  }

  handleMove(_newPos: Vector2Int, oldPos: Vector2Int): void {
    this.floor?.put(new BlobSlime(oldPos));
  }
}

entityRegistry.register('Blobmother', Blobmother);
