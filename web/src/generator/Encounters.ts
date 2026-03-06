/**
 * All encounter placement functions.
 * Port of C# Encounters.cs + EnemyEncounters.cs.
 *
 * Each encounter function places entities on a floor within a room.
 * Entity classes are resolved via entityRegistry — unported entities silently no-op.
 */
import { Vector2Int } from '../core/Vector2Int';
import { MyRandom } from '../core/MyRandom';
import type { Floor } from '../model/Floor';
import type { Tile } from '../model/Tile';
import { Ground, Wall, Chasm, HardGround, FancyGround, Water } from '../model/Tile';
import type { Room } from './Room';
import { entityRegistry } from './entityRegistry';
import * as FloorUtils from './FloorUtils';
import { concavitySections } from './TileSectionConcavity';
import type { Encounter } from './EncounterGroup';
import { Tunnelroot } from '../model/grasses/Tunnelroot';
import { Destructible } from '../model/enemies/Destructible';

// ---- Helpers ----

function randomPick<T>(items: T[]): T | null {
  if (items.length === 0) return null;
  return items[MyRandom.Range(0, items.length)];
}

function clampGet<T>(index: number, values: T[]): T {
  return values[Math.max(0, Math.min(index, values.length - 1))];
}

function randomRangeBasedOnIndex(index: number, ...values: [number, number][]): number {
  const [min, max] = clampGet(index, values);
  return MyRandom.Range(min, max + 1);
}

/** Spawn entity by name at position; returns true if entity was created */
function spawn(floor: Floor, name: string, pos: Vector2Int, ...args: any[]): boolean {
  const entity = entityRegistry.create(name, pos, ...args);
  if (!entity) return false;
  floor.put(entity);
  return true;
}

function twice(fn: Encounter): Encounter {
  return (floor, room) => { fn(floor, room); fn(floor, room); };
}

/**
 * SpectrumPos: Get BFS-ordered tiles starting from a row at spectrum% up the floor.
 * Used for spatially biasing enemy placement (e.g., enemies spawn toward the top).
 */
function spectrumPos(floor: Floor, spectrum: number): Tile[] {
  const posY = MyRandom.RandRound((floor.height - 2) * spectrum);
  const line = [...floor.enumerateLine(
    new Vector2Int(0, posY),
    new Vector2Int(floor.width - 1, posY),
  )]
    .map(pos => floor.tiles.get(pos))
    .filter((t): t is Tile => t != null && t.canBeOccupied());

  let startTile = randomPick(line);

  if (!startTile) {
    startTile = [...floor.breadthFirstSearch(new Vector2Int(Math.floor(floor.width / 2), posY))]
      .find(t => t.canBeOccupied()) ?? null;
  }

  if (!startTile) return [];
  return [...floor.breadthFirstSearch(startTile.pos, t => t.canBeOccupied())];
}

// ---- Structural Encounters (always active — no entity dependencies) ----

export function empty(_floor: Floor, _room: Room | null): void {}

export function wallPillars(floor: Floor, room: Room | null): void {
  const positions = [...floor.enumerateRoom(room)]
    .filter(p => { const t = floor.tiles.get(p); return t instanceof Ground; });
  MyRandom.Shuffle(positions);
  const num = MyRandom.Range(3, Math.floor((positions.length + 1) / 2));
  const isWall = MyRandom.value < 0.8;
  for (const pos of positions.slice(0, num)) {
    if (!floor.getAdjacentTiles(pos).some(t => t instanceof Wall)) {
      if (isWall) {
        floor.put(new Wall(pos));
      } else {
        spawn(floor, 'Rubble', pos);
      }
    }
  }
}

export function concavity(floor: Floor, _room: Room | null): void {
  const section = randomPick(concavitySections);
  if (!section) return;
  const center = floor.center;
  const topLeft = Vector2Int.add(center, new Vector2Int(
    -Math.floor(section.width / 2),
    Math.floor(section.height / 2),
  ));
  section.blit(floor, topLeft);
}

export function chunkInMiddle(floor: Floor, room: Room | null): void {
  if (!room) return;
  const chunkSize = MyRandom.Range(1, 6);
  const positions = [...floor.breadthFirstSearch(room.center)]
    .filter(t => t instanceof Ground)
    .slice(0, chunkSize)
    .map(t => t.pos);
  const isWall = MyRandom.value < 0.8;
  for (const pos of positions) {
    if (isWall) {
      floor.put(new Wall(pos));
    } else {
      spawn(floor, 'Rubble', pos);
    }
  }
}

export function lineWithOpening(floor: Floor, _room: Room | null): void {
  const perimeterPoints = [...floor.enumeratePerimeter()];
  const start = randomPick(perimeterPoints);
  if (!start) return;
  const end = new Vector2Int(floor.width - 1 - start.x, floor.height - 1 - start.y);
  const line = [...floor.enumerateLine(start, end)]
    .map(p => floor.tiles.get(p))
    .filter((t): t is Tile => t != null && t.canBeOccupied());

  const openingSize = MyRandom.Range(2, Math.floor(line.length / 2) + 1);
  const openingStart = MyRandom.Range(0, line.length - openingSize);
  const toWall = [...line.slice(0, openingStart), ...line.slice(openingStart + openingSize)];
  for (const tile of toWall) {
    floor.put(new Wall(tile.pos));
  }
}

export function insetLayerWithOpening(floor: Floor, _room: Room | null): void {
  const insetLength = MyRandom.Range(3, 6);
  let inset = [...floor.enumeratePerimeter(insetLength)];
  const startIdx = MyRandom.Range(0, inset.length);
  inset = [...inset.slice(startIdx), ...inset.slice(0, startIdx)];
  let openingLength = 2;
  while (MyRandom.value < 0.66 && openingLength < Math.floor(inset.length / 2)) {
    openingLength++;
  }
  for (const pos of inset.slice(openingLength)) {
    floor.put(new Wall(pos));
  }
}

