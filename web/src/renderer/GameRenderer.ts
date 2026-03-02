import { Application, Container, Sprite, Graphics, Texture } from 'pixi.js';
import { Floor } from '../model/Floor';
import { Tile, Wall, Chasm, Water, Soil, FancyGround, Signpost, HardGround } from '../model/Tile';
import { Entity } from '../model/Entity';
import { TileVisibility } from '../core/types';
import { Vector2Int } from '../core/Vector2Int';
import { Camera } from './Camera';
import { SpriteManager } from './SpriteManager';
import { FogOverlay } from './FogOverlay';
import { SPRITE_TINTS } from './spriteTints';

/** Unity SleepTaskController: deep sleep tints the actor sprite blue. */
const DEEP_SLEEP_TINT = 0x5DABFF; // Color(0.365, 0.6712619, 1)

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
  readonly fog: FogOverlay;

  // Layered containers (back to front)
  private tileLayer = new Container();
  private grassLayer = new Container();
  private itemLayer = new Container();
  private bodyLayer = new Container();
  private effectLayer = new Container();
  private fogLayer = new Container();

  // Entity guid → Container node (position/scale/alpha target for animations)
  private entityNodes = new Map<string, Container>();
  // Entity guid → visual Sprite child (tint target for animations)
  private entityVisuals = new Map<string, Sprite>();
  // Tile key → Graphics for tile backgrounds
  private tileGraphics = new Map<string, Graphics>();
  // Entity guid → status indicator sprite container (child of entityNode)
  private statusIndicators = new Map<string, Container>();
  // Active targeting highlights on the effect layer
  private targetHighlights: Graphics[] = [];

  private floor: Floor | null = null;

  constructor(app: Application, camera: Camera, sprites: SpriteManager) {
    this.app = app;
    this.camera = camera;
    this.sprites = sprites;
    this.fog = new FogOverlay(camera);

    // Add layers in draw order
    app.stage.addChild(this.tileLayer);
    app.stage.addChild(this.grassLayer);
    app.stage.addChild(this.itemLayer);
    app.stage.addChild(this.bodyLayer);
    app.stage.addChild(this.effectLayer);
    this.fogLayer.addChild(this.fog.container);
    app.stage.addChild(this.fogLayer);
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
    this.fog.rebuild(this.floor);
  }

  /** Sync visuals to current model state (call after each turn step). */
  syncToModel(): void {
    if (!this.floor) return;
    this.syncEntities();
    this.fog.sync(this.floor);
  }

  /** Get the entity node Container (for position/scale/alpha animations). */
  getEntitySprite(guid: string): Container | undefined {
    return this.entityNodes.get(guid);
  }

  /** Get the visual Sprite child (for tint animations). */
  getEntityVisual(guid: string): Sprite | undefined {
    return this.entityVisuals.get(guid);
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

  // ─── Private ───

  private clearAll(): void {
    this.tileLayer.removeChildren();
    this.grassLayer.removeChildren();
    this.itemLayer.removeChildren();
    this.bodyLayer.removeChildren();
    this.effectLayer.removeChildren();
    this.entityNodes.clear();
    this.entityVisuals.clear();
    this.tileGraphics.clear();
    this.statusIndicators.clear();
  }

  private buildTiles(): void {
    const floor = this.floor!;
    const ts = this.camera.tileSize;
    const depth = floor.depth;

    for (const pos of floor.enumerateFloor()) {
      const tile = floor.tiles.get(pos);
      if (!tile) continue;

      const px = this.camera.tileToPixel(pos);

      // Try tilesheet sprite first (ground, wall, fancy-ground)
      const sheetName = tilesheetName(tile);
      const sheetTex = sheetName ? this.sprites.getTileTexture(sheetName, depth) : null;

      if (sheetTex) {
        const sprite = new Sprite(sheetTex);
        sprite.width = ts;
        sprite.height = ts;
        sprite.position.set(px.x, px.y);
        this.tileLayer.addChild(sprite);
      } else {
        // Individual sprite (chasm, water, soil, signpost) or fallback color
        const tex = this.sprites.getTexture(tile.displayName);
        if (tex) {
          const sprite = new Sprite(tex);
          sprite.width = ts;
          sprite.height = ts;
          sprite.position.set(px.x, px.y);
          this.tileLayer.addChild(sprite);
        } else {
          // Colored rectangle fallback
          const g = new Graphics();
          const colorKey = tile.constructor.name;
          const color = TILE_COLORS[colorKey] ?? 0x8b7355;
          g.rect(0, 0, ts, ts).fill(color);
          g.position.set(px.x, px.y);
          this.tileLayer.addChild(g);
          this.tileGraphics.set(Vector2Int.key(pos), g);
        }
      }

      // Chasm border edges: draw on chasm tiles where neighbor is non-chasm
      if (tile instanceof Chasm) {
        this.addChasmBorders(floor, pos, px, ts);
      }
    }
  }

  /**
   * Draw border edges and fade gradient on chasm tiles.
   * Unity Chasm.prefab uses a single border-left sprite (1×16, pivot 0,0.5)
   * rotated for all 4 edges, and gradient-top.png for the fade overlay.
   */
  private addChasmBorders(
    floor: Floor, pos: Vector2Int, px: { x: number; y: number }, ts: number,
  ): void {
    const depth = floor.depth;
    const tint = chasmTint(depth);
    const bw = ts / 16; // 1 source pixel scaled to tile size

    const borderTex = this.sprites.getBorderTexture();
    if (borderTex) {
      // Each edge: border-left (1×16) sized bw×ts, anchored at center, positioned
      // at center of each tile edge. Rotation swings around the center point.
      const edges: Array<{ dir: Vector2Int; x: number; y: number; rot: number }> = [
        { dir: Vector2Int.left,  x: px.x + bw / 2,       y: px.y + ts / 2,       rot: 0 },
        { dir: Vector2Int.right, x: px.x + ts - bw / 2,  y: px.y + ts / 2,       rot: Math.PI },
        { dir: Vector2Int.up,    x: px.x + ts / 2,        y: px.y + bw / 2,       rot: -Math.PI / 2 },
        { dir: Vector2Int.down,  x: px.x + ts / 2,        y: px.y + ts - bw / 2,  rot: Math.PI / 2 },
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
          this.tileLayer.addChild(sprite);
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
        sprite.position.set(px.x, px.y - ts * 0.25);
        sprite.width = ts;
        sprite.height = ts * 1.5;
        sprite.tint = tint;
        this.tileLayer.addChild(sprite);
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
   * Create a Container node with a visual Sprite child (mirrors Unity's
   * Actor GameObject → SpriteRenderer child). Tint goes on the Sprite so
   * sibling children (status indicators) don't inherit it.
   */
  private addEntitySprite(entity: Entity, layer: Container): Container {
    const tex = this.sprites.getTexture(entity.displayName);
    const ts = this.camera.tileSize;
    const px = this.camera.tileToPixel(entity.pos);

    const node = new Container();
    node.position.set(px.x, px.y);

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

    node.addChild(sprite);
    layer.addChild(node);
    this.entityNodes.set(entity.guid, node);
    this.entityVisuals.set(entity.guid, sprite);
    return node;
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
      }
      const px = this.camera.tileToPixel(body.pos);
      node.position.set(px.x, px.y);
      node.visible = body.isVisible;
    }

    // Sync grasses
    for (const pos of floor.enumerateFloor()) {
      const grass = floor.grasses.get(pos);
      if (grass) {
        seenGuids.add(grass.guid);
        let node = this.entityNodes.get(grass.guid);
        if (!node) {
          node = this.addEntitySprite(grass, this.grassLayer);
        }
        node.visible = grass.isVisible;
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
        }
        node.visible = item.isVisible;
      }
    }

    // Remove nodes for dead/removed entities
    for (const [guid, node] of this.entityNodes) {
      if (!seenGuids.has(guid)) {
        node.destroy({ children: true });
        this.entityNodes.delete(guid);
        this.entityVisuals.delete(guid);
        this.statusIndicators.delete(guid);
      }
    }

    // Sync status indicators on bodies
    this.syncStatusIndicators(floor);
  }

  /**
   * Sync status/task visuals as siblings of the visual sprite inside the
   * entity's Container node (mirrors Unity's Actor → Statuses child).
   * Each status has its own position/scale from its Unity prefab — no generic row.
   */
  private syncStatusIndicators(floor: Floor): void {
    const ts = this.camera.tileSize;

    for (const body of floor.bodies) {
      if (!body.isVisible || !('statuses' in body)) {
        const existing = this.statusIndicators.get(body.guid);
        if (existing) { existing.destroy(); this.statusIndicators.delete(body.guid); }
        continue;
      }

      const actor = body as any;
      const node = this.entityNodes.get(body.guid);
      if (!node) continue;

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
        if (container.parent !== node) {
          container.removeFromParent();
          node.addChild(container);
        }
      } else {
        container = new Container();
        node.addChild(container);
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
