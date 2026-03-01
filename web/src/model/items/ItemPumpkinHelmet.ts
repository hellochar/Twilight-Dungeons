import { EquippableItem, DURABLE_TAG, type IDurable } from '../Item';
import { EquipmentSlot } from '../Equipment';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  type IAttackDamageTakenModifier,
} from '../../core/Modifiers';
import {
  BODY_TAKE_ATTACK_DAMAGE_HANDLER,
  type IBodyTakeAttackDamageHandler,
} from '../Body';

/**
 * Durable headwear. Blocks 1 damage per hit; destroyed if damage still goes through.
 * Port of C# ItemPumpkinHelmet.cs.
 */
export class ItemPumpkinHelmet
  extends EquippableItem
  implements IDurable, IAttackDamageTakenModifier, IBodyTakeAttackDamageHandler
{
  readonly [DURABLE_TAG] = true as const;
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;
  readonly [BODY_TAKE_ATTACK_DAMAGE_HANDLER] = true as const;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Headwear;
  }

  private _durability: number;

  get durability(): number {
    return this._durability;
  }

  set durability(v: number) {
    this._durability = v;
    if (v <= 0) this.Destroy();
  }

  get maxDurability(): number {
    return 5;
  }

  constructor() {
    super();
    this._durability = this.maxDurability;
  }

  /** ATTACK_DAMAGE_TAKEN_MOD: reduce damage by 1, consume durability. */
  modify(input: any): any {
    if (typeof input === 'number') {
      if (input > 0) {
        this.durability--;
      }
      return input - 1;
    }
    return input;
  }

  /** If damage still went through after reduction, destroy the helmet. */
  handleTakeAttackDamage(damage: number, _hp: number, _source: any): void {
    if (damage > 0) {
      this.Destroy();
    }
  }

  getStats(): string {
    return 'Blocks 1 damage. If you still take attack damage, the Pumpkin Helmet breaks.';
  }
}
