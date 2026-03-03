import { Entity, NoSpaceException } from './Entity';
import { Tile, Ground, Wall, type IBlocksVision } from './Tile';
import { Vector2Int } from '../core/Vector2Int';
import { TileVisibility, CollisionLayer, Faction } from '../core/types';
import { EventEmitter } from '../core/EventEmitter';
import { GameModelRef } from './GameModelRef';
import { MyRandom } from '../core/MyRandom';

// ─── Entity Store types ───

/** Dense 2D grid for non-moving entities (tiles, grasses, items, triggers). */
export class StaticEntityGrid<T extends Entity> {
  private grid: (T | null)[][];
  private placementBehavior: ((entity: T) => void) | null;
  readonly width: number;
  readonly height: number;

  constructor(
    private floor: Floor,
    placementBehavior?: (entity: T) => void,
  ) {
    this.width = floor.width;
    this.height = floor.height;
    this.grid = Array.from({ length: floor.width }, () =>
      Array.from<T | null>({ length: floor.height }, () => null),
    );
    this.placementBehavior = placementBehavior ?? ((entity: T) => {
      const old = this.get(entity.pos);
      if (old) old.kill(entity);
    });
  }

  get(pos: Vector2Int): T | null {
    if (!this.floor.inBounds(pos)) return null;
    return this.grid[pos.x][pos.y];
  }

  getXY(x: number, y: number): T | null {
    return this.grid[x]?.[y] ?? null;
  }

  put(entity: T): void {
    this.placementBehavior?.(entity);
    this.grid[entity.pos.x][entity.pos.y] = entity;
  }

  remove(entity: T): void {
    this.grid[entity.pos.x][entity.pos.y] = null;
  }

  has(pos: Vector2Int): boolean {
    return this.get(pos) != null;
  }

  *[Symbol.iterator](): IterableIterator<T> {
    for (let x = 0; x < this.width; x++) {
      for (let y = 0; y < this.height; y++) {
        const e = this.grid[x][y];
        if (e) yield e;
      }
    }
  }
}

/** Sparse list for moving entities (bodies). Maintains a lazy grid cache. */
export class MovingEntityList<T extends Entity> {
  private list: T[] = [];
  private grid: (T | null)[][] | null = null;
  private needsRecompute = true;
  private placementBehavior: ((entity: T) => void) | null;

  constructor(
    private floor: Floor,
    placementBehavior?: (entity: T) => void,
  ) {
    this.placementBehavior = placementBehavior ?? null;
  }

  scheduleRecompute(): void {
    this.needsRecompute = true;
  }

  get(pos: Vector2Int): T | null {
    if (!this.floor.inBounds(pos)) return null;
    this.ensureGrid();
    return this.grid![pos.x][pos.y];
  }

  private ensureGrid(): void {
    if (!this.needsRecompute && this.grid) return;
    if (!this.grid) {
      this.grid = Array.from({ length: this.floor.width }, () =>
        Array.from<T | null>({ length: this.floor.height }, () => null),
      );
    } else {
      for (let x = 0; x < this.floor.width; x++) {
        for (let y = 0; y < this.floor.height; y++) {
          this.grid[x][y] = null;
        }
      }
    }
    for (const t of this.list) {
      this.grid[t.pos.x][t.pos.y] = t;
    }
    this.needsRecompute = false;
  }

  put(entity: T): void {
    const tile = this.floor.tiles.get(entity.pos);
    if (this.get(entity.pos) != null && this.placementBehavior) {
      this.placementBehavior(entity);
    }
    if (entity.floor) {
      entity.floor.remove(entity);
    }
    this.list.push(entity);
    this.needsRecompute = true;
  }

  remove(entity: T): void {
    const idx = this.list.indexOf(entity);
    if (idx !== -1) this.list.splice(idx, 1);
    this.needsRecompute = true;
  }

  *[Symbol.iterator](): IterableIterator<T> {
    yield* this.list;
  }

  where(predicate: (e: T) => boolean): T[] {
    return this.list.filter(predicate);
  }

  get count(): number {
    return this.list.length;
  }
}

// ─── ISteppable interface ───

export interface ISteppable {
  timeNextAction: number;
  step(): number;
  turnPriority: number;
  catchUpStep?(lastTime: number, currentTime: number): void;
}

// ─── Floor ───

