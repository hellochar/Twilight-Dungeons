/**
 * Main floor generation pipeline.
 * Port of C# FloorGenerator.cs — all 28 depth-specific generators.
 */
import { Vector2Int } from '../core/Vector2Int';
import { MyRandom } from '../core/MyRandom';
import { Floor } from '../model/Floor';
import { Ground, Wall, Chasm, FancyGround, Soil, Signpost } from '../model/Tile';
import { Room } from './Room';
import * as FloorUtils from './FloorUtils';
import * as TileGroup from './TileGroup';
import { EncounterGroup, createEncounterGroups, type Encounter } from './EncounterGroup';
import { allEncounters as E } from './Encounters';
import { entityRegistry } from './entityRegistry';
import { tipMap } from './Tips';

export class FloorGenerator {
  encounterGroup!: EncounterGroup;
  floorSeeds: number[];
  // aka basic
  private earlyGame: EncounterGroup;
  // aka medium
  private everything: EncounterGroup;
  // aka complex
  private midGame: EncounterGroup;

  private shared: EncounterGroup;
  private floorGenerators: Array<() => Floor>;

  constructor(floorSeeds: number[]) {
    this.floorSeeds = floorSeeds;

    // Ensure we have enough seeds
    while (this.floorSeeds.length < 28) {
      this.floorSeeds.push(MyRandom.Range(0, 0x7fffffff));
    }

    const groups = createEncounterGroups(E);
    this.shared = groups.shared;
    this.earlyGame = groups.earlyGame;
    this.everything = groups.everything;
    this.midGame = groups.midGame;

    this.floorGenerators = this.initFloorGenerators();
  }

  private initFloorGenerators(): Array<() => Floor> {
    var basic = () => this.generateSingleRoomFloor(3, 7, 10, 2, 1);
    var medium = () => this.generateSingleRoomFloor(14, 8, 12, 3, 2);
    var complex = () => this.generateSingleRoomFloor(23, 9, 14, 5, 4);

    return [
      // early game (depths 0-9)
      () => this.generateFloor0(0),
      () => this.generateSingleRoomFloor(1, 6, 9, 1, 1),
      () => this.generateSingleRoomFloor(2, 6, 9, 1, 1, false, undefined, E.oneAstoria),
      basic,
      () => this.generateSingleRoomFloor(4, 7, 9, 2, 1, true, undefined, E.oneAstoria),
      () => this.generateSingleRoomFloor(5, 8, 10, 3, 2),
      () => this.generateSingleRoomFloor(6, 8, 10, 3, 2, false, undefined, E.addOneWater),
      () => this.generateSingleRoomFloor(7, 8, 10, 3, 3),
      () => this.generateSingleRoomFloor(8, 8, 10, 3, 3),
      () => this.generateSingleRoomFloor(9, 8, 10, 3, 3),
      // () => this.generateRewardFloor(8, this.shared.plants.getRandomAndDiscount(1), E.addDownstairsInRoomCenter),
      // () => this.generateBlobBossFloor(9),

      // midgame (depths 10-18)
      () => this.generateSingleRoomFloor(10, 7, 9, 2, 1),
      () => this.generateSingleRoomFloor(11, 7, 9, 2, 1, false, undefined, E.addOneWater),
      () => this.generateSingleRoomFloor(12, 8, 10, 3, 1),
      () => this.generateSingleRoomFloor(13, 8, 10, 3, 2, true, undefined, E.oneAstoria),
      medium,
      () => this.generateSingleRoomFloor(15, 8, 12, 4, 3),
      () => this.generateSingleRoomFloor(16, 8, 12, 5, 5),
      () => this.generateSingleRoomFloor(17, 8, 12, 5, 5),
      () => this.generateSingleRoomFloor(18, 8, 12, 5, 5),
      // () => this.generateRewardFloor(17, E.addDownstairsInRoomCenter, E.fungalColonyAnticipation, this.shared.plants.getRandomAndDiscount(1)),
      // () => this.generateFungalColonyBossFloor(18),

      // endgame (depths 19-27)
      () => this.generateSingleRoomFloor(19, 8, 10, 2, 2),
      () => this.generateSingleRoomFloor(20, 8, 12, 3, 2),
      () => this.generateSingleRoomFloor(21, 9, 14, 4, 3, true, [E.lineWithOpening, E.chasmsAwayFromWalls1]),
      () => this.generateSingleRoomFloor(22, 9, 13, 5, 2, false, undefined, E.addWater),
      complex,
      () => this.generateSingleRoomFloor(24, 9, 14, 7, 4),
      () => this.generateSingleRoomFloor(25, 9, 14, 8, 5),
      () => this.generateSingleRoomFloor(26, 9, 14, 9, 6),
      () => this.generateSingleRoomFloor(27, 9, 14, 9, 6),
      // () => this.generateEndFloor(27),
    ];
  }

