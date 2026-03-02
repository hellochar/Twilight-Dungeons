import { Body } from '../Body';
import { Grass } from '../grasses/Grass';
import { Ground } from '../Tile';
import { Vector2Int } from '../../core/Vector2Int';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { GameModelRef } from '../GameModelRef';
import { WeaknessStatus } from '../statuses/WeaknessStatus';
import { entityRegistry } from '../../generator/entityRegistry';
import type { ISteppable } from '../Floor';
import type { Actor } from '../Actor';
import type { Entity } from '../Entity';
import type { Tile } from '../Tile';

/**
 * Harmless body obstacle. 1 HP. No AI.
 * Port of C# CheshireWeed.
 */
export class CheshireWeed extends Body {
  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 1;
  }
}

/**
 * Grass that replicates to cardinal neighbors (age <= 2).
 * At age >= 4, replaces self with CheshireWeed body.
 * On enter when mature: applies WeaknessStatus(1).
 * Port of C# CheshireWeedSprout.
 */
export class CheshireWeedSprout extends Grass implements ISteppable, IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  timeNextAction: number;

  get turnPriority(): number {
    return 9;
  }

  get isMature(): boolean {
    return this.age >= 4;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.timeNextAction = (GameModelRef.mainOrNull?.time ?? 0) + 1;
  }

  static canOccupy(tile: Tile, floor: { grasses: { get(pos: Vector2Int): Entity | null } }): boolean {
    return floor.grasses.get(tile.pos) == null && tile.canBeOccupied() && tile instanceof Ground;
  }

  step(): number {
    const floor = this.floor;
    if (!floor) return 1;

    if (this.isMature) {
      floor.put(new CheshireWeed(this.pos));
      this.killSelf();
      return 1;
    }

    if (this.age <= 2) {
      const neighbors = floor.getCardinalNeighbors(this.pos)
        .filter(t => CheshireWeedSprout.canOccupy(t, floor));
      const sprouts = neighbors.map(t => new CheshireWeedSprout(t.pos));
      floor.putAll(sprouts);
    }

    return 1;
  }

  catchUpStep(_lastTime: number, _currentTime: number): void {}

  handleActorEnter(who: any): void {
    if (this.isMature) {
      (who as Actor).statuses.add(new WeaknessStatus(1));
    }
    this.kill(who);
  }
}

entityRegistry.register('CheshireWeed', CheshireWeed);
entityRegistry.register('CheshireWeedSprout', CheshireWeedSprout);
