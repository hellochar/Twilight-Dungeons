import { EquippableItem, STACKABLE_TAG, type IStackable } from '../Item';
import { EquipmentSlot } from '../Equipment';
import { BODY_MOVE_HANDLER, type IBodyMoveHandler } from '../Body';
import { GameModelRef } from '../GameModelRef';
import { Web } from '../grasses/Web';
import type { Vector2Int } from '../../core/Vector2Int';

/**
 * Footwear that leaves a trail of webs. Has limited stacks.
 * Port of C# ItemSpiderSandals from Spider.cs.
 */
export class ItemSpiderSandals extends EquippableItem implements IStackable, IBodyMoveHandler {
  readonly [STACKABLE_TAG] = true as const;
  readonly [BODY_MOVE_HANDLER] = true as const;

  readonly stacksMax = 999;
  private _stacks: number;

  get stacks(): number {
    return this._stacks;
  }

  set stacks(value: number) {
    if (value < 0) throw new Error('Setting negative stack! ' + this + ' to ' + value);
    this._stacks = value;
    if (this._stacks === 0) {
      GameModelRef.main.enqueuEvent(() => {
        const player = GameModelRef.main.player;
        const grass = player.grass;
        if (grass && '_isWeb' in grass) {
          grass.kill(player);
        }
        this.Destroy();
      });
    }
  }

  get slot(): EquipmentSlot {
    return EquipmentSlot.Footwear;
  }

  constructor(stacks: number) {
    super();
    this._stacks = stacks;
  }

  handleMove(newPos: Vector2Int, oldPos: Vector2Int): void {
    const player = GameModelRef.main.player;
    const floor = player.floor!;
    const oldTile = floor.tiles.get(oldPos);
    if (oldTile && Web.canOccupy(oldTile)) {
      floor.put(new Web(oldPos));
      this.stacks--;
    }
    if (this.stacks > 0) {
      const newTile = floor.tiles.get(newPos);
      if (newTile && Web.canOccupy(newTile)) {
        floor.put(new Web(player.pos));
        this.stacks--;
      }
    }
  }
}
