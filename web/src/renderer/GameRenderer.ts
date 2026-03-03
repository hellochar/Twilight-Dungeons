import { Application, Container, Sprite, Graphics, Texture } from 'pixi.js';
import { Floor } from '../model/Floor';
import { Tile, Wall, Chasm, Water, Soil, FancyGround, Signpost, HardGround } from '../model/Tile';
import { Entity } from '../model/Entity';
import { TileVisibility } from '../core/types';
import { Vector2Int } from '../core/Vector2Int';
import { Camera } from './Camera';
import { SpriteManager } from './SpriteManager';
import { SPRITE_TINTS } from './spriteTints';
import { TelegraphedTask } from '../model/tasks/TelegraphedTask';

/** Unity SleepTaskController: deep sleep tints the actor sprite blue. */
const DEEP_SLEEP_TINT = 0x5DABFF; // Color(0.365, 0.6712619, 1)

/**
 * Grasses whose Unity prefab overrides Shadow rotation to (0,0,0) — flat shadow
 * instead of the default angled (60°,20°,0°). Matched by displayName lowercase.
 */
const FLAT_SHADOW_ENTITIES = new Set([
  'blob slime', 'bloodwort', 'cheshire weed sprout', 'coralmoss',
  'dandypuff', 'necroroot', 'poisonmoss', 'vibrant ivy', 'web',
]);

/**
 * Per-status visual config matching Unity prefab positioning.
 * Only statuses with prefabs in Assets/Prefabs/Resources/Statuses/ get visuals.
 */
interface StatusVisualConfig {
  spriteKey: string;
  /** X offset in tile units from entity center. */
  offsetX: number;
  /** Y offset in tile units from entity center (Unity Y-up convention). */
  offsetY: number;
  /** Scale relative to 1 tile. */
  scale: number;
  /** Hide when actor is sleeping. */
  hideWhenSleeping?: boolean;
}

/**
 * Status visuals from Unity prefabs. Each status has individual positioning —
 * some render overhead, some on body, some below. No generic row layout.
 */
const STATUS_VISUALS: Record<string, StatusVisualConfig> = {
  // Overhead sprite icons
  PoisonedStatus:  { spriteKey: 'poisoned-status', offsetX: 0, offsetY: 0.65, scale: 0.75 },
  CharmedStatus:   { spriteKey: 'charmed',         offsetX: 0, offsetY: 0.65, scale: 0.75 },
  ConfusedStatus:  { spriteKey: 'confused',         offsetX: 0, offsetY: 0.65, scale: 0.75 },
  SurprisedStatus: { spriteKey: 'colored_transparent_packed_658', offsetX: 0, offsetY: 0.65, scale: 0.75 },
  PacifiedStatus:  { spriteKey: 'peace',            offsetX: 0, offsetY: 0.5,  scale: 0.4 },
  // Body-level sprites
  WebbedStatus:    { spriteKey: 'web',              offsetX: 0, offsetY: -0.25, scale: 1.0 },
  ParasiteStatus:  { spriteKey: 'parasite',         offsetX: 0, offsetY: 0,    scale: 0.35 },
  // Particle-based (static sprite fallback — TODO: implement PixiJS particles)
  SlimedStatus:    { spriteKey: 'slimed',           offsetX: 0, offsetY: 0,     scale: 1.0 },
  VulnerableStatus:{ spriteKey: 'vulnerable',       offsetX: 0, offsetY: 0,     scale: 0.5 },
  WeaknessStatus:  { spriteKey: 'weakness',         offsetX: 0, offsetY: 0.5,   scale: 1.0 },
  FreeMoveStatus:  { spriteKey: 'free-move',        offsetX: 0, offsetY: -0.415, scale: 1.0, hideWhenSleeping: true },
  // Sprint 14
  SporedStatus:    { spriteKey: 'spored-status',    offsetX: 0, offsetY: 0,     scale: 0.75 },
  ConstrictedStatus: { spriteKey: 'hanging-vines',  offsetX: 0, offsetY: 0,     scale: 0.5 },
  FrenziedStatus:  { spriteKey: 'deathbloom-stem',  offsetX: 0, offsetY: 0.65,  scale: 0.75 },
  // Sprint 15
  InfectedStatus:  { spriteKey: 'infected',         offsetX: 0, offsetY: 0,     scale: 0.5 },
  ThirdEyeStatus:  { spriteKey: 'third-eye',        offsetX: 0, offsetY: 0.65,  scale: 0.75 },
};

/** SleepTask visual (from Assets/Prefabs/Resources/Tasks/SleepTask.prefab). */
const SLEEP_VISUAL: StatusVisualConfig = {
  spriteKey: 'sleep', offsetX: 0, offsetY: 0.5, scale: 0.5,
};

