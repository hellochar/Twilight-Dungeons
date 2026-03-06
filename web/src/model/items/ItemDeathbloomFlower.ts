import { Item, EDIBLE_TAG, type IEdible } from '../Item';
import type { Actor } from '../Actor';
import { GameModelRef } from '../GameModelRef';
import { WeaknessStatus } from '../statuses/WeaknessStatus';
import { FrenziedStatus } from '../statuses/FrenziedStatus';

/**
 * Edible flower that removes Weakness and grants a stack of Frenzied.
 * Port of C# ItemDeathbloomFlower from Deathbloom.cs.
 */
export class ItemDeathbloomFlower extends Item implements IEdible {
  readonly [EDIBLE_TAG] = true as const;

  eat(actor: Actor): void {
    const weakness = actor.statuses.findOfType(WeaknessStatus);
    if (weakness) {
      weakness.Remove();
      GameModelRef.main.drainEventQueue();
    }
    actor.statuses.add(new FrenziedStatus(1));
    this.Destroy();
  }

  getStats(): string {
    return 'Eat: Remove Weakness, gain 3 Frenzied.';
  }
}
