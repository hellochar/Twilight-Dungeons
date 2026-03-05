import { AIActor } from './AIActor';
import { Actor } from '../Actor';
import { Body } from '../Body';
import { ActorTask } from '../ActorTask';
import { WaitTask } from '../tasks/WaitTask';
import { GenericTask } from '../tasks/GenericTask';
import { WaitBaseAction } from '../BaseAction';
import { BASE_ACTION_MOD, type IBaseActionModifier } from '../../core/Modifiers';
import { ActionType, Faction } from '../../core/types';
import type { IDeathHandler } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { SurprisedStatus } from '../tasks/SleepTask';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Entity } from '../Entity';
import type { Tile } from '../Tile';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * Shoots tendrils toward the player. If 3+ contiguous tendrils surround
 * the player, deals 3 attack damage.
 * Port of C# Grasper.cs.
 */
export class Grasper extends AIActor implements IBaseActionModifier {
  readonly [BASE_ACTION_MOD] = true as const;
  override get isStationary() { return true; }

  readonly tendrils: Tendril[] = [];

  constructor(pos: Vector2Int) {
    super(pos);
    this.faction = Faction.Enemy;
    this._hp = this._baseMaxHp = 6;
  }

  baseAttackDamage(): [number, number] {
    return [0, 0];
  }

  protected getNextTask(): ActorTask {
    const surroundingTendrils = this.getContiguousTendrilsSurroundingPlayer(3);
    if (surroundingTendrils) {
      return new GenericTask(this, () => this.damagePlayer());
    }
    if (!this.getNextTendrilTile()) {
      return new WaitTask(this, 1);
    }
    return new GenericTask(this, () => this.spawnTendril());
  }

  private getContiguousTendrilsSurroundingPlayer(minLength: number): Tendril[] | null {
    for (let i = 0; i < this.tendrils.length; i++) {
      const run = this.findContiguousRunSurroundingPlayer(i);
      if (run.length >= minLength) {
        return run;
      }
    }
    return null;
  }

  private findContiguousRunSurroundingPlayer(startIndex: number): Tendril[] {
    const player = GameModelRef.main.player;
    const list: Tendril[] = [];
    let index = startIndex;
    while (index < this.tendrils.length && this.tendrils[index].isNextTo(player)) {
      list.push(this.tendrils[index]);
      index++;
    }
    return list;
  }

  private damagePlayer(): void {
    const player = GameModelRef.main.player;
    (player as Body).takeAttackDamage(3, this);
  }

  private getNextTendrilTile(): Tile | null {
    const floor = this.floor;
    if (!floor) return null;
    const lastPos = this.tendrils.length > 0
      ? this.tendrils[this.tendrils.length - 1].pos
      : this.pos;
    const target = GameModelRef.main.player.pos;

    const candidates = floor.getCardinalNeighbors(lastPos)
      .filter(t => t.canBeOccupied());
    candidates.sort((a, b) =>
      Vector2Int.distance(a.pos, target) - Vector2Int.distance(b.pos, target)
    );
    return candidates[0] ?? null;
  }

  private spawnTendril(): void {
    const tile = this.getNextTendrilTile();
    if (tile) {
      const tendril = new Tendril(tile.pos, this);
      this.tendrils.push(tendril);
      this.floor!.put(tendril);
      tendril.clearTasks();
    }
  }

  /** Called by Tendril on death. Can be called after Grasper is dead. */
  tendrilDied(tendril: Tendril, source: Entity): void {
    if (source === this) return;

    if (!this.isDead) {
      this.statuses.add(new SurprisedStatus());
    }

    const index = this.tendrils.indexOf(tendril);
    if (index === -1) return;

    // Kill all tendrils from index onward
    const toKill = this.tendrils.slice(index);
    for (const t of toKill) {
      if (!t.isDead) {
        t.kill(this);
      }
    }
    this.tendrils.splice(index, this.tendrils.length - index);
  }