/** Fallback colors when tilesheet sprite is missing. */
const TILE_COLORS: Record<string, number> = {
  Ground: 0x8b7355,
  HardGround: 0x9a8866,
  FancyGround: 0xa09070,
  Wall: 0x3a3a4a,
  Chasm: 0x1a1a2e,
  Soil: 0x6b5b3a,
  Water: 0x3a6b8b,
  Signpost: 0x8b7355,
  FungalWall: 0x4a5a3a,
  Muck: 0x4a3a2a,
};

/**
 * Depth-dependent chasm border/fade tint colors, from Unity Chasm.prefab.
 * Borders and fade overlay are tinted to match the surrounding tile palette.
 */
function chasmTint(depth: number): number {
  if (depth >= 19) return 0x272C3A; // rgb(0.153, 0.173, 0.227) — dark blue
  if (depth >= 10) return 0x212D23; // rgb(0.129, 0.176, 0.137) — dark green
  return 0x382C33;                  // rgb(0.220, 0.173, 0.200) — dark brown
}

/**
 * Map tile instance → tilesheet sub-sprite name.
 * Ground/HardGround → "ground", Wall → "wall", FancyGround → "fancy-ground".
 * Chasm, Water, Soil, Signpost use their own individual sprites instead.
 */
function tilesheetName(tile: Tile): string | null {
  if (tile instanceof Wall) return 'wall';
  if (tile instanceof FancyGround) return 'fancy-ground';
  if (tile instanceof Chasm || tile instanceof Water ||
      tile instanceof Soil || tile instanceof Signpost) return null;
  // Ground, HardGround, and any other walkable tile
  return 'ground';
}

/**
 * PixiJS-based renderer for the game floor.
 * Layered containers: tiles → grasses → items → bodies → effects → fog.
 */
export class GameRenderer {
  readonly app: Application;
  readonly camera: Camera;
  readonly sprites: SpriteManager;

  // Layered containers (back to front)
  private tileLayer = new Container();
  private grassLayer = new Container();
  private itemLayer = new Container();
  private bodyLayer = new Container();
  private effectLayer = new Container();
  private dimLayer = new Container();

  // Entity guid → Container node (position/scale/alpha target for animations)
  private entityNodes = new Map<string, Container>();
  // Entity guid → visual Sprite child (tint target for animations)
  private entityVisuals = new Map<string, Sprite>();
  // Entity guid → inner Container with center pivot (scale/alpha target for death/spawn)
  private entityScaleRoots = new Map<string, Container>();
  // Tile position key → Container wrapping all display objects for that tile
  private tileContainers = new Map<string, Container>();
  // Tile position key → Tile object rendered in that container (for change detection)
  private renderedTiles = new Map<string, Tile>();
  // Tile position key → Graphics dim overlay for Explored tiles
  private dimCells = new Map<string, Graphics>();
  // Entity guid → status indicator sprite container (child of entityNode)
  private statusIndicators = new Map<string, Container>();
  // Active targeting highlights on the effect layer
  private targetHighlights: Graphics[] = [];
  // Entity guids currently mid-animation — skip position snapping for these
  readonly animatingGuids = new Set<string>();
  // Telegraph charging particle effects (TelegraphedTask.prefab port)
  private telegraphEffects = new Map<string, {
    container: Container;
    particles: Array<{ g: Graphics; age: number; angle: number }>;
    spawnAccum: number;
    fadingOut: boolean;
  }>();
  // Body guids — excluded from renderer spawn/death animations (AnimationPlayer handles them)
  private bodyGuids = new Set<string>();
  // Guid → spawn grow state — GrowAtStart iterative lerp (grasses + items only)
  private spawnStates = new Map<string, { elapsed: number; scale: number }>();
  // Guid → fade-out state — FadeThenDestroy animation (grasses + items only; 0.5s, shrink=0.5)
  private fadingNodes = new Map<string, { node: Container; scaleRoot: Container; startScale: number; startTime: number }>();

  private floor: Floor | null = null;

  constructor(app: Application, camera: Camera, sprites: SpriteManager) {
    this.app = app;
    this.camera = camera;
    this.sprites = sprites;

    // Add layers in draw order
    app.stage.addChild(this.tileLayer);
    app.stage.addChild(this.grassLayer);
    app.stage.addChild(this.itemLayer);
    app.stage.addChild(this.bodyLayer);
    app.stage.addChild(this.effectLayer);
    app.stage.addChild(this.dimLayer);
  }

  /** Set the floor to render and do full rebuild. */
  setFloor(floor: Floor): void {
    this.floor = floor;
    this.camera.resize(
      this.app.screen.width,
      this.app.screen.height,
      floor.width,
      floor.height,
    );
    this.rebuildAll();
  }

  /** Handle viewport resize. */
  resize(): void {
    if (!this.floor) return;
    this.camera.resize(
      this.app.screen.width,
      this.app.screen.height,
      this.floor.width,
      this.floor.height,
    );
    this.rebuildAll();
  }

