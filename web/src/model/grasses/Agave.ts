import { Grass } from './Grass';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { GameModelRef } from '../GameModelRef';
import { Item, STACKABLE_TAG, type IStackable } from '../Item';
import { EDIBLE_TAG, type IEdible } from '../Item';
import { ItemOnGround } from '../ItemOnGround';
import { Mushroom } from './Mushroom';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';
import type { Actor } from '../Actor';

/**
 * Walk over to harvest. Collect 4 to refine into Agave Honey.
 * Port of C# Agave from Agave.cs.
 */
export class Agave extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  static canOccupy(tile: Tile): boolean {
    return Mushroom.canOccupy(tile);
  }

  constructor(pos: Vector2Int) {
    super(pos);
  }

  handleActorEnter(actor: any): void {
    const player = GameModelRef.main.player;
    if (actor === player) {
      this.becomeItemInInventory(new ItemAgave(1));
    }
  }

  private becomeItemInInventory(item: Item): void {
    const player = GameModelRef.main.player;
    const floor = this.floor;
    this.kill(player);
    if (!player.inventory.addItem(item, this)) {
      if (floor) {
        floor.put(new ItemOnGround(this.pos, item, this.pos));
      }
    }
  }
}

/**
 * Stackable item. Refine at 4 stacks to get Agave Honey.
 * Port of C# ItemAgave from Agave.cs.
 */
export class ItemAgave extends Item implements IStackable {
  readonly [STACKABLE_TAG] = true as const;

  private _stacks: number;
  get stacks(): number { return this._stacks; }
  set stacks(value: number) {
    if (value < 0) throw new Error('Negative stacks');
    this._stacks = value;
    if (this._stacks === 0) this.Destroy();
  }
  get stacksMax(): number { return 4; }

  constructor(stacks: number) {
    super();
    this._stacks = stacks;
  }

  getAvailableMethods(): string[] {
    const methods = super.getAvailableMethods();
    if (this.stacks >= this.stacksMax) {
      methods.push('Refine');
    }
    return methods;
  }

  refine(): void {
    if (this.stacks < this.stacksMax) return;
    const player = GameModelRef.main.player;
    player.floor.put(new ItemOnGround(player.pos, new ItemAgaveHoney(), player.pos));
    this.Destroy();
  }
}

/**
 * Edible item. Heals 1 HP and removes all debuffs.
 * Port of C# ItemAgaveHoney from Agave.cs.
 */
export class ItemAgaveHoney extends Item implements IEdible {
  readonly [EDIBLE_TAG] = true as const;

  eat(actor: Actor): void {
    actor.heal(1);
    const debuffs = [...actor.statuses.list].filter(s => s.isDebuff);
    for (const debuff of debuffs) {
      debuff.Remove();
    }
    if (debuffs.length > 0) {
      GameModelRef.main.drainEventQueue();
    }
    this.Destroy();
  }
}

entityRegistry.register('Agave', Agave);
