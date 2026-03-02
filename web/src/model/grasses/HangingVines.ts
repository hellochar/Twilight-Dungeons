import { Grass } from './Grass';
import { Trigger } from '../Trigger';
import { Inventory } from '../Inventory';
import { ConstrictedStatus } from '../statuses/ConstrictedStatus';
import { ItemVineWhip } from '../items/ItemVineWhip';
import { Vector2Int } from '../../core/Vector2Int';
import { entityRegistry } from '../../generator/entityRegistry';
import type { IDeathHandler } from '../../core/types';
import type { Entity } from '../Entity';
import type { Actor } from '../Actor';
import type { Tile } from '../Tile';
import { Wall } from '../Tile';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * Constricts any creature that walks into its hook (tile below).
 * Destroy the wall it's attached to (or break free) to remove it.
 * Port of C# HangingVines.cs.
 */
export class HangingVines extends Grass implements IDeathHandler {
  readonly [DEATH_HANDLER] = true as const;

  private inventory = new Inventory(1);
  private triggerBelow: Trigger | null = null;
  private appliedStatus: ConstrictedStatus | null = null;

  constructor(pos: Vector2Int) {
    super(pos);
    // Pre-load the vine whip into the internal inventory
    this.inventory.addItem(new ItemVineWhip(1));
  }

  static canOccupy(tile: Tile): boolean {
    return tile instanceof Wall;
  }

  private get tileBelow(): Tile | null {
    return this.floor?.tiles.get(Vector2Int.add(this.pos, Vector2Int.down)) ?? null;
  }

  protected handleEnterFloor(): void {
    const below = this.tileBelow;
    if (below) {
      this.triggerBelow = new Trigger(below.pos, (who: Actor) => this.handleActorEnterBelow(who));
      this.floor!.put(this.triggerBelow);
    }
  }

  protected handleLeaveFloor(): void {
    if (this.triggerBelow && this.floor) {
      this.floor.remove(this.triggerBelow);
      this.triggerBelow = null;
    }
  }

  handleDeath(_source: Entity): void {
    const below = this.tileBelow;
    if (below) {
      this.inventory.tryDropAllItems(this.floor!, below.pos);
    }
    if (this.appliedStatus) {
      this.appliedStatus.Remove();
    }
  }

  private handleActorEnterBelow(who: Actor): void {
    this.appliedStatus = new ConstrictedStatus(this);
    who.statuses.add(this.appliedStatus);
    this.onNoteworthyAction();
  }

  constrictedStatusEnded(): void {
    const actor = this.appliedStatus?.actor;
    this.appliedStatus = null;
    if (actor) {
      this.kill(actor as any);
    }
  }

  constrictedCreatureDied(): void {
    this.appliedStatus = null;
  }
}

entityRegistry.register('HangingVines', HangingVines);
