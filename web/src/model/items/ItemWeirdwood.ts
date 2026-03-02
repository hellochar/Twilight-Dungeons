import {
  Item,
  EquippableItem,
  WEAPON_TAG,
  DURABLE_TAG,
  STACKABLE_TAG,
  USABLE_TAG,
  reduceDurability,
  type IWeapon,
  type IDurable,
  type IStackable,
  type IUsable,
} from '../Item';
import { EquipmentSlot } from '../Equipment';
import { GameModelRef } from '../GameModelRef';
import { Grass } from '../grasses/Grass';
import { Ground } from '../Tile';
import { entityRegistry } from '../../generator/entityRegistry';
import { Faction, type INoTurnDelay } from '../../core/types';
import { FreeMoveStatus } from '../statuses/FreeMoveStatus';
import { RunAwayTask } from '../tasks/RunAwayTask';
import { ATTACK_HANDLER, type IAttackHandler } from '../Actor';
import type { ISteppable } from '../Floor';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Actor } from '../Actor';
import type { Body } from '../Body';
import type { Player } from '../Player';

// ─── ItemVilePotion ───

/**
 * Spawns VileGrowth lines towards every visible enemy.
 * Port of C# ItemVilePotion from Weirdwood.cs.
 */
export class ItemVilePotion extends Item implements IStackable, IUsable {
  readonly [STACKABLE_TAG] = true as const;
  readonly [USABLE_TAG] = true as const;

  readonly stacksMax = 3;
  private _stacks: number;

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

  constructor(stacks = 3) {
    super();
    this._stacks = stacks;
  }

  use(actor: Actor): void {
    const player = actor as Player;
    const floor = actor.floor;
    if (!floor) return;

    const enemies = player.getVisibleActors(Faction.Enemy);
    const start = actor.pos;

    if (enemies.length > 0) {
      for (const enemy of enemies) {
        for (const pos of floor.enumerateLine(start, enemy.pos)) {
          if (floor.tiles.get(pos) instanceof Ground) {
            floor.put(new VileGrowth(pos));
          }
        }
      }
      this.stacks--;
    }
  }

  getStats(): string {
    return 'Spawns Vile Growths in lines towards every visible enemy.\nVile Growth does 1 damage per turn to any creature standing over it. Lasts 9 turns.';
  }
}

// ─── VileGrowth ───

/**
 * Deals 1 damage/turn to actor standing on it. Self-destructs after 9 turns.
 * Port of C# VileGrowth from Weirdwood.cs.
 */
export class VileGrowth extends Grass implements ISteppable, INoTurnDelay {
  readonly noTurnDelay = true as const;

  timeNextAction: number;
  get turnPriority(): number { return 14; }
  private turns = 9;

  private get actor(): any {
    return this.floor?.bodies.get(this.pos) ?? null;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.timeNextAction = this.timeCreated;
  }

  step(): number {
    const a = this.actor;
    if (a != null) {
      a.takeDamage(1, GameModelRef.main.player);
    }
    this.onNoteworthyAction();
    if (--this.turns <= 0) {
      this.killSelf();
    }
    return 1;
  }
}

entityRegistry.register('VileGrowth', VileGrowth);

// ─── ItemBackstepShoes ───

/**
 * Footwear that grants 3 Free Moves after attacking.
 * Port of C# ItemBackstepShoes from Weirdwood.cs.
 */
export class ItemBackstepShoes
  extends EquippableItem
  implements IDurable, IAttackHandler
{
  readonly [DURABLE_TAG] = true as const;
  readonly [ATTACK_HANDLER] = true as const;

  durability: number;

  get maxDurability(): number {
    return 7;
  }

  get slot(): EquipmentSlot {
    return EquipmentSlot.Footwear;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  onAttack(_damage: number, _target: Body): void {
    if (this.player.statuses.findOfType(FreeMoveStatus) == null) {
      this.player.statuses.add(new FreeMoveStatus(3));
      reduceDurability(this);
    }
  }

  getStats(): string {
    return 'After you make an attack, get 3 Free Moves.';
  }
}

// ─── ItemWitchsShiv ───

/**
 * Weapon that fears the target for 10 turns on attack.
 * Port of C# ItemWitchsShiv from Weirdwood.cs.
 */
export class ItemWitchsShiv
  extends EquippableItem
  implements IWeapon, IDurable, IAttackHandler
{
  readonly [WEAPON_TAG] = true as const;
  readonly [DURABLE_TAG] = true as const;
  readonly [ATTACK_HANDLER] = true as const;

  readonly attackSpread: [number, number] = [2, 2];

  durability: number;

  get maxDurability(): number {
    return 3;
  }

  get slot(): EquipmentSlot {
    return EquipmentSlot.Weapon;
  }

  get displayName(): string {
    return "Witch's Shiv";
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  onAttack(_damage: number, target: Body): void {
    if ('faction' in target) {
      const actor = target as Actor;
      actor.setTasks(new RunAwayTask(actor, this.player.pos, 10));
    }
  }

  getStats(): string {
    return 'Attacking a target fears them for 10 turns.';
  }
}