export class Floor {
  readonly width: number;
  readonly height: number;
  depth: number;
  isCleared = false;

  tiles: StaticEntityGrid<Tile>;
  grasses: StaticEntityGrid<Entity>;
  items: StaticEntityGrid<Entity>;
  triggers: StaticEntityGrid<Entity>;
  bodies: MovingEntityList<Entity>;
  entities = new Set<Entity>();
  steppableEntities: ISteppable[] = [];

  readonly onEntityAdded = new EventEmitter<[Entity]>();
  readonly onEntityRemoved = new EventEmitter<[Entity]>();

  startPos: Vector2Int;
  timePlayerEntered = 0;

  // BSP generator data (typed as any to avoid circular deps — Room imported lazily)
  root: any = null;
  rooms: any[] = [];
  upstairsRoom: any = null;
  downstairsRoom: any = null;
  downstairsPos: Vector2Int | null = null;

  constructor(depth: number, width: number, height: number) {
    this.depth = depth;
    this.width = width;
    this.height = height;
    this.startPos = new Vector2Int(1, Math.floor(height / 2));

    this.tiles = new StaticEntityGrid<Tile>(this);
    this.grasses = new StaticEntityGrid<Entity>(this);
    this.items = new StaticEntityGrid<Entity>(this, (item) => {
      // ItemOnGround placement: find nearest open tile via BFS
      const spot = this.breadthFirstSearch(item.pos)
        .find(t => t.canBeOccupied() && !this.items.has(t.pos));
      if (spot) {
        (item as any)._pos = spot.pos; // force position update for items
      }
    });
    this.triggers = new StaticEntityGrid<Entity>(this);
    this.bodies = new MovingEntityList<Entity>(this, (body) => {
      const spot = this.breadthFirstSearch(body.pos)
        .find(t => t.canBeOccupied());
      if (!spot) throw new NoSpaceException();
      body.pos = spot.pos;
    });
  }

  get boundsMin(): Vector2Int {
    return Vector2Int.zero;
  }
  get boundsMax(): Vector2Int {
    return new Vector2Int(this.width, this.height);
  }
  get center(): Vector2Int {
    return new Vector2Int(Math.floor(this.width / 2), Math.floor(this.height / 2));
  }

  bodyMoved(): void {
    this.bodies.scheduleRecompute();
  }

  inBounds(pos: Vector2Int): boolean {
    return pos.x >= 0 && pos.y >= 0 && pos.x < this.width && pos.y < this.height;
  }

  // ─── Entity management ───

  put(entity: Entity): void {
    try {
      this.entities.add(entity);

      if ('timeNextAction' in entity && typeof (entity as any).step === 'function') {
        this.steppableEntities.push(entity as unknown as ISteppable);
      }

      if (entity instanceof Tile) {
        this.tiles.put(entity);
      } else if (this.isBody(entity)) {
        this.bodies.put(entity);
      } else if (this.isGrass(entity)) {
        this.grasses.put(entity);
      } else if (this.isItem(entity)) {
        this.items.put(entity);
      } else {
        this.triggers.put(entity);
      }

      if ('blocksVision' in entity) {
        this.recomputeVisibility();
      }

      entity.setFloor(this);
      this.onEntityAdded.emit(entity);
    } catch (e) {
      if (e instanceof NoSpaceException) {
        this.remove(entity);
      } else {
        throw e;
      }
    }
  }

  remove(entity: Entity): void {
    if (!this.entities.has(entity)) {
      console.error('Removing', entity.toString(), 'from a floor it doesn\'t live in!');
      return;
    }
    this.entities.delete(entity);

    if ('timeNextAction' in entity) {
      const idx = this.steppableEntities.indexOf(entity as unknown as ISteppable);
      if (idx !== -1) this.steppableEntities.splice(idx, 1);
    }

    if (entity instanceof Tile) {
      this.tiles.remove(entity);
    } else if (this.isBody(entity)) {
      this.bodies.remove(entity);
    } else if (this.isGrass(entity)) {
      this.grasses.remove(entity);
    } else if (this.isItem(entity)) {
      this.items.remove(entity);
    } else {
      this.triggers.remove(entity);
    }

    if ('blocksVision' in entity) {
      this.recomputeVisibility();
    }

    entity.setFloor(null);
    this.onEntityRemoved.emit(entity);

    if (this.isEnemy(entity)) {
      this.checkCleared();
    }
  }

