import { Vector2Int } from '../core/Vector2Int';
import { MyRandom } from '../core/MyRandom';
import type { Floor } from '../model/Floor';
import { Ground, Wall } from '../model/Tile';

/**
 * Pattern-match 3×3 tile neighborhoods and transform center tile.
 * Port of C# ShapeTransform.cs.
 *
 * Each cell in the input pattern is a pathfinding weight:
 *   0 = unwalkable (wall/chasm), 1 = walkable (ground)
 * When a 3×3 neighborhood matches, the center tile is replaced
 * with a tile of the given output weight.
 */
export class ShapeTransform {
  private rot90: ShapeTransform | null = null;
  private rot180: ShapeTransform | null = null;
  private rot270: ShapeTransform | null = null;

  constructor(
    public readonly input: number[][],
    public readonly output: number,
    public readonly probability: number = 1,
  ) {}

  /** Apply this transform (all 4 rotations) to the floor */
  applyWithRotations(floor: Floor): void {
    if (!this.rot90) this.rot90 = new ShapeTransform(rotate90(this.input), this.output, this.probability);
    if (!this.rot180) this.rot180 = new ShapeTransform(rotate90(this.rot90.input), this.output, this.probability);
    if (!this.rot270) this.rot270 = new ShapeTransform(rotate90(this.rot180.input), this.output, this.probability);
    this.apply(floor);
    this.rot90.apply(floor);
    this.rot180.apply(floor);
    this.rot270.apply(floor);
  }

  private apply(floor: Floor): void {
    const places = this.getPlacesToChange(floor);
    for (const [x, y] of places) {
      if (MyRandom.value <= this.probability) {
        this.applyOutputAt(floor, x, y);
      }
    }
  }

  private getPlacesToChange(floor: Floor): [number, number][] {
    const places: [number, number][] = [];
    for (let x = 1; x < floor.width - 1; x++) {
      for (let y = 1; y < floor.height - 1; y++) {
        const chunk = fillChunkCenteredAt(floor, x, y);
        if (chunkEquals(chunk, this.input)) {
          places.push([x, y]);
        }
      }
    }
    return places;
  }

  private applyOutputAt(floor: Floor, x: number, y: number): void {
    const pos = new Vector2Int(x, y);
    const currentTile = floor.tiles.get(pos);
    if (currentTile && currentTile.basePathfindingWeight() !== this.output) {
      floor.put(this.output === 0 ? new Wall(pos) : new Ground(pos));
    }
  }
}

/** Rotate a 3×3 grid 90° counterclockwise */
function rotate90(chunk: number[][]): number[][] {
  return [
    [chunk[0][2], chunk[1][2], chunk[2][2]],
    [chunk[0][1], chunk[1][1], chunk[2][1]],
    [chunk[0][0], chunk[1][0], chunk[2][0]],
  ];
}

/** Fill a 3×3 chunk with pathfinding weights centered at (x, y) */
function fillChunkCenteredAt(floor: Floor, x: number, y: number): number[][] {
  const chunk: number[][] = [[], [], []];
  for (let dx = -1; dx <= 1; dx++) {
    for (let dy = -1; dy <= 1; dy++) {
      const pos = new Vector2Int(x + dx, y + dy);
      if (floor.inBounds(pos)) {
        const tile = floor.tiles.get(pos);
        chunk[dx + 1][dy + 1] = tile ? tile.basePathfindingWeight() : 0;
      } else {
        chunk[dx + 1][dy + 1] = 0;
      }
    }
  }
  return chunk;
}

function chunkEquals(a: number[][], b: number[][]): boolean {
  for (let i = 0; i < 3; i++) {
    for (let j = 0; j < 3; j++) {
      if (a[i][j] !== b[i][j]) return false;
    }
  }
  return true;
}

// ---- Predefined transforms used by FloorUtils.naturalizeEdges ----

export const SMOOTH_WALL_EDGES = new ShapeTransform(
  [
    [1, 1, 1],
    [0, 0, 1],
    [0, 0, 1],
  ],
  1,
);

export const SMOOTH_ROOM_EDGES = new ShapeTransform(
  [
    [0, 0, 0],
    [1, 1, 0],
    [1, 1, 0],
  ],
  0,
);

export const MAKE_WALL_BUMPS = new ShapeTransform(
  [
    [0, 0, 0],
    [1, 1, 1],
    [1, 1, 1],
  ],
  0,
  // 50% chance to make a 2-run: 1 - sqrt(0.5)
  1 - Math.sqrt(0.5),
);
