import { Grass } from './Grass';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { Ground } from '../Tile';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';

/**
 * Take 1 attack damage when walking into Brambles.
 * Port of C# Brambles.cs.
 */
export class Brambles extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground;
  }

  handleActorEnter(actor: any): void {
    actor.takeAttackDamage(1, actor);
    this.onNoteworthyAction();
  }
}

entityRegistry.register('Brambles', Brambles);
