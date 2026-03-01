import { AIActor } from './AIActor';
import { ActorTask } from '../ActorTask';
import { ChaseTargetTask } from '../tasks/ChaseTargetTask';
import { MoveRandomlyTask } from '../tasks/MoveRandomlyTask';
import { RunAwayTask } from '../tasks/RunAwayTask';
import { WaitTask } from '../tasks/WaitTask';
import { TelegraphedTask } from '../tasks/TelegraphedTask';
import { GenericBaseAction, ActionCosts } from '../BaseAction';
import { SurprisedStatus } from '../tasks/SleepTask';
import { Vector2Int } from '../../core/Vector2Int';
import { ActionType } from '../../core/types';
import { GameModelRef } from '../GameModelRef';
import { TAKE_ANY_DAMAGE_HANDLER, type ITakeAnyDamageHandler } from '../Body';
import { entityRegistry } from '../../generator/entityRegistry';

/**
 * Moves slowly. Summons Brambles around the player (needs vision).
 * Interrupted when taking damage.
 * Port of C# Thistlebog.cs.
 */
export class Thistlebog extends AIActor implements ITakeAnyDamageHandler {
  readonly [TAKE_ANY_DAMAGE_HANDLER] = true as const;
  private cooldown = 0;

  get turnPriority(): number {
    return 50;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 10;
  }

  protected get actionCosts(): ActionCosts {
    const costs = ActionCosts.default();
    costs.set(ActionType.MOVE, 2);
    return costs;
  }

  baseAttackDamage(): [number, number] {
    return [0, 0];
  }

  step(): number {
    const dt = super.step();
    if (this.cooldown > 0) {
      this.cooldown -= dt;
    }
    return dt;
  }

  handleTakeAnyDamage(damage: number): void {
    if (damage > 0 && this.task instanceof TelegraphedTask) {
      this.clearTasks();
      this.setTasks(new WaitTask(this, 1));
      this.statuses.add(new SurprisedStatus());
    }
  }

  protected getNextTask(): ActorTask {
    if (!this.canTargetPlayer()) {
      return new MoveRandomlyTask(this);
    }
    const player = GameModelRef.main.player;
    if (this.isNextTo(player)) {
      return new RunAwayTask(this, player.pos, 1, false);
    }
    if (this.cooldown > 0) {
      return new MoveRandomlyTask(this);
    }
    // Diamond distance to player
    const dx = Math.abs(this.pos.x - player.pos.x);
    const dy = Math.abs(this.pos.y - player.pos.y);
    if (dx + dy <= 2) {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, () => this.summonBramblesAroundPlayer()));
    }
    const chase = new ChaseTargetTask(this, player, 1);
    chase.setMaxMoves(1);
    return chase;
  }

  private summonBramblesAroundPlayer(): void {
    if (!this.canTargetPlayer() || this.cooldown > 0) return;
    this.cooldown = 10;
    // TODO: Summon Brambles grass around player when Brambles is ported
    // For now, this is a stub. When Brambles exists:
    // const center = GameModelRef.main.player.pos;
    // for (const tile of this.floor!.getAdjacentTiles(center)) {
    //   if (Brambles.canOccupy(tile) && !tile.pos.equals(center)) {
    //     const brambles = new Brambles(tile.pos);
    //     this.floor!.put(brambles);
    //     brambles.addTimedEvent(10, brambles.killSelf);
    //   }
    // }
  }
}

entityRegistry.register('Thistlebog', Thistlebog);
