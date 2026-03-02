import {
  EquippableItem,
  WEAPON_TAG,
  DURABLE_TAG,
  reduceDurability,
  type IWeapon,
  type IDurable,
} from '../Item';
import { EquipmentSlot } from '../Equipment';
import { ACTION_PERFORMED_HANDLER, type IActionPerformedHandler } from '../Actor';
import { AttackBaseAction } from '../BaseAction';
import { ActionType, Faction } from '../../core/types';
import type { BaseAction } from '../BaseAction';
import type { Actor } from '../Actor';

/**
 * Weapon that auto-attacks an adjacent enemy when you move.
 * Port of C# ItemWildwoodRod from Wildwood.cs.
 */
export class ItemWildwoodRod
  extends EquippableItem
  implements IWeapon, IDurable, IActionPerformedHandler
{
  readonly [WEAPON_TAG] = true as const;
  readonly [DURABLE_TAG] = true as const;
  readonly [ACTION_PERFORMED_HANDLER] = true as const;

  readonly attackSpread: [number, number] = [3, 5];

  durability: number;

  get maxDurability(): number {
    return 20;
  }

  get slot(): EquipmentSlot {
    return EquipmentSlot.Weapon;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  handleActionPerformed(finalAction: BaseAction, _initialAction: BaseAction): void {
    if (finalAction.type === ActionType.MOVE) {
      const p = this.player;
      const floor = p.floor;
      if (!floor) return;

      const target = floor.adjacentActors(p.pos).find(
        (a: any) => (a as Actor).faction === Faction.Enemy,
      ) as Actor | undefined;

      if (target) {
        p.perform(new AttackBaseAction(p, target));
        reduceDurability(this);
      }
    }
  }

  getStats(): string {
    return 'Automatically attack an adjacent enemy when you move.';
  }
}