  putAll(entities: Entity[]): void {
    for (const e of entities) this.put(e);
  }

  // Type discrimination helpers — will be refined when Body/Actor/Grass classes exist
  private isBody(e: Entity): boolean {
    return 'hp' in e;
  }
  private isGrass(e: Entity): boolean {
    return '_isGrass' in e;
  }
  private isItem(e: Entity): boolean {
    return '_isItem' in e;
  }
  private isEnemy(e: Entity): boolean {
    return ('faction' in e && (e as any).faction === Faction.Enemy);
  }

  // ─── Queries ───

  enemiesLeft(): number {
    let count = 0;
    for (const b of this.bodies) {
      if (this.isEnemy(b)) count++;
    }
    return count;
  }

  /** Get 3x3 adjacent tiles (includes center tile at pos) */
  getAdjacentTiles(pos: Vector2Int): Tile[] {
    const list: Tile[] = [];
    const xMin = Math.max(pos.x - 1, 0);
    const xMax = Math.min(pos.x + 1, this.width - 1);
    const yMin = Math.max(pos.y - 1, 0);
    const yMax = Math.min(pos.y + 1, this.height - 1);
    for (let x = xMin; x <= xMax; x++) {
      for (let y = yMin; y <= yMax; y++) {
        const t = this.tiles.getXY(x, y);
        if (t) list.push(t);
      }
    }
    return list;
  }

  getCardinalNeighbors(pos: Vector2Int, includeSelf = false): Tile[] {
    const result: Tile[] = [];
    if (includeSelf) {
      const self = this.tiles.get(pos);
      if (self) result.push(self);
    }
    for (const dir of Vector2Int.cardinalDirections) {
      const p = Vector2Int.add(pos, dir);
      if (this.inBounds(p)) {
        const t = this.tiles.get(p);
        if (t) result.push(t);
      }
    }
    return result;
  }

  adjacentBodies(pos: Vector2Int): Entity[] {
    return this.getAdjacentTiles(pos)
      .map(t => t.body)
      .filter((b): b is Entity => b != null);
  }

  adjacentActors(pos: Vector2Int): Entity[] {
    return this.adjacentBodies(pos).filter(b => 'faction' in b);
  }

  // ─── Pathfinding (A*) ───

  findPath(start: Vector2Int, target: Vector2Int, pretendTargetEmpty = false, body?: Entity): Vector2Int[] {
    const startTile = this.tiles.get(start);
    const targetTile = this.tiles.get(target);
    if (!startTile || !targetTile) return [];

    const openSet: Tile[] = [startTile];
    const closedSet = new Set<Tile>();
    const gCosts = new Map<Tile, number>();
    const hCosts = new Map<Tile, number>();
    const parents = new Map<Tile, Tile>();

    gCosts.set(startTile, 0);

    const isTarget = (t: Tile) => Vector2Int.equals(t.pos, target);
    const canTraverse = (t: Tile) => {
      if (pretendTargetEmpty && isTarget(t)) return true;
      return body ? t.canBeOccupiedBy(body) : t.canBeOccupied();
    };
    const weight = (t: Tile) => {
      if (pretendTargetEmpty && isTarget(t)) return 1;
      return body ? t.getPathfindingWeightFor(body) : t.getPathfindingWeight();
    };

    while (openSet.length > 0) {
      // Find node with lowest fCost
      let bestIdx = 0;
      let bestF = (gCosts.get(openSet[0]) ?? 0) + (hCosts.get(openSet[0]) ?? 0);
      for (let i = 1; i < openSet.length; i++) {
        const f = (gCosts.get(openSet[i]) ?? 0) + (hCosts.get(openSet[i]) ?? 0);
        if (f < bestF || (f === bestF && (hCosts.get(openSet[i]) ?? 0) < (hCosts.get(openSet[bestIdx]) ?? 0))) {
          bestIdx = i;
          bestF = f;
        }
      }

      const current = openSet[bestIdx];
      openSet.splice(bestIdx, 1);
      closedSet.add(current);

      if (current === targetTile) {
        // Retrace path
        const path: Vector2Int[] = [];
        let node: Tile | undefined = targetTile;
        while (node && node !== startTile) {
          path.push(node.pos);
          node = parents.get(node);
        }
        path.reverse();
        return path;
      }

      for (const neighbour of this.getAdjacentTiles(current.pos)) {
        if (!canTraverse(neighbour) || closedSet.has(neighbour)) continue;

        const w = Math.round(10 * weight(neighbour));
        const newG = (gCosts.get(current) ?? 0) + getDistance(current, neighbour) * w;
        const oldG = gCosts.get(neighbour);

        if (oldG === undefined || newG < oldG) {
          gCosts.set(neighbour, newG);
          hCosts.set(neighbour, getDistance(neighbour, targetTile));
          parents.set(neighbour, current);

          if (!openSet.includes(neighbour)) {
            openSet.push(neighbour);
          }
        }
      }
    }

    return [];
  }