  /** Main entry point: generate a floor for a given depth */
  generateCaveFloor(depth: number): Floor {
    // Configure encounter group by depth
    if (depth <= 9) {
      this.encounterGroup = this.earlyGame;
    } else if (depth <= 18) {
      this.encounterGroup = this.everything;
    } else {
      this.encounterGroup = this.midGame;
    }

    // Set the seed
    MyRandom.setSeed(this.floorSeeds[depth]);

    const generator = this.floorGenerators[depth];
    let floor: Floor | null = null;
    let lastError: Error | null = null;

    for (let guard = 0; floor == null && guard < 20; guard++) {
      try {
        floor = generator();
      } catch (e) {
        lastError = e as Error;
        console.error(`Floor generation attempt ${guard} failed for depth ${depth}:`, e);
        MyRandom.Range(0, 100); // advance RNG stream
      }
    }

    if (!floor) {
      throw lastError ?? new Error(`Failed to generate floor at depth ${depth}`);
    }

    if (floor.depth !== depth) {
      throw new Error(`floorGenerator depth ${depth} produced floor marked as depth ${floor.depth}`);
    }

    this.postProcessFloor(floor);
    return floor;
  }

  private postProcessFloor(floor: Floor): void {
    // Direct cleared check — GameModel doesn't exist yet during generation,
    // so we can't use floor.checkCleared() (which defers via event queue).
    if (floor.enemiesLeft() === 0) {
      floor.isCleared = true;
    }
    // this.postProcessAddSignpost(floor);
  }

  private postProcessAddSignpost(floor: Floor): void {
    const tip = tipMap.get(floor.depth);
    if (!tip) return;
    const searchStart = floor.startPos;
    const candidate = [...floor.breadthFirstSearch(searchStart)]
      .slice(5)
      .find(t => t instanceof Ground && t.canBeOccupied() && floor.grasses.get(t.pos) == null);
    if (candidate) {
      floor.put(new Signpost(candidate.pos, tip));
    }
  }

  // ---- Floor 0: Tutorial / Home ----

  private generateFloor0(depth: number): Floor {
    const floor = new Floor(depth, 7, 12);
    floor.isCleared = true;

    FloorUtils.carveGround(floor);
    FloorUtils.surroundWithWalls(floor);

    // Soil rows
    const soils: Soil[] = [];
    for (let x = 2; x < floor.width - 2; x += 2) {
      let y = Math.floor(floor.height / 2) - 1;
      let tile = floor.tiles.get(new Vector2Int(x, y));
      if (tile instanceof Ground) soils.push(new Soil(new Vector2Int(x, y)));
      y = Math.floor(floor.height / 2) + 1;
      tile = floor.tiles.get(new Vector2Int(x, y));
      if (tile instanceof Ground) soils.push(new Soil(new Vector2Int(x, y)));
    }
    floor.putAll(soils);

    const room0 = Room.fromFloor(floor);
    floor.rooms = [room0];
    floor.root = room0;

    // Place plant (or no-op if not ported)
    this.encounterGroup.plants.getRandomAndDiscount(1)?.(floor, room0);

    // Downstairs
    floor.downstairsPos = new Vector2Int(Math.floor(floor.width / 2), floor.height - 1);

    E.addHomeWater(floor, room0);

    FloorUtils.tidyUpAroundStairs(floor);
    return floor;
  }

