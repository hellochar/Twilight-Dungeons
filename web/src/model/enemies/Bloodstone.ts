import { Body } from '../Body';
import { Vector2Int } from '../../core/Vector2Int';
import { GameModelRef } from '../GameModelRef';
import { BloodstoneStatus } from '../statuses/BloodstoneStatus';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Entity } from '../Entity';
import type { Player } from '../Player';

/**
 * Applies BloodstoneStatus to the player: +1 attack damage dealt and taken.
 * Destroy the Bloodstone to remove the effect.
 * Port of C# Bloodstone.cs — note: extends Body, not AIActor.
 */
export class Bloodstone extends Body {
  private status: BloodstoneStatus;

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = this._baseMaxHp = 1;
    this.status = new BloodstoneStatus(this);
  }

  protected handleEnterFloor(): void {
    super.handleEnterFloor();
    const player = GameModelRef.mainOrNull?.player;
    if (player && player.floor === this.floor) {
      player.statuses.add(this.status);
    }
    this.floor!.onEntityAdded.on(this.handleEntityAdded);
  }

  protected handleLeaveFloor(): void {
    this.floor!.onEntityAdded.off(this.handleEntityAdded);
    super.handleLeaveFloor();
    this.status.refresh();
  }

  private handleEntityAdded = (entity: Entity): void => {
    // Check if a Player entered this floor
    if (entity.constructor.name === 'Player' && 'statuses' in entity) {
      (entity as unknown as Player).statuses.add(this.status);
    }
  };
}

entityRegistry.register('Bloodstone', Bloodstone);
