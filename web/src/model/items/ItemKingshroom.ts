import {
  Item,
  EquippableItem,
  DURABLE_TAG,
  USABLE_TAG,
  STICKY_TAG,
  TARGETED_ACTION_TAG,
  reduceDurability,
  type IDurable,
  type IUsable,
  type ISticky,
  type ITargetedAction,
} from '../Item';
import { EquipmentSlot } from '../Equipment';
import { Status } from '../Status';
import { Actor, ACTION_PERFORMED_HANDLER, STATUS_ADDED_HANDLER, type IActionPerformedHandler } from '../Actor';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  type IAttackDamageTakenModifier,
} from '../../core/Modifiers';
import { ActionType, Faction } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { MyRandom } from '../../core/MyRandom';
import { SporedStatus } from '../grasses/Spores';
import { Boss } from '../enemies/Boss';
import { entityRegistry } from '../../generator/entityRegistry';
import type { BaseAction } from '../BaseAction';
import type { Entity } from '../Entity';
import type { Player } from '../Player';
import type { ISteppable } from '../Floor';

// ─── ItemGerm ───

/**
 * Use to spawn ThickMushroom allies at adjacent tiles.
 * Port of C# ItemGerm from Kingshroom.cs.
 */
export class ItemGerm extends Item implements IDurable, IUsable {
  readonly [DURABLE_TAG] = true as const;
  readonly [USABLE_TAG] = true as const;

  durability: number;

  get maxDurability(): number {
    return 4;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  use(actor: Actor): void {
    const floor = actor.floor!;
    for (const tile of floor.getAdjacentTiles(actor.pos)) {
      if (tile.canBeOccupied()) {
        floor.put(new ThickMushroom(tile.pos));
      }
    }
    reduceDurability(this);
  }

  getStats(): string {
    return 'Spawn allied Thick Mushrooms around you. You can swap positions with them.';
  }
}

// ─── ItemMushroomCap ───

interface IStatusAddedHandler {
  handleStatusAdded(status: Status): void;
}

/**
 * Headwear. If SporedStatus is added, prevent it and heal 1 HP.
 * Port of C# ItemMushroomCap from Kingshroom.cs.
 */
export class ItemMushroomCap
  extends EquippableItem
  implements IDurable, IStatusAddedHandler
{
  readonly [DURABLE_TAG] = true as const;
  readonly [STATUS_ADDED_HANDLER] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Headwear;
  }

  get maxDurability(): number {
    return 3;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  handleStatusAdded(status: Status): void {
    if (status instanceof SporedStatus) {
      status.actor?.heal(1);
      status.Remove();
      reduceDurability(this);
    }
  }

  getStats(): string {
    return "If you'd get the Spored Status, prevent it and heal 1 HP instead.";
  }
}

// ─── ItemKingshroomPowder ───

/**
 * Target adjacent actors, apply InfectedStatus.
 * Port of C# ItemKingshroomPowder from Kingshroom.cs.
 */
export class ItemKingshroomPowder extends Item implements IDurable, ITargetedAction {
  readonly [DURABLE_TAG] = true as const;
  readonly [TARGETED_ACTION_TAG] = true as const;
  readonly targetedActionName = 'Infect';

  durability: number;

  get maxDurability(): number {
    return 3;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  targets(player: Player): Entity[] {
    const floor = player.floor;
    if (!floor) return [];
    return floor.adjacentActors(player.pos).filter(
      (a: any) => a !== player && !(a instanceof Boss),
    );
  }

  performTargetedAction(player: Player, target: Entity): void {
    const actor = target as Actor;
    // Apply infection directly if adjacent
    if (actor.isNextTo(player)) {
      actor.statuses.add(new InfectedStatus());
      reduceDurability(this);
    }
  }

  getStats(): string {
    return 'Infect an adjacent creature. Each turn, it takes 1 damage and spawns a Thick Mushroom adjacent to it.';
  }
}

// ─── ItemLivingArmor ───

/**
 * Sticky armor. Blocks 2 attack damage. Moving reduces durability.
 * Port of C# ItemLivingArmor from Kingshroom.cs.
 */
export class ItemLivingArmor
  extends EquippableItem
  implements ISticky, IDurable, IActionPerformedHandler, IAttackDamageTakenModifier
{
  readonly [STICKY_TAG] = true as const;
  readonly [DURABLE_TAG] = true as const;
  readonly [ACTION_PERFORMED_HANDLER] = true as const;
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Armor;
  }

  get maxDurability(): number {
    return 60;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  handleActionPerformed(finalAction: BaseAction, _initialAction: BaseAction): void {
    if (finalAction.type === ActionType.MOVE) {
      reduceDurability(this);
    }
  }

  modify(input: any): any {
    if (typeof input === 'number') {
      return input - 2;
    }
    return input;
  }

  getStats(): string {
    return 'Blocks 2 attack damage.\nMoving reduces durability.\nCannot be unequipped.';
  }
}

// ─── InfectedStatus ───

/**
 * Each turn: spawn ThickMushroom at random adjacent pos + take 1 damage.
 * Port of C# InfectedStatus from Kingshroom.cs.
 */
export class InfectedStatus extends Status {
  get isDebuff(): boolean {
    return true;
  }

  Consume(_other: Status): boolean {
    return true;
  }

  Start(): void {
    this.actor!.addTimedEvent(1, () => this.independentStep());
  }

  private independentStep(): void {
    const actor = this.actor;
    if (!actor || actor.isDead || !actor.floor) return;

    const floor = actor.floor;
    const tiles = floor.getAdjacentTiles(actor.pos).filter(t => t.canBeOccupied());
    const tile = tiles.length > 0 ? MyRandom.Pick(tiles) : undefined;
    if (tile) {
      floor.put(new ThickMushroom(tile.pos));
    }
    actor.takeDamage(1, GameModelRef.main.player);
    actor.addTimedEvent(1, () => this.independentStep());
  }
}

// ─── ThickMushroom ───

/**
 * Allied 1 HP stationary actor. Effectively never acts (huge timeNextAction).
 * Port of C# ThickMushroom from Kingshroom.cs.
 */
export class ThickMushroom extends Actor implements ISteppable {
  constructor(pos: Vector2Int) {
    super(pos);
    this.faction = Faction.Ally;
    this._hp = this._baseMaxHp = 1;
    this.timeNextAction += 999999;
  }

  step(): number {
    return 999999;
  }
}

entityRegistry.register('ThickMushroom', ThickMushroom);