  /** Full rebuild of all visual elements from model state. */
  rebuildAll(): void {
    if (!this.floor) return;
    this.clearAll();
    this.buildTiles();
    this.buildEntities();
    this.syncTileVisibility();
  }

  /** Sync visuals to current model state (call after each turn step). */
  syncToModel(): void {
    if (!this.floor) return;
    this.syncTiles();
    this.syncEntities();
    this.syncTileVisibility();
  }

  /** Get the entity node Container (for position/scale/alpha animations). */
  getEntitySprite(guid: string): Container | undefined {
    return this.entityNodes.get(guid);
  }

  /** Get the visual Sprite child (for tint animations). */
  getEntityVisual(guid: string): Sprite | undefined {
    return this.entityVisuals.get(guid);
  }

  /** Get the scale/alpha inner Container (center-pivoted; for death/spawn animations). */
  getEntityScaleRoot(guid: string): Container | undefined {
    return this.entityScaleRoots.get(guid);
  }

  /** Get the effect layer container (for animation overlays). */
  getEffectLayer(): Container {
    return this.effectLayer;
  }

  /** Show green highlight rectangles on valid target positions. */
  showTargetHighlights(positions: Vector2Int[]): void {
    this.clearTargetHighlights();
    const ts = this.camera.tileSize;
    for (const pos of positions) {
      const px = this.camera.tileToPixel(pos);
      const g = new Graphics();
      g.rect(0, 0, ts, ts).fill({ color: 0x44ff44, alpha: 0.25 });
      g.rect(0, 0, ts, ts).stroke({ color: 0x44ff44, alpha: 0.6, width: 2 });
      g.position.set(px.x, px.y);
      this.effectLayer.addChild(g);
      this.targetHighlights.push(g);
    }
  }

  /** Remove all target highlight graphics. */
  clearTargetHighlights(): void {
    for (const g of this.targetHighlights) g.destroy();
    this.targetHighlights = [];
  }

  /**
   * Lerp non-animated entity positions toward their model positions.
   * Call from app.ticker each frame.
   * Matches Unity ActorController.Update() lines 82-89:
   *   speed = 16 / actionCost, snap if distance > 3 tiles.
   */
  lerpPositions(dt: number): void {
    const floor = this.floor;
    if (!floor) return;
    const ts = this.camera.tileSize;

    for (const body of floor.bodies) {
      if (body.isDead) continue;
      if (this.animatingGuids.has(body.guid)) continue;
      const node = this.entityNodes.get(body.guid);
      if (!node) continue;

      const target = this.camera.tileToPixel(body.pos);
      const dx = target.x - node.position.x;
      const dy = target.y - node.position.y;
      const dist = Math.sqrt(dx * dx + dy * dy);

      if (dist < 0.5) {
        node.position.set(target.x, target.y);
        continue;
      }

      // Snap if distance > 3 tiles (matching Unity ActorController line 85-86)
      if (dist > ts * 3) {
        node.position.set(target.x, target.y);
        continue;
      }

      // Lerp speed: 16 tiles per second (Unity ActorController line 82: 16 / actionCost)
      // Most actors have move cost 1, so 16 t/s is the standard speed.
      const speed = 16 * ts * dt;
      const ratio = Math.min(speed / dist, 1);
      node.position.set(
        node.position.x + dx * ratio,
        node.position.y + dy * ratio,
      );
    }
  }

  // ─── Private ───

  private clearAll(): void {
    this.tileLayer.removeChildren();
    this.grassLayer.removeChildren();
    this.itemLayer.removeChildren();
    this.bodyLayer.removeChildren();
    this.effectLayer.removeChildren();
    this.dimLayer.removeChildren();
    this.entityNodes.clear();
    this.entityVisuals.clear();
    this.entityScaleRoots.clear();
    this.tileContainers.clear();
    this.renderedTiles.clear();
    this.dimCells.clear();
    this.statusIndicators.clear();
    for (const effect of this.telegraphEffects.values()) {
      effect.container.destroy({ children: true });
    }
    this.telegraphEffects.clear();
    this.bodyGuids.clear();
    this.spawnStates.clear();
    for (const f of this.fadingNodes.values()) f.node.destroy({ children: true });
    this.fadingNodes.clear();
  }

