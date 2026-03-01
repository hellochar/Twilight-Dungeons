import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { WaitTask } from '../tasks/WaitTask';
import { MoveBaseAction } from '../BaseAction';
import {
  ANY_DAMAGE_TAKEN_MOD,
  type IAnyDamageTakenModifier,
} from '../../core/Modifiers';
import {
  BODY_TAKE_ATTACK_DAMAGE_HANDLER,
  type IBodyTakeAttackDamageHandler,
} from '../Body';
import { Faction } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Actor } from '../Actor';

/**
 * Invulnerable. Attacking the Iron Jelly pushes it away,
 * first attacking any Creature in its way.
 * Port of C# IronJelly.cs.
 */
export class IronJelly extends AIActor implements IAnyDamageTakenModifier, IBodyTakeAttackDamageHandler {
  readonly [ANY_DAMAGE_TAKEN_MOD] = true as const;
  readonly [BODY_TAKE_ATTACK_DAMAGE_HANDLER] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
    this.faction = Faction.Neutral;
    this.hp = this._baseMaxHp = 99;
    this.clearTasks();
  }

  baseAttackDamage(): [number, number] {
    return [99, 99];
  }

  /** IAnyDamageTakenModifier — reduce all damage to 0 (invulnerable). */
  modify(_input: number): number {
    return 0;
  }

  /** When attacked, get pushed away and attack anything in the pushed-to tile. */
  handleTakeAttackDamage(_damage: number, _hp: number, source: Actor): void {
    if (!this.floor) return;
    const isAdjacent = this.isNextTo(source);
    const offset = Vector2Int.sub(source.pos, this.pos);
    const newPos = Vector2Int.sub(this.pos, offset);

    if (isAdjacent && !Vector2Int.equals(offset, Vector2Int.zero) && this.floor.inBounds(newPos)) {
      const tile = this.floor.tiles.get(newPos);
      if (!tile) return;

      // Attack whatever is in the way
      if (tile.body) {
        this.attack(tile.body as import('../Body').Body);
      }

      // Move to the new position if it's free
      if (tile.canBeOccupied()) {
        this.pos = tile.pos;
        this.perform(new MoveBaseAction(this, tile.pos));
      }
    }
  }

  protected getNextTask(): ActorTask {
    return new WaitTask(this, 1);
  }
}

entityRegistry.register('IronJelly', IronJelly);
