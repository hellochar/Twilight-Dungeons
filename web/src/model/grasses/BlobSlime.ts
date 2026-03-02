import { Grass } from './Grass';
import { ACTOR_ENTER_HANDLER, type IActorEnterHandler } from '../../core/types';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Actor } from '../Actor';

/**
 * Deals 1 damage to any non-Blob that walks into it. Removed on contact.
 * Port of C# BlobSlime from Blobmother.cs.
 */
export class BlobSlime extends Grass implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  handleActorEnter(actor: Actor): void {
    // Don't damage blobs or blobmother
    const name = actor.constructor.name;
    if (name === 'Blob' || name === 'Blobmother' || name === 'MiniBlob') return;
    actor.takeDamage(1, this);
    this.kill(actor);
  }
}

entityRegistry.register('BlobSlime', BlobSlime);
