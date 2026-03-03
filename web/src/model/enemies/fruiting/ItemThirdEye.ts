import { EquippableItem, DURABLE_TAG, STICKY_TAG, reduceDurability, type IDurable, type ISticky } from '../../Item';
import { ACTION_PERFORMED_HANDLER, type IActionPerformedHandler } from '../../Actor';
import { ATTACK_DAMAGE_TAKEN_MOD, type IAttackDamageTakenModifier } from '../../../core/Modifiers';
import { ThirdEyeStatus } from '../../statuses/ThirdEyeStatus';
import { EquipmentSlot } from '../../Equipment';
import type { BaseAction } from '../../BaseAction';

/**
 * Headwear infection. See creatures' exact HP. +1 attack damage taken. Durability lost per action.
 * Port of C# ItemThirdEye from FruitingBody.cs.
 */
export class ItemThirdEye
  extends EquippableItem
  implements IDurable, ISticky, IActionPerformedHandler, IAttackDamageTakenModifier
{
  readonly [DURABLE_TAG] = true as const;
  readonly [STICKY_TAG] = true as const;
  readonly [ACTION_PERFORMED_HANDLER] = true as const;
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Headwear;
  }

  get maxDurability(): number {
    return 40;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  override OnEquipped(): void {
    this.player.statuses.add(new ThirdEyeStatus());
  }

  override OnUnequipped(): void {
    this.player.statuses.removeOfType(ThirdEyeStatus);
  }

  handleActionPerformed(_finalAction: BaseAction, _initialAction: BaseAction): void {
    reduceDurability(this);
  }

  modify(input: any): any {
    return input + 1;
  }

  getStats(): string {
    return "You're infected with a Third Eye!\nYou can see creatures' exact HP.\nTake 1 more attack damage.";
  }
}
