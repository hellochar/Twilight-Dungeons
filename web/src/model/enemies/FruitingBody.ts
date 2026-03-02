import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { WaitTask } from '../tasks/WaitTask';
import { TelegraphedTask } from '../tasks/TelegraphedTask';
import { GenericBaseAction } from '../BaseAction';
import type { BaseAction } from '../BaseAction';
import { GenericPlayerTask } from '../tasks/GenericTask';
import { Faction } from '../../core/types';
import type { INoTurnDelay } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { MyRandom } from '../../core/MyRandom';
import { GameModelRef } from '../GameModelRef';
import {
  EquippableItem,
  DURABLE_TAG,
  WEAPON_TAG,
  STICKY_TAG,
  reduceDurability,
  type IDurable,
  type IWeapon,
  type ISticky,
} from '../Item';
import { ItemOnGround } from '../ItemOnGround';
import { EquipmentSlot } from '../Equipment';
import { BODY_MOVE_HANDLER, type IBodyMoveHandler } from '../Body';
import {
  ACTION_PERFORMED_HANDLER,
  type IActionPerformedHandler,
} from '../Actor';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  type IAttackDamageTakenModifier,
} from '../../core/Modifiers';
import {
  PseudoRandomDistribution,
  CfromP,
} from '../../core/PseudoRandomDistribution';
import { ConstrictedStatus } from '../statuses/ConstrictedStatus';
import { Guardleaf } from '../grasses/Guardleaf';
import { Mushroom } from '../grasses/Mushroom';
import { ThirdEyeStatus } from '../statuses/ThirdEyeStatus';
import { Ground } from '../Tile';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Player } from '../Player';

// ─── Infection item constructors by slot ───

const InfectionTypes = new Map<EquipmentSlot, () => EquippableItem>([
  [EquipmentSlot.Footwear, () => new ItemTanglefoot()],
  [EquipmentSlot.Weapon, () => new ItemStiffarm()],
  [EquipmentSlot.Armor, () => new ItemBulbousSkin()],
  [EquipmentSlot.Headwear, () => new ItemThirdEye()],
  [EquipmentSlot.Offhand, () => new ItemScalySkin()],
]);

// ─── FruitingBody enemy ───

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

// ─── ItemTanglefoot (Footwear infection) ───

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

// ─── ItemStiffarm (Weapon infection) ───

/**
 * Weapon infection. +1 attack damage taken.
 * Port of C# ItemStiffarm from FruitingBody.cs.
 */
export class ItemStiffarm
  extends EquippableItem
  implements IDurable, IWeapon, IAttackDamageTakenModifier, ISticky
{
  readonly [DURABLE_TAG] = true as const;
  readonly [WEAPON_TAG] = true as const;
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;
  readonly [STICKY_TAG] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Weapon;
  }

  get maxDurability(): number {
    return 15;
  }

  get attackSpread(): [number, number] {
    return [2, 3];
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  modify(input: any): any {
    return input + 1;
  }

  getStats(): string {
    return "You're infected with Stiffarm!\nYou take +1 damage from attacks.";
  }
}

// ─── ItemBulbousSkin (Armor infection) ───

/**
 * Armor infection. Germinate: self-damage 1 + spawn Mushrooms on cardinal Ground neighbors.
 * Port of C# ItemBulbousSkin from FruitingBody.cs.
 */
export class ItemBulbousSkin extends EquippableItem implements IDurable, ISticky {
  readonly [DURABLE_TAG] = true as const;
  readonly [STICKY_TAG] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Armor;
  }

  get maxDurability(): number {
    return 1;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  germinate(player: Player): void {
    player.setTasks(new GenericPlayerTask(player, () => this.germinateAction()));
    reduceDurability(this);
  }

  private germinateAction(): void {
    this.player.takeDamage(1, this.player);
    for (const tile of this.player.floor!.getCardinalNeighbors(this.player.pos)) {
      if (tile instanceof Ground) {
        this.player.floor!.put(new Mushroom(tile.pos));
      }
    }
  }

  override getAvailableMethods(): string[] {
    const methods = super.getAvailableMethods();
    methods.push('Germinate');
    return methods;
  }

  getStats(): string {
    return "You're infected with Bulbous Skin!\nPress Germinate to take 1 damage and create 4 Mushrooms around you.";
  }
}

// ─── ItemThirdEye (Headwear infection) ───

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

// ─── ItemScalySkin (Offhand infection) ───

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

// ─── Registry ───

entityRegistry.register('FruitingBody', FruitingBody);