export function addStalk(floor: Floor, room: Room | null): void {
  if (!room) return;
  const x = room.center.x;
  const line = [...floor.enumerateLine(new Vector2Int(x, 0), new Vector2Int(x, floor.height - 1))]
    .filter(pos => { const t = floor.tiles.get(pos); return t != null && t.canBeOccupied(); });
  for (const pos of line) {
    spawn(floor, 'Stalk', pos);
  }
}

export function rubbleCluster(floor: Floor, room: Room | null): void {
  objectClusterImpl(floor, room, 'Rubble');
}

function objectClusterImpl(floor: Floor, room: Room | null, entityName: string, num?: number): void {
  if (num == null) num = MyRandom.Range(1, 5);
  const tiles = FloorUtils.tilesFromCenter(floor, room);
  for (const t of tiles.slice(0, num)) {
    spawn(floor, entityName, t.pos);
  }
}

// ---- Chasm Encounters ----

export function chasmsAwayFromWalls2(floor: Floor, room: Room | null): void {
  chasmsAwayFromWallsImpl(floor, room, 2, 1);
}

export function chasmsAwayFromWalls1(floor: Floor, room: Room | null): void {
  chasmsAwayFromWallsImpl(floor, room, 1, 2);
}

/** Place a chasm, removing any Destructible body at the position first. */
function placeChasm(floor: Floor, pos: Vector2Int): void {
  const body = floor.bodies.get(pos);
  if (body instanceof Destructible) floor.remove(body);
  floor.put(new Chasm(pos));
}

function chasmsAwayFromWallsImpl(floor: Floor, room: Room | null, cliffEdgeSize: number, extrude = 1): void {
  if (!room) return;
  const roomTiles = new Set(floor.enumerateRoomTiles(room, extrude));
  const walls = [...roomTiles].filter(t => t instanceof Wall);
  const floorsOnCliffEdge: Tile[] = [];

  const frontier: Tile[] = [...walls];
  const seen = new Set<Tile>(walls);
  const distanceToWall = new Map<Tile, number>();
  for (const w of walls) distanceToWall.set(w, 0);

  while (frontier.length > 0) {
    const tile = frontier.shift()!;
    const distance = distanceToWall.get(tile) ?? 0;

    if (tile instanceof Ground) {
      floorsOnCliffEdge.push(tile);
    }

    for (const next of floor.getAdjacentTiles(tile.pos)) {
      if (seen.has(next)) continue;
      const existingDist = distanceToWall.get(next) ?? 9999;
      const nextDist = Math.min(existingDist, distance + 1);
      distanceToWall.set(next, nextDist);
      if (nextDist <= cliffEdgeSize) {
        frontier.push(next);
        seen.add(next);
      }
    }
  }

  const cliffEdgeSet = new Set(floorsOnCliffEdge);
  const grounds = [...roomTiles].filter(t => t instanceof Ground);
  for (const t of grounds) {
    if (!cliffEdgeSet.has(t)) {
      placeChasm(floor, t.pos);
    }
  }
}

export function chasmBridge(floor: Floor, _room: Room | null): void {
  switch (MyRandom.Range(0, 4)) {
    case 0: chasmBridgeImpl(floor, 1, 1); break;
    case 1: chasmBridgeImpl(floor, 1, -1); break;
    case 2: chasmBridgeImpl(floor, 2, 0); break;
    default: chasmBridgeImpl(floor, 3, 0); break;
  }
}

function chasmBridgeImpl(floor: Floor, thickness: number, crossScalar: number): void {
  const origin = new Vector2Int(1, 1);
  const end = new Vector2Int(floor.boundsMax.x - 2, floor.boundsMax.y - 3);
  const offsetX = end.x - origin.x;
  const offsetY = end.y - origin.y;
  const mag = Math.sqrt(offsetX * offsetX + offsetY * offsetY);
  const dirX = offsetX / mag;
  const dirY = offsetY / mag;

  for (const pos of floor.enumerateFloor()) {
    const tileOffX = pos.x - origin.x;
    const tileOffY = pos.y - origin.y;
    const dot = tileOffX * dirX + tileOffY * dirY;
    const cross = (tileOffX * dirY - dirX * tileOffY) * crossScalar;
    const projX = origin.x + dirX * dot;
    const projY = origin.y + dirY * dot;
    const dist = Math.sqrt((projX - pos.x) ** 2 + (projY - pos.y) ** 2);
    if (dist > thickness && cross <= 0) {
      placeChasm(floor, pos);
    }
  }
  floor.startPos = origin;
  floor.downstairsPos = end;
}

export function chasmGrowths(floor: Floor, _room: Room | null): void {
  const tiles = FloorUtils.clusters(floor, Vector2Int.zero, 7);
  for (const t of tiles) {
    placeChasm(floor, t.pos);
  }
}

// ---- Water Encounters ----

export function addWater(floor: Floor, room: Room | null): void {
  addWaterImpl(floor, room, MyRandom.Range(2, 5));
}

export function addHomeWater(floor: Floor, room: Room | null): void {
  addWaterImpl(floor, room, 5, true);
}

export function addOneWater(floor: Floor, room: Room | null): void {
  addWaterImpl(floor, room, 1);
}

function addWaterImpl(_floor: Floor, _room: Room | null, _num: number, _randomize = false): void {
  // commented out for now
  // let tiles = FloorUtils.tilesAwayFrom(floor, room, floor.downstairsPos ?? floor.center)
  //   .filter(t => t instanceof Ground && floor.grasses.get(t.pos) == null &&
  //     !Vector2Int.equals(t.pos, floor.startPos));
  // if (randomize) {
  //   tiles = tiles.filter(() => MyRandom.value < 0.5);
  // }
  // for (const tile of tiles.slice(0, num)) {
  //   floor.put(new Water(tile.pos));
  // }
}

