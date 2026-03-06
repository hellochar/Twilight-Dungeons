import { Assets, Texture, Rectangle } from 'pixi.js';
import { SPRITE_NAME_MAP } from './spriteNameMap';

export interface SpriteInfo {
  file: string;
  width: number;
  height: number;
  frameCount: number;
  frameWidth: number;
  frameHeight: number;
  ax: number;
  ay: number;
  /** Horizontal stride between frame origins (defaults to frameWidth). */
  stride?: number;
}

export type SpriteManifest = Record<string, SpriteInfo>;

/**
 * Sub-sprite layout within each tilesheet (tiles0, tiles12, tiles24).
 * Coordinates are in PixiJS space (y-down). Each tilesheet is 48×48 with 16px tiles.
 * Unity .meta defines: ground(0,16), wall(16,16), fancy-ground(32,16),
 * upstairs(0,0), downstairs(16,0) — Unity y is bottom-up, so we flip:
 *   Unity y=16 → PixiJS y = 48-16-16 = 16
 *   Unity y=0  → PixiJS y = 48-0-16  = 32
 */
const TILESHEET_REGIONS: Record<string, { x: number; y: number }> = {
  ground:        { x: 0,  y: 16 },
  wall:          { x: 16, y: 16 },
  'fancy-ground': { x: 32, y: 16 },
  upstairs:      { x: 0,  y: 32 },
  downstairs:    { x: 16, y: 32 },
};

/** Tilesheet key for a floor depth: 0-9 → tiles0, 10-18 → tiles12, 19+ → tiles24 */
function tilesheetForDepth(depth: number): string {
  if (depth >= 19) return 'tiles24';
  if (depth >= 10) return 'tiles12';
  return 'tiles0';
}

/**
 * Manages loading and caching of sprite textures.
 * Loads individual PNGs from public/sprites/ and slices multi-frame strips.
 */
export class SpriteManager {
  private manifest: SpriteManifest = {};
  private textures = new Map<string, Texture>();
  private frames = new Map<string, Texture[]>();
  private loaded = false;
  /** Sliced tile sub-sprites: key = "tiles0/ground", "tiles12/wall", etc. */
  private tileSubSprites = new Map<string, Texture>();
  /** Single border-left sub-sprite used for all chasm edges (rotated per edge). */
  private borderTexture: Texture | null = null;
  /** Cached bottom padding fraction (0–1) per sprite key. */
  private bottomPaddingCache = new Map<string, number>();


  async load(): Promise<void> {
    if (this.loaded) return;

    const base = import.meta.env.BASE_URL;
    const resp = await fetch(`${base}sprites/manifest.json`);
    this.manifest = await resp.json();

    // Load single texture atlas instead of 264 individual PNGs
    const atlasTexture = await Assets.load<Texture>(`${base}sprites/atlas.png`);
    const atlasSource = atlasTexture.source;

    for (const [name, info] of Object.entries(this.manifest)) {
      const tex = new Texture({
        source: atlasSource,
        frame: new Rectangle(info.ax, info.ay, info.width, info.height),
      });
      this.textures.set(name, tex);

      // Slice multi-frame strips (frames are horizontal within the atlas region)
      if (info.frameCount > 1) {
        const stride = info.stride ?? info.frameWidth;
        const frameTextures: Texture[] = [];
        for (let i = 0; i < info.frameCount; i++) {
          frameTextures.push(new Texture({
            source: atlasSource,
            frame: new Rectangle(
              info.ax + i * stride,
              info.ay,
              info.frameWidth,
              info.frameHeight,
            ),
          }));
        }
        this.frames.set(name, frameTextures);
      }
    }

    this.sliceTilesheets();
    this.sliceSkullySheet();
    this.sliceBorders();
    this.applyFrameAliases();
    this.loaded = true;
    console.log(`SpriteManager: loaded ${this.textures.size} sprites from atlas, ${this.tileSubSprites.size} tile sub-sprites`);
  }

  /**
   * Named frame aliases: key → [sourceKey, frameIndex].
   * Creates a texture alias for a specific frame of a multi-frame sprite strip.
   */
  private static FRAME_ALIASES: Record<string, [string, number]> = {
    'scuttler-underground': ['scuttler', 1],
  };

  private applyFrameAliases(): void {
    for (const [alias, [sourceKey, frameIndex]] of Object.entries(SpriteManager.FRAME_ALIASES)) {
      const frames = this.frames.get(sourceKey);
      if (frames?.[frameIndex]) {
        this.textures.set(alias, frames[frameIndex]);
      }
    }
  }