  // ---- Reward Floor ----

  private generateRewardFloor(depth: number, ...extraEncounters: Encounter[]): Floor {
    const floor = new Floor(depth, 8, 12);
    FloorUtils.carveGround(floor);
    FloorUtils.surroundWithWalls(floor);
    FloorUtils.naturalizeEdges(floor);

    const room0 = Room.fromFloor(floor);
    E.addOneWater(floor, room0);
    for (const enc of extraEncounters) {
      enc(floor, room0);
    }

    this.encounterGroup.rewards.getRandomAndSubtractWeight(1)?.(floor, room0);

    FloorUtils.tidyUpAroundStairs(floor);
    floor.root = room0;
    return floor;
  }

  // ---- Single Room Floor (the main workhorse) ----

  generateSingleRoomFloor(
    depth: number, width: number, height: number,
    numMobs: number, numGrasses: number,
    addWater = false,
    preMobEncounters?: Encounter[],
    ...extraEncounters: Encounter[]
  ): Floor {
    const floor = this.tryGenerateSingleRoomFloor(depth, width, height, preMobEncounters == null);

    // Ensure perimeter is walls
    for (const pos of floor.enumeratePerimeter()) {
      const tile = floor.tiles.get(pos);
      if (tile instanceof Ground) {
        floor.put(new Wall(pos));
      }
    }

    const room0 = floor.root!;

    if (preMobEncounters) {
      for (const enc of preMobEncounters) {
        enc(floor, room0);
      }
    }

    // X mobs
    for (let i = 0; i < numMobs; i++) {
      this.encounterGroup.mobs.getRandomAndDiscount()?.(floor, room0);
    }

    // Y grasses
    for (let i = 0; i < numGrasses; i++) {
      this.encounterGroup.grasses.getRandomAndDiscount()?.(floor, room0);
    }

    // Spice
    this.encounterGroup.spice.getRandom()?.(floor, room0);

    for (const enc of extraEncounters) {
      enc(floor, room0);
    }

    // Water
    E.addOneWater(floor, room0);
    if (addWater) {
      E.addWater(floor, room0);
    }

    FloorUtils.tidyUpAroundStairs(floor);
    return floor;
  }

  private tryGenerateSingleRoomFloor(depth: number, width: number, height: number, defaultEncounters = true): Floor {
    const floor = new Floor(depth, width, height);
    floor.startPos = new Vector2Int(Math.floor(floor.width / 2), 1);

    // Fill with walls
    for (const p of floor.enumerateFloor()) {
      floor.put(new Wall(p));
    }

    const room0 = Room.fromFloor(floor);
    FloorUtils.carveGround(floor, floor.enumerateRoom(room0));

    if (defaultEncounters) {
      // One wall variation
      this.encounterGroup.walls.getRandomAndDiscount()?.(floor, room0);
      FloorUtils.naturalizeEdges(floor);

      // Chasms (rare — 4% discount rate for exponential decrease)
      this.encounterGroup.chasms.getRandomAndDiscount(0.04)?.(floor, room0);
    }

    ensureConnectedness(floor);

    floor.root = room0;
    floor.rooms = [];
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    return floor;
  }

  // ---- Boss Floors ----