// ---- Downstairs Placement ----

export function addDownstairsInRoomCenter(floor: Floor, room: Room | null): void {
  if (!room) return;
  const posY = floor.depth < 9 ? room.max.y - 1 : room.max.y - 2;
  const center = new Vector2Int(room.center.x, posY);
  for (const tile of floor.getAdjacentTiles(center)) {
    floor.put(new HardGround(tile.pos));
    const grass = floor.grasses.get(tile.pos);
    if (grass) floor.remove(grass);
  }
  floor.downstairsPos = center;
}

// ---- Fancy Ground & Rubble Surround ----

export function placeFancyGround(floor: Floor, room: Room | null): void {
  if (!room) return;
  for (const tile of floor.enumerateRoomTiles(room)) {
    if (tile instanceof Ground) {
      floor.put(new FancyGround(tile.pos));
    }
  }
}

export function surroundWithRubble(floor: Floor, room: Room | null): void {
  if (!room) return;
  const inner = new Set(floor.enumerateRoomTiles(room, 0).map(t => t.pos.toString()));
  const outer = floor.enumerateRoomTiles(room, 1).filter(t => !inner.has(t.pos.toString()));
  for (const tile of outer) {
    if (tile.canBeOccupied()) {
      spawn(floor, 'Rubble', tile.pos);
    }
  }
}

// ---- Enemy Encounters (use entityRegistry — graceful no-op for unported) ----

export function addBird(floor: Floor, _room: Room | null): void {
  const tile = spectrumPos(floor, 0.9)[0];
  if (!tile) return;
  for (let i = 0; i < 2; i++) spawn(floor, 'Bird', tile.pos);
}

export function addSnake(floor: Floor, _room: Room | null): void {
  const tile = spectrumPos(floor, 0.6)[0];
  for (let i = 0; i < 2; i++) spawn(floor, 'Snake', tile.pos);
}

export function aFewBlobs(floor: Floor, _room: Room | null): void {
  const tiles = spectrumPos(floor, 0.8);
  // let budget = 1 + Math.pow(floor.depth, 0.9) / 3.6;
  // let numMini = 0;
  // let numNormal = 0;
  // while (budget > 0) {
  //   const isMini = MyRandom.value < 0.5;
  //   const cost = isMini ? 0.75 : 1;
  //   if (cost > budget) {
  //     if (MyRandom.value * cost > budget) break;
  //   }
  //   if (isMini) numMini++; else numNormal++;
  //   budget -= cost;
  // }
  const choice = MyRandom.Range(0, 3);
  let numMini: number;
  let numNormal: number;
  
  if (choice == 0) {
    numMini = 0;
    numNormal = 1;
  } else if (choice == 1) {
    numMini = 1;
    numNormal = 1;
  } else {
    numMini = 2;
    numNormal = 0;
  }
  
  let idx = 0;
  for (let i = 0; i < numMini && idx < tiles.length; i++, idx++) {
    spawn(floor, 'MiniBlob', tiles[idx].pos);
  }
  for (let i = 0; i < numNormal && idx < tiles.length; i++, idx++) {
    spawn(floor, 'Blob', tiles[idx].pos);
  }
}

export function addWallflowers(floor: Floor, _room: Room | null): void {
  const tiles = spectrumPos(floor, 0.9).filter(t =>
    t.canBeOccupied() && floor.getCardinalNeighbors(t.pos).some(n => n instanceof Wall)
  );
  for (const t of tiles.slice(0, 1)) spawn(floor, 'Wallflower', t.pos);
}

export function jackalPile(floor: Floor, _room: Room | null): void {
  const tiles = spectrumPos(floor, 0.8);
  const num = 3; // randomRangeBasedOnIndex(Math.floor(floor.depth / 4), [1, 1], [2, 2], [3, 3]);
  for (const t of tiles.slice(0, num)) spawn(floor, 'Jackal', t.pos);
}

export function addSkullys(floor: Floor, room: Room | null): void {
  const tiles = FloorUtils.tilesFromCenter(floor, room);
  for (const t of tiles.slice(0, 2)) spawn(floor, 'Skully', t.pos);
}

export function addOctopus(floor: Floor, _room: Room | null): void {
  const tiles = spectrumPos(floor, 0.95);
  const num = 2; // randomRangeBasedOnIndex(Math.floor(floor.depth / 6), [1, 1], [2, 2]);
  for (const t of tiles.slice(0, num)) spawn(floor, 'Octopus', t.pos);
}

export function aFewSnails(floor: Floor, room: Room | null): void {
  const tiles = FloorUtils.emptyTilesInRoom(floor, room);
  MyRandom.Shuffle(tiles);
  const num = 2; // randomRangeBasedOnIndex(Math.floor(floor.depth / 4), [1, 1], [2, 2], [2, 3]);
  for (const t of tiles.slice(0, num)) spawn(floor, 'Snail', t.pos);
}

export function addSpiders(floor: Floor, _room: Room | null): void {
  const tiles = spectrumPos(floor, 0.5);
  const num = 2;
  // randomRangeBasedOnIndex(
  //   Math.floor(floor.depth / 4), [1, 1], [2, 2], [3, 3], [3, 3], [4, 4], [4, 4],
  // );
  for (const t of tiles.slice(0, num)) spawn(floor, 'Spider', t.pos);
}

export function addBats(floor: Floor, _room: Room | null): void {
  const tiles = spectrumPos(floor, 0.8);
  const num = floor.depth < 15 ? 1 : 2;
  for (const t of tiles.slice(0, num)) spawn(floor, 'Bat', t.pos);
}

