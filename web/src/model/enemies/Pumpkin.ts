import { Destructible } from './Destructible';
import { ItemOnGround } from '../ItemOnGround';
import { ItemPumpkin } from '../items/ItemPumpkin';
import { GameModelRef } from '../GameModelRef';
import { Vector2Int } from '../../core/Vector2Int';
import { entityRegistry } from '../../generator/entityRegistry';
import type { IDeathHandler } from '../../core/types';
import type { Entity } from '../Entity';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * Destructible that drops an ItemPumpkin on death.
 * Port of C# Pumpkin from ItemPumpkin.cs.
 */
export class Pumpkin extends Destructible implements IDeathHandler {
  readonly [DEATH_HANDLER] = true;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  handleDeath(_source: Entity | null): void {
    const floor = this.floor;
    const pos = this.pos;
    GameModelRef.main.enqueuEvent(() => {
      if (floor) {
        floor.put(new ItemOnGround(pos, new ItemPumpkin()));
      }
    });
  }
}

entityRegistry.register('Pumpkin', Pumpkin);