  private generateBlobBossFloor(depth: number): Floor {
    const floor = new Floor(depth, 9, 14);

    // Fill with ground
    for (const p of floor.enumerateFloor()) {
      floor.put(new Ground(p));
    }
    for (const p of floor.enumeratePerimeter()) {
      floor.put(new Wall(p));
    }

    // Corner walls
    floor.put(new Wall(new Vector2Int(1, 1)));
    floor.put(new Wall(new Vector2Int(floor.width - 2, 1)));
    floor.put(new Wall(new Vector2Int(floor.width - 2, floor.height - 2)));
    floor.put(new Wall(new Vector2Int(1, floor.height - 2)));

    const room0 = Room.fromFloor(floor);

    // Cross walls around center
    floor.put(new Wall(Vector2Int.add(room0.center, new Vector2Int(2, 2))));
    floor.put(new Wall(Vector2Int.add(room0.center, new Vector2Int(2, -2))));
    floor.put(new Wall(Vector2Int.add(room0.center, new Vector2Int(-2, -2))));
    floor.put(new Wall(Vector2Int.add(room0.center, new Vector2Int(-2, 2))));

    this.encounterGroup.grasses.getRandomAndDiscount()?.(floor, room0);

    floor.root = room0;
    floor.rooms = [];
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    // Boss at top-center
    entityRegistry.create('Blobmother', new Vector2Int(Math.floor(floor.width / 2), floor.height - 2));

    FloorUtils.tidyUpAroundStairs(floor);
    return floor;
  }

  private generateFungalColonyBossFloor(depth: number): Floor {
    const floor = new Floor(depth, 9, 13);

    for (const p of floor.enumerateFloor()) {
      floor.put(new Wall(p));
    }

    const room0 = Room.fromFloor(floor);

    // Carve circles: entrance, exit, center boss chamber
    const cutCircle = (center: Vector2Int, radius: number) => {
      FloorUtils.carveGround(floor, floor.enumerateCircle(center, radius));
    };

    cutCircle(new Vector2Int(Math.floor(floor.width / 2), 3), 2);
    cutCircle(new Vector2Int(Math.floor(floor.width / 2), floor.height - 4), 2);
    cutCircle(room0.center, 4.5);

    // Boss in center
    entityRegistry.create('FungalColony', room0.center);

    // Convert some walls/ground to fungal walls
    for (const pos of floor.enumerateFloor()) {
      const t = floor.tiles.get(pos);
      if (t instanceof Wall && floor.getAdjacentTiles(t.pos).some(t2 => t2.canBeOccupied())) {
        // FungalWall or regular Wall
        if (!entityRegistry.create('FungalWall', pos)) {
          // FungalWall not ported — leave as Wall
        } else {
          floor.put(entityRegistry.create('FungalWall', pos)!);
        }
      } else if (t instanceof Ground && t.canBeOccupied()) {
        if (MyRandom.value < 0.25) {
          const fw = entityRegistry.create('FungalWall', pos);
          if (fw) floor.put(fw); else floor.put(new Wall(pos));
        }
      }
    }

    // Re-apply normal walls to outer perimeter
    for (const pos of floor.enumeratePerimeter()) {
      floor.put(new Wall(pos));
    }

    floor.root = room0;
    floor.rooms = [];
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    FloorUtils.tidyUpAroundStairs(floor);
    return floor;
  }

  // ---- End Floor (Tree of Life) ----

  private generateEndFloor(depth: number): Floor {
    const floor = new Floor(depth, 14, 42);
    FloorUtils.surroundWithWalls(floor);

    const room0 = Room.fromFloor(floor);
    floor.rooms = [room0];
    floor.root = room0;

    const treePos = Vector2Int.add(room0.center, Vector2Int.scale(Vector2Int.up, 10));

    // Carve wave-pattern chasms/ground
    for (let y = 0; y < floor.height; y++) {
      const angle = mapLinear(y, 0, floor.height / 2, 0, Math.PI / 2 * 1.7);
      const w = Math.max(0, Math.cos(angle)) * (floor.width - 2) + 1;
      const xMin = (floor.width / 2 - w / 2) - 1;
      const xMax = xMin + w;
      for (let x = 0; x < floor.width; x++) {
        const pos = new Vector2Int(x, y);
        if (x < xMin || x > xMax || y > treePos.y) {
          floor.put(new Chasm(pos));
        } else if (floor.tiles.get(pos) == null) {
          floor.put(new Ground(pos));
        }
      }
    }

    floor.startPos = new Vector2Int(Math.floor(floor.width / 2) - 1, 1);

    const roomBot = new Room(Vector2Int.add(Vector2Int.zero, new Vector2Int(1, 1)), new Vector2Int(floor.width - 2, 11));
    E.addWater(floor, roomBot);
    E.twelveRandomAstoria(floor, roomBot);
    for (let i = 0; i < 12; i++) E.addGuardleaf(floor, roomBot);

    // Fill remaining ground with soft grass
    for (const pos of floor.enumerateRoom(roomBot)) {
      const tile = floor.tiles.get(pos);
      if (tile instanceof Ground && floor.grasses.get(pos) == null) {
        entityRegistry.create('SoftGrass', pos); // May no-op
      }
    }

    // FancyGround circle around tree
    for (const pos of floor.enumerateCircle(Vector2Int.add(treePos, Vector2Int.down), 2.5)) {
      if (pos.y <= treePos.y) {
        const grass = floor.grasses.get(pos);
        if (grass) floor.remove(grass);
        floor.put(new FancyGround(pos));
      }
    }

    // Tree of Life and Ezra
    entityRegistry.create('TreeOfLife', treePos);
    entityRegistry.create('Ezra', Vector2Int.add(treePos, Vector2Int.scale(Vector2Int.down, 2)));

    return floor;
  }

