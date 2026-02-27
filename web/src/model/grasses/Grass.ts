import { Entity } from '../Entity';
import { Vector2Int } from '../../core/Vector2Int';

/**
 * Base class for all grass entities.
 * Grasses are immobile entities placed on tiles.
 * Port of C# Grass.cs (30 lines).
 */
export class Grass extends Entity {
  readonly _isGrass = true;

  private _pos: Vector2Int;

  get pos(): Vector2Int {
    return this._pos;
  }

  /** Grasses cannot move. */
  set pos(_value: Vector2Int) {}

  /** Optional modifier applied to bodies standing on this grass. */
  get bodyModifier(): object | null {
    return null;
  }

  /** Called when a noteworthy action occurs (for controller/view hooks). */
  onNoteworthyAction: (() => void) = () => {};

  constructor(pos: Vector2Int) {
    super();
    this._pos = pos;
  }
}
