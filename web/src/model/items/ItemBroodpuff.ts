import {
  Item,
  EquippableItem,
  WEAPON_TAG,
  DURABLE_TAG,
  USABLE_TAG,
  TARGETED_ACTION_TAG,
  reduceDurability,
  type IWeapon,
  type IDurable,
  type IUsable,
  type ITargetedAction,
} from '../Item';
import { EquipmentSlot } from '../Equipment';
import { AIActor } from '../enemies/AIActor';
import { Grass } from '../grasses/Grass';
import { ItemOnGround } from '../ItemOnGround';
import { GameModelRef } from '../GameModelRef';
import { WaitTask } from '../tasks/WaitTask';
import { AttackTask } from '../tasks/AttackTask';
import { ActorTask } from '../ActorTask';
import { Vector2Int } from '../../core/Vector2Int';
import { ActionType, Faction, TileVisibility } from '../../core/types';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  ACTION_COST_MOD,
  type IAttackDamageTakenModifier,
  type IActionCostModifier,
} from '../../core/Modifiers';
import { ATTACK_HANDLER, type IAttackHandler } from '../Actor';
import { ActionCosts } from '../BaseAction';
import { VulnerableStatus } from '../statuses/VulnerableStatus';
import { Ground } from '../Tile';
import { entityRegistry } from '../../generator/entityRegistry';
import { MyRandom } from '../../core/MyRandom';
import type { Entity } from '../Entity';
import type { Player } from '../Player';
import type { Actor } from '../Actor';
import type { Body } from '../Body';
import type { Tile } from '../Tile';

// ─── ItemLeecher ───

/**
 * Summon a stationary ally Leecher at a visible tile.
 * Port of C# ItemLeecher from Broodpuff.cs.
 */
export class ItemLeecher extends Item implements IDurable, ITargetedAction {
  readonly [DURABLE_TAG] = true as const;
  readonly [TARGETED_ACTION_TAG] = true as const;
  readonly targetedActionName = 'Summon';

  durability: number;

  get maxDurability(): number {
    return 6;
  }

  constructor(durability = 6) {
    super();
    this.durability = durability;
  }

  targets(player: Player): Entity[] {
    const floor = player.floor;
    if (!floor) return [];
    const result: Entity[] = [];
    for (const tile of floor.tiles) {
      if (tile.visibility === TileVisibility.Visible && tile.canBeOccupied()) {
        result.push(tile);
      }
    }
    return result;
  }

  performTargetedAction(player: Player, target: Entity): void {
    player.floor!.put(new Leecher(target.pos, this.durability));
    this.Destroy();
  }

  getStats(): string {
    return 'Summon a stationary ally. It attacks enemies for 1 damage (this uses Durability).\n\nYou may pickup the Leecher by tapping it.\n\nAt zero Durability, it becomes a Broodpuff Seed.';
  }
}

// ─── Leecher ───

/**
 * Allied AI actor that attacks adjacent enemies. Pickupable.
 * Port of C# Leecher from Broodpuff.cs.
 */
export class Leecher extends AIActor implements IAttackHandler {
  readonly [ATTACK_HANDLER] = true as const;

  durability: number;

  get turnPriority(): number {
    return 15;
  }

  get description(): string {
    return super.description + `\nDurability: ${this.durability}/10`;
  }

  constructor(pos: Vector2Int, durability: number) {
    super(pos);
    this.durability = durability;
    this._hp = this._baseMaxHp = 1;
    this.faction = Faction.Ally;
    this.clearTasks();
  }

  pickup(): void {
    const player = GameModelRef.main.player;
    player.inventory.addItem(new ItemLeecher(this.durability), this);
    // Do NOT "kill" to prevent infinite triggers
    this.floor!.remove(this);
  }

  handleDeath(source: Entity | null): void {
    super.handleDeath(source);
    if (source !== this && this.floor) {
      this.floor.put(new ItemOnGround(this.pos, new ItemLeecher(this.durability)));
    }
  }

  baseAttackDamage(): [number, number] {
    return [1, 1];
  }

  protected getNextTask(): ActorTask {
    const floor = this.floor;
    if (!floor) return new WaitTask(this, 1);
    const enemies = floor.adjacentActors(this.pos).filter(
      (a: any) => a.faction === Faction.Enemy,
    );
    const target = enemies.length > 0 ? MyRandom.Pick(enemies) as Actor : undefined;
    if (target) {
      return new AttackTask(this, target);
    }
    return new WaitTask(this, 1);
  }

  onAttack(_damage: number, _target: Body): void {
    this.durability--;
    if (this.durability <= 0) {
      // In C#, adds ItemSeed(typeof(Broodpuff)) — we just kill self for now
      this.killSelf();
    }
  }
}

// ─── ItemBacillomyte ───

/**
 * Use to grow Bacillomytes around you.
 * Port of C# ItemBacillomyte from Broodpuff.cs.
 */
export class ItemBacillomyte extends Item implements IUsable, IDurable {
  readonly [USABLE_TAG] = true as const;
  readonly [DURABLE_TAG] = true as const;

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
      if (Bacillomyte.canOccupy(tile)) {
        floor.put(new Bacillomyte(tile.pos));
      }
    }
    reduceDurability(this);
  }

  getStats(): string {
    return 'Use to grow Bacillomytes around you. Enemies standing over Bacillomyte take 1 extra attack damage.';
  }
}

// ─── Bacillomyte ───

class BacillomyteBodyModifier implements IAttackDamageTakenModifier {
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;

  modify(input: any): any {
    if (typeof input === 'number') {
      return input + 1;
    }
    return input;
  }
}

const bacillomyteBodyModifierInstance = new BacillomyteBodyModifier();

/**
 * Grass that makes enemies standing on it take +1 attack damage.
 * Port of C# Bacillomyte from Broodpuff.cs.
 */
export class Bacillomyte extends Grass {
  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground;
  }

  constructor(pos: Vector2Int) {
    super(pos);
  }

  get bodyModifier(): object | null {
    const b = this.body;
    if (b && 'faction' in b && (b as any).faction !== Faction.Ally) {
      return bacillomyteBodyModifierInstance;
    }
    return null;
  }
}

// ─── ItemBroodleaf ───

/**
 * Weapon that applies Vulnerable on attack and halves ATTACK action cost.
 * Port of C# ItemBroodleaf from Broodpuff.cs.
 */
export class ItemBroodleaf
  extends EquippableItem
  implements IWeapon, IDurable, IAttackHandler, IActionCostModifier
{
  readonly [WEAPON_TAG] = true as const;
  readonly [DURABLE_TAG] = true as const;
  readonly [ATTACK_HANDLER] = true as const;
  readonly [ACTION_COST_MOD] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Weapon;
  }

  get maxDurability(): number {
    return 25;
  }

  get attackSpread(): [number, number] {
    return [1, 1];
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  onAttack(_damage: number, target: Body): void {
    if ('statuses' in target) {
      (target as Actor).statuses.add(new VulnerableStatus(20));
    }
  }

  modify(input: any): any {
    if (input instanceof ActionCosts) {
      const cost = input.get(ActionType.ATTACK) ?? 1;
      input.set(ActionType.ATTACK, cost / 2);
      return input;
    }
    return input;
  }

  getStats(): string {
    return 'Applies the Vulnerable Status to attacked Creatures, making them take 1 more attack damage for 20 turns.\nAttacks twice as fast.';
  }
}

entityRegistry.register('Leecher', Leecher);
entityRegistry.register('Bacillomyte', Bacillomyte);
