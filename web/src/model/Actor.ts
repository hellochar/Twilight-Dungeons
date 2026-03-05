import { Body } from './Body';
import { Vector2Int } from '../core/Vector2Int';
import {
  ActionType,
  Faction,
} from '../core/types';
import {
  collectModifiers,
  processModifiers,
  ACTION_COST_MOD,
  BASE_ACTION_MOD,
  ATTACK_DAMAGE_MOD,
  STEP_MOD,
  MAX_HP_MOD,
  type IActionCostModifier,
  type IBaseActionModifier,
  type IAttackDamageModifier,
  type IStepModifier,
  type IMaxHPModifier,
} from '../core/Modifiers';
import { MyRandom } from '../core/MyRandom';
import { EventEmitter } from '../core/EventEmitter';
import { BaseAction, ActionCosts } from './BaseAction';
import { GameModelRef } from './GameModelRef';
import { ActorTask, TaskStage } from './ActorTask';
import { StatusList } from './StatusList';
import type { ISteppable } from './Floor';

// ─── Handler symbols ───
export const ATTACK_HANDLER = Symbol.for('IAttackHandler');
export const DEAL_ATTACK_DAMAGE_HANDLER = Symbol.for('IDealAttackDamageHandler');
export const ACTION_PERFORMED_HANDLER = Symbol.for('IActionPerformedHandler');
export const ACTOR_KILLED_HANDLER = Symbol.for('IActorKilledHandler');
export const STATUS_ADDED_HANDLER = Symbol.for('IStatusAddedHandler');
export const STATUS_REMOVED_HANDLER = Symbol.for('IStatusRemovedHandler');

export interface IAttackHandler {
  onAttack(damage: number, target: Body): void;
}
export interface IDealAttackDamageHandler {
  handleDealAttackDamage(damage: number, target: Body): void;
}
export interface IActionPerformedHandler {
  handleActionPerformed(finalAction: BaseAction, initialAction: BaseAction): void;
}

export class NoActionException extends Error {
  constructor() {
    super('No action available');
    this.name = 'NoActionException';
  }
}

export class ActorDiedException extends Error {
  constructor() {
    super('Actor died during step');
    this.name = 'ActorDiedException';
  }
}

/**
 * Body with tasks, statuses, combat, and turn-stepping.
 * Port of C# Actor.cs.
 */
export class Actor extends Body implements ISteppable {
  timeNextAction: number;
  get turnPriority(): number {
    return 50;
  }

  readonly statuses: StatusList;
  faction: Faction = Faction.Neutral;

  protected taskQueue: ActorTask[] = [];

  readonly onSetTask = new EventEmitter<[ActorTask | null]>();
  readonly onAttackGround = new EventEmitter<[Vector2Int]>();
  readonly afterActionPerformed = new EventEmitter<[BaseAction, BaseAction]>();

  get myModifiers(): Iterable<object | null | undefined> {
    return [...super.myModifiers, ...this.statuses.list, this.task ?? null];
  }

  get maxHp(): number {
    const mods = collectModifiers<IMaxHPModifier>(this, MAX_HP_MOD);
    return processModifiers(mods, this._baseMaxHp);
  }

  /** Stationary actors never move — suppresses idle bob animation. Override to true in subclasses. */
  get isStationary(): boolean {
    return false;
  }

  protected get actionCosts(): ActionCosts {
    return ActionCosts.default();
  }

  get baseActionCost(): number {
    return this.getActionCost(ActionType.WAIT);
  }

  get task(): ActorTask | null {
    return this.taskQueue[0] ?? null;
  }

  set task(value: ActorTask | null) {
    if (value) {
      this.setTasks(value);
    } else {
      this.clearTasks();
    }
  }

  get tasks(): readonly ActorTask[] {
    return this.taskQueue;
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this.statuses = new StatusList(this);
    this._hp = this._baseMaxHp = 8;
    this.timeNextAction = this.timeCreated;
  }

  /** Attack the target with the specified raw damage (no modifiers). */
  attackWithDamage(target: Body, damage: number): void {
    if (target.isDead) {
      throw new Error('Cannot attack dead target.');
    }
    GameModelRef.mainOrNull?.emitAnimation({ type: 'attack', entityGuid: this.guid, from: this.pos, to: target.pos, targetGuid: target.guid, amount: damage });
    this.onAttackEvent(damage, target);
    target.attacked(damage, this);
  }

  /** Attack the target, using this Actor's final attack damage. */
  attack(target: Body): void {
    this.attackWithDamage(target, this.getFinalAttackDamage());
  }

  /** Get base attack damage range [min, max]. Override in subclasses. */
  baseAttackDamage(): [number, number] {
    return [0, 0];
  }

