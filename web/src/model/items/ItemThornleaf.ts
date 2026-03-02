import {
  EquippableItem,
  DURABLE_TAG,
  reduceDurability,
  type IDurable,
} from '../Item';
import { EquipmentSlot } from '../Equipment';
import { GameModelRef } from '../GameModelRef';
import {
  ATTACK_DAMAGE_TAKEN_MOD,
  ATTACK_DAMAGE_MOD,
  ANY_DAMAGE_TAKEN_MOD,
  type IAttackDamageTakenModifier,
  type IAttackDamageModifier,
  type IAnyDamageTakenModifier,
  type IModifierProvider,
} from '../../core/Modifiers';
import {
  BODY_TAKE_ATTACK_DAMAGE_HANDLER,
  TAKE_ANY_DAMAGE_HANDLER,
  type IBodyTakeAttackDamageHandler,
  type ITakeAnyDamageHandler,
} from '../Body';
import { Bladegrass } from '../grasses/Bladegrass';

// ─── ItemCrownOfThorns ───

/**
 * Headwear that deals 2 damage back to attackers.
 * Port of C# ItemCrownOfThorns from Thornleaf.cs.
 */
export class ItemCrownOfThorns
  extends EquippableItem
  implements IDurable, IBodyTakeAttackDamageHandler
{
  readonly [DURABLE_TAG] = true as const;
  readonly [BODY_TAKE_ATTACK_DAMAGE_HANDLER] = true as const;

  durability: number;

  get maxDurability(): number {
    return 14;
  }

  get slot(): EquipmentSlot {
    return EquipmentSlot.Headwear;
  }

  get displayName(): string {
    return 'Crown of Thorns';
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  handleTakeAttackDamage(_damage: number, _hp: number, source: any): void {
    const player = GameModelRef.main.player;
    // rarely, the thing that attacks you may already be dead (AKA parasite)
    if (source !== player && !source.isDead) {
      source.takeDamage(2, player);
      reduceDurability(this);
    }
  }

  getStats(): string {
    return 'When an enemy attacks you, deal 2 damage back to the attacker.';
  }
}

// ─── ItemThornShield ───

/**
 * Offhand shield that blocks 1 damage and deals 1 more attack damage.
 * Both effects reduce durability.
 * Port of C# ItemThornShield from Thornleaf.cs.
 */
export class ItemThornShield
  extends EquippableItem
  implements IDurable, IModifierProvider
{
  readonly [DURABLE_TAG] = true as const;

  durability: number;

  get maxDurability(): number {
    return 11;
  }

  get slot(): EquipmentSlot {
    return EquipmentSlot.Offhand;
  }

  private readonly _modifiers: object[];

  get myModifiers(): Iterable<object | null | undefined> {
    return this._modifiers;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
    this._modifiers = [
      new TakeLessDamage(this),
      new DealMoreDamage(this),
    ];
  }

  getStats(): string {
    return 'Blocks 1 damage.\nDeal 1 more attack damage.\nBoth attacking and blocking use durability.';
  }
}

class TakeLessDamage implements IAttackDamageTakenModifier {
  readonly [ATTACK_DAMAGE_TAKEN_MOD] = true as const;
  readonly shield: ItemThornShield;

  constructor(shield: ItemThornShield) {
    this.shield = shield;
  }

  modify(input: any): any {
    if (typeof input === 'number') {
      reduceDurability(this.shield);
      return input - 1;
    }
    return input;
  }
}

class DealMoreDamage implements IAttackDamageModifier {
  readonly [ATTACK_DAMAGE_MOD] = true as const;
  readonly shield: ItemThornShield;

  constructor(shield: ItemThornShield) {
    this.shield = shield;
  }

  modify(input: any): any {
    if (typeof input === 'number') {
      reduceDurability(this.shield);
      return input + 1;
    }
    return input;
  }
}

// ─── ItemBlademail ───

/**
 * Armor that blocks 2 damage from all sources and grows/sharpens/triggers
 * Bladegrass on adjacent tiles when taking any damage.
 * Port of C# ItemBlademail from Thornleaf.cs.
 */
export class ItemBlademail
  extends EquippableItem
  implements IDurable, IAnyDamageTakenModifier, ITakeAnyDamageHandler
{
  readonly [DURABLE_TAG] = true as const;
  readonly [ANY_DAMAGE_TAKEN_MOD] = true as const;
  readonly [TAKE_ANY_DAMAGE_HANDLER] = true as const;

  durability: number;

  get maxDurability(): number {
    return 29;
  }

  get slot(): EquipmentSlot {
    return EquipmentSlot.Armor;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  handleTakeAnyDamage(_damage: number): void {
    const p = this.player;
    const floor = p.floor;
    if (!floor) return;

    for (const tile of floor.getAdjacentTiles(p.pos).filter(Bladegrass.canOccupy)) {
      if (tile.grass instanceof Bladegrass) {
        const g = tile.grass;
        if (g.isSharp) {
          const actor = floor.bodies.get(g.pos);
          if (actor != null) {
            g.handleActorEnter(actor);
          }
        } else {
          g.sharpen();
        }
      } else {
        floor.put(new Bladegrass(tile.pos));
      }
    }
    reduceDurability(this);
  }

  modify(input: any): any {
    if (typeof input === 'number') {
      return input - 2;
    }
    return input;
  }

  getStats(): string {
    return 'Take 2 less damage from all sources.\nWhen you would take damage from any source, grow, sharpen, or trigger Bladegrass on all adjacent tiles.';
  }
}