export function addFungalSentinel(floor: Floor, _room: Room | null): void {
  const tiles = spectrumPos(floor, 0.45);
  for (const t of tiles.slice(0, 3)) spawn(floor, 'FungalSentinel', t.pos);
}

export function addFungalBreeder(floor: Floor, _room: Room | null): void {
  const tile = spectrumPos(floor, 0.8)[0];
  if (tile) spawn(floor, 'FungalBreeder', tile.pos);
}

export function addScorpions(floor: Floor, _room: Room | null): void {
  const tiles = spectrumPos(floor, 1.0);
  const num = 1;
  for (const t of tiles.slice(0, num)) spawn(floor, 'Scorpion', t.pos);
}

export function addGolems(floor: Floor, _room: Room | null): void {
  const tiles = spectrumPos(floor, 1.0);
  const num = 1;
  for (const t of tiles.slice(0, num)) spawn(floor, 'Golem', t.pos);
}

export function addParasite(floor: Floor, room: Room | null): void {
  const tiles = FloorUtils.emptyTilesInRoom(floor, room);
  MyRandom.Shuffle(tiles);
  for (const t of tiles.slice(0, 3)) spawn(floor, 'Parasite', t.pos);
}

export const addParasite8x: Encounter = twice(twice(twice(addParasite)));

export function addHydra(floor: Floor, room: Room | null): void {
  const tile = FloorUtils.tilesFromCenter(floor, room).find(t => t.canBeOccupied());
  if (tile) spawn(floor, 'HydraHeart', tile.pos);
}

export function addClumpshroom(floor: Floor, room: Room | null): void {
  if (!room) return;
  const tiles = spectrumPos(floor, 1.0)
    .filter(t => t.pos.x >= room.center.x);
  const startTile = tiles[MyRandom.Range(0, Math.min(4, tiles.length))];
  if (startTile) spawn(floor, 'Clumpshroom', startTile.pos);
}

export function addGrasper(floor: Floor, room: Room | null): void {
  if (!room) return;
  const tile = randomPick(
    floor.enumerateRoomTiles(room).filter(t => t.canBeOccupied() && t.pos.x > Math.floor(floor.width / 2)),
  );
  if (tile) spawn(floor, 'Grasper', tile.pos);
}

export function addThistlebog(floor: Floor, _room: Room | null): void {
  const tile = spectrumPos(floor, 0.9)[0];
  if (tile) spawn(floor, 'Thistlebog', tile.pos);
}

export function addWildekins(floor: Floor, _room: Room | null): void {
  const tiles = spectrumPos(floor, 0.85);
  for (const t of tiles.slice(0, 1)) spawn(floor, 'Wildekin', t.pos);
}

export function addDizapper(floor: Floor, _room: Room | null): void {
  const tile = spectrumPos(floor, 0.75)[0];
  if (tile) spawn(floor, 'Dizapper', tile.pos);
}

export function addGoo(floor: Floor, _room: Room | null): void {
  const tile = spectrumPos(floor, 0.5)[0];
  if (tile) spawn(floor, 'Goo', tile.pos);
}

export function addHardShell(floor: Floor, room: Room | null): void {
  const tile = randomPick(FloorUtils.emptyTilesInRoom(floor, room));
  if (tile) spawn(floor, 'HardShell', tile.pos);
}

export function addHoppers(floor: Floor, room: Room | null): void {
  const startTile = randomPick(FloorUtils.emptyTilesInRoom(floor, room));
  if (!startTile) return;
  const num = 2;
  const tiles = [...floor.breadthFirstSearch(startTile.pos, t => t.canBeOccupied())].slice(0, num);
  for (const t of tiles) spawn(floor, 'Hopper', t.pos);
}

export function addHealer(floor: Floor, _room: Room | null): void {
  const tiles = spectrumPos(floor, 0.65);
  if (tiles[0]) spawn(floor, 'Healer', tiles[0].pos);
}

export function addPoisoner(floor: Floor, _room: Room | null): void {
  const tiles = spectrumPos(floor, 0.95);
  if (tiles[0]) spawn(floor, 'Poisoner', tiles[0].pos);
}

export function addMuckola(floor: Floor, _room: Room | null): void {
  const tiles = spectrumPos(floor, 0.9);
  if (tiles[0]) spawn(floor, 'Muckola', tiles[0].pos);
}

export function addIronJelly(floor: Floor, _room: Room | null): void {
  // const tiles = spectrumPos(floor, 0.35);
  const posX = MyRandom.RandRound(floor.width / 2);
  const posY = Math.floor(floor.height * 0.25);
  const score = (t: Tile) => floor.getAdjacentTiles(t.pos).filter(a => a instanceof Ground).length;
  const candidates = [...floor.breadthFirstSearch(new Vector2Int(posX, posY))]
    .filter(t => t.canBeOccupied())
    .slice(0, 20)
    .sort((a, b) => score(b) - score(a));
  if (candidates[0]) spawn(floor, 'IronJelly', candidates[0].pos);
}

// ---- Grass Encounters (entity-dependent — no-op until grasses are ported) ----

export function addSoftGrass(floor: Floor, room: Room | null): void {
  addSoftGrassImpl(floor, room, 1);
}

function addSoftGrassImpl(floor: Floor, room: Room | null, mult: number): void {
  if (!room) return;
  const occupiable = floor.enumerateRoomTiles(room)
    .filter(t => t instanceof Ground && floor.grasses.get(t.pos) == null);
  if (occupiable.length === 0) return;
  // const start = randomPick(occupiable);
  // if (!start) return;
  // const occupiableSet = new Set(occupiable);
  // const bfs = [...floor.breadthFirstSearch(start.pos, t => occupiableSet.has(t))];
  // const num = Math.round(MyRandom.Range(Math.floor(occupiable.length / 4), Math.floor(occupiable.length / 2) + 1) * mult);
  for (const tile of occupiable) {
    if (MyRandom.value < 0.5) {
      spawn(floor, 'SoftGrass', tile.pos);
    }
  }
}

