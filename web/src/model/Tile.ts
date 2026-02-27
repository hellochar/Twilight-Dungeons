import { Entity } from './Entity';
import { Vector2Int } from '../core/Vector2Int';
import { TileVisibility, CollisionLayer, type IBlocksMovement, type IActorEnterHandler, type IActorLeaveHandler, ACTOR_ENTER_HANDLER, ACTOR_LEAVE_HANDLER } from '../core/types';
import { collectModifiers } from '../core/Modifiers';
import { GameModelRef } from './GameModelRef';

/** Implement on Body or Grass to block line-of-sight */
export interface IBlocksVision {
  readonly blocksVision: true;
}

/**
 * Abstract base for all tile types.
 * Port of C# Tile.cs.
 */
export abstract class Tile extends Entity {
  visibility: TileVisibility = TileVisibility.Unexplored;
  private _pos: Vector2Int;

  get pos(): Vector2Int {
    return this._pos;
  }

  /** Tiles don't move */
  set pos(_value: Vector2Int) {}

  get myModifiers(): Iterable<object | null | undefined> {
    return [...super.myModifiers, this.grass, this.item];
  }

  constructor(pos: Vector2Int) {
    super();
    this._pos = pos;
  }

  protected handleEnterFloor(): void {
    const player = GameModelRef.mainOrNull?.player;
    if (player && this.floor === player.floor) {
      if (this.floor!.testVisibility(player.pos, this.pos)) {
        this.visibility = TileVisibility.Visible;
      }
    }
  }

  /** Which collision layers this tile blocks. */
  get blocksMovement(): CollisionLayer {
    return this.basePathfindingWeight() === 0
      ? CollisionLayer.All
      : CollisionLayer.None;
  }

  /** 0 = unwalkable, 1 = normal weight */
  getPathfindingWeight(): number {
    return this.body != null ? 0 : this.basePathfindingWeight();
  }

  /** Pathfinding weight considering a specific body's movement layers */
  getPathfindingWeightFor(mover: any): number {
    if (this.body != null) return 0;
    let blocked = this.blocksMovement;
    const g = this.grass;
    if (g && 'blockedLayers' in g) {
      blocked |= (g as IBlocksMovement).blockedLayers;
    }
    return (mover.movementLayer & ~blocked) !== CollisionLayer.None ? 1 : 0;
  }

  bodyLeft(body: any): void {
    if (body.faction !== undefined && GameModelRef.mainOrNull) {
      const actor = body;
      GameModelRef.main.enqueuEvent(() => {
        for (const handler of collectModifiers<IActorLeaveHandler>(this, ACTOR_LEAVE_HANDLER)) {
          handler.handleActorLeave(actor);
        }
      });
    }
  }

  bodyEntered(body: any): void {
    if (body.faction !== undefined && GameModelRef.mainOrNull) {
      const actor = body;
      GameModelRef.main.enqueuEvent(() => {
        for (const handler of collectModifiers<IActorEnterHandler>(this, ACTOR_ENTER_HANDLER)) {
          handler.handleActorEnter(actor);
        }
      });
    }
  }

  basePathfindingWeight(): number {
    return 1;
  }

  obstructsVision(): boolean {
    if (this.basePathfindingWeight() === 0) return true;
    const b = this.body;
    const g = this.grass;
    return (b && 'blocksVision' in b) || (g && 'blocksVision' in g) || false;
  }

  canBeOccupied(): boolean {
    if (this.body != null) return false;
    let blocked = this.blocksMovement;
    const g = this.grass;
    if (g && 'blockedLayers' in g) {
      blocked |= (g as IBlocksMovement).blockedLayers;
    }
    return (blocked & CollisionLayer.Walking) === 0;
  }

  canBeOccupiedBy(mover: any): boolean {
    if (this.body != null) return false;
    let blocked = this.blocksMovement;
    const g = this.grass;
    if (g && 'blockedLayers' in g) {
      blocked |= (g as IBlocksMovement).blockedLayers;
    }
    return (mover.movementLayer & ~blocked) !== CollisionLayer.None;
  }
}

// --- Concrete tile types ---

export class Ground extends Tile {
  constructor(pos: Vector2Int) {
    super(pos);
  }
}

export class Signpost extends Ground {
  hasRead = false;
  text: string;

  constructor(pos: Vector2Int, text = '') {
    super(pos);
    this.text = text;
  }
}

export class HardGround extends Tile {
  constructor(pos: Vector2Int) {
    super(pos);
  }

  protected handleEnterFloor(): void {
    super.handleEnterFloor();
    this.grass?.killSelf();
  }
}

export class FancyGround extends Ground {
  constructor(pos: Vector2Int) {
    super(pos);
  }
}

export class Wall extends Tile {
  constructor(pos: Vector2Int) {
    super(pos);
  }

  get blocksMovement(): CollisionLayer {
    return CollisionLayer.All;
  }

  basePathfindingWeight(): number {
    return 0;
  }
}

export class Chasm extends Tile {
  constructor(pos: Vector2Int) {
    super(pos);
  }

  /** Chasms block walking but not flying */
  get blocksMovement(): CollisionLayer {
    return CollisionLayer.Walking;
  }

  protected handleEnterFloor(): void {
    super.handleEnterFloor();
    if (this.grass != null) {
      this.floor!.remove(this.grass);
    }
    if (this.item != null) {
      this.floor!.remove(this.item);
      this.floor!.put(this.item);
    }
  }

  basePathfindingWeight(): number {
    return 0;
  }

  obstructsVision(): boolean {
    const b = this.body;
    const g = this.grass;
    return (b && 'blocksVision' in b) || (g && 'blocksVision' in g) || false;
  }
}

export class Soil extends Tile {
  constructor(pos: Vector2Int) {
    super(pos);
  }
}

export class Water extends Tile implements IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;
  private collected = false;

  constructor(pos: Vector2Int) {
    super(pos);
  }

  handleActorEnter(who: any): void {
    const player = GameModelRef.mainOrNull?.player;
    if (who === player) {
      this.collect();
    }
  }

  private collect(): void {
    this.collected = true;
    // In the daily puzzle version, water just becomes ground (no water meter)
    this.floor!.put(new Ground(this.pos));
  }
}
