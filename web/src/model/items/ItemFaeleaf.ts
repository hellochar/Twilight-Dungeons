import {
  Item,
  STACKABLE_TAG,
  USABLE_TAG,
  type IStackable,
  type IUsable,
} from '../Item';
import { Grass } from '../grasses/Grass';
import { TAKE_ANY_DAMAGE_HANDLER, type ITakeAnyDamageHandler } from '../Body';
import { CannotPerformActionException } from '../BaseAction';
import { GameModelRef } from '../GameModelRef';
import { Vector2Int } from '../../core/Vector2Int';
import { MyRandom } from '../../core/MyRandom';
import { SurprisedStatus } from '../tasks/SleepTask';
import { AIActor } from '../enemies/AIActor';
import { Ground } from '../Tile';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Actor } from '../Actor';
import type { Tile } from '../Tile';

// ─── ItemFaegrass ───

/**
 * Stackable consumable. Use to scatter Faegrass across the level.
 * Port of C# ItemFaegrass from Faeleaf.cs.
 */
export class ItemFaegrass extends Item implements IStackable, IUsable {
  readonly [STACKABLE_TAG] = true as const;
  readonly [USABLE_TAG] = true as const;

  readonly stacksMax = 100;
  private _stacks: number;

  get stacks(): number {
    return this._stacks;
  }

  set stacks(value: number) {
    if (value < 0) throw new Error('Setting negative stack! ' + this + ' to ' + value);
    this._stacks = value;
    if (this._stacks === 0) {
      this.Destroy();
    }
  }

  constructor(stacks = 1) {
    super();
    this._stacks = stacks;
  }

  use(actor: Actor): void {
    if (actor.floor!.isCleared) {
      throw new CannotPerformActionException('Use when enemies are nearby!');
    }
    addFaegrassImpl(actor.floor!, 12);
    this.stacks--;
  }

  getStats(): string {
    return 'Use to disperse Faegrass randomly across the Level.\n\nFaegrass teleports creatures that are attacked while standing over it.';
  }
}

// ─── Faegrass placement helper ───

function addFaegrassImpl(floor: any, num: number): void {
  if (num === 0) return;

  const tiles: Tile[] = [];
  for (const tile of floor.tiles) {
    if (Faegrass.canOccupy(tile)) {
      tiles.push(tile);
    }
  }
  MyRandom.Shuffle(tiles);

  let numPlaced = 0;
  for (const tile of tiles) {
    const adj = floor.getAdjacentTiles(tile.pos);
    const hasFaegrass = adj.some((t: Tile) => t.grass instanceof Faegrass);
    if (!hasFaegrass) {
      floor.put(new Faegrass(tile.pos));
      numPlaced++;
      if (numPlaced >= num) break;
    }
  }
}

// ─── Faegrass ───

/**
 * Grass that teleports damaged creatures to other Faegrass.
 * Port of C# Faegrass from Faeleaf.cs.
 */
export class Faegrass extends Grass implements ITakeAnyDamageHandler {
  readonly [TAKE_ANY_DAMAGE_HANDLER] = true as const;

  static canOccupy(tile: Tile): boolean {
    return tile.canBeOccupied() && tile instanceof Ground;
  }

  constructor(pos: Vector2Int) {
    super(pos);
  }

  get bodyModifier(): object | null {
    return this;
  }

  handleTakeAnyDamage(_damage: number): void {
    const b = this.body;
    if (b && 'statuses' in b) {
      const actor = b as Actor;
      GameModelRef.main.enqueuEvent(() => {
        if (actor.isDead) return;

        // Find another unoccupied Faegrass
        const candidates: Faegrass[] = [];
        for (const grass of actor.floor!.grasses) {
          if (
            grass instanceof Faegrass &&
            grass !== this &&
            grass.tile.canBeOccupied()
          ) {
            candidates.push(grass);
          }
        }

        const nextFaegrass = candidates.length > 0 ? MyRandom.Pick(candidates) : undefined;
        if (nextFaegrass) {
          actor.pos = nextFaegrass.pos;
        }
        this.kill(actor);

        // AIActors get Surprised when teleported
        if (actor instanceof AIActor) {
          actor.statuses.add(new SurprisedStatus());
        }
      });
    }
  }
}

entityRegistry.register('Faegrass', Faegrass);
