import { Item, STACKABLE_TAG, USABLE_TAG, type IStackable, type IUsable } from '../Item';
import type { Actor } from '../Actor';
import { AIActor } from '../enemies/AIActor';
import { CharmAI } from '../enemies/CharmAI';
import { Faction } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';

/**
 * Stackable consumable. Using charms the nearest enemy (sets Ally faction + CharmAI).
 * C# uses ITargetedAction but web port simplifies to IUsable targeting nearest enemy.
 * Port of C# ItemCharmBerry.cs.
 */
export class ItemCharmBerry extends Item implements IStackable, IUsable {
  readonly [STACKABLE_TAG] = true as const;
  readonly [USABLE_TAG] = true as const;

  readonly stacksMax = 12;
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

  constructor(stacks = 1) {
    super();
    this._stacks = stacks;
  }

  use(actor: Actor): void {
    const floor = actor.floor;
    if (!floor) return;

    // Find nearest enemy on the floor
    let nearest: AIActor | null = null;
    let bestDist = Infinity;

    for (const body of floor.bodies) {
      if (body instanceof AIActor && body.faction === Faction.Enemy) {
        const dist = Vector2Int.manhattanDistance(actor.pos, body.pos);
        if (dist < bestDist) {
          bestDist = dist;
          nearest = body;
        }
      }
    }

    if (nearest) {
      nearest.setAI(new CharmAI(nearest));
      this.stacks--;
    }
  }

  getStats(): string {
    return 'Charms the nearest enemy, making them loyal to you.';
  }
}
