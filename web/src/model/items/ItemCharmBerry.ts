import { Item, STACKABLE_TAG, USABLE_TAG, type IStackable, type IUsable } from '../Item';
import type { Actor } from '../Actor';
import type { AIActor } from '../enemies/AIActor';
import { CharmedStatus } from '../statuses/CharmedStatus';
import { Faction } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';

/**
 * Stackable consumable. Using charms the nearest enemy (sets Ally faction + CharmedStatus).
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
    // Find nearest enemy on the floor
    const floor = actor.floor;
    if (!floor) return;

    let nearest: AIActor | null = null;
    let bestDist = Infinity;

    for (const entity of floor.entities) {
      if ('faction' in entity && (entity as any).faction === Faction.Enemy) {
        const dist = Vector2Int.manhattanDistance(actor.pos, entity.pos);
        if (dist < bestDist) {
          bestDist = dist;
          nearest = entity as AIActor;
        }
      }
    }

    if (nearest) {
      nearest.faction = Faction.Ally;
      nearest.statuses.add(new CharmedStatus());
      this.stacks--;
    }
  }

  getStats(): string {
    return 'Charms the nearest enemy, making them loyal to you.';
  }
}
