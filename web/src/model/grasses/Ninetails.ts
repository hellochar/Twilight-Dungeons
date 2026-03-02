import { Destructible } from '../enemies/Destructible';
import { Body } from '../Body';
import { Item, USABLE_TAG, type IUsable } from '../Item';
import { ItemOnGround } from '../ItemOnGround';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';
import type { IDeathHandler } from '../../core/types';
import type { Entity } from '../Entity';
import type { Actor } from '../Actor';
import type { ISteppable } from '../Floor';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * Destructible plant. On death, gives player an ItemPlantableNinetails.
 * Port of C# Ninetails.cs.
 */
export class Ninetails extends Destructible implements IDeathHandler {
  readonly [DEATH_HANDLER] = true as const;

  constructor(pos: Vector2Int) {
    super(pos, 1);
  }

  handleDeath(_source: Entity): void {
    this.becomeItemInInventory(new ItemPlantableNinetails());
  }

  private becomeItemInInventory(item: Item): void {
    const player = GameModelRef.main.player;
    if (!player.inventory.addItem(item, this)) {
      if (this.floor) {
        this.floor.put(new ItemOnGround(this.pos, item, this.pos));
      }
    }
  }
}

/**
 * Usable item that places a HomeNinetails at the player's position.
 * Port of C# ItemPlantableNinetails from Ninetails.cs.
 */
export class ItemPlantableNinetails extends Item implements IUsable {
  readonly [USABLE_TAG] = true as const;

  use(_actor: Actor): void {
    const player = GameModelRef.main.player;
    const floor = player.floor;
    if (floor) {
      floor.put(new HomeNinetails(player.pos));
      this.Destroy();
    }
  }

  getStats(): string {
    return 'Use to place at your position. Occasionally drops a Floof.';
  }
}

/**
 * Placed Ninetails that drops an ItemFloof every 50 turns.
 * Port of C# HomeNinetails from Ninetails.cs.
 */
export class HomeNinetails extends Body implements ISteppable {
  timeNextAction: number;

  get turnPriority(): number {
    return 50;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.timeNextAction = this.timeCreated + 50;
  }

  step(): number {
    this.floor!.put(new ItemOnGround(this.pos, new ItemFloof(), this.pos));
    return 50;
  }

  catchUpStep(_lastTime: number, _currentTime: number): void {}
}

/**
 * Simple collectible item.
 * Port of C# ItemFloof from Ninetails.cs.
 */
export class ItemFloof extends Item {}

entityRegistry.register('Ninetails', Ninetails);
entityRegistry.register('HomeNinetails', HomeNinetails);
