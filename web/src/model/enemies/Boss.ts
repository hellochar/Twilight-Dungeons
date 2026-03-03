import { AIActor } from './AIActor';
import { Faction, type IDeathHandler } from '../../core/types';
import { Trigger } from '../Trigger';
import { GameModelRef } from '../GameModelRef';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Entity } from '../Entity';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * Boss base class. Places HeartTrigger on death.
 * Port of C# Boss.cs.
 */
export abstract class Boss extends AIActor implements IDeathHandler {
  readonly [DEATH_HANDLER] = true as const;
  override readonly isBoss = true;
  isSeen = false;

  constructor(pos: Vector2Int) {
    super(pos);
    this.faction = Faction.Enemy;
  }

  handleDeath(source: Entity): void {
    super.handleDeath(source);
    // Place HeartTrigger at death position — acts as floor-clear trigger
    const floor = this.floor;
    const pos = this.pos;
    GameModelRef.main.enqueuEvent(() => {
      if (floor) {
        const trigger = new Trigger(pos, () => {
          const model = GameModelRef.main;
          model.currentFloor.clearFloor();
        });
        floor.put(trigger);
      }
    });
  }
}
