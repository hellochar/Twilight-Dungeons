import { Entity } from './Entity';
import { Item } from './Item';
import { GameModelRef } from './GameModelRef';
import { Vector2Int } from '../core/Vector2Int';
import { CannotPerformActionException } from './BaseAction';
import type { Floor } from './Floor';
import type { Tile } from './Tile';

const ACTOR_ENTER_HANDLER = Symbol.for('IActorEnterHandler');

/**
 * Wraps an Item as a floor Entity so it can be placed on the ground.
 * When the player walks over it, the item is auto-picked up.
 * Port of C# ItemOnGround.cs.
 */
export class ItemOnGround extends Entity {
  readonly _isItem = true;
  readonly [ACTOR_ENTER_HANDLER] = true;

  private _pos: Vector2Int;
  readonly item: Item;
  readonly start: Vector2Int | null;

  get pos(): Vector2Int {
    return this._pos;
  }

  set pos(_value: Vector2Int) {
    // Immobile — cannot be moved
  }

  constructor(pos: Vector2Int, item: Item, start: Vector2Int | null = null) {
    super();
    this._pos = pos;
    this.item = item;
    this.start = start ?? pos;
  }

  /** Find a valid tile for placement via BFS. */
  static placementBehavior(floor: Floor, itemOnGround: ItemOnGround): void {
    const newPos = floor
      .BreadthFirstSearch(itemOnGround.pos, () => true)
      .find((tile: Tile) => ItemOnGround.canOccupy(tile));
    if (newPos) {
      itemOnGround._pos = newPos.pos;
    }
  }

  static canOccupy(tile: Tile): boolean {
    return tile.canBeOccupied() && tile.item == null;
  }

  /** IActorEnterHandler — auto-pickup when player steps on it. */
  handleActorEnter(who: any): void {
    const player = GameModelRef.main.player;
    if (who === player) {
      this.pickUp();
    }
  }

  private pickUp(): void {
    const player = GameModelRef.main.player;
    if (!this.isNextTo(player)) return;

    if (player.inventory.addItem(this.item, this)) {
      this.kill(player);
    } else {
      GameModelRef.main.turnManager.onPlayerCannotPerform.emit(
        new CannotPerformActionException('Inventory is full!')
      );
    }
  }
}
