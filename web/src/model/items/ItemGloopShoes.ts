import {
  EquippableItem,
  STICKY_TAG,
  DURABLE_TAG,
  reduceDurability,
  type ISticky,
  type IDurable,
} from '../Item';
import { EquipmentSlot } from '../Equipment';
import {
  ACTION_PERFORMED_HANDLER,
  type IActionPerformedHandler,
} from '../Actor';
import type { BaseAction } from '../BaseAction';

/**
 * Sticky footwear that heals 1 HP every 50 actions.
 * Port of C# ItemGloopShoes from Goo.cs.
 */
export class ItemGloopShoes
  extends EquippableItem
  implements ISticky, IDurable, IActionPerformedHandler
{
  readonly [STICKY_TAG] = true as const;
  readonly [DURABLE_TAG] = true as const;
  readonly [ACTION_PERFORMED_HANDLER] = true as const;

  durability: number;
  private counter = 0;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Footwear;
  }

  get maxDurability(): number {
    return 3;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  handleActionPerformed(_finalAction: BaseAction, _initialAction: BaseAction): void {
    this.counter++;
    if (this.counter >= 50) {
      this.counter = 0;
      this.player.heal(1);
      reduceDurability(this);
    }
  }

  getStats(): string {
    return 'Sticky. Heals 1 HP every 50 actions.';
  }
}