  // ─── Visibility ───

  recomputeVisibility(): void {
    const player = GameModelRef.mainOrNull?.player;
    if (!player || player.floor !== this) return;

    for (const pos of this.enumerateFloor()) {
      const t = this.tiles.getXY(pos.x, pos.y);
      if (!t) continue;

      const isEnclosedByWalls = this.getAdjacentTiles(pos).every(adj => adj instanceof Wall);
      if (isEnclosedByWalls) continue;

      if (player.isCamouflaged) {
        t.visibility = Vector2Int.equals(pos, player.pos) ? TileVisibility.Visible : TileVisibility.Explored;
        continue;
      }

      const isVisible = this.testVisibility(player.pos, pos);
      t.visibility = isVisible ? TileVisibility.Visible : TileVisibility.Explored;
    }
  }

  testVisibility(source: Vector2Int, end: Vector2Int): boolean {
    return this.testVisibilityOneDir(source, end) || this.testVisibilityOneDir(end, source);
  }

  private testVisibilityOneDir(source: Vector2Int, end: Vector2Int): boolean {
    for (const pos of this.enumerateLine(source, end)) {
      if (Vector2Int.equals(pos, source) || Vector2Int.equals(pos, end)) continue;
      const t = this.tiles.getXY(pos.x, pos.y);
      if (t && t.obstructsVision()) return false;
    }
    return true;
  }

  forceAddVisibility(positions?: Iterable<Vector2Int>): void {
    const posIter = positions ?? this.enumerateFloor();
    for (const pos of posIter) {
      const t = this.tiles.get(pos);
      if (t) t.visibility = TileVisibility.Visible;
    }
  }

  // ─── Cleared check ───

  checkCleared(): void {
    if (this.isCleared) return;
    GameModelRef.main.enqueuEvent(() => {
      GameModelRef.main.enqueuEvent(() => {
        GameModelRef.main.enqueuEvent(() => {
          if (this.enemiesLeft() === 0 && !this.isCleared) {
            this.clearFloor();
          }
        });
      });
    });
  }

  clearFloor(): void {
    this.isCleared = true;
    const model = GameModelRef.mainOrNull;
    if (model) {
      model.gameOver(true);
      model.floorCleared(this);
    }
  }

  // ─── Enumeration helpers ───

  *enumerateFloor(): IterableIterator<Vector2Int> {
    for (let x = 0; x < this.width; x++) {
      for (let y = 0; y < this.height; y++) {
        yield new Vector2Int(x, y);
      }
    }
  }

  *enumerateRectangle(min: Vector2Int, max: Vector2Int): IterableIterator<Vector2Int> {
    const clampedMin = new Vector2Int(
      Math.max(min.x, 0),
      Math.max(min.y, 0),
    );
    const clampedMax = new Vector2Int(
      Math.min(max.x, this.width),
      Math.min(max.y, this.height),
    );
    for (let x = clampedMin.x; x < clampedMax.x; x++) {
      for (let y = clampedMin.y; y < clampedMax.y; y++) {
        yield new Vector2Int(x, y);
      }
    }
  }

  /** Bresenham line enumeration */
  *enumerateLine(start: Vector2Int, end: Vector2Int): IterableIterator<Vector2Int> {
    let px = start.x;
    let py = start.y;
    const w = end.x - start.x;
    const h = end.y - start.y;
    let dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
    if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
    if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
    if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
    let longest = Math.abs(w);
    let shortest = Math.abs(h);
    if (longest <= shortest) {
      longest = Math.abs(h);
      shortest = Math.abs(w);
      if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
      dx2 = 0;
    }
    let numerator = longest >> 1;
    for (let i = 0; i <= longest; i++) {
      yield new Vector2Int(px, py);
      numerator += shortest;
      if (numerator >= longest) {
        numerator -= longest;
        px += dx1;
        py += dy1;
      } else {
        px += dx2;
        py += dy2;
      }
    }
  }

