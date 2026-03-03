import { EquippableItem, DURABLE_TAG, STICKY_TAG, reduceDurability, type IDurable, type ISticky } from '../../Item';
import { ACTION_PERFORMED_HANDLER, type IActionPerformedHandler } from '../../Actor';
import { ATTACK_DAMAGE_TAKEN_MOD, type IAttackDamageTakenModifier } from '../../../core/Modifiers';
import { EquipmentSlot } from '../../Equipment';
import type { BaseAction } from '../../BaseAction';

/**
 * Offhand infection. Blocks 1 damage per hit (consumes durability).
 * Water loss mechanic removed from daily puzzle.
 * Port of C# ItemScalySkin from FruitingBody.cs.
 */
export class ItemScalySkin
  extends EquippableItem
  implements ISticky, IDurable, IAttackDamageTakenModifier, IActionPerformedHandler
{
  readonly [STICKY_TAG] = true as const;
  readonly [DURABLE_TAG] = true as const;
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;
  readonly [ACTION_PERFORMED_HANDLER] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Offhand;
  }

  get maxDurability(): number {
    return 8;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  modify(input: any): any {
    if (input > 0) {
      reduceDurability(this);
    }
    return input - 1;
  }

  handleActionPerformed(_finalAction: BaseAction, _initialAction: BaseAction): void {
    // Water mechanic removed from daily puzzle — no-op
  }

  getStats(): string {
    return "You're infected with Scaly Skin!\nBlock 1 damage.";
  }
}