export function addBladegrass(floor: Floor, room: Room | null): void {
  addBladegrassImpl(floor, room, 1);
}

function addBladegrassImpl(floor: Floor, room: Room | null, mult: number): void {
  if (!room) return;
  const occupiable = spectrumPos(floor, 0.35)
    .filter(t => t instanceof Ground && floor.grasses.get(t.pos) == null);
  if (occupiable.length === 0) return;
  const num = 5;
  for (const tile of occupiable.slice(0, num)) {
    spawn(floor, 'Bladegrass', tile.pos);
  }
}

export function addGuardleaf(floor: Floor, room: Room | null): void {
  addGuardleafImpl(floor, room, 1);
}

export const addGuardleaf4x: Encounter = (floor, room) => addGuardleafImpl(floor, room, 4);

function addGuardleafImpl(floor: Floor, room: Room | null, mult: number): void {
  if (!room) return;
  const occupiable = spectrumPos(floor, 0.30)
    .filter(t => t instanceof Ground && floor.grasses.get(t.pos) == null);
  if (occupiable.length === 0) return;
  // const start = randomPick(occupiable);
  // if (!start) return;
  // const occupiableSet = new Set(occupiable);
  // const bfs = [...floor.breadthFirstSearch(start.pos, t => occupiableSet.has(t))];
  const num = 5 * mult;
  for (const tile of occupiable.slice(0, num)) {
    spawn(floor, 'Guardleaf', tile.pos);
  }
}

export function addViolets(floor: Floor, room: Room | null): void {
  const occupiable = FloorUtils.tilesFromCenter(floor, room)
    .filter(t => t instanceof Ground && floor.grasses.get(t.pos) == null);
  if (occupiable.length === 0) return;
  let num = Math.floor(occupiable.length / 4);
  // if (MyRandom.value < 0.2) num = occupiable.length;
  for (const tile of occupiable.slice(0, num)) {
    spawn(floor, 'Violets', tile.pos);
  }
}

export function addDeathbloom4x(floor: Floor, room: Room | null): void {
  for (var i = 0; i < 4; i++) {
    addDeathbloom(floor, room);
  }
}

export function addDeathbloom(floor: Floor, room: Room | null): void {
  if (!room) return;
  const tiles = FloorUtils.emptyTilesInRoom(floor, room)
    .filter(t => t instanceof Ground && floor.grasses.get(t.pos) == null)
    .sort((a, b) => Vector2Int.distance(a.pos, room.center) - Vector2Int.distance(b.pos, room.center));
  if (tiles[0]) spawn(floor, 'Deathbloom', tiles[0].pos);
}

export function addWebs(floor: Floor, room: Room | null): void {
  const tiles = FloorUtils.tilesSortedByCorners(floor, room)
    .filter(t => floor.grasses.get(t.pos) == null && t instanceof Ground);
  const num = tiles.length * 0.75;
  for (const tile of tiles.slice(0, num)) {
    if (MyRandom.value < 0.5) spawn(floor, 'Web', tile.pos);
  }
}

export function addHangingVines(floor: Floor, room: Room | null): void {
  if (!room) return;
  const candidates = new Set<string>();
  const candidatePositions: Vector2Int[] = [];
  for (const tile of floor.enumerateRoomTiles(room, 1)) {
    if (!(tile instanceof Wall)) continue;
    if (tile.pos.y <= 0 || tile.pos.x <= 0 || tile.pos.x >= floor.width - 1) continue;
    const below = floor.tiles.get(Vector2Int.add(tile.pos, Vector2Int.down));
    if (!(below instanceof Ground)) continue;
    if (floor.grasses.get(tile.pos) != null) continue;
    const key = Vector2Int.key(tile.pos);
    candidates.add(key);
    candidatePositions.push(tile.pos);
  }
  const num = MyRandom.Range(2, 5);
  for (let i = 0; i < num && candidatePositions.length > 0; i++) {
    const idx = MyRandom.Range(0, candidatePositions.length);
    const pos = candidatePositions[idx];
    spawn(floor, 'HangingVines', pos);
    // Disallow consecutive vines
    const leftKey = Vector2Int.key(Vector2Int.add(pos, Vector2Int.left));
    const rightKey = Vector2Int.key(Vector2Int.add(pos, Vector2Int.right));
    const thisKey = Vector2Int.key(pos);
    candidatePositions.splice(idx, 1);
    // Remove neighbors
    for (let j = candidatePositions.length - 1; j >= 0; j--) {
      const k = Vector2Int.key(candidatePositions[j]);
      if (k === leftKey || k === rightKey || k === thisKey) {
        candidatePositions.splice(j, 1);
      }
    }
  }
}

export const addHangingVines2x: Encounter = twice(addHangingVines);

export function addSpore(floor: Floor, room: Room | null): void {
  const tiles = FloorUtils.tilesFromCenter(floor, room)
    .filter(t => t instanceof Ground && floor.grasses.get(t.pos) == null);
  if (tiles[0]) spawn(floor, 'Spores', tiles[0].pos);
}

export const addSpore8x: Encounter = twice(twice(twice(addSpore)));

export function addAgave(floor: Floor, room: Room | null): void {
  if (!room) return;
  const livable = floor.enumerateRoomTiles(room).filter(t => t instanceof Ground && floor.grasses.get(t.pos) == null);
  const start = randomPick(livable);
  if (!start) return;
  const livableSet = new Set(livable);
  const num = MyRandom.Range(3, 7);
  const bfs = [...floor.breadthFirstSearch(start.pos, t => livableSet.has(t))].slice(0, num);
  for (const t of bfs) spawn(floor, 'Agave', t.pos);
}

