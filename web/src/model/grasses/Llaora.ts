import { Grass } from './Grass';
import { Faction } from '../../core/types';
import { Ground } from '../Tile';
import { ConfusedStatus } from '../statuses/ConfusedStatus';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';

/**
 * You may Disperse the Llaora, confusing enemies in radius 2 for 10 turns.
 * Port of C# Llaora.cs.
 */
export class Llaora extends Grass {
  static readonly radius = 2.5;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Ground;
  }

  disperse(who: any): void {
    this.onNoteworthyAction();
    const floor = this.floor!;
    for (const pos of floor.enumerateCircle(this.pos, Llaora.radius)) {
      const body = floor.bodies.get(pos);
      if (body && 'faction' in body && (body as any).faction === Faction.Enemy) {
        (body as any).clearTasks();
        (body as any).statuses.add(new ConfusedStatus(10));
      }
    }
    this.kill(who);
  }
}

entityRegistry.register('Llaora', Llaora);
