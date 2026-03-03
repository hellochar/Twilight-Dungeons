import { AIActor } from '../AIActor';
import { ActorTask } from '../../ActorTask';
import { WaitTask } from '../../tasks/WaitTask';
import { TelegraphedTask } from '../../tasks/TelegraphedTask';
import { GenericBaseAction } from '../../BaseAction';
import { Faction } from '../../../core/types';
import type { INoTurnDelay } from '../../../core/types';
import { Vector2Int } from '../../../core/Vector2Int';
import { MyRandom } from '../../../core/MyRandom';
import { GameModelRef } from '../../GameModelRef';
import { type EquippableItem, STICKY_TAG } from '../../Item';
import { ItemOnGround } from '../../ItemOnGround';
import { EquipmentSlot } from '../../Equipment';
import { entityRegistry } from '../../../generator/entityRegistry';
import type { Player } from '../../Player';
import { ItemTanglefoot } from './ItemTanglefoot';
import { ItemStiffarm } from './ItemStiffarm';
import { ItemBulbousSkin } from './ItemBulbousSkin';
import { ItemThirdEye } from './ItemThirdEye';
import { ItemScalySkin } from './ItemScalySkin';

// ─── Infection item constructors by slot ───

const InfectionTypes = new Map<EquipmentSlot, () => EquippableItem>([
  [EquipmentSlot.Footwear, () => new ItemTanglefoot()],
  [EquipmentSlot.Weapon, () => new ItemStiffarm()],
  [EquipmentSlot.Armor, () => new ItemBulbousSkin()],
  [EquipmentSlot.Headwear, () => new ItemThirdEye()],
  [EquipmentSlot.Offhand, () => new ItemScalySkin()],
]);

/**
 * Neutral fungus that sprays infection onto the player.
 * If all 5 equipment slots are already infected, heals the player instead.
 * Port of C# FruitingBody.cs.
 */
export class FruitingBody extends AIActor implements INoTurnDelay {
  readonly noTurnDelay = true as const;

  private cooldown: number;

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 1;
    this.faction = Faction.Neutral;
    this.cooldown = MyRandom.Range(0, 10);
    this.clearTasks();
  }

  baseAttackDamage(): [number, number] {
    return [0, 0];
  }

  protected getNextTask(): ActorTask {
    if (this.cooldown > 0) {
      this.cooldown--;
      return new WaitTask(this, 1);
    } else {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, () => this.spray()));
    }
  }

  private spray(): void {
    this.cooldown = 10;
    const player = GameModelRef.main.player;
    if (player.isNextTo(this)) {
      const uninfectedSlots: EquipmentSlot[] = [];
      for (const [slot, factory] of InfectionTypes) {
        const equipped = player.equipment.get(slot);
        // Check if the equipped item is NOT the infection type for this slot
        const infectionInstance = factory();
        if (!equipped || equipped.constructor !== infectionInstance.constructor) {
          uninfectedSlots.push(slot);
        }
      }

      if (uninfectedSlots.length === 0) {
        player.heal(1);
      } else {
        this.infect(uninfectedSlots, player);
      }
      this.killSelf();
    }
  }

  private infect(uninfectedSlots: EquipmentSlot[], player: Player): void {
    const slot = MyRandom.Pick(uninfectedSlots);
    const factory = InfectionTypes.get(slot)!;
    const infection = factory();

    const existingEquipment = player.equipment.get(infection.slot);
    if (existingEquipment != null && existingEquipment.constructor.name !== 'ItemHands') {
      player.equipment.removeItem(existingEquipment);
      if (!(STICKY_TAG in existingEquipment)) {
        player.floor!.put(new ItemOnGround(player.pos, existingEquipment));
      }
    }
    player.equipment.addItem(infection);
  }
}

entityRegistry.register('FruitingBody', FruitingBody);
