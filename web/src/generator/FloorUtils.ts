import { Vector2Int } from '../core/Vector2Int';
import { MyRandom } from '../core/MyRandom';
import type { Floor } from '../model/Floor';
import type { Tile } from '../model/Tile';
import { Ground, HardGround, Wall } from '../model/Tile';
import type { Room } from './Room';
import { SMOOTH_WALL_EDGES, SMOOTH_ROOM_EDGES, MAKE_WALL_BUMPS } from './ShapeTransform';

/**
 * Floor generation utility functions.
 * Port of C# FloorUtils.cs.
 */

/**
 * Clear vines near start, move adjacent enemies, ensure HardGround around stairs.
 * Replaces all HardGround with Ground after generation.
 */
export function tidyUpAroundStairs(floor: Floor): void {
  for (const tile of floor.getAdjacentTiles(floor.startPos)) {
    // Remove hanging vines (grass type check — graceful if not ported)
    const grass = floor.grasses.get(tile.pos);
    if (grass && (grass as any).constructor?.name === 'HangingVines') {
      floor.remove(grass);
    }
    // Move adjacent actors to a different spot
    const actor = (tile as any).actor;
    if (actor) {
      const emptyTiles = enumerateRoomTiles(floor, floor.root)
        .filter(t => t.canBeOccupied());
      if (emptyTiles.length > 0) {
        actor.pos = MyRandom.Pick(emptyTiles).pos;
      }
    }
  }

  // Ensure start position is walkable
  floor.put(new HardGround(floor.startPos));

  // Ensure downstairs area is walkable (if set)
  if (floor.downstairsPos) {
    const dsTile = floor.tiles.get(floor.downstairsPos);
    if (dsTile && dsTile.basePathfindingWeight() === 0) {
      floor.put(new HardGround(floor.downstairsPos));
    }
    const entrancePos = Vector2Int.add(floor.downstairsPos, Vector2Int.down);
    if (floor.inBounds(entrancePos)) {
      const entranceTile = floor.tiles.get(entrancePos);
      if (entranceTile && !(entranceTile instanceof (Water as any))) {
        floor.put(new HardGround(entrancePos));
      }
    }
  }

  // Replace all HardGround with normal Ground
  for (const pos of floor.enumerateFloor()) {
    const tile = floor.tiles.get(pos);
    if (tile instanceof HardGround) {
      floor.put(new Ground(pos));
    }
  }
}

// Import Water dynamically to avoid circular dep issues
let Water: any;
import('../model/Tile').then(m => { Water = m.Water; }).catch(() => {});

/** Get occupiable, non-stairs tiles in a room */
export function emptyTilesInRoom(floor: Floor, room: Room | null): Tile[] {
  if (!room) return [];
  return floor.enumerateRoomTiles(room)
    .filter(t => t.canBeOccupied());
}

/** Tiles sorted by distance from a point (closest first) */
export function tilesFrom(floor: Floor, room: Room | null, pos: { x: number; y: number }): Tile[] {
  return emptyTilesInRoom(floor, room)
    .sort((a, b) => {
      const da = Math.hypot(a.pos.x - pos.x, a.pos.y - pos.y);
      const db = Math.hypot(b.pos.x - pos.x, b.pos.y - pos.y);
      return da - db;
    });
}

/** Tiles sorted by distance from a point (farthest first) */
export function tilesAwayFrom(floor: Floor, room: Room | null, pos: Vector2Int | { x: number; y: number }): Tile[] {
  return tilesFrom(floor, room, pos).reverse();
}

/** Tiles sorted from center of room (closest first) */
export function tilesFromCenter(floor: Floor, room: Room | null): Tile[] {
  if (!room) return [];
  return tilesFrom(floor, room, room.centerFloat);
}

/** Tiles sorted from center of room (farthest first) */
export function tilesAwayFromCenter(floor: Floor, room: Room | null): Tile[] {
  return tilesFromCenter(floor, room).reverse();
}

/** Alias — tiles sorted by corners (farthest from center) */
export function tilesSortedByCorners(floor: Floor, room: Room | null): Tile[] {
  return tilesAwayFromCenter(floor, room);
}

/** 3-wide corridor: all positions adjacent to each point on the line */
export function line3x3(floor: Floor, start: Vector2Int, end: Vector2Int): Vector2Int[] {
  const result = new Set<string>();
  const positions: Vector2Int[] = [];
  for (const pos of floor.enumerateLine(start, end)) {
    for (const tile of floor.getAdjacentTiles(pos)) {
      const key = Vector2Int.key(tile.pos);
      if (!result.has(key)) {
        result.add(key);
        positions.push(tile.pos);
      }
    }
  }
  return positions;
}

/** Surround floor perimeter with walls */
export function surroundWithWalls(floor: Floor): void {
  for (const p of floor.enumeratePerimeter()) {
    floor.put(new Wall(p));
  }
}

/** Replace unwalkable tiles with Ground. If points not given, applies to entire floor. */
export function carveGround(floor: Floor, points?: Iterable<Vector2Int>): void {
  const iter = points ?? floor.enumerateFloor();
  for (const pos of iter) {
    const tile = floor.tiles.get(pos);
    const isUnwalkable = !tile || tile.basePathfindingWeight() === 0;
    if (isUnwalkable) {
      floor.put(new Ground(pos));
    }
  }
}

/** Random BFS cluster of tiles starting from random position within inset */
export function clusters(floor: Floor, inset: Vector2Int, num: number): Tile[] {
  const minPos = Vector2Int.add(floor.boundsMin, inset);
  const maxPos = Vector2Int.sub(floor.boundsMax, inset);
  const pos = MyRandom.RangeVec(minPos, maxPos);
  return [...floor.breadthFirstSearch(pos)].slice(0, num);
}

/**
 * Apply a natural look by smoothing wall corners and space corners.
 * 3 ShapeTransform passes × 4 rotations each.
 */
export function naturalizeEdges(floor: Floor): void {
  SMOOTH_ROOM_EDGES.applyWithRotations(floor);
  SMOOTH_WALL_EDGES.applyWithRotations(floor);
  MAKE_WALL_BUMPS.applyWithRotations(floor);
}

/** Helper: enumerate room tiles (delegates to floor method) */
function enumerateRoomTiles(floor: Floor, room: Room | null): Tile[] {
  if (!room) return [];
  return floor.enumerateRoomTiles(room);
}
