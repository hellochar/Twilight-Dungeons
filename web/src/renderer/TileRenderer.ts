import { Container, Sprite, Graphics, Texture } from 'pixi.js';
import { Floor } from '../model/Floor';
import { Tile, Wall, Chasm, Water, Soil, FancyGround, Signpost } from '../model/Tile';
import { TileVisibility } from '../core/types';
import { Vector2Int } from '../core/Vector2Int';
import { Camera } from './Camera';
import { SpriteManager } from './SpriteManager';

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
 * Handles all tile visual concerns: build, change-detection sync, visibility/fog, chasm borders.
 * Owns tileLayer and dimLayer containers — GameRenderer adds them to the stage.
 */
export class TileRenderer {
  readonly tileLayer = new Container();
  readonly dimLayer = new Container();

  private tileContainers = new Map<string, Container>();
  private renderedTiles = new Map<string, Tile>();
  private dimCells = new Map<string, Graphics>();

  constructor(
    private readonly camera: Camera,
    private readonly sprites: SpriteManager,
  ) {}

  /** Build all tile visuals from scratch. */
  build(floor: Floor): void {
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

      this.buildTileSprite(tile, container, ts, depth);

      if (tile instanceof Chasm) {
        this.addChasmBorders(floor, pos, container, ts);
      }
    }
  }

  /**
   * Detect tiles that changed (e.g. encounter replaced Ground with Wall)
   * and rebuild their sprite containers.
   */
  sync(floor: Floor): void {
    const ts = this.camera.tileSize;
    const depth = floor.depth;

    for (const pos of floor.enumerateFloor()) {
      const tile = floor.tiles.get(pos);
      if (!tile) continue;
      const key = Vector2Int.key(pos);
      if (this.renderedTiles.get(key) === tile) continue;

      const old = this.tileContainers.get(key);
      if (old) old.destroy({ children: true });

      const px = this.camera.tileToPixel(pos);
      const container = new Container();
      container.position.set(px.x, px.y);
      this.tileLayer.addChild(container);
      this.tileContainers.set(key, container);
      this.renderedTiles.set(key, tile);

      this.buildTileSprite(tile, container, ts, depth);

      if (tile instanceof Chasm) {
        this.addChasmBorders(floor, pos, container, ts);
      }
    }
  }

  /**
   * Sync tile visibility state:
   * - Unexplored: hide tile container
   * - Explored: show tile + dim overlay
   * - Visible: show tile, no dim
   */
  syncVisibility(floor: Floor): void {
    const ts = this.camera.tileSize;
    for (const pos of floor.enumerateFloor()) {
      const tile = floor.tiles.get(pos);
      if (!tile) continue;
      const key = Vector2Int.key(pos);

      const container = this.tileContainers.get(key);
      if (container) {
        container.visible = tile.visibility !== TileVisibility.Unexplored;
      }

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

  /** Clear all tile visuals. */
  clear(): void {
    this.tileLayer.removeChildren();
    this.dimLayer.removeChildren();
    this.tileContainers.clear();
    this.renderedTiles.clear();
    this.dimCells.clear();
  }

  private buildTileSprite(tile: Tile, container: Container, ts: number, depth: number): void {
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
    const bw = ts / 16;

    const borderTex = this.sprites.getBorderTexture();
    if (borderTex) {
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
}
