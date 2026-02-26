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

  // Entity guid → Sprite mapping for updates/animations
  private entitySprites = new Map<string, Sprite>();
  // Tile key → Graphics for tile backgrounds
  private tileGraphics = new Map<string, Graphics>();

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

  /** Get the sprite for an entity (for animation). */
  getEntitySprite(guid: string): Sprite | undefined {
    return this.entitySprites.get(guid);
  }

  /** Get the effect layer container (for animation overlays). */
  getEffectLayer(): Container {
    return this.effectLayer;
  }

  // ─── Private ───

  private clearAll(): void {
    this.tileLayer.removeChildren();
    this.grassLayer.removeChildren();
    this.itemLayer.removeChildren();
    this.bodyLayer.removeChildren();
    this.effectLayer.removeChildren();
    this.entitySprites.clear();
    this.tileGraphics.clear();
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
      // Each edge: border-left (1×16) sized bw×ts in local space, then rotated.
      // Left/right: no rotation (natural vertical orientation).
      // Top/bottom: rotated ±90° to become horizontal.
      const edges: Array<{ dir: Vector2Int; x: number; y: number; rot: number }> = [
        { dir: Vector2Int.left,  x: px.x,            y: px.y,      rot: 0 },
        { dir: Vector2Int.right, x: px.x + ts - bw,  y: px.y,      rot: 0 },
        { dir: Vector2Int.up,    x: px.x + ts,        y: px.y,      rot: -Math.PI / 2 },
        { dir: Vector2Int.down,  x: px.x,             y: px.y + ts, rot: Math.PI / 2 },
      ];

      for (const edge of edges) {
        const neighbor = Vector2Int.add(pos, edge.dir);
        const neighborTile = floor.inBounds(neighbor) ? floor.tiles.get(neighbor) : null;
        if (neighborTile && !(neighborTile instanceof Chasm)) {
          const sprite = new Sprite(borderTex);
          sprite.anchor.set(0, 0);
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

  private addEntitySprite(entity: Entity, layer: Container): Sprite {
    const tex = this.sprites.getTexture(entity.displayName);
    const ts = this.camera.tileSize;
    const px = this.camera.tileToPixel(entity.pos);

    const sprite = new Sprite(tex ?? Texture.WHITE);
    sprite.width = ts;
    sprite.height = ts;
    sprite.position.set(px.x, px.y);

    // Apply per-entity tint from Unity prefab colors
    const tint = SPRITE_TINTS[entity.displayName.toLowerCase()];
    if (tint !== undefined) {
      sprite.tint = tint;
    } else if (!tex) {
      // Tint white texture as fallback for unknown sprites
      sprite.tint = this.fallbackColor(entity.displayName);
    }

    layer.addChild(sprite);
    this.entitySprites.set(entity.guid, sprite);
    return sprite;
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
      let sprite = this.entitySprites.get(body.guid);
      if (!sprite) {
        sprite = this.addEntitySprite(body, this.bodyLayer);
      }
      const px = this.camera.tileToPixel(body.pos);
      sprite.position.set(px.x, px.y);
      sprite.visible = body.isVisible;
    }

    // Sync grasses
    for (const pos of floor.enumerateFloor()) {
      const grass = floor.grasses.get(pos);
      if (grass) {
        seenGuids.add(grass.guid);
        let sprite = this.entitySprites.get(grass.guid);
        if (!sprite) {
          sprite = this.addEntitySprite(grass, this.grassLayer);
        }
        sprite.visible = grass.isVisible;
      }
    }

    // Sync items
    for (const pos of floor.enumerateFloor()) {
      const item = floor.items.get(pos);
      if (item) {
        seenGuids.add(item.guid);
        let sprite = this.entitySprites.get(item.guid);
        if (!sprite) {
          sprite = this.addEntitySprite(item, this.itemLayer);
        }
        sprite.visible = item.isVisible;
      }
    }

    // Remove sprites for dead/removed entities
    for (const [guid, sprite] of this.entitySprites) {
      if (!seenGuids.has(guid)) {
        sprite.destroy();
        this.entitySprites.delete(guid);
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