  private buildTiles(): void {
    const floor = this.floor!;
    const ts = this.camera.tileSize;
    const depth = floor.depth;

    for (const pos of floor.enumerateFloor()) {
      const tile = floor.tiles.get(pos);
      if (!tile) continue;

      const px = this.camera.tileToPixel(pos);
      const container = new Container();
      container.position.set(px.x, px.y);
      const key = Vector2Int.key(pos);
      this.tileLayer.addChild(container);
      this.tileContainers.set(key, container);
      this.renderedTiles.set(key, tile);

      // Try tilesheet sprite first (ground, wall, fancy-ground)
      const sheetName = tilesheetName(tile);
      const sheetTex = sheetName ? this.sprites.getTileTexture(sheetName, depth) : null;

      if (sheetTex) {
        const sprite = new Sprite(sheetTex);
        sprite.width = ts;
        sprite.height = ts;
        container.addChild(sprite);
      } else {
        // Individual sprite (chasm, water, soil, signpost) or fallback color
        const tex = this.sprites.getTexture(tile.displayName);
        if (tex) {
          const sprite = new Sprite(tex);
          sprite.width = ts;
          sprite.height = ts;
          container.addChild(sprite);
        } else {
          // Colored rectangle fallback
          const g = new Graphics();
          const colorKey = tile.constructor.name;
          const color = TILE_COLORS[colorKey] ?? 0x8b7355;
          g.rect(0, 0, ts, ts).fill(color);
          container.addChild(g);
        }
      }

      // Chasm border edges: draw on chasm tiles where neighbor is non-chasm
      if (tile instanceof Chasm) {
        this.addChasmBorders(floor, pos, container, ts);
      }
    }
  }

  /**
   * Draw border edges and fade gradient on chasm tiles.
   * Unity Chasm.prefab uses a single border-left sprite (1×16, pivot 0,0.5)
   * rotated for all 4 edges, and gradient-top.png for the fade overlay.
   */
  private addChasmBorders(
    floor: Floor, pos: Vector2Int, container: Container, ts: number,
  ): void {
    const depth = floor.depth;
    const tint = chasmTint(depth);
    const bw = ts / 16; // 1 source pixel scaled to tile size

    const borderTex = this.sprites.getBorderTexture();
    if (borderTex) {
      // Each edge: border-left (1×16) sized bw×ts, anchored at center, positioned
      // at center of each tile edge. Rotation swings around the center point.
      // Coordinates are local to the container (origin = tile top-left).
      const edges: Array<{ dir: Vector2Int; x: number; y: number; rot: number }> = [
        { dir: Vector2Int.left,  x: bw / 2,       y: ts / 2,       rot: 0 },
        { dir: Vector2Int.right, x: ts - bw / 2,  y: ts / 2,       rot: Math.PI },
        { dir: Vector2Int.up,    x: ts / 2,        y: bw / 2,       rot: -Math.PI / 2 },
        { dir: Vector2Int.down,  x: ts / 2,        y: ts - bw / 2,  rot: Math.PI / 2 },
      ];

      for (const edge of edges) {
        const neighbor = Vector2Int.add(pos, edge.dir);
        const neighborTile = floor.inBounds(neighbor) ? floor.tiles.get(neighbor) : null;
        if (neighborTile && !(neighborTile instanceof Chasm)) {
          const sprite = new Sprite(borderTex);
          sprite.anchor.set(0.5, 0.5);
          sprite.width = bw;
          sprite.height = ts;
          sprite.rotation = edge.rot;
          sprite.position.set(edge.x, edge.y);
          sprite.tint = tint;
          container.addChild(sprite);
        }
      }
    }

    // Fade gradient: when the tile above (game up) is not a chasm,
    // draw gradient-top.png (white→transparent) tinted with depth color.
    // Unity: positioned y=-0.25, scaled 1×1.5, tinted with depth color.
    const above = Vector2Int.add(pos, Vector2Int.up);
    const aboveTile = floor.inBounds(above) ? floor.tiles.get(above) : null;
    if (aboveTile && !(aboveTile instanceof Chasm)) {
      const fadeTex = this.sprites.getTexture('gradient-top');
      if (fadeTex) {
        const sprite = new Sprite(fadeTex);
        sprite.position.set(0, -ts * 0.25);
        sprite.width = ts;
        sprite.height = ts * 1.5;
        sprite.tint = tint;
        container.addChild(sprite);
      }
    }
  }

  private buildEntities(): void {
    const floor = this.floor!;

    // Grasses
    for (const pos of floor.enumerateFloor()) {
      const grass = floor.grasses.get(pos);
      if (grass) this.addEntitySprite(grass, this.grassLayer);
    }

    // Items
    for (const pos of floor.enumerateFloor()) {
      const item = floor.items.get(pos);
      if (item) this.addEntitySprite(item, this.itemLayer);
    }

    // Bodies
    for (const body of floor.bodies) {
      this.addEntitySprite(body, this.bodyLayer);
    }
  }

