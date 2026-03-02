import { Grass } from './Grass';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler, type IDeathHandler } from '../../core/types';
import { GameModelRef } from '../GameModelRef';
import { ItemDeathbloomFlower } from '../items/ItemDeathbloomFlower';
import { ItemOnGround } from '../ItemOnGround';
import { Ground } from '../Tile';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';
import type { Entity } from '../Entity';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * Blooms when an adjacent creature dies. Walk over a bloomed Deathbloom to obtain a flower.
 * Port of C# Deathbloom.cs.
 */
export class Deathbloom extends Grass implements IActorEnterHandler, IDeathHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;
  readonly [DEATH_HANDLER] = true as const;

  isBloomed = false;
  onBloomed: (() => void) | null = null;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  static canOccupy(tile: Tile): boolean {
    if (!(tile instanceof Ground)) return false;
    if (tile.grass != null) return false;
    // No adjacent Deathbloom
    const floor = tile.floor!;
    return !floor.getAdjacentTiles(tile.pos).some(
      t => t.grass instanceof Deathbloom
    );
  }

  private entityRemovedHandler = (entity: Entity) => {
    if ('faction' in entity && (entity as any).isDead && (entity as any).isNextTo(this)) {
      // Don't bloom from BoombugCorpse
      if (entity.constructor.name === 'BoombugCorpse') return;
      this.isBloomed = true;
      this.onBloomed?.();
    }
  };

  protected handleEnterFloor(): void {
    this.floor!.onEntityRemoved.on(this.entityRemovedHandler);
  }

  protected handleLeaveFloor(): void {
    this.floor!.onEntityRemoved.off(this.entityRemovedHandler);
  }

  handleActorEnter(actor: any): void {
    if (this.isBloomed && actor.constructor.name === 'Player') {
      this.kill(actor);
    }
  }

  handleDeath(_source: Entity): void {
    if (this.isBloomed) {
      const player = GameModelRef.main.player;
      if (player.pos.x === this.pos.x && player.pos.y === this.pos.y) {
        const item = new ItemDeathbloomFlower();
        if (!player.inventory.addItem(item, this)) {
          this.floor?.put(new ItemOnGround(this.pos, item, this.pos));
        }
      }
    }
  }
}

entityRegistry.register('Deathbloom', Deathbloom);
