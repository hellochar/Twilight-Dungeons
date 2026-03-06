import { Grass } from './Grass';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { Ground } from '../Tile';
import { PacifiedStatus } from '../statuses/PacifiedStatus';
import { entityRegistry } from '../../generator/entityRegistry';
import { Actor } from '../Actor';
import type { ISteppable } from '../Floor';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';

/**
 * Alternately opens and closes every 12 turns.
 * While open, Pacifies the creature standing over it.
 * Port of C# Violets.cs.
 */
export class Violets extends Grass implements IActorEnterHandler, ISteppable {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  static readonly turnsToChange = 12;
  timeNextAction: number;
  get turnPriority(): number { return 20; }
  isOpen = false;
  countUp: number;

  constructor(pos: Vector2Int) {
    super(pos);
    this.timeNextAction = this.timeCreated + 1;
    this.countUp = Math.floor(Math.random() * 4);
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground;
  }

  /** Actor standing on this grass's tile (null if body is not an Actor). */
  private get actor(): Actor | null {
    const body = this.floor?.bodies.get(this.pos);
    return body instanceof Actor ? body : null;
  }

  handleActorEnter(_who: any): void {
    if (this.isOpen) {
      this.actor?.statuses.add(new PacifiedStatus());
    }
  }

  step(): number {
    this.countUp++;
    if (this.countUp >= Violets.turnsToChange) {
      this.isOpen = !this.isOpen;
      this.countUp = 0;
    }
    if (this.isOpen) {
      this.actor?.statuses.add(new PacifiedStatus());
    }
    return 1;
  }
}

entityRegistry.register('Violets', Violets);