  getFinalAttackDamage(): number {
    const [min, max] = this.baseAttackDamage();
    const baseDamage = MyRandom.Range(min, max + 1);
    const mods = collectModifiers<IAttackDamageModifier>(this, ATTACK_DAMAGE_MOD);
    return processModifiers(mods, baseDamage);
  }

  attackGround(targetPosition: Vector2Int): void {
    const target = this.floor?.bodies.get(targetPosition);
    const grass = this.floor?.grasses.get(targetPosition);
    // Suppress attackGround event when hitting a body — the 'attack' event from this.attack() handles the bump.
    if (!target) {
      GameModelRef.mainOrNull?.emitAnimation({ type: 'attackGround', entityGuid: this.guid, from: this.pos, to: targetPosition });
    }
    this.onAttackGround.emit(targetPosition);
    if (target) {
      this.attack(target as Body);
    } else if (grass) {
      grass.kill(this);
    }
  }

  kill(source: Entity): void {
    if (!this.isDead) {
      this.taskQueue.length = 0;
      this.taskChanged();
      const mods = collectModifiers<{ onKilled(a: Actor): void }>(this, ACTOR_KILLED_HANDLER);
      for (const handler of [...mods]) {
        handler.onKilled(this);
      }
      super.kill(source);
    }
  }

  clearTasks(): void {
    this.setTasks();
  }

  setTasks(...tasks: ActorTask[]): void {
    this.taskQueue.length = 0;
    this.taskQueue.push(...tasks);
    this.taskChanged();
  }

  insertTasks(...tasks: ActorTask[]): void {
    this.taskQueue.unshift(...tasks);
    this.taskChanged();
  }

  protected taskChanged(): void {
    this.onSetTask.emit(this.task);
  }

  /** Get action cost with modifiers applied. */
  getActionCost(t: ActionType): number {
    const mods = collectModifiers<IActionCostModifier>(this, ACTION_COST_MOD);
    const costs = processModifiers(mods, this.actionCosts.copy());
    return costs.get(t) ?? 1;
  }

  getActionCostForAction(action: BaseAction): number {
    return this.getActionCost(action.type);
  }

  step(): number {
    if (!this.task) throw new NoActionException();

    this.task.preStep();

    // Clear out all done tasks
    while (this.task?.isDone()) {
      this.goToNextTask();
      if (!this.task) throw new NoActionException();
      this.task.preStep();
    }

    const isFree = this.task.isFreeTask;
    const action = this.task.getNextAction();
    const finalAction = this.perform(action);

    if (this.isDead) throw new ActorDiedException();

    // Process step modifiers (status effects etc.)
    const stepMods = collectModifiers<IStepModifier>(this, STEP_MOD);
    processModifiers(stepMods, {} as object);

    this.task?.postStep(action, finalAction);

    // Handle close-ended tasks
    while (
      this.task &&
      (this.task.whenToCheckIsDone & TaskStage.After) !== 0 &&
      !this.task.forceOnlyCheckBefore &&
      this.task.isDone()
    ) {
      this.goToNextTask();
    }

    if (isFree) return 0;
    return this.getActionCostForAction(finalAction);
  }

  perform(action: BaseAction): BaseAction {
    const mods = collectModifiers<IBaseActionModifier>(this, BASE_ACTION_MOD);
    const finalAction = processModifiers(mods, action);
    finalAction.perform();
    this.onActionPerformed(finalAction, action);
    return finalAction;
  }

  goToNextTask(): void {
    this.task?.ended();
    this.taskQueue.shift();
    this.taskChanged();
  }

  /** Called when this actor deals attack damage to a target. */
  onDealAttackDamage(damage: number, target: Body): void {
    const handlers = collectModifiers<IDealAttackDamageHandler>(this, DEAL_ATTACK_DAMAGE_HANDLER);
    for (const handler of handlers) {
      handler.handleDealAttackDamage(damage, target);
    }
  }

  canTargetPlayer(): boolean {
    return this.isVisible;
  }

  toString(): string {
    return `${super.toString()}, HP ${this._hp}/${this.maxHp}`;
  }

  // ─── Private event dispatchers ───

  private onAttackEvent(damage: number, target: Body): void {
    const handlers = collectModifiers<IAttackHandler>(this, ATTACK_HANDLER);
    for (const handler of handlers) {
      handler.onAttack(damage, target);
    }
  }

  private onActionPerformed(finalAction: BaseAction, initialAction: BaseAction): void {
    const handlers = collectModifiers<IActionPerformedHandler>(this, ACTION_PERFORMED_HANDLER);
    for (const handler of handlers) {
      handler.handleActionPerformed(finalAction, initialAction);
    }
    this.afterActionPerformed.emit(finalAction, initialAction);
  }

  // ISteppable
  catchUpStep(_lastTime: number, _currentTime: number): void {}
}

// Re-export for convenience
import { Entity } from './Entity';