export function addPoisonmoss(floor: Floor, room: Room | null): void {
  if (!room) return;
  const occupiable = floor.enumerateRoomTiles(room)
    .filter(t => t instanceof Ground && floor.grasses.get(t.pos) == null && t.canBeOccupied());
  if (occupiable.length === 0) return;
  const start = randomPick(occupiable);
  if (!start) return;
  const occupiableSet = new Set(occupiable);
  const bfs = [...floor.breadthFirstSearch(start.pos, t => occupiableSet.has(t))];
  const num = MyRandom.Range(2, 6);
  for (const tile of bfs.slice(0, num)) spawn(floor, 'Poisonmoss', tile.pos);
}

export function addBrambles(floor: Floor, room: Room | null): void {
  const tiles = FloorUtils.tilesSortedByCorners(floor, room)
    .filter(t => t instanceof Ground && floor.grasses.get(t.pos) == null && t.canBeOccupied());
  let num = MyRandom.Range(5, 9);
  while (num >= 2 && tiles.length >= 2) {
    const tile = tiles.pop()!;
    spawn(floor, 'Brambles', tile.pos);
    tiles.pop(); // skip adjacent tile
    num -= 2;
  }
}

export function addVibrantIvy(floor: Floor, room: Room | null): void {
  if (!room) return;
  const canOccupy = (t: Tile) =>
    (t instanceof Ground || t instanceof Water) &&
    floor.grasses.get(t.pos) == null &&
    floor.getCardinalNeighbors(t.pos).some(n => n instanceof Wall);
  const candidates = floor.enumerateRoomTiles(room).filter(canOccupy);
  const startTile = randomPick(candidates);
  if (!startTile) return;
  let num = 10;
  if (MyRandom.value < 0.2) num += 40;
  const bfs = floor.breadthFirstSearch(startTile.pos).filter(canOccupy).slice(0, num);
  for (const t of bfs) spawn(floor, 'VibrantIvy', t.pos);
}

export function addNecroroot(floor: Floor, room: Room | null): void {
  if (!room) return;
  const tiles = floor.enumerateRoomTiles(room)
    .filter(t => t instanceof Ground && floor.grasses.get(t.pos) == null)
    .sort((a, b) => Vector2Int.distance(a.pos, room.centerFloat as any) - Vector2Int.distance(b.pos, room.centerFloat as any));
  for (const tile of tiles.slice(0, Math.floor(tiles.length / 2))) {
    spawn(floor, 'Necroroot', tile.pos);
  }
}

export function addFaegrass(floor: Floor, _room: Room | null): void {
  const num = MyRandom.Range(10, 20);
  const tiles = [...floor.enumerateFloor()]
    .map(p => floor.tiles.get(p))
    .filter((t): t is Tile => t != null && t instanceof Ground && floor.grasses.get(t.pos) == null);
  MyRandom.Shuffle(tiles);
  let placed = 0;
  for (const tile of tiles) {
    if (!floor.getAdjacentTiles(tile.pos).some(t => {
      const g = floor.grasses.get(t.pos);
      return g && (g as any).constructor?.name === 'Faegrass';
    })) {
      if (spawn(floor, 'Faegrass', tile.pos)) {
        placed++;
        if (placed >= num) break;
      }
    }
  }
}

export function addEveningBells(floor: Floor, room: Room | null): void {
  const empty = FloorUtils.emptyTilesInRoom(floor, room);
  if (empty.length === 0) return;
  const score = (t: Tile) =>
    floor.getAdjacentTiles(t.pos).filter(a => EveningBellsCanOccupy(a, floor)).length;
  const sorted = [...empty].sort((a, b) => score(b) - score(a));
  const highestScore = score(sorted[0]);
  const tilesWithBestScore = sorted.filter(t => score(t) === highestScore);
  const tile = randomPick(tilesWithBestScore);
  if (!tile) return;
  const center = tile.pos;
  spawn(floor, 'Stump', center);
  for (const adj of floor.getAdjacentTiles(center)) {
    if (EveningBellsCanOccupy(adj, floor)) {
      // Angle from downward direction to direction from center to bell
      const dx = adj.pos.x - center.x;
      const dy = adj.pos.y - center.y;
      const angle = Math.atan2(dy, -dx) * (180 / Math.PI) + 90;
      spawn(floor, 'EveningBells', adj.pos, angle);
    }
  }
}

function EveningBellsCanOccupy(t: Tile, floor: Floor): boolean {
  return t instanceof Ground && t.canBeOccupied() && floor.grasses.get(t.pos) == null;
}

export function addLlaora(floor: Floor, room: Room | null): void {
  const tile = spectrumPos(floor, 0.25)[0];
  // const tile = randomPick(
  //   FloorUtils.tilesFromCenter(floor, room)
  //     .filter(t => t instanceof Ground && floor.grasses.get(t.pos) == null && t.pos.x <= 5),
  // );
  if (tile) spawn(floor, 'Llaora', tile.pos);
}

export function addBloodwort(floor: Floor, room: Room | null): void {
  if (!room) return;
  const tile = randomPick(
    FloorUtils.tilesFromCenter(floor, room)
      .filter(t => t instanceof Ground && floor.grasses.get(t.pos) == null && t.pos.x < room.center.x),
  );
  if (tile) spawn(floor, 'Bloodwort', tile.pos);
}

export function addBloodstone(floor: Floor, room: Room | null): void {
  const tile = randomPick(
    FloorUtils.emptyTilesInRoom(floor, room).filter(t => t.pos.x < 3),
  );
  if (tile) spawn(floor, 'Bloodstone', tile.pos);
}

