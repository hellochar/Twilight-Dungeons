import { AIActor } from '../AIActor';
import { ActorTask } from '../../ActorTask';
import { WaitTask } from '../../tasks/WaitTask';
import { TelegraphedTask } from '../../tasks/TelegraphedTask';
import { GenericBaseAction } from '../../BaseAction';
import { Faction } from '../../../core/types';
import type { INoTurnDelay } from '../../../core/types';
import { Vector2Int } from '../../../core/Vector2Int';
import { GameModelRef } from '../../GameModelRef';
import { entityRegistry } from '../../../generator/entityRegistry';
import { Player } from '../../Player';
// import { FungalInfectionStatus } from '../../statuses/InfectedStatus';
import type { Actor } from '../../Actor';

/**
 * Neutral fungus that periodically sprays adjacent creatures.
 * Non-player creatures hit by the spray are instantly converted into FruitingBodies.
 * The player instead gains an Infected stack. At 4 stacks, the player dies.
 */
export class FruitingBody extends AIActor implements INoTurnDelay {
  readonly noTurnDelay = true as const;
  override get isStationary() { return true; }

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 1;
    this.faction = Faction.Neutral;
    this.clearTasks();
  }

  baseAttackDamage(): [number, number] {
    return [0, 0];
  }

  protected getNextTask(): ActorTask {
    const player = GameModelRef.main.player;
    if (player.isNextTo(this)) {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, () => this.spray()));
    }
    return new WaitTask(this, 1);
  }

  private spray(): void {
    const floor = this.floor;
    if (!floor) return;

    const adjacentActors = floor.adjacentActors(this.pos);

    for (const entity of adjacentActors) {
      const actor = entity as Actor;
      if (actor instanceof FruitingBody) continue;

      // if (actor instanceof Player) {
      //   actor.statuses.add(new FungalInfectionStatus(1));
      // } else {
        // Convert non-player creature into a new FruitingBody
        const pos = actor.pos;
        actor.kill(this);
        floor.put(new FruitingBody(pos));
      // }
    }

    GameModelRef.main.emitAnimation({ type: 'spray', entityGuid: this.guid, from: this.pos, color: 0x8634FE });
    this.killSelf();
  }
}

entityRegistry.register('FruitingBody', FruitingBody);
