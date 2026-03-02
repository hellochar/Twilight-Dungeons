import { Item, STACKABLE_TAG, TARGETED_ACTION_TAG, type IStackable, type ITargetedAction } from '../Item';
import type { Entity } from '../Entity';
import type { Player } from '../Player';
import { AIActor } from '../enemies/AIActor';
import { CharmAI } from '../enemies/CharmAI';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { GenericPlayerTask } from '../tasks/GenericTask';
import { Faction } from '../../core/types';

/**
 * Stackable consumable. Player selects an enemy to charm via ITargetedAction UI.
 * Charmed enemies become allies with CharmAI.
 * Port of C# ItemCharmBerry.cs.
 */
export class ItemCharmBerry extends Item implements IStackable, ITargetedAction {
  readonly [STACKABLE_TAG] = true as const;
  readonly [TARGETED_ACTION_TAG] = true as const;
  readonly targetedActionName = 'Charm';

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

  targets(player: Player): Entity[] {
    const floor = player.floor;
    if (!floor) return [];
    const result: Entity[] = [];
    for (const body of floor.bodies) {
      if (body instanceof AIActor && body.faction === Faction.Enemy) {
        result.push(body);
      }
    }
    return result;
  }

  performTargetedAction(player: Player, target: Entity): void {
    const actor = target as AIActor;
    player.setTasks(
      new ChaseTargetTask(player, actor),
      new GenericPlayerTask(player, () => {
        actor.setAI(new CharmAI(actor));
        this.stacks--;
      }),
    );
  }

  getStats(): string {
    return 'Makes a target loyal to you; they will follow you and attack nearby enemies.';
  }
}