  // ---- Multi-Room Floor ----

  generateMultiRoomFloor(
    depth: number, width = 60, height = 20, numSplits = 20,
    hasReward = false, ...specialDownstairsEncounters: Encounter[]
  ): Floor {
    const floor = this.tryGenerateMultiRoomFloor(depth, width, height, numSplits);
    ensureConnectedness(floor);

    const intermediateRooms = floor.rooms
      .filter(room => room !== floor.upstairsRoom && room !== floor.downstairsRoom);

    let rewardRoom: Room | null = null;
    if (hasReward && intermediateRooms.length > 0) {
      rewardRoom = intermediateRooms
        .sort((a, b) => {
          const pathA = floor.findPath(floor.startPos, a.center);
          const pathB = floor.findPath(floor.startPos, b.center);
          return pathB.length - pathA.length;
        })[0];

      E.placeFancyGround(floor, rewardRoom);
      E.surroundWithRubble(floor, rewardRoom);
      this.encounterGroup.rewards.getRandomAndDiscount()?.(floor, rewardRoom);
    }

    // Dead-end rooms get spice
    const deadEndRooms = intermediateRooms
      .filter(room => room !== rewardRoom && room.connections.length < 2);
    for (const room of deadEndRooms) {
      if (MyRandom.value < 0.05) {
        E.surroundWithRubble(floor, room);
      }
      this.encounterGroup.spice.getRandomAndDiscount()?.(floor, room);
    }

    // Mobs in every room except upstairs and reward
    for (const room of floor.rooms) {
      if (room !== floor.upstairsRoom && room !== rewardRoom) {
        this.encounterGroup.mobs.getRandomAndDiscount()?.(floor, room);
      }
    }

    // Grasses in BSP nodes at depth >= 2
    for (const room of floor.root!.traverse()) {
      if (room.depth >= 2) {
        this.encounterGroup.grasses.getRandomAndDiscount()?.(floor, room);
      }
    }

    for (const enc of specialDownstairsEncounters) {
      enc(floor, floor.downstairsRoom ?? null);
    }

    FloorUtils.tidyUpAroundStairs(floor);
    return floor;
  }