  /**
   * Create a Container node with a scaleRoot inner Container and a visual Sprite child.
   * Structure: node (position/lerp) → scaleRoot (center pivot, scale/alpha) → shadow + sprite.
   * Tint goes on the Sprite so sibling children (status indicators) don't inherit it.
   * scaleRoot pivot=(ts/2,ts/2) + position=(ts/2,ts/2) keeps visual origin at node(0,0)
   * while scaling from tile center.
   */
  private addEntitySprite(entity: Entity, layer: Container): Container {
    const tex = this.sprites.getTexture(entity.displayName);
    const ts = this.camera.tileSize;
    const px = this.camera.tileToPixel(entity.pos);

    const node = new Container();
    node.position.set(px.x, px.y);

    // scaleRoot: center-pivoted inner Container so scale/alpha animations use tile center
    const scaleRoot = new Container();
    scaleRoot.pivot.set(ts / 2, ts / 2);
    scaleRoot.position.set(ts / 2, ts / 2);
    node.addChild(scaleRoot);

    // Shadow — bodies and grasses only (items have no shadow in Unity).
    // Matches Unity Shadow.prefab: color rgba(0.055, 0.059, 0.075, 0.4).
    // Most entities: angled (60°,20°,0°). Some grasses: flat (0°,0°,0°).
    const hasShadow = layer === this.bodyLayer || layer === this.grassLayer;
    if (hasShadow) {
      const shadow = new Sprite(tex ?? Texture.WHITE);
      shadow.tint = 0x0E0F13;
      shadow.alpha = 0.4;
      shadow.width = ts;
      shadow.height = ts;

      // Offset shadow up by transparent bottom padding so it aligns with visible feet
      const bottomPad = this.sprites.getBottomPadding(entity.displayName) * ts;

      const isFlat = FLAT_SHADOW_ENTITIES.has(entity.displayName.toLowerCase());
      if (isFlat) {
        // Flat shadow: dark copy offset slightly right and up (Unity: 0.05, 0.05)
        shadow.position.set(ts * 0.05, -ts * 0.05 - bottomPad);
      } else {
        // Angled shadow: skewed up-right from entity's feet
        shadow.anchor.set(0.5, 1.0);
        shadow.position.set(ts / 2, ts - bottomPad);
        shadow.scale.y *= 0.5;
        shadow.skew.x = -0.35;
      }
      scaleRoot.addChild(shadow);
    }

    const sprite = new Sprite(tex ?? Texture.WHITE);
    sprite.width = ts;
    sprite.height = ts;

    // Tint on sprite only — siblings (status indicators) won't inherit
    const tint = SPRITE_TINTS[entity.displayName.toLowerCase()];
    if (tint !== undefined) {
      sprite.tint = tint;
    } else if (!tex) {
      sprite.tint = this.fallbackColor(entity.displayName);
    }

    // Apply rotation for entities with angle property (e.g. EveningBells)
    if ('angle' in entity && typeof (entity as any).angle === 'number') {
      const angleDeg = (entity as any).angle as number;
      sprite.anchor.set(0.5, 0.5);
      sprite.position.set(ts / 2, ts / 2);
      sprite.rotation = angleDeg * (Math.PI / 180);
    }

    scaleRoot.addChild(sprite);
    layer.addChild(node);
    this.entityNodes.set(entity.guid, node);
    this.entityVisuals.set(entity.guid, sprite);
    this.entityScaleRoots.set(entity.guid, scaleRoot);
    return node;
  }

  /**
   * Detect tiles that changed (e.g. encounter replaced Ground with Wall)
   * and rebuild their sprite containers.
   */
  private syncTiles(): void {
    const floor = this.floor!;
    const ts = this.camera.tileSize;
    const depth = floor.depth;

    for (const pos of floor.enumerateFloor()) {
      const tile = floor.tiles.get(pos);
      if (!tile) continue;
      const key = Vector2Int.key(pos);
      if (this.renderedTiles.get(key) === tile) continue;

      // Tile object changed — destroy old container and rebuild
      const old = this.tileContainers.get(key);
      if (old) old.destroy({ children: true });

      const px = this.camera.tileToPixel(pos);
      const container = new Container();
      container.position.set(px.x, px.y);
      this.tileLayer.addChild(container);
      this.tileContainers.set(key, container);
      this.renderedTiles.set(key, tile);

      const sheetName = tilesheetName(tile);
      const sheetTex = sheetName ? this.sprites.getTileTexture(sheetName, depth) : null;
      if (sheetTex) {
        const sprite = new Sprite(sheetTex);
        sprite.width = ts;
        sprite.height = ts;
        container.addChild(sprite);
      } else {
        const tex = this.sprites.getTexture(tile.displayName);
        if (tex) {
          const sprite = new Sprite(tex);
          sprite.width = ts;
          sprite.height = ts;
          container.addChild(sprite);
        } else {
          const g = new Graphics();
          const color = TILE_COLORS[tile.constructor.name] ?? 0x8b7355;
          g.rect(0, 0, ts, ts).fill(color);
          container.addChild(g);
        }
      }

      if (tile instanceof Chasm) {
        this.addChasmBorders(floor, pos, container, ts);
      }
    }
  }

