import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { TelegraphedTask } from '../tasks/TelegraphedTask';
import { GenericBaseAction } from '../BaseAction';
import { Vector2Int } from '../../core/Vector2Int';
import { Faction } from '../../core/types';
import { GameModelRef } from '../GameModelRef';
import { Grass } from '../grasses/Grass';
import { Ground } from '../Tile';
import { CharmedStatus } from '../statuses/CharmedStatus';
import { entityRegistry } from '../../generator/entityRegistry';

/**
 * Allied butterfly. Every 5 turns, duplicates the player's grass to cardinal neighbors.
 * Port of C# Butterfly.cs.
 */
export class Butterfly extends AIActor {
  private static DUPLICATE_CD = 5;
  private cooldown = 0;

  constructor(pos: Vector2Int) {
    super(pos);
    this.faction = Faction.Ally;
    this._hp = this._baseMaxHp = 1;
    this.clearTasks();
    this.statuses.add(new CharmedStatus());
  }

  baseAttackDamage(): [number, number] {
    return [0, 0];
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;

    // We want to duplicate if off cooldown and player is on grass
    if (this.cooldown <= 0 && player.grass != null) {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, () => this.duplicateGrass()));
    }

    // Can't duplicate — either on cooldown or no grass
    if (this.cooldown > 0) {
      this.cooldown--;
    }
    return new ChaseTargetTask(this, player);
  }

  private duplicateGrass(): void {
    const player = GameModelRef.main.player;
    const grass = player.grass;
    if (grass == null || !(grass instanceof Grass)) return;

    const floor = grass.floor!;
    const neighbors = floor.getCardinalNeighbors(grass.pos)
      .filter(tile => tile instanceof Ground && floor.grasses.get(tile.pos) == null);

    // Use the grass constructor to create copies
    const GrassType = grass.constructor as new (pos: Vector2Int) => Grass;
    for (const tile of neighbors) {
      try {
        const newGrass = new GrassType(tile.pos);
        floor.put(newGrass);
      } catch {
        // Constructor may not match expected signature — skip
      }
    }
    this.cooldown = Butterfly.DUPLICATE_CD;
  }
}

entityRegistry.register('Butterfly', Butterfly);