  private tryGenerateMultiRoomFloor(depth: number, width: number, height: number, numSplits: number): Floor {
    const floor = new Floor(depth, width, height);

    // Fill with walls
    for (const p of floor.enumerateFloor()) {
      floor.put(new Wall(p));
    }

    // BSP partition
    const root = Room.fromFloor(floor);
    for (let i = 0; i < numSplits; i++) {
      if (!root.randomlySplit()) break;
    }

    const rooms = [...root.traverse()].filter(n => n.isTerminal);
    rooms.forEach(room => room.randomlyShrink());

    // Sort by distance to top-left
    const topLeft = new Vector2Int(0, floor.height);
    rooms.sort((a, b) => {
      const da = Vector2Int.manhattanDistance(Vector2Int.sub(a.getTopLeft(), topLeft), Vector2Int.zero);
      const db = Vector2Int.manhattanDistance(Vector2Int.sub(b.getTopLeft(), topLeft), Vector2Int.zero);
      return da - db;
    });

    const upstairsRoom = rooms[0];
    const downstairsRoom = rooms[rooms.length - 1];

    floor.startPos = new Vector2Int(upstairsRoom.min.x + 1, upstairsRoom.max.y - 1);
    floor.downstairsPos = new Vector2Int(downstairsRoom.max.x - 1, downstairsRoom.min.y + 1);

    floor.root = root;
    floor.rooms = rooms;
    floor.upstairsRoom = upstairsRoom;
    floor.downstairsRoom = downstairsRoom;

    // Carve ground in each room
    for (const room of rooms) {
      FloorUtils.carveGround(floor, floor.enumerateRoom(room));
    }

    // Connect rooms: 20% use connectedness algorithm, 80% use BSP sibling connections
    if (MyRandom.value < 0.2) {
      ensureConnectedness(floor);
    } else {
      for (const [a, b] of computeRoomConnections(rooms, root)) {
        FloorUtils.carveGround(floor, FloorUtils.line3x3(floor, a, b));
      }
    }

    FloorUtils.naturalizeEdges(floor);

    // Occasionally create chasm in a depth-2 room
    if (MyRandom.value < 0.1) {
      const depth2Rooms = [...root.traverse()].filter(r => r.depth === 2);
      if (depth2Rooms.length > 0) {
        const chasmRoom = depth2Rooms[MyRandom.Range(0, depth2Rooms.length)];
        E.chasmsAwayFromWalls1(floor, chasmRoom);
      }
    }

    return floor;
  }
}

// ---- Connectivity ----

function ensureConnectedness(floor: Floor): void {
  const walkableTiles = new Set<any>();
  for (const p of floor.enumerateFloor()) {
    const t = floor.tiles.get(p);
    if (t && t.basePathfindingWeight() !== 0) {
      walkableTiles.add(t);
    }
  }

  const startTile = floor.tiles.get(floor.startPos);
  if (!startTile || !walkableTiles.has(startTile)) return;

  let mainland = TileGroup.makeGroupAndRemove(startTile, walkableTiles);
  let guard = 0;
  while (walkableTiles.size > 0 && guard++ < 99) {
    const first = walkableTiles.values().next().value!;
    const island = TileGroup.makeGroupAndRemove(first, walkableTiles);
    // Connect island to mainland
    const newTiles = connectGroups(floor, mainland, island);
    for (const tile of newTiles) {
      floor.put(tile);
      mainland.add(tile);
    }
    for (const tile of island) {
      mainland.add(tile);
    }
  }
}

function connectGroups(floor: Floor, mainland: Set<any>, island: Set<any>): Ground[] {
  const mainlandArr = [...mainland];
  const islandArr = [...island];
  const mainlandNode = mainlandArr[MyRandom.Range(0, mainlandArr.length)];
  const islandNode = islandArr[MyRandom.Range(0, islandArr.length)];
  return FloorUtils.line3x3(floor, mainlandNode.pos, islandNode.pos)
    .filter(p => { const t = floor.tiles.get(p); return t && t.basePathfindingWeight() === 0; })
    .map(p => new Ground(p));
}

/** BSP sibling room connections — draw paths from parent center to child centers */
function computeRoomConnections(_rooms: Room[], root: Room): Array<[Vector2Int, Vector2Int]> {
  const paths: Array<[Vector2Int, Vector2Int]> = [];
  for (const node of root.traverse()) {
    if (node.isTerminal) continue;
    const nodeCenter = node.getCenter();
    const { a, b } = node.split!;
    a.connections.push(b);
    b.connections.push(a);
    paths.push([nodeCenter, a.getCenter()]);
    paths.push([nodeCenter, b.getCenter()]);
  }
  return paths;
}

/** Linear interpolation utility */
function mapLinear(value: number, from1: number, to1: number, from2: number, to2: number): number {
  return from2 + (value - from1) * (to2 - from2) / (to1 - from1);
}