  /**
   * Sync tile visibility state:
   * - Unexplored: hide tile container (background bleeds through)
   * - Explored: show tile + dim overlay (player sees it, enemies can't target)
   * - Visible: show tile, no dim
   */
  private syncTileVisibility(): void {
    const floor = this.floor!;
    const ts = this.camera.tileSize;
    for (const pos of floor.enumerateFloor()) {
      const tile = floor.tiles.get(pos);
      if (!tile) continue;
      const key = Vector2Int.key(pos);

      // Hide/show tile sprite
      const container = this.tileContainers.get(key);
      if (container) {
        container.visible = tile.visibility !== TileVisibility.Unexplored;
      }

      // Dim overlay for Explored tiles
      const explored = tile.visibility === TileVisibility.Explored;
      let dim = this.dimCells.get(key);
      if (explored && !dim) {
        const px = this.camera.tileToPixel(pos);
        dim = new Graphics();
        dim.rect(0, 0, ts, ts).fill(0x000000);
        dim.position.set(px.x, px.y);
        dim.alpha = 0.5;
        this.dimLayer.addChild(dim);
        this.dimCells.set(key, dim);
      } else if (!explored && dim) {
        dim.destroy();
        this.dimCells.delete(key);
      }
    }
  }

  /**
   * Sync entity positions/visibility. Add new entities, remove dead ones.
   * This is a lightweight update — doesn't rebuild tiles.
   */
  private syncEntities(): void {
    const floor = this.floor!;
    const seenGuids = new Set<string>();

    // Sync bodies
    for (const body of floor.bodies) {
      seenGuids.add(body.guid);
      let node = this.entityNodes.get(body.guid);
      if (!node) {
        node = this.addEntitySprite(body, this.bodyLayer);
        this.bodyGuids.add(body.guid);
        // No initSpawnAnimation — AnimationPlayer handles body spawn/death events
      }
      // Never snap body positions — lerpPositions handles smooth movement
      // (matching Unity's ActorController.Update() lerp-only approach).
      // Only animatingGuids (attack bumps etc.) bypass lerp.
      node.visible = !body.isDead;
    }

    // Sync grasses
    for (const pos of floor.enumerateFloor()) {
      const grass = floor.grasses.get(pos);
      if (grass) {
        seenGuids.add(grass.guid);
        let node = this.entityNodes.get(grass.guid);
        if (!node) {
          node = this.addEntitySprite(grass, this.grassLayer);
          this.initSpawnAnimation(grass.guid);
        }
        node.visible = !grass.isDead;
      }
    }

    // Sync items
    for (const pos of floor.enumerateFloor()) {
      const item = floor.items.get(pos);
      if (item) {
        seenGuids.add(item.guid);
        let node = this.entityNodes.get(item.guid);
        if (!node) {
          node = this.addEntitySprite(item, this.itemLayer);
          this.initSpawnAnimation(item.guid);
        }
        node.visible = !item.isDead;
      }
    }

    // Remove nodes for dead/removed entities
    for (const [guid, node] of this.entityNodes) {
      if (!seenGuids.has(guid)) {
        if (this.bodyGuids.has(guid)) {
          // Bodies: AnimationPlayer already ran death animation — just destroy
          node.destroy({ children: true });
          this.bodyGuids.delete(guid);
        } else {
          // Grasses + items: FadeThenDestroy (Unity FloorController behavior)
          const scaleRoot = this.entityScaleRoots.get(guid)!;
          this.fadingNodes.set(guid, { node, scaleRoot, startScale: scaleRoot.scale.x, startTime: performance.now() });
        }
        this.spawnStates.delete(guid);
        this.entityNodes.delete(guid);
        this.entityVisuals.delete(guid);
        this.entityScaleRoots.delete(guid);
        this.statusIndicators.delete(guid);
      }
    }

    // Sync status indicators on bodies
    this.syncStatusIndicators(floor);
    // Sync telegraph charging effects
    this.syncTelegraphEffects(floor);
  }

