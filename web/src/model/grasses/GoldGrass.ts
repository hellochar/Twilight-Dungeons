import { Grass } from './Grass';
import { ACTOR_ENTER_HANDLER, ActionType, type IActorEnterHandler } from '../../core/types';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';
import { ACTION_COST_MOD, type IActionCostModifier } from '../../core/Modifiers';
import { Status } from '../Status';
import type { ActionCosts } from '../BaseAction';
import type { Vector2Int } from '../../core/Vector2Int';

/**
 * All player movement is a free move over Gold Grass.
 * Port of C# GoldGrass from SoftGrass.cs.
 */
export class GoldGrass extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  handleActorEnter(who: any): void {
    const player = GameModelRef.mainOrNull?.player;
    if (who === player) {
      player!.statuses.add(new GoldGrassStatus());
      this.onNoteworthyAction();
    }
  }
}

entityRegistry.register('GoldGrass', GoldGrass);

/**
 * Movement over Gold Grass is free (MOVE costs 0).
 * Removed when actor leaves Gold Grass.
 * Port of C# GoldGrassStatus from SoftGrass.cs.
 */
export class GoldGrassStatus extends Status implements IActionCostModifier {
  readonly [ACTION_COST_MOD] = true as const;

  Consume(_other: Status): boolean {
    return true;
  }

  info(): string {
    return 'Movement over Gold Grass is free.';
  }

  Step(): void {
    if (this.actor?.grass?.constructor.name !== 'GoldGrass') {
      this.Remove();
    }
  }

  modify(input: any): any {
    if (input instanceof Map) {
      const costs = input as ActionCosts;
      costs.set(ActionType.MOVE, 0);
      return costs;
    }
    return super.modify(input);
  }
}
