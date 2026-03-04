import { AIActor } from './AIActor';
import { Actor } from '../Actor';
import { Body } from '../Body';
import { ActorTask } from '../ActorTask';
import { WaitTask } from '../tasks/WaitTask';
import { MoveToTargetTask } from '../tasks/MoveToTargetTask';
import { ExplodeTask } from '../tasks/ExplodeTask';
import { ActionCosts } from '../BaseAction';
import { ActionType, Faction } from '../../core/types';
import type { IDeathHandler } from '../../core/types';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { MyRandom } from '../../core/MyRandom';
import { Item, STACKABLE_TAG, USABLE_TAG, type IStackable, type IUsable } from '../Item';
import { Inventory } from '../Inventory';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Entity } from '../Entity';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * Neutral bug that wanders randomly. On death, leaves an explosive corpse.
 * Port of C# Boombug.cs.
 */
export class Boombug extends AIActor implements IDeathHandler {
  readonly [DEATH_HANDLER] = true;

  protected get actionCosts(): ActionCosts {
    return new ActionCosts([
      [ActionType.WAIT, 2],
      [ActionType.MOVE, 2],
      [ActionType.ATTACK, 1],
      [ActionType.GENERIC, 1],
    ]);
  }

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 1;
    this.faction = Faction.Neutral;
  }

  baseAttackDamage(): [number, number] {
    return [0, 0];
  }

  protected getNextTask(): ActorTask {
    if (MyRandom.value < 0.5) {
      return new WaitTask(this, MyRandom.Range(1, 5));
    }
    const floor = this.floor!;
    const candidates: Vector2Int[] = [];
    for (const p of floor.enumerateCircle(this.pos, 5)) {
      const t = floor.tiles.get(p);
      if (t && t.canBeOccupied()) {
        candidates.push(p);
      }
    }
    if (candidates.length > 0) {
      const target = MyRandom.Pick(candidates);
      return new MoveToTargetTask(this, target);
    }
    return new WaitTask(this, 1);
  }

  handleDeath(_source: Entity | null): void {
    const floor = this.floor;
    const pos = this.pos;
    GameModelRef.main.enqueuEvent(() => {
      if (floor) {
        floor.put(new BoombugCorpse(pos));
      }
    });
  }
}

/**
 * Explodes after one turn, dealing 3 damage to adjacent bodies.
 * Chain-explodes adjacent BoombugCorpses. Destroys adjacent grasses.
 * If killed before exploding, drops an ItemBoombugCorpse.
 * Port of C# BoombugCorpse.
 */
export class BoombugCorpse extends Actor implements IDeathHandler {
  readonly [DEATH_HANDLER] = true;

  get turnPriority(): number {
    return 30;
  }

  private exploded = false;

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 1;
    this.faction = Faction.Neutral;
    this.timeNextAction += 1;
    this.setTasks(new ExplodeTask(this));
  }

  baseAttackDamage(): [number, number] {
    return [3, 3];
  }

  handleDeath(_source: Entity | null): void {
    if (!this.exploded) {
      // Died before exploding — drop a corpse item
      const inv = new Inventory(1);
      inv.addItem(new ItemBoombugCorpse(1));
      const floor = this.floor;
      const pos = this.pos;
      GameModelRef.main.enqueuEvent(() => {
        if (floor) {
          inv.tryDropAllItems(floor, pos);
        }
      });
    }
  }

  step(): number {
    this.explode();
    return this.baseActionCost;
  }

  explode(): void {
    if (this.exploded) return;
    this.exploded = true;

    const floor = this.floor;
    if (!floor) return;

    const adjacentBoombugs: BoombugCorpse[] = [];
    for (const tile of floor.getAdjacentTiles(this.pos)) {
      const body = floor.bodies.get(tile.pos);
      if (body instanceof BoombugCorpse && body !== this) {
        adjacentBoombugs.push(body);
      } else if (body && body !== this && body instanceof Body) {
        this.attack(body);
      }
      // Destroy grasses on empty tiles or own tile
      if ((!body || Vector2Int.equals(tile.pos, this.pos)) && floor.grasses.get(tile.pos)) {
        floor.grasses.get(tile.pos)!.kill(this);
      }
    }

    GameModelRef.mainOrNull?.emitAnimation({ type: 'explosion', entityGuid: this.guid, from: this.pos });
    this.kill(this);

    GameModelRef.main.enqueuEvent(() => {
      for (const b of adjacentBoombugs) {
        if (!b.isDead) {
          b.explode();
        }
      }
    });
  }
}

/**
 * Throwable item that places a BoombugCorpse at a target position.
 * Port of C# ItemBoombugCorpse.
 */
export class ItemBoombugCorpse extends Item implements IStackable, IUsable {
  readonly [STACKABLE_TAG] = true as const;
  readonly [USABLE_TAG] = true as const;

  readonly stacksMax = 7;
  private _stacks: number;

  get stacks(): number {
    return this._stacks;
  }

  set stacks(value: number) {
    if (value < 0) throw new Error('Setting negative stack!');
    this._stacks = value;
    if (this._stacks === 0) {
      this.Destroy();
    }
  }

  constructor(stacks = 1) {
    super();
    this._stacks = stacks;
  }

  getStats(): string {
    return 'Use to place an explosive Boombug Corpse at your position.';
  }

  use(actor: Actor): void {
    if (actor.floor) {
      actor.floor.put(new BoombugCorpse(actor.pos));
      this.stacks--;
    }
  }
}

entityRegistry.register('Boombug', Boombug);
entityRegistry.register('BoombugCorpse', BoombugCorpse);