  /**
   * Sync status/task visuals as siblings of the visual sprite inside the
   * entity's Container node (mirrors Unity's Actor → Statuses child).
   * Each status has its own position/scale from its Unity prefab — no generic row.
   */
  private syncStatusIndicators(floor: Floor): void {
    const ts = this.camera.tileSize;

    for (const body of floor.bodies) {
      if (body.isDead || !('statuses' in body)) {
        const existing = this.statusIndicators.get(body.guid);
        if (existing) { existing.destroy(); this.statusIndicators.delete(body.guid); }
        continue;
      }

      const actor = body as any;
      const node = this.entityNodes.get(body.guid);
      const scaleRoot = this.entityScaleRoots.get(body.guid);
      if (!node || !scaleRoot) continue;

      const isSleeping = actor.task?.constructor?.name === 'SleepTask';
      const statuses = actor.statuses as { list: any[] };

      // Collect visuals to render
      const visuals: Array<{ config: StatusVisualConfig; status?: any; tint?: number }> = [];

      // SleepTask visual
      if (isSleeping) {
        visuals.push({
          config: SLEEP_VISUAL,
          tint: actor.task.isDeepSleep ? DEEP_SLEEP_TINT : undefined,
        });
      }

      // Status visuals — only statuses with Unity prefabs
      for (const s of statuses.list) {
        const config = STATUS_VISUALS[s.constructor.name];
        if (!config) continue;
        if (config.hideWhenSleeping && isSleeping) continue;
        visuals.push({ config, status: s });
      }

      if (visuals.length === 0) {
        const existing = this.statusIndicators.get(body.guid);
        if (existing) { existing.destroy(); this.statusIndicators.delete(body.guid); }
        continue;
      }

      let container = this.statusIndicators.get(body.guid);
      if (container) {
        container.removeChildren();
        if (container.parent !== scaleRoot) {
          container.removeFromParent();
          scaleRoot.addChild(container);
        }
      } else {
        container = new Container();
        scaleRoot.addChild(container);
        this.statusIndicators.set(body.guid, container);
      }

      for (const { config, status, tint } of visuals) {
        // PoisonedStatus: select frame from spritesheet based on stack count
        let tex: Texture | null = null;
        if (status?.constructor?.name === 'PoisonedStatus') {
          const frames = this.sprites.getFrames('poisoned');
          if (frames && frames.length > 0) {
            const idx = Math.min(Math.max((status.stacks ?? 1) - 1, 0), frames.length - 1);
            tex = frames[idx];
          }
        }
        if (!tex) {
          tex = this.sprites.getTexture(config.spriteKey);
        }
        if (!tex) continue;

        const icon = new Sprite(tex);
        icon.anchor.set(0.5, 0.5);
        const size = config.scale * ts;
        icon.width = size;
        icon.height = size;

        // Convert Unity coords to PixiJS node-local coords
        // Node center = (ts/2, ts/2), Y-flip for Unity Y-up → PixiJS Y-down
        let cx = ts / 2 + config.offsetX * ts;
        let cy = ts / 2 - config.offsetY * ts;

        // ParasiteStatus: pseudo-random offset based on stacks (simplified wiggle)
        if (status?.constructor?.name === 'ParasiteStatus') {
          const seed = ((status.stacks ?? 1) * 7919) | 0;
          cx += ((seed % 80) - 40) / 100 * ts;
          cy += (((seed * 13) % 80) - 40) / 100 * ts;
        }

        icon.position.set(cx, cy);
        if (tint != null) icon.tint = tint;
        container.addChild(icon);
      }
    }
  }

  /**
   * Detect which actors have TelegraphedTask and create/fade effects.
   * Called from syncEntities each model sync.
   */
  private syncTelegraphEffects(floor: Floor): void {
    const activeGuids = new Set<string>();

    for (const body of floor.bodies) {
      if (body.isDead) continue;
      const actor = body as any;
      if (actor.task instanceof TelegraphedTask) {
        activeGuids.add(body.guid);
        if (!this.telegraphEffects.has(body.guid)) {
          const container = new Container();
          this.effectLayer.addChild(container);
          this.telegraphEffects.set(body.guid, {
            container,
            particles: [],
            spawnAccum: 0,
            fadingOut: false,
          });
        }
      }
    }

    // Start fade-out for effects whose actor no longer has TelegraphedTask
    for (const [guid, effect] of this.telegraphEffects) {
      if (!activeGuids.has(guid) && !effect.fadingOut) {
        effect.fadingOut = true;
      }
    }
  }

  /**
   * Per-frame update for telegraph charging particle effects.
   * Matches Unity TelegraphedTask.prefab:
   * - 30 white particles/sec from circle rim (radiusThickness 0), radius 0.5 tiles
   * - Particle life 0.4s, startSpeed 0
   * - VelocityModule radial curve 0→-1 (scalar 1, speedModifier 5): particles
   *   accelerate inward, reaching center exactly at end of life
   * - Fades out over 0.25s when task ends
   * @param dt Delta time in seconds.
   */
  updateTelegraphEffects(dt: number): void {
    const ts = this.camera.tileSize;
    const SPAWN_RATE = 30;
    const PARTICLE_LIFE = 0.4;
    const RADIUS = 0.5 * ts;
    const PARTICLE_RADIUS = Math.max(1.5, 0.05 * ts);
    // Radial displacement coefficient: particles travel from RADIUS to 0 over lifetime.
    // v(a) = -6.25*ts*a, displacement = -3.125*ts*a² (reaches -RADIUS at a=0.4)
    const RADIAL_COEFF = 3.125 * ts;

    for (const [guid, effect] of this.telegraphEffects) {
      const node = this.entityNodes.get(guid);
      if (!node) {
        effect.container.destroy({ children: true });
        this.telegraphEffects.delete(guid);
        continue;
      }

      // Position at entity center
      effect.container.position.set(
        node.position.x + ts / 2,
        node.position.y + ts / 2,
      );

      // Spawn new particles at random angles on the circle rim
      if (!effect.fadingOut) {
        effect.spawnAccum += dt;
        const interval = 1 / SPAWN_RATE;
        while (effect.spawnAccum >= interval) {
          effect.spawnAccum -= interval;
          const angle = Math.random() * Math.PI * 2;
          const g = new Graphics();
          g.circle(0, 0, PARTICLE_RADIUS).fill({ color: 0xffffff });
          g.position.set(Math.cos(angle) * RADIUS, Math.sin(angle) * RADIUS);
          g.alpha = 0;
          effect.container.addChild(g);
          effect.particles.push({ g, age: 0, angle });
        }
      }

      // Age particles: accelerate inward along their spawn angle
      for (let i = effect.particles.length - 1; i >= 0; i--) {
        const p = effect.particles[i];
        p.age += dt;
        if (p.age >= PARTICLE_LIFE) {
          p.g.destroy();
          effect.particles.splice(i, 1);
        } else {
          const r = RADIUS - RADIAL_COEFF * p.age * p.age;
          p.g.position.set(Math.cos(p.angle) * r, Math.sin(p.angle) * r);
          // Opacity: 0→1 over first 10%, hold at 1, then 1→0 over last 10%
          const t = p.age / PARTICLE_LIFE;
          p.g.alpha = t < 0.1 ? t / 0.1 : t > 0.9 ? (1 - t) / 0.1 : 1;
        }
      }

      // Fade out container
      if (effect.fadingOut) {
        effect.container.alpha = Math.max(0, effect.container.alpha - dt / 0.25);
        if (effect.container.alpha <= 0) {
          effect.container.destroy({ children: true });
          this.telegraphEffects.delete(guid);
        }
      }
    }
  }

