import { Body } from '../Body';
import { Vector2Int } from '../../core/Vector2Int';
import {
  ANY_DAMAGE_TAKEN_MOD,
  type IAnyDamageTakenModifier,
} from '../../core/Modifiers';
import type { IBlocksVision } from '../Tile';
import type { Entity } from '../Entity';
import type { IDeathHandler } from '../../core/types';
import { entityRegistry } from '../../generator/entityRegistry';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * Base class for destructible terrain objects.
 * Forces all damage to 1 HP. Implements IHideInSidebar (no sidebar display).
 * Port of C# Destructible from Rubble.cs.
 */
export class Destructible extends Body implements IAnyDamageTakenModifier {
  readonly [ANY_DAMAGE_TAKEN_MOD] = true as const;
  readonly _isBody = true;

  constructor(pos: Vector2Int, hp = 1) {
    super(pos);
    this._hp = hp;
    this._baseMaxHp = hp;
  }

  modify(_input: number): number {
    return 1;
  }
}

/**
 * Destructible that blocks vision. 1 HP.
 * Port of C# Rubble.
 */
export class Rubble extends Destructible implements IBlocksVision {
  readonly blocksVision = true as const;

  constructor(pos: Vector2Int) {
    super(pos, 1);
  }
}

/**
 * Simple 1 HP destructible. No vision block.
 * Port of C# Stump.
 */
export class Stump extends Destructible {
  constructor(pos: Vector2Int) {
    super(pos, 1);
  }
}

/**
 * Destructible that blocks vision. Destroying one destroys all Stalks.
 * Port of C# Stalk.
 */
export class Stalk extends Destructible implements IBlocksVision, IDeathHandler {
  readonly blocksVision = true as const;
  readonly [DEATH_HANDLER] = true;

  constructor(pos: Vector2Int) {
    super(pos, 1);
  }

  handleDeath(source: Entity | null): void {
    if (!(source instanceof Stalk)) {
      const stalks = this.floor!.bodies.where((b: Entity) => b instanceof Stalk && b !== this) as Stalk[];
      for (const stalk of stalks) {
        stalk.kill(this);
      }
    }
  }
}

entityRegistry.register('Rubble', Rubble);
entityRegistry.register('Stump', Stump);
entityRegistry.register('Stalk', Stalk);