  /** Slice skully.png (32×16) into 'skully' (left 16×16) and 'muck' (right 16×16) sub-sprites. */
  private sliceSkullySheet(): void {
    const info = this.manifest['skully'];
    const tex = this.textures.get('skully');
    if (!tex || !info) return;
    this.textures.set('skully', new Texture({ source: tex.source, frame: new Rectangle(info.ax, info.ay, 16, 16) }));
    this.textures.set('muck', new Texture({ source: tex.source, frame: new Rectangle(info.ax + 16, info.ay, 16, 16) }));
  }

  /** Slice tilesheets (tiles0, tiles12, tiles24) into named sub-sprites. */
  private sliceTilesheets(): void {
    for (const sheet of ['tiles0', 'tiles12', 'tiles24']) {
      const tex = this.textures.get(sheet);
      const info = this.manifest[sheet];
      if (!tex || !info) continue;
      for (const [name, region] of Object.entries(TILESHEET_REGIONS)) {
        const sub = new Texture({
          source: tex.source,
          frame: new Rectangle(info.ax + region.x, info.ay + region.y, 16, 16),
        });
        this.tileSubSprites.set(`${sheet}/${name}`, sub);
      }
    }
  }

  /** Slice border-left sub-sprite from border.png for chasm edge rendering. */
  private sliceBorders(): void {
    const tex = this.textures.get('border');
    const info = this.manifest['border'];
    if (!tex || !info) return;

    this.borderTexture = new Texture({
      source: tex.source,
      frame: new Rectangle(info.ax + 1, info.ay, 1, 16),
    });
  }

  /** Get the border-left texture used for all chasm edges. */
  getBorderTexture(): Texture | null {
    return this.borderTexture;
  }

  /**
   * Get the tile texture for a given tile type and floor depth.
   * Returns the depth-appropriate sub-sprite from the correct tilesheet.
   */
  getTileTexture(tileType: string, depth: number): Texture | null {
    const sheet = tilesheetForDepth(depth);
    return this.tileSubSprites.get(`${sheet}/${tileType}`) ?? null;
  }

  /** Get the sprite key for a given entity display name. */
  resolveKey(displayName: string): string {
    const lower = displayName.toLowerCase();
    return SPRITE_NAME_MAP[lower] ?? lower;
  }

  /** Get the first frame texture for an entity. */
  getTexture(displayName: string): Texture | null {
    const key = this.resolveKey(displayName);
    const frameList = this.frames.get(key);
    if (frameList) return frameList[0];
    return this.textures.get(key) ?? null;
  }

  /** Get all animation frames for an entity. */
  getFrames(displayName: string): Texture[] | null {
    const key = this.resolveKey(displayName);
    return this.frames.get(key) ?? null;
  }

  /** Get the raw SpriteInfo for a key. */
  getInfo(displayName: string): SpriteInfo | null {
    const key = this.resolveKey(displayName);
    return this.manifest[key] ?? null;
  }

  /** Get texture by raw sprite key (no NAME_MAP lookup). */
  getTextureByKey(key: string): Texture | null {
    return this.textures.get(key) ?? null;
  }

  /** Check if a sprite exists for the given name. */
  has(displayName: string): boolean {
    const key = this.resolveKey(displayName);
    return this.textures.has(key);
  }

  /**
   * Fraction of transparent rows at the bottom of a sprite (0 = none, 1 = all).
   * Scans pixel data once per sprite key and caches the result.
   */
  getBottomPadding(displayName: string): number {
    const key = this.resolveKey(displayName);
    const cached = this.bottomPaddingCache.get(key);
    if (cached !== undefined) return cached;

    const tex = this.getTexture(displayName);
    if (!tex || tex === Texture.WHITE) {
      this.bottomPaddingCache.set(key, 0);
      return 0;
    }

    // PixiJS v8 uses ImageBitmap by default; HTMLImageElement as fallback.
    // Both are valid CanvasImageSource for drawImage().
    const source = tex.source.resource as CanvasImageSource;
    if (!(source instanceof HTMLImageElement || source instanceof ImageBitmap)) {
      this.bottomPaddingCache.set(key, 0);
      return 0;
    }

    const frame = tex.frame;
    const canvas = document.createElement('canvas');
    canvas.width = frame.width;
    canvas.height = frame.height;
    const ctx = canvas.getContext('2d')!;
    ctx.drawImage(
      source,
      frame.x, frame.y, frame.width, frame.height,
      0, 0, frame.width, frame.height,
    );
    const pixels = ctx.getImageData(0, 0, frame.width, frame.height).data;

    let emptyRows = 0;
    for (let y = frame.height - 1; y >= 0; y--) {
      let hasContent = false;
      for (let x = 0; x < frame.width; x++) {
        if (pixels[(y * frame.width + x) * 4 + 3] > 10) {
          hasContent = true;
          break;
        }
      }
      if (hasContent) break;
      emptyRows++;
    }

    const padding = emptyRows / frame.height;
    this.bottomPaddingCache.set(key, padding);
    return padding;
  }
}
