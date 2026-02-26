import type { Tile } from '../model/Tile';
import { Vector2Int } from '../core/Vector2Int';

/**
 * Tile connectivity utilities using BFS flood-fill.
 * Port of C# TileGroup.cs.
 */
export function makeGroupAndRemove(start: Tile, allTiles: Set<Tile>): Set<Tile> {
  const group = new Set<Tile>();
  const queue: Tile[] = [start];
  group.add(start);
  allTiles.delete(start);

  while (queue.length > 0) {
    const current = queue.shift()!;
    for (const neighbor of current.floor!.getAdjacentTiles(current.pos)) {
      if (allTiles.has(neighbor) && !group.has(neighbor)) {
        group.add(neighbor);
        allTiles.delete(neighbor);
        queue.push(neighbor);
      }
    }
  }
  return group;
}

export function partitionIntoDisjointGroups(allTiles: Set<Tile>): Set<Tile>[] {
  const groups: Set<Tile>[] = [];
  let guard = 0;
  const remaining = new Set(allTiles);
  while (remaining.size > 0 && guard++ < 99) {
    const first = remaining.values().next().value!;
    const group = makeGroupAndRemove(first, remaining);
    groups.push(group);
  }
  return groups;
}

export function getCenterTile(tiles: Iterable<Tile>): Tile | null {
  const arr = [...tiles];
  if (arr.length === 0) return null;
  const centroidX = arr.reduce((sum, t) => sum + t.pos.x, 0) / arr.length;
  const centroidY = arr.reduce((sum, t) => sum + t.pos.y, 0) / arr.length;
  const centroid = { x: centroidX, y: centroidY };

  let closest: Tile | null = null;
  let closestDist = Infinity;
  for (const t of arr) {
    const dx = t.pos.x - centroid.x;
    const dy = t.pos.y - centroid.y;
    const dist = dx * dx + dy * dy;
    if (dist < closestDist) {
      closestDist = dist;
      closest = t;
    }
  }
  return closest;
}
