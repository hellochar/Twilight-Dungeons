import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { WaitTask } from '../tasks/WaitTask';
import { TelegraphedTask } from '../tasks/TelegraphedTask';
import { GenericBaseAction, WaitBaseAction, type BaseAction } from '../BaseAction';
import { ClumpedLungStatus } from '../statuses/ClumpedLungStatus';
import { BASE_ACTION_MOD, type IBaseActionModifier } from '../../core/Modifiers';
import { ActionType, type INoTurnDelay, type IDeathHandler } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { MyRandom } from '../../core/MyRandom';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Entity } from '../Entity';
import type { Actor } from '../Actor';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * After 5-7 turns, splits into 2-3 Clumpshrooms in adjacent tiles, then dies.
 * Does not attack or move. On death, applies Clumped Lung to the killer.
 * Port of C# Clumpshroom.cs.
 */
export class Clumpshroom extends AIActor implements IBaseActionModifier, INoTurnDelay, IDeathHandler {
  readonly [BASE_ACTION_MOD] = true as const;
  readonly noTurnDelay = true as const;
  readonly [DEATH_HANDLER] = true as const;
  override get isStationary() { return true; }

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = 1;
    this._baseMaxHp = 1;
    this.setTasks(new WaitTask(this, MyRandom.Range(5, 8)));
  }

  protected getNextTask(): ActorTask {
    return new TelegraphedTask(this, 1, new GenericBaseAction(this, () => this.duplicate()));
  }

  private duplicate(): void {
    const adjacent = this.floor!
      .getAdjacentTiles(this.pos)
      .filter(t => t.canBeOccupied() && !Vector2Int.equals(t.pos, this.pos));
    MyRandom.Shuffle(adjacent);
    const count = Math.min(MyRandom.Range(2, 4), adjacent.length);
    const newShrooms = adjacent.slice(0, count).map(t => new Clumpshroom(t.pos));
    this.floor!.putAll(newShrooms);
    this.killSelf();
  }

  handleDeath(source: Entity): void {
    if (source && 'statuses' in source) {
      (source as Actor).statuses.add(new ClumpedLungStatus());
    }
  }

  /** IBaseActionModifier — cannot move or attack. */
  modify(input: BaseAction): BaseAction {
    if (input.type === ActionType.MOVE || input.type === ActionType.ATTACK) {
      return new WaitBaseAction(this);
    }
    return input;
  }

  baseAttackDamage(): [number, number] {
    return [0, 0];
  }
}

entityRegistry.register('Clumpshroom', Clumpshroom);