export function addGoldGrass(floor: Floor, room: Room | null): void {
  if (!room) return;
  const roomTiles = floor.enumerateRoomTiles(room);
  const innerTiles = floor.enumerateRoomTiles(room, -1);
  const innerSet = new Set(innerTiles);
  const perimeter = roomTiles.filter(t => !innerSet.has(t) && t.canBeOccupied() && t instanceof Ground && floor.grasses.get(t.pos) == null);
  const start = randomPick(perimeter);
  if (!start) return;
  const candidates = roomTiles.filter(t => t.canBeOccupied() && t instanceof Ground && floor.grasses.get(t.pos) == null);
  const end = candidates.sort((a, b) => Vector2Int.distance(b.pos, start.pos) - Vector2Int.distance(a.pos, start.pos))[0];
  if (!end) return;
  const path = floor.findPath(start.pos, end.pos, true);
  for (const pos of path) {
    const tile = floor.tiles.get(pos);
    if (tile instanceof Ground) spawn(floor, 'GoldGrass', pos);
  }
}

export function addRedcaps(floor: Floor, room: Room | null): void {
  const corners = FloorUtils.tilesSortedByCorners(floor, room).filter(t => t instanceof Ground).slice(0, 9);
  const start = randomPick(corners);
  if (!start) return;
  const num = MyRandom.Range(2, 6);
  const bfs = [...floor.breadthFirstSearch(start.pos, t => t instanceof Ground)].slice(0, num);
  for (const t of bfs) spawn(floor, 'Redcap', t.pos);
}

export function addTunnelroot(floor: Floor, room: Room | null): void {
  const start = FloorUtils.tilesAwayFromCenter(floor, room)
    .filter(t => Tunnelroot.canOccupy(t))
    .slice(MyRandom.Range(0, 4))[0];
  if (!start) return;

  const partner = randomPick(
    floor.enumerateRoomTiles(floor.root)
      .filter(tile =>
        tile !== start &&
        Tunnelroot.canOccupy(tile) &&
        tile.canBeOccupied() &&
        !floor.getAdjacentTiles(tile.pos).some(t2 =>
          t2.grass instanceof Tunnelroot ||
          (floor.downstairsPos != null && Vector2Int.equals(t2.pos, floor.downstairsPos))
        )
      )
      .sort((a, b) => Vector2Int.distance(b.pos, start.pos) - Vector2Int.distance(a.pos, start.pos))
      .slice(0, 8)
  );
  if (!partner) return;

  const root1 = new Tunnelroot(start.pos);
  const root2 = new Tunnelroot(partner.pos);
  floor.put(root1);
  floor.put(root2);
  root1.partnerWith(root2);
}

export const addTunnelroot4x: Encounter = twice(twice(addTunnelroot));

export function addCrabs(floor: Floor, room: Room | null): void {
  const tiles = spectrumPos(floor, MyRandom.value * 0.2 + 0.4);
  for (const t of tiles.slice(0, 2)) spawn(floor, 'Crab', t.pos);
}

export function addScuttlers(floor: Floor, room: Room | null): void {
  const tiles = FloorUtils.emptyTilesInRoom(floor, room).filter(t => floor.grasses.get(t.pos) == null);
  const startTile = randomPick(tiles);
  if (!startTile) return;
  const tileSet = new Set(tiles);
  const bfs = [...floor.breadthFirstSearch(startTile.pos, t => tileSet.has(t))].slice(0, 3);
  for (const t of bfs) spawn(floor, 'ScuttlerUnderground', t.pos);
}

export const addScuttlers4x: Encounter = twice(twice(addScuttlers));

export function addFruitingBodies(floor: Floor, room: Room | null): void {
  const positions = FloorUtils.emptyTilesInRoom(floor, room);
  MyRandom.Shuffle(positions);
  const num = 3; // MyRandom.Range(3, Math.floor((positions.length + 1) / 4));
  for (const tile of positions.slice(0, num)) {
    spawn(floor, 'FruitingBody', tile.pos);
  }
}

export function addMushroom(floor: Floor, room: Room | null): void {
  const livable = floor.enumerateRoomTiles(room).filter(t => t instanceof Ground);
  MyRandom.Shuffle(livable);
  const num = MyRandom.Range(6, 12);
  for (const t of livable.slice(0, num)) spawn(floor, 'Mushroom', t.pos);
}

export function addPumpkin(floor: Floor, room: Room | null): void {
  const tile = randomPick(FloorUtils.emptyTilesInRoom(floor, room));
  if (tile) spawn(floor, 'Pumpkin', tile.pos);
}

export function scatteredBoombugs(floor: Floor, room: Room | null): void {
  const tiles = spectrumPos(floor, 0.3);
  for (const t of tiles.slice(0, 2)) spawn(floor, 'Boombug', t.pos);
}

export const scatteredBoombugs4x: Encounter = (floor, room) => {
  const startTile = randomPick(FloorUtils.emptyTilesInRoom(floor, room));
  if (!startTile) return;
  const num = MyRandom.Range(4, 11);
  const bfs = [...floor.breadthFirstSearch(startTile.pos, t => t.canBeOccupied())].slice(0, num);
  for (const t of bfs) spawn(floor, 'Boombug', t.pos);
};

// ---- Fill encounters ----

export function fillWithFerns(floor: Floor, room: Room | null): void {
  fillWithGrass(floor, room, 'Fern');
}

export function fillWithBladegrass(floor: Floor, room: Room | null): void {
  fillWithGrass(floor, room, 'Bladegrass');
}

export function fillWithViolets(floor: Floor, room: Room | null): void {
  fillWithGrass(floor, room, 'Violets');
}

export function fillWithGuardleaf(floor: Floor, room: Room | null): void {
  fillWithGrass(floor, room, 'Guardleaf');
}

function fillWithGrass(floor: Floor, room: Room | null, entityName: string): void {
  if (!room) return;
  for (const tile of floor.enumerateRoomTiles(room)) {
    if (tile instanceof Ground && floor.grasses.get(tile.pos) == null) {
      spawn(floor, entityName, tile.pos);
    }
  }
}

