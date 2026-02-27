import { EquippableItem, WEAPON_TAG, type IWeapon } from '../Item';
import { EquipmentSlot } from '../Equipment';
import { DEAL_ATTACK_DAMAGE_HANDLER, type IDealAttackDamageHandler } from '../Actor';
import { GameModelRef } from '../GameModelRef';

/**
 * Heals the player equal to damage dealt, then self-destructs.
 * Port of C# ItemBatTooth from Bat.cs.
 */
export class ItemBatTooth extends EquippableItem implements IWeapon, IDealAttackDamageHandler {
  readonly [WEAPON_TAG] = true as const;
  readonly [DEAL_ATTACK_DAMAGE_HANDLER] = true as const;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Weapon;
  }

  get attackSpread(): [number, number] {
    return [1, 1];
  }

  handleDealAttackDamage(dmg: number, target: any): void {
    if (target.faction !== undefined && dmg > 0) {
      GameModelRef.main.player.heal(dmg);
    }
    this.Destroy();
  }
}
