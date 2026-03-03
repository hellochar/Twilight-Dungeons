import { EquippableItem, DURABLE_TAG, STICKY_TAG, reduceDurability, type IDurable, type ISticky } from '../../Item';
import { BODY_MOVE_HANDLER, type IBodyMoveHandler } from '../../Body';
import { Vector2Int } from '../../../core/Vector2Int';
import { PseudoRandomDistribution, CfromP } from '../../../core/PseudoRandomDistribution';
import { ConstrictedStatus } from '../../statuses/ConstrictedStatus';
import { Guardleaf } from '../../grasses/Guardleaf';
import { EquipmentSlot } from '../../Equipment';

const tanglefootC = CfromP(0.04);

/**
 * Footwear infection. Moving occasionally Constricts you and grows a Guardleaf.
 * Port of C# ItemTanglefoot from FruitingBody.cs.
 */
export class ItemTanglefoot
  extends EquippableItem
  implements IDurable, IBodyMoveHandler, ISticky
{
  readonly [DURABLE_TAG] = true as const;
  readonly [BODY_MOVE_HANDLER] = true as const;
  readonly [STICKY_TAG] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Footwear;
  }

  get maxDurability(): number {
    return 5;
  }

  private prd: PseudoRandomDistribution;

  constructor() {
    super();
    this.durability = this.maxDurability;
    this.prd = new PseudoRandomDistribution(tanglefootC);
  }

  handleMove(newPos: Vector2Int, oldPos: Vector2Int): void {
    const didMove = !Vector2Int.equals(newPos, oldPos);
    const floorNotCleared = !this.player.floor!.isCleared;
    const guardleafCanOccupy = Guardleaf.canOccupy(this.player.tile);
    const canTrigger = didMove && floorNotCleared && guardleafCanOccupy;
    if (!canTrigger) return;

    const shouldTrigger = this.prd.test();
    if (shouldTrigger) {
      this.player.statuses.add(new ConstrictedStatus(null));
      this.player.floor!.put(new Guardleaf(this.player.pos));
      reduceDurability(this);
    }
  }

  getStats(): string {
    return "You're infected with Tanglefoot!\nMoving will occasionally Constrict you and grow a Guardleaf at your location.";
  }
}
