import { Assets, Texture, Rectangle } from 'pixi.js';

export interface SpriteInfo {
  file: string;
  width: number;
  height: number;
  frameCount: number;
  frameWidth: number;
  frameHeight: number;
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

  /** Entity displayName → sprite key overrides. */
  private static NAME_MAP: Record<string, string> = {
    'player': 'player',
    'hands': 'hands',
    'stick': 'stick',
    'chasm': 'chasm',
    'soil': 'soil',
    'water': 'water',
    'signpost': 'sign',
    'mini blob': 'miniblob',
    // Monochrome atlas sprites
    'blob': 'monochrome-blob',
    // Colored atlas sprites
    'bat': 'colored_transparent_packed_409',
    'jackal': 'colored_transparent_packed_414',
    'jackal boss': 'jackalboss',
    'gambler': 'gambler',
    'mercenary': 'mercenary',
    'octopus': 'octopus',
    'moss man': 'moss-man',
    'scorpion': 'colored_transparent_packed_263',
    'old dude': 'colored_transparent_packed_311',
    'stump': 'colored_transparent_packed_305',
    'rubble': 'colored_transparent_packed_100',
    'fungal wall': 'fungal-wall',
    // Grasses
    'soft grass': 'softgrass',
    'guardleaf': 'guardroot',
    // Items
    'bat tooth': 'bat-tooth',
    'spider sandals': 'spider-silk-shoes',
    'snail shell': 'snail-shell',
  };

  async load(): Promise<void> {
    if (this.loaded) return;

    const resp = await fetch('/sprites/manifest.json');
    this.manifest = await resp.json();

    // Preload all sprite textures
    const entries = Object.entries(this.manifest);
    const loadPromises = entries.map(async ([name, info]) => {
      try {
        const texture = await Assets.load<Texture>(`/sprites/${info.file}`);
        this.textures.set(name, texture);

        // Slice multi-frame strips
        if (info.frameCount > 1) {
          const frameTextures: Texture[] = [];
          for (let i = 0; i < info.frameCount; i++) {
            const frame = new Texture({
              source: texture.source,
              frame: new Rectangle(
                i * info.frameWidth,
                0,
                info.frameWidth,
                info.frameHeight,
              ),
            });
            frameTextures.push(frame);
          }
          this.frames.set(name, frameTextures);
        }
      } catch {
        // Skip sprites that fail to load
      }
    });

    await Promise.all(loadPromises);
    this.sliceTilesheets();
    this.sliceBorders();
    this.loaded = true;
    console.log(`SpriteManager: loaded ${this.textures.size} sprites, ${this.tileSubSprites.size} tile sub-sprites`);
  }

  /** Slice tilesheets (tiles0, tiles12, tiles24) into named sub-sprites. */
  private sliceTilesheets(): void {
    for (const sheet of ['tiles0', 'tiles12', 'tiles24']) {
      const tex = this.textures.get(sheet);
      if (!tex) continue;
      for (const [name, region] of Object.entries(TILESHEET_REGIONS)) {
        const sub = new Texture({
          source: tex.source,
          frame: new Rectangle(region.x, region.y, 16, 16),
        });
        this.tileSubSprites.set(`${sheet}/${name}`, sub);
      }
    }
  }

  /** Slice border-left sub-sprite from border.png for chasm edge rendering. */
  private sliceBorders(): void {
    const tex = this.textures.get('border');
    if (!tex) return;

    this.borderTexture = new Texture({
      source: tex.source,
      frame: new Rectangle(1, 0, 1, 16),
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
    return SpriteManager.NAME_MAP[lower] ?? lower;
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

  /** Check if a sprite exists for the given name. */
  has(displayName: string): boolean {
    const key = this.resolveKey(displayName);
    return this.textures.has(key);
  }
}
