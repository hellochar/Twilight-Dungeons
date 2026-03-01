import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { AttackTask } from '../tasks/AttackTask';
import { MoveToTargetTask } from '../tasks/MoveToTargetTask';
import { WaitTask } from '../tasks/WaitTask';
import { RunAwayTask } from '../tasks/RunAwayTask';
import { ATTACK_HANDLER, type IAttackHandler } from '../Actor';
import { Wall, type Tile } from '../Tile';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { MyRandom } from '../../core/MyRandom';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Body } from '../Body';

/**
 * Chases you. Stays one tile away from walls or non-Wildekins.
 * Attacks you if possible, then runs away for 3 turns.
 * Port of C# Wildekin.cs.
 */
export class Wildekin extends AIActor implements IAttackHandler {
  readonly [ATTACK_HANDLER] = true as const;

  get turnPriority(): number { return 60; }

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = 7;
    this._baseMaxHp = 7;
  }

  baseAttackDamage(): [number, number] {
    return [3, 3];
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;
    if (this.canTargetPlayer()) {
      if (this.isNextTo(player)) {
        return new AttackTask(this, player);
      } else {
        const tiles = this.adjacentTilesInPreferenceOrder();
        if (tiles.length > 0) {
          const bestScore = this.tilePreference(tiles[0]);
          const bestTiles = tiles.filter(t => this.tilePreference(t) === bestScore);
          const tile = bestTiles.sort(
            (a, b) => a.distanceTo(GameModelRef.main.player.pos) - b.distanceTo(GameModelRef.main.player.pos)
          )[0];
          return new MoveToTargetTask(this, tile.pos);
        } else {
          return new WaitTask(this, 1);
        }
      }
    } else {
      const tiles = this.adjacentTilesInPreferenceOrder();
      if (tiles.length > 0) {
        const bestScore = this.tilePreference(tiles[0]);
        const bestTiles = tiles.filter(t => this.tilePreference(t) === bestScore);
        const tile = MyRandom.Pick(bestTiles);
        return new MoveToTargetTask(this, tile.pos);
      } else {
        return new WaitTask(this, 1);
      }
    }
  }

  private adjacentTilesInPreferenceOrder(): Tile[] {
    return this.floor!
      .getAdjacentTiles(this.pos)
      .filter(t => t.canBeOccupied() || t === this.tile)
      .sort((a, b) => this.tilePreference(a) - this.tilePreference(b));
  }

  /** Lower = more preferred. */
  private tilePreference(t: Tile): number {
    const adjacentBad = this.floor!
      .getAdjacentTiles(t.pos)
      .filter(t2 =>
        t2 instanceof Wall ||
        (t2.body != null && !(t2.body instanceof Wildekin))
      ).length;
    const nearPlayer = t.isNextTo(GameModelRef.main.player) ? 100 : 0;
    return adjacentBad + nearPlayer;
  }

  onAttack(_damage: number, target: Body): void {
    if ('faction' in target) {
      this.setTasks(new RunAwayTask(this, target.pos, 3, false));
    }
  }
}

entityRegistry.register('Wildekin', Wildekin);
