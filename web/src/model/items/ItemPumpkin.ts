import { Item, EDIBLE_TAG, type IEdible } from '../Item';
import type { Actor } from '../Actor';
import { StrengthStatus } from '../statuses/StrengthStatus';
import { ItemPumpkinHelmet } from './ItemPumpkinHelmet';
import { ItemOnGround } from '../ItemOnGround';

/**
 * Edible pumpkin. Grants 4 stacks of Strength and drops a Pumpkin Helmet.
 * Port of C# ItemPumpkin.cs.
 */
export class ItemPumpkin extends Item implements IEdible {
  readonly [EDIBLE_TAG] = true as const;

  eat(actor: Actor): void {
    actor.statuses.add(new StrengthStatus(4));
    if (actor.floor) {
      const helmet = new ItemOnGround(actor.pos, new ItemPumpkinHelmet(), actor.pos);
      actor.floor.put(helmet);
    }
    this.Destroy();
  }

  getStats(): string {
    return 'Eat: Gain 4 Strength. Drops a Pumpkin Helmet.';
  }
}