  modify(input: any): any {
    if (input != null && typeof input === 'object' && 'type' in input) {
      if (input.type === ActionType.ATTACK || input.type === ActionType.MOVE) {
        return new WaitBaseAction(input.actor);
      }
    }
    return input;
  }
}

/**
 * A segment of the Grasper's tendril chain. Neutral, 3 HP.
 * Cannot move or attack. Signals parent on death; killing one kills
 * all descendant tendrils.
 * Port of C# Tendril.
 */
export class Tendril extends Actor implements IBaseActionModifier, IDeathHandler {
  readonly [BASE_ACTION_MOD] = true as const;
  readonly [DEATH_HANDLER] = true;
  override get isStationary() { return true; }

  readonly owner: Grasper;

  constructor(pos: Vector2Int, owner: Grasper) {
    super(pos);
    this.owner = owner;
    this.faction = Faction.Neutral;
    this._hp = this._baseMaxHp = 3;
    this.timeNextAction += 999999;
  }

  step(): number {
    return 999999;
  }

  baseAttackDamage(): [number, number] {
    return [0, 0];
  }

  handleDeath(source: Entity | null): void {
    this.owner.tendrilDied(this, source ?? this);
  }

  modify(input: any): any {
    if (input != null && typeof input === 'object' && 'type' in input) {
      if (input.type === ActionType.ATTACK || input.type === ActionType.MOVE) {
        return new WaitBaseAction(input.actor);
      }
    }
    return input;
  }

  /**
   * Sprite variant key for this tendril segment.
   * Port of TendrilController.UpdateSprite() sprite selection logic.
   */
  get spriteKey(): string {
    const tendrils = this.owner.tendrils;
    const index = tendrils.indexOf(this);
    const nextPos = index >= tendrils.length - 1 ? null : tendrils[index + 1]?.pos ?? null;
    if (!nextPos) return 'monochrome-thick-path-end';

    const prevPos = index === 0 ? this.owner.pos : tendrils[index - 1].pos;
    const prevToMe = { x: this.pos.x - prevPos.x, y: this.pos.y - prevPos.y };
    const meToNext = { x: nextPos.x - this.pos.x, y: nextPos.y - this.pos.y };
    return (prevToMe.x === meToNext.x && prevToMe.y === meToNext.y)
      ? 'monochrome-thick-path'
      : 'monochrome-thick-path-angled';
  }

  /**
   * Rotation in degrees (PixiJS CW-positive, Y-down) for this tendril segment.
   * Port of TendrilController.UpdateSprite() rotation logic.
   */
  get angle(): number {
    const tendrils = this.owner.tendrils;
    const index = tendrils.indexOf(this);
    const prevPos = index === 0 ? this.owner.pos : tendrils[index - 1].pos;
    const prevToMe = { x: this.pos.x - prevPos.x, y: this.pos.y - prevPos.y };
    const angleBase = Math.atan2(prevToMe.x, prevToMe.y) * (180 / Math.PI);

    const nextPos = index >= tendrils.length - 1 ? null : tendrils[index + 1]?.pos ?? null;
    if (!nextPos) return angleBase;

    const meToNext = { x: nextPos.x - this.pos.x, y: nextPos.y - this.pos.y };
    if (prevToMe.x === meToNext.x && prevToMe.y === meToNext.y) return angleBase;

    // Angled sprite goes bottom→right by default.
    // spriteTurnDir = -Perpendicular(prevToMe) = (prevToMe.y, -prevToMe.x) = right-turn direction
    const spriteTurnX = prevToMe.y;
    const spriteTurnY = -prevToMe.x;
    return (meToNext.x === spriteTurnX && meToNext.y === spriteTurnY)
      ? angleBase       // right turn: sprite already aligned
      : angleBase - 90; // left turn: -90° offset via sprite symmetry
  }
}

entityRegistry.register('Grasper', Grasper);
entityRegistry.register('Tendril', Tendril);