  /** Initialize GrowAtStart spawn animation on a newly created entity. */
  private initSpawnAnimation(guid: string): void {
    const scaleRoot = this.entityScaleRoots.get(guid);
    if (!scaleRoot) return;
    scaleRoot.scale.set(0.01, 0.01);
    scaleRoot.alpha = 0.01;
    this.spawnStates.set(guid, { elapsed: 0, scale: 0.01 });
  }

  /**
   * Update GrowAtStart spawn and FadeThenDestroy despawn animations each frame.
   *
   * GrowAtStart (Unity _Grass.prefab): iterative lerp each frame from current scale toward 1.
   *   lerpAmount = 1 - 1/exp(t * PI * 2)  where t = elapsed / ANIMATION_TIME (3s).
   *   scale = lerp(scale, 1, lerpAmount)  — same formula applied each frame, not direct.
   *   This causes rapid early growth; visually done in ~0.25s despite the 3s window.
   *   Alpha mirrors scale for a fade-in effect. Scaling is center-pivoted.
   *
   * FadeThenDestroy (Unity FloorController): alpha 1→0, scale shrinks to 50% over 0.5s.
   *
   * @param dt Delta time in seconds.
   */
  updateEntityAnimations(dt: number): void {
    // GrowAtStart: iterative lerp matching Unity's frame-by-frame accumulation
    for (const [guid, state] of this.spawnStates) {
      const scaleRoot = this.entityScaleRoots.get(guid);
      if (!scaleRoot) { this.spawnStates.delete(guid); continue; }
      state.elapsed += dt;
      const t = state.elapsed / 3.0; // ANIMATION_TIME = 3s
      if (t >= 1) {
        scaleRoot.scale.set(1, 1);
        scaleRoot.alpha = 1;
        this.spawnStates.delete(guid);
      } else {
        const la = 1 - 1 / Math.exp(t * Math.PI * 2);
        state.scale = state.scale + la * (1 - state.scale); // lerp(current, 1, la)
        scaleRoot.scale.set(state.scale, state.scale);
        scaleRoot.alpha = state.scale;
      }
    }

    // FadeThenDestroy: alpha 1→0, scale shrinks to 50% over 0.5s
    const now = performance.now();
    for (const [guid, fade] of this.fadingNodes) {
      const t = Math.min((now - fade.startTime) / 500, 1);
      if (t >= 1) {
        fade.node.destroy({ children: true });
        this.fadingNodes.delete(guid);
      } else {
        fade.scaleRoot.alpha = 1 - t;
        const s = fade.startScale * (1 - 0.5 * t);
        fade.scaleRoot.scale.set(s, s);
      }
    }
  }

  /** Generate a deterministic fallback color from a name string. */
  private fallbackColor(name: string): number {
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = ((hash << 5) - hash + name.charCodeAt(i)) | 0;
    }
    // Ensure decent saturation/brightness
    const h = Math.abs(hash) % 360;
    const s = 50 + (Math.abs(hash >> 8) % 30);
    const l = 40 + (Math.abs(hash >> 16) % 20);
    return hslToHex(h, s, l);
  }
}

function hslToHex(h: number, s: number, l: number): number {
  s /= 100;
  l /= 100;
  const a = s * Math.min(l, 1 - l);
  const f = (n: number) => {
    const k = (n + h / 30) % 12;
    const color = l - a * Math.max(Math.min(k - 3, 9 - k, 1), -1);
    return Math.round(255 * color);
  };
  return (f(0) << 16) | (f(8) << 8) | f(4);
}
