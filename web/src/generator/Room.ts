import { Vector2Int } from '../core/Vector2Int';
import { MyRandom } from '../core/MyRandom';
import type { Floor } from '../model/Floor';

export enum SplitDirection {
  Vertical,
  Horizontal,
}

export interface RoomSplit {
  a: Room;
  b: Room;
  direction: SplitDirection;
  coordinate: number;
}

/**
 * BSP tree node representing a room in a floor.
 * Port of C# Room.cs.
 */
export class Room {
  static readonly MIN_ROOM_SIZE = 3;

  min: Vector2Int;
  max: Vector2Int;
  split: RoomSplit | null = null;
  parent: Room | null;
  connections: Room[] = [];
  name: string | null = null;

  get isRoot(): boolean {
    return this.parent == null;
  }

  get isTerminal(): boolean {
    return this.split == null;
  }

  get center(): Vector2Int {
    return new Vector2Int(
      Math.floor((this.min.x + this.max.x) / 2),
      Math.floor((this.min.y + this.max.y) / 2),
    );
  }

  get centerFloat(): { x: number; y: number } {
    return {
      x: (this.min.x + this.max.x) / 2,
      y: (this.min.y + this.max.y) / 2,
    };
  }

  get width(): number {
    // +1 because max is inclusive
    return this.max.x - this.min.x + 1;
  }

  get height(): number {
    return this.max.y - this.min.y + 1;
  }

  get depth(): number {
    return this.parent == null ? 0 : this.parent.depth + 1;
  }

  constructor(min: Vector2Int, max: Vector2Int, parent: Room | null = null) {
    this.min = min;
    this.max = max;
    this.parent = parent;
  }

  /** Create a room that covers the interior of a floor (1-tile border) */
  static fromFloor(floor: Floor): Room {
    return new Room(
      new Vector2Int(1, 1),
      new Vector2Int(floor.width - 2, floor.height - 2),
    );
  }

  randomlyShrink(): void {
    if (!this.isTerminal) {
      throw new Error('Tried shrinking a non-terminal BSPNode.');
    }
    const roomWidth = MyRandom.Range(Room.MIN_ROOM_SIZE, this.width + 1);
    const roomHeight = MyRandom.Range(Room.MIN_ROOM_SIZE, this.height + 1);

    const startX = MyRandom.Range(this.min.x, this.max.x - roomWidth + 1 + 1);
    const startY = MyRandom.Range(this.min.y, this.max.y - roomHeight + 1 + 1);

    this.min = new Vector2Int(startX, startY);
    this.max = new Vector2Int(startX + roomWidth - 1, startY + roomHeight - 1);
  }

  shrink(amount: number): void {
    this.min = Vector2Int.add(this.min, new Vector2Int(amount, amount));
    this.max = Vector2Int.sub(this.max, new Vector2Int(amount, amount));
  }

  randomlySplit(): boolean {
    if (this.isTerminal) {
      return this.doSplit();
    } else {
      const { a, b } = this.split!;
      const [firstChoice, secondChoice] = MyRandom.value < 0.5 ? [a, b] : [b, a];
      return firstChoice.randomlySplit() || secondChoice.randomlySplit();
    }
  }

  private get canSplitVertical(): boolean {
    return this.height >= Room.MIN_ROOM_SIZE * 2 + 1;
  }

  private get canSplitHorizontal(): boolean {
    return this.width >= Room.MIN_ROOM_SIZE * 2 + 1;
  }

  private doSplit(): boolean {
    if (!this.isTerminal) {
      throw new Error('Attempted to call doSplit() on a BSPNode that is already split!');
    }
    if (!this.canSplitVertical && !this.canSplitHorizontal) {
      return false;
    } else if (this.canSplitVertical && !this.canSplitHorizontal) {
      this.doSplitVertical();
      return true;
    } else if (!this.canSplitVertical && this.canSplitHorizontal) {
      this.doSplitHorizontal();
      return true;
    } else {
      // Both possible — split weighted by aspect ratio
      const chanceToBeHorizontal = this.width / (this.width + this.height);
      if (MyRandom.value < chanceToBeHorizontal) {
        this.doSplitHorizontal();
      } else {
        this.doSplitVertical();
      }
      return true;
    }
  }

  private doSplitHorizontal(): void {
    const splitMax = this.max.x - Room.MIN_ROOM_SIZE;
    const splitMin = this.min.x + Room.MIN_ROOM_SIZE;
    const splitPoint = MyRandom.Range(splitMin, splitMax + 1);
    const a = new Room(this.min, new Vector2Int(splitPoint - 1, this.max.y), this);
    const b = new Room(new Vector2Int(splitPoint + 1, this.min.y), this.max, this);
    this.split = { a, b, direction: SplitDirection.Horizontal, coordinate: splitPoint };
  }

  private doSplitVertical(): void {
    const splitMax = this.max.y - Room.MIN_ROOM_SIZE;
    const splitMin = this.min.y + Room.MIN_ROOM_SIZE;
    const splitPoint = MyRandom.Range(splitMin, splitMax + 1);
    const a = new Room(this.min, new Vector2Int(this.max.x, splitPoint - 1), this);
    const b = new Room(new Vector2Int(this.min.x, splitPoint + 1), this.max, this);
    this.split = { a, b, direction: SplitDirection.Vertical, coordinate: splitPoint };
  }

  /** Traverse all nodes in the BSP tree (pre-order) */
  *traverse(): IterableIterator<Room> {
    yield this;
    if (!this.isTerminal) {
      yield* this.split!.a.traverse();
      yield* this.split!.b.traverse();
    }
  }

  getCenter(): Vector2Int {
    return new Vector2Int(
      Math.floor((this.max.x + this.min.x) / 2),
      Math.floor((this.max.y + this.min.y) / 2),
    );
  }

  getTopLeft(): Vector2Int {
    return new Vector2Int(this.min.x, this.max.y);
  }

  contains(pos: Vector2Int): boolean {
    return pos.x >= this.min.x && pos.x <= this.max.x &&
           pos.y >= this.min.y && pos.y <= this.max.y;
  }

  extendToEncompass(room: Room): void {
    this.min = Vector2Int.min(this.min, room.min);
    this.max = Vector2Int.max(this.max, room.max);
  }

  /** Chebyshev distance from pos to closest point in this room */
  distanceTo(pos: Vector2Int): number {
    if (this.contains(pos)) return 0;
    const distanceX = Math.max(-(pos.x - this.min.x), pos.x - this.max.x);
    const distanceY = Math.max(-(pos.y - this.min.y), pos.y - this.max.y);
    return Math.max(distanceX, distanceY);
  }

  toString(): string {
    return this.name ?? `${this.min.toString()} to ${this.max.toString()}`;
  }
}
