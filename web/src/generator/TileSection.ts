import { Vector2Int } from '../core/Vector2Int';
import type { Floor } from '../model/Floor';
import { Ground, Wall, Chasm } from '../model/Tile';

type TileFactory = (pos: Vector2Int) => any;

/**
 * Tile pattern template for stamping onto floors.
 * Port of C# TileSection.cs.
 */
export class TileSection {
  /** 2D array [x][y] of tile factory functions */
  readonly types: (TileFactory | null)[][];
  readonly source: string;

  constructor(types: (TileFactory | null)[][], source: string) {
    this.types = types;
    this.source = source;
  }

  get width(): number { return this.types.length; }
  get height(): number { return this.types[0]?.length ?? 0; }

  /** Rotate 90° clockwise */
  rot90(): TileSection {
    const w = this.width;
    const h = this.height;
    const newTypes: (TileFactory | null)[][] = [];
    for (let y = 0; y < h; y++) {
      newTypes[y] = [];
      for (let x = 0; x < w; x++) {
        newTypes[y][w - 1 - x] = this.types[x][y];
      }
    }
    return new TileSection(newTypes, this.source);
  }

  /** Stamp this pattern onto the floor, topLeft is the top-left corner in game coords */
  blit(floor: Floor, topLeft: Vector2Int): void {
    const bottomLeft = Vector2Int.sub(topLeft, new Vector2Int(0, this.height - 1));
    for (let x = 0; x < this.width; x++) {
      for (let y = 0; y < this.height; y++) {
        const pos = Vector2Int.add(bottomLeft, new Vector2Int(x, y));
        if (!floor.inBounds(pos)) continue;
        const factory = this.types[x][y];
        if (factory) {
          floor.put(factory(pos));
        }
      }
    }
  }

  /** Parse ASCII art into a TileSection. x=Wall, c=Chasm, _/default=Ground */
  static fromString(source: string): TileSection {
    const trimmed = source.trim();
    if (trimmed.length === 0) throw new Error('Empty TileSection string!');

    // Text is +y down; game is +y up — reverse line order
    const lines = trimmed.split('\n').map(l => l.trimEnd()).reverse();
    const height = lines.length;
    const width = Math.max(...lines.map(l => l.length));

    const types: (TileFactory | null)[][] = [];
    for (let x = 0; x < width; x++) {
      types[x] = [];
      for (let y = 0; y < height; y++) {
        const ch = x < lines[y].length ? lines[y][x] : '_';
        switch (ch) {
          case 'x': types[x][y] = (pos) => new Wall(pos); break;
          case 'c': types[x][y] = (pos) => new Chasm(pos); break;
          default: types[x][y] = (pos) => new Ground(pos); break;
        }
      }
    }
    return new TileSection(types, source);
  }

  /** Parse multiple sections separated by blank lines */
  static fromMultiString(source: string): TileSection[] {
    return source.split(/\n\s*\n/)
      .map(s => s.trim())
      .filter(s => s.length > 0)
      .map(s => TileSection.fromString(s));
  }

  /** Generate all 4 rotations for each section */
  static withRotations(sections: TileSection[]): TileSection[] {
    const result: TileSection[] = [];
    for (const s of sections) {
      const r90 = s.rot90();
      const r180 = r90.rot90();
      const r270 = r180.rot90();
      result.push(s, r90, r180, r270);
    }
    return result;
  }
}