// ---- Plant encounters (stub — plants not ported) ----

export function matureBerryBush(floor: Floor, room: Room | null): void { plantStub(floor, room, 'BerryBush'); }
export function matureThornleaf(floor: Floor, room: Room | null): void { plantStub(floor, room, 'Thornleaf'); }
export function matureWildWood(floor: Floor, room: Room | null): void { plantStub(floor, room, 'Wildwood'); }
export function matureWeirdwood(floor: Floor, room: Room | null): void { plantStub(floor, room, 'Weirdwood'); }
export function matureKingshroom(floor: Floor, room: Room | null): void { plantStub(floor, room, 'Kingshroom'); }
export function matureFrizzlefen(floor: Floor, room: Room | null): void { plantStub(floor, room, 'Frizzlefen'); }
export function matureChangErsWillow(floor: Floor, room: Room | null): void { plantStub(floor, room, 'ChangErsWillow'); }
export function matureStoutShrub(floor: Floor, room: Room | null): void { plantStub(floor, room, 'StoutShrub'); }
export function matureBroodpuff(floor: Floor, room: Room | null): void { plantStub(floor, room, 'Broodpuff'); }

function plantStub(_floor: Floor, _room: Room | null, _name: string): void {
  // Plants need GoNextStage() twice — will be implemented when Plant class is ported
  // For now, consume no RNG to keep stream consistent
}

// ---- Item encounters (stub — items not ported) ----

export function oneButterfly(floor: Floor, room: Room | null): void {
  const tile = randomPick(FloorUtils.emptyTilesInRoom(floor, room));
  if (tile) spawn(floor, 'ItemOnGround_Butterfly', tile.pos);
}

export function addSpiderSandals(floor: Floor, room: Room | null): void {
  const tile = randomPick(FloorUtils.emptyTilesInRoom(floor, room));
  if (tile) spawn(floor, 'ItemOnGround_SpiderSandals', tile.pos);
}

// ---- NPC encounters (stub) ----

export function addOldDude(_floor: Floor, _room: Room | null): void {
  // NPC — will be ported later
}

export function addGambler(_floor: Floor, _room: Room | null): void {
  // NPC — will be ported later
}

export function addMercenary(_floor: Floor, _room: Room | null): void {
  // NPC — will be ported later
}

// ---- Boss floor special encounters ----

export function fungalColonyAnticipation(floor: Floor, _room: Room | null): void {
  if (!floor.downstairsPos) return;
  const dp = floor.downstairsPos;
  const min = Vector2Int.sub(dp, new Vector2Int(2, 2));
  const max = Vector2Int.add(dp, new Vector2Int(3, 3));
  for (const pos of floor.enumerateRectangle(min, max)) {
    if (!Vector2Int.equals(pos, dp) && MyRandom.value < 0.75) {
      spawn(floor, 'FungalWall', pos) || floor.put(new Wall(pos));
    }
  }
}

export function oneAstoria(floor: Floor, room: Room | null): void {
  const tiles = FloorUtils.tilesSortedByCorners(floor, room)
    .filter(t => t.canBeOccupied() && t instanceof Ground && floor.grasses.get(t.pos) == null);
  for (const t of tiles.slice(0, 1)) spawn(floor, 'Astoria', t.pos);
}

export function twelveRandomAstoria(floor: Floor, room: Room | null): void {
  const tiles = FloorUtils.emptyTilesInRoom(floor, room);
  MyRandom.Shuffle(tiles);
  for (const t of tiles.slice(0, 12)) spawn(floor, 'Astoria', t.pos);
}

/**
 * Encounter map — all encounters keyed by name for EncounterGroup population.
 * This is the single source of truth for encounter names.
 */
export const allEncounters: Record<string, Encounter> = {
  empty, wallPillars, concavity, chunkInMiddle, lineWithOpening, insetLayerWithOpening,
  addStalk, rubbleCluster, chasmsAwayFromWalls2, chasmsAwayFromWalls1,
  chasmBridge, chasmGrowths,
  addWater, addHomeWater, addOneWater,
  addDownstairsInRoomCenter, placeFancyGround, surroundWithRubble,
  addBird, addSnake, aFewBlobs, addWallflowers, jackalPile, addSkullys,
  addOctopus, aFewSnails, addSpiders, addBats,
  addFungalSentinel, addFungalBreeder, addScorpions, addGolems,
  addParasite, addParasite8x, addHydra, addClumpshroom, addGrasper,
  addThistlebog, addWildekins, addDizapper, addGoo, addHardShell,
  addHoppers, addHealer, addPoisoner, addMuckola, addIronJelly,
  addSoftGrass, addBladegrass, addGuardleaf, addGuardleaf4x, addViolets,
  addDeathbloom, addDeathbloom4x, addWebs, addHangingVines, addHangingVines2x,
  addSpore, addSpore8x, addAgave, addPoisonmoss, addBrambles,
  addVibrantIvy, addNecroroot, addFaegrass, addEveningBells,
  addLlaora, addBloodwort, addBloodstone, addGoldGrass, addRedcaps,
  addTunnelroot, addTunnelroot4x, addCrabs, addScuttlers, addScuttlers4x,
  addFruitingBodies, addMushroom, addPumpkin,
  scatteredBoombugs, scatteredBoombugs4x,
  fillWithFerns, fillWithBladegrass, fillWithViolets, fillWithGuardleaf,
  matureBerryBush, matureThornleaf, matureWildWood, matureWeirdwood,
  matureKingshroom, matureFrizzlefen, matureChangErsWillow,
  matureStoutShrub, matureBroodpuff,
  oneButterfly, addSpiderSandals,
  addOldDude, addGambler, addMercenary,
  fungalColonyAnticipation, oneAstoria, twelveRandomAstoria,
};