  *enumerateCircle(center: Vector2Int, radius: number): IterableIterator<Vector2Int> {
    const r = Math.ceil(radius);
    const min = new Vector2Int(center.x - r, center.y - r);
    const max = new Vector2Int(center.x + r + 1, center.y + r + 1);
    for (const pos of this.enumerateRectangle(min, max)) {
      if (Vector2Int.distance(pos, center) < radius) {
        yield pos;
      }
    }
  }

  /** Get all bodies within a circle of given radius. */
  bodiesInCircle(center: Vector2Int, radius: number): Entity[] {
    const result: Entity[] = [];
    for (const pos of this.enumerateCircle(center, radius)) {
      const body = this.bodies.get(pos);
      if (body) result.push(body);
    }
    return result;
  }

  *enumeratePerimeter(inset = 0): IterableIterator<Vector2Int> {
    for (let x = inset; x < this.width - inset - 1; x++) yield new Vector2Int(x, inset);
    for (let y = inset; y < this.height - inset - 1; y++) yield new Vector2Int(this.width - 1 - inset, y);
    for (let x = this.width - inset - 1; x > inset; x--) yield new Vector2Int(x, this.height - 1 - inset);
    for (let y = this.height - inset - 1; y > inset; y--) yield new Vector2Int(inset, y);
  }

  /** BFS from a start position, yields tiles in order of distance */
  breadthFirstSearch(
    startPos: Vector2Int,
    predicate: (t: Tile) => boolean = () => true,
    randomizeNeighborOrder = true,
    mooreNeighborhood = false,
  ): Tile[] {
    return this.breadthFirstSearchMulti([startPos], predicate, randomizeNeighborOrder, mooreNeighborhood);
  }

  breadthFirstSearchMulti(
    startPositions: Vector2Int[],
    predicate: (t: Tile) => boolean = () => true,
    randomizeNeighborOrder = true,
    mooreNeighborhood = false,
  ): Tile[] {
    const result: Tile[] = [];
    const frontier: Tile[] = [];
    const seen = new Set<Tile>();

    for (const p of startPositions) {
      const t = this.tiles.get(p);
      if (t) {
        frontier.push(t);
        seen.add(t);
      }
    }

    while (frontier.length > 0) {
      const tile = frontier.shift()!;
      result.push(tile);

      const neighbors = mooreNeighborhood
        ? this.getAdjacentTiles(tile.pos)
        : this.getCardinalNeighbors(tile.pos);

      let adjacent = neighbors.filter(n => !seen.has(n) && predicate(n));
      if (randomizeNeighborOrder) {
        MyRandom.Shuffle(adjacent);
      }

      for (const next of adjacent) {
        frontier.push(next);
        seen.add(next);
      }
    }

    return result;
  }

  /** Enumerate all tiles in a Room's bounds */
  *enumerateRoom(room: any, extrude = 0): IterableIterator<Vector2Int> {
    const ext = new Vector2Int(extrude, extrude);
    yield* this.enumerateRectangle(
      Vector2Int.sub(room.min, ext),
      Vector2Int.add(Vector2Int.add(room.max, new Vector2Int(1, 1)), ext),
    );
  }

  enumerateRoomTiles(room: any, extrude = 0): Tile[] {
    const result: Tile[] = [];
    for (const pos of this.enumerateRoom(room, extrude)) {
      const t = this.tiles.get(pos);
      if (t) result.push(t);
    }
    return result;
  }

  *enumerateWallPerimeter(): IterableIterator<Tile> {
    const walls = this.breadthFirstSearchMulti(
      [...this.enumeratePerimeter()],
      t => t instanceof Wall,
    ).filter(t => t instanceof Wall && this.getAdjacentTiles(t.pos).some(adj => adj.canBeOccupied()));
    yield* walls;
  }
}

// ─── Helpers ───

function getDistance(a: Tile, b: Tile): number {
  const dx = Math.abs(a.pos.x - b.pos.x);
  const dy = Math.abs(a.pos.y - b.pos.y);
  return dx > dy ? 14 * dy + 10 * (dx - dy) : 14 * dx + 10 * (dy - dx);
}
