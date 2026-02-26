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
 * Manages loading and caching of sprite textures.
 * Loads individual PNGs from public/sprites/ and slices multi-frame strips.
 */
export class SpriteManager {
  private manifest: SpriteManifest = {};
  private textures = new Map<string, Texture>();
  private frames = new Map<string, Texture[]>();
  private loaded = false;

  /** Entity displayName → sprite key overrides. */
  private static NAME_MAP: Record<string, string> = {
    'player': 'player',
    'hands': 'hands',
    'stick': 'stick',
    'ground': 'square',
    'wall': 'square',
    'chasm': 'chasm',
    'hard ground': 'square',
    'fancy ground': 'square',
    'soil': 'square',
    'water': 'square',
    'signpost': 'speech',
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
    this.loaded = true;
    console.log(`SpriteManager: loaded ${this.textures.size} sprites`);
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
