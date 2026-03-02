import {
  EquippableItem,
  WEAPON_TAG,
  STACKABLE_TAG,
  type IWeapon,
  type IStackable,
} from '../Item';
import { EquipmentSlot } from '../Equipment';
import { ATTACK_HANDLER, type IAttackHandler } from '../Actor';
import type { Body } from '../Body';
import type { Actor } from '../Actor';

/**
 * Stackable vine weapon. Damage equals stacks. Destroyed on hitting an Actor.
 * Port of C# ItemVineWhip from HangingVines.cs.
 */
export class ItemVineWhip
  extends EquippableItem
  implements IWeapon, IStackable, IAttackHandler
{
  readonly [WEAPON_TAG] = true as const;
  readonly [STACKABLE_TAG] = true as const;
  readonly [ATTACK_HANDLER] = true as const;

  private _stacks: number;

  get stacksMax(): number {
    return 7;
  }

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

  get attackSpread(): [number, number] {
    return [this._stacks, this._stacks];
  }

  get slot(): EquipmentSlot {
    return EquipmentSlot.Weapon;
  }

  constructor(stacks = 7) {
    super();
    this._stacks = stacks;
  }

  onAttack(damage: number, target: Body): void {
    if ('faction' in target) {
      // target is an Actor
      this.Destroy();
    }
  }

  getStats(): string {
    return 'Damage equals stacks. Destroyed on hitting an enemy.';
  }
}
