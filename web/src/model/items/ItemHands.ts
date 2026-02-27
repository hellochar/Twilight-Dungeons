import { EquippableItem, WEAPON_TAG, type IWeapon } from '../Item';
import { EquipmentSlot } from '../Equipment';
import type { Inventory } from '../Inventory';
import type { Player } from '../Player';

/**
 * The player's bare hands — always available as weapon fallback.
 * Port of C# ItemHands.cs.
 */
export class ItemHands extends EquippableItem implements IWeapon {
  readonly [WEAPON_TAG] = true as const;
  private _player: Player;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Weapon;
  }

  get attackSpread(): [number, number] {
    return [1, 1];
  }

  /** Hands always "live" in the player's equipment. */
  get inventory(): Inventory | null {
    return this._player.equipment;
  }

  set inventory(_value: Inventory | null) {
    // no-op — hands can't be removed
  }

  constructor(player: Player) {
    super();
    this._player = player;
  }

  /** Hands have no available methods (can't drop/destroy). */
  getAvailableMethods(): string[] {
    return [];
  }
}
