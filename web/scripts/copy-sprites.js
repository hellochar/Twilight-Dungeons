/**
 * Copies sprite PNGs from Assets/Textures/ to web/public/sprites/,
 * extracts sub-sprites from Unity sprite atlases,
 * and generates a manifest.json with sprite metadata.
 *
 * Run: node scripts/copy-sprites.js
 */
import { readdir, copyFile, mkdir, writeFile, readFile } from 'fs/promises';
import { join, basename, extname, resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import sharp from 'sharp';

const __dirname = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(__dirname, '..', '..');
const outDir = resolve(__dirname, '..', 'public', 'sprites');

const SPRITE_SOURCES = [
  join(projectRoot, 'Assets', 'Textures', 'Resources'),
  join(projectRoot, 'Assets', 'Textures', 'Plants'),
  join(projectRoot, 'Assets', '3rd Party', 'Resources'),
];

const ATLAS_SOURCES = [
  join(projectRoot, 'Assets', '3rd Party', '1bitpack_kenney_1.1', 'Tilesheet', 'monochrome_transparent_packed.png'),
  join(projectRoot, 'Assets', '3rd Party', '1bitpack_kenney_1.1', 'Tilesheet', 'colored_transparent_packed.png'),
  join(projectRoot, 'Assets', '3rd Party', 'Resources', 'plants.png'),
  join(projectRoot, 'Assets', 'Textures', 'Resources', 'Purple.png'),
];

/** Specific sub-sprites to extract from sparse atlases (avoids extracting hundreds of unused sprites). */
const TARGETED_SPRITES = [
  {
    atlas: join(projectRoot, 'Assets', '3rd Party', 'vectorpixelstar', '1-bit 16px icons part-1.png'),
    wanted: ['Wildekin'],
  },
  {
    atlas: join(projectRoot, 'Assets', '3rd Party', 'DawnLike', 'Characters', 'Undead0.png'),
    wanted: ['Undead0_29'],
  },
  {
    atlas: join(projectRoot, 'Assets', '3rd Party', 'DawnLike', 'Characters', 'Misc0.png'),
    wanted: ['Misc0_14'],
  },
];

/** Get PNG dimensions by reading the IHDR chunk (bytes 16-23). */
async function getPngDimensions(filePath) {
  const buf = await readFile(filePath);
  // PNG IHDR starts at byte 16: 4 bytes width, 4 bytes height (big-endian)
  if (buf[0] !== 0x89 || buf[1] !== 0x50) return null;
  const width = buf.readUInt32BE(16);
  const height = buf.readUInt32BE(16 + 4);
  return { width, height };
}

/**
 * Parse Unity .meta YAML to extract sprite name → rect mappings.
 * Unity meta files have a consistent structure for sprite entries.
 */
function parseMetaSpriteRects(metaText) {
  const sprites = [];
  // Match each sprite entry: serializedVersion, name, rect with x, y, width, height
  // Unity .meta format: "- serializedVersion: 2\n      name: X\n      rect:\n        serializedVersion: 2\n        x: N\n ..."
  const regex = /- serializedVersion: \d+\r?\n\s+name: (.+)\r?\n\s+rect:\r?\n\s+serializedVersion: \d+\r?\n\s+x: ([\d.]+)\r?\n\s+y: ([\d.]+)\r?\n\s+width: ([\d.]+)\r?\n\s+height: ([\d.]+)/g;
  let match;
  while ((match = regex.exec(metaText)) !== null) {
    sprites.push({
      name: match[1].trim(),
      x: Math.round(parseFloat(match[2])),
      y: Math.round(parseFloat(match[3])),
      width: Math.round(parseFloat(match[4])),
      height: Math.round(parseFloat(match[5])),
    });
  }
  return sprites;
}

/** Extract individual sprite PNGs from Unity sprite atlases. */
async function extractAtlasSprites(manifest) {
  let extracted = 0;

  for (const atlasPng of ATLAS_SOURCES) {
    const metaPath = atlasPng + '.meta';
    let metaText;
    try {
      metaText = await readFile(metaPath, 'utf-8');
    } catch {
      console.warn(`Skipping atlas (no .meta): ${atlasPng}`);
      continue;
    }

    const sprites = parseMetaSpriteRects(metaText);
    if (sprites.length === 0) {
      console.warn(`No sprites found in: ${metaPath}`);
      continue;
    }

    // Get image height for Y-flip (Unity Y=0 at bottom, PNG Y=0 at top)
    const metadata = await sharp(atlasPng).metadata();
    const imageHeight = metadata.height;

    const atlasName = basename(atlasPng, '.png');
    console.log(`Atlas "${atlasName}": extracting ${sprites.length} sprites...`);

    for (const s of sprites) {
      const pixelY = imageHeight - s.y - s.height;
      const spriteName = s.name.toLowerCase();
      const destPath = join(outDir, `${spriteName}.png`);

      await sharp(atlasPng)
        .extract({ left: s.x, top: pixelY, width: s.width, height: s.height })
        .toFile(destPath);

      manifest[spriteName] = {
        file: `${spriteName}.png`,
        width: s.width,
        height: s.height,
        frameCount: 1,
        frameWidth: s.width,
        frameHeight: s.height,
      };
      extracted++;
    }
  }

  return extracted;
}

/** Extract a specific set of named sub-sprites from sparse atlases. */
async function extractTargetedSprites(manifest) {
  let extracted = 0;
  for (const { atlas, wanted } of TARGETED_SPRITES) {
    const metaPath = atlas + '.meta';
    let metaText;
    try {
      metaText = await readFile(metaPath, 'utf-8');
    } catch {
      console.warn(`Skipping targeted atlas (no .meta): ${atlas}`);
      continue;
    }
    const allSprites = parseMetaSpriteRects(metaText);
    const metadata = await sharp(atlas).metadata();
    const imageHeight = metadata.height;
    for (const name of wanted) {
      const s = allSprites.find(sp => sp.name === name);
      if (!s) { console.warn(`Targeted sprite "${name}" not found in ${atlas}`); continue; }
      const pixelY = imageHeight - s.y - s.height;
      const spriteName = s.name.toLowerCase();
      const destPath = join(outDir, `${spriteName}.png`);
      await sharp(atlas)
        .extract({ left: s.x, top: pixelY, width: s.width, height: s.height })
        .toFile(destPath);
      manifest[spriteName] = {
        file: `${spriteName}.png`,
        width: s.width,
        height: s.height,
        frameCount: 1,
        frameWidth: s.width,
        frameHeight: s.height,
      };
      extracted++;
    }
  }
  return extracted;
}

async function main() {
  await mkdir(outDir, { recursive: true });

  const manifest = {};
  let copied = 0;

  for (const srcDir of SPRITE_SOURCES) {
    let files;
    try {
      files = await readdir(srcDir);
    } catch {
      console.warn(`Skipping missing directory: ${srcDir}`);
      continue;
    }

    for (const file of files) {
      if (extname(file).toLowerCase() !== '.png') continue;

      const srcPath = join(srcDir, file);
      const name = basename(file, '.png').toLowerCase();
      const destPath = join(outDir, `${name}.png`);

      await copyFile(srcPath, destPath);
      copied++;

      const dims = await getPngDimensions(srcPath);
      if (dims) {
        // Custom frame overrides for non-standard sprite sheets
        // Crab: 5 frames of 18×16 at x=0,20,40,60,80 (2px gap between frames)
        const CUSTOM_FRAMES = {
          'crab': { frameWidth: 18, frameHeight: 16, frameCount: 5, stride: 20 },
        };
        const custom = CUSTOM_FRAMES[name];
        const frameHeight = custom?.frameHeight ?? 16;
        const frameWidth = custom?.frameWidth ?? 16;
        const frameCount = custom?.frameCount ?? (Math.floor(dims.width / frameWidth) > 1 ? Math.floor(dims.width / frameWidth) : 1);
        manifest[name] = {
          file: `${name}.png`,
          width: dims.width,
          height: dims.height,
          frameCount,
          frameWidth,
          frameHeight,
          ...(custom?.stride ? { stride: custom.stride } : {}),
        };
      }
    }
  }

  // Extract sprites from Unity atlas spritesheets
  const extracted = await extractAtlasSprites(manifest);

  // Extract specific sub-sprites from sparse atlases
  const targetedExtracted = await extractTargetedSprites(manifest);

  // Extract heart sub-sprites from animated spritesheet (5 × 17×17 horizontal strip)
  const heartsDir = join(outDir, 'hearts');
  await mkdir(heartsDir, { recursive: true });
  const heartSheet = join(projectRoot, 'Assets', '3rd Party', 'Hearts', 'PNG', 'animated', 'border', 'heart_animated_2.png');
  try {
    for (let i = 0; i < 5; i++) {
      await sharp(heartSheet)
        .extract({ left: i * 17, top: 0, width: 17, height: 17 })
        .toFile(join(heartsDir, `heart-${i}.png`));
    }
    console.log('Extracted 5 heart sub-sprites');
  } catch (e) {
    console.warn('Could not extract heart sprites:', e.message);
  }

  // Extract specific UI sprites from roguelikeSheet atlas
  const roguelikeSheet = join(projectRoot, 'Assets', '3rd Party', 'Resources', 'roguelikeSheet_transparent.png');
  const roguelikeMeta = roguelikeSheet + '.meta';
  try {
    const rlMeta = await readFile(roguelikeMeta, 'utf-8');
    const rlSprites = parseMetaSpriteRects(rlMeta);
    const rlDims = await sharp(roguelikeSheet).metadata();
    const wantedUI = ['panel-grey', 'panel-grey-inset'];
    for (const name of wantedUI) {
      const s = rlSprites.find(sp => sp.name === name);
      if (!s) { console.warn(`roguelikeSheet: sprite "${name}" not found`); continue; }
      const pixelY = rlDims.height - s.y - s.height;
      await sharp(roguelikeSheet)
        .extract({ left: s.x, top: pixelY, width: s.width, height: s.height })
        .toFile(join(outDir, `${s.name}.png`));
      console.log(`Extracted roguelikeSheet sprite: ${name}`);
    }
  } catch (e) {
    console.warn('Could not extract roguelikeSheet sprites:', e.message);
  }

  // explosion.png: 6-frame 32×32 horizontal strip from 3rd Party
  const explosionSrc = join(projectRoot, 'Assets', '3rd Party', 'explosion.png');
  try {
    await copyFile(explosionSrc, join(outDir, 'explosion.png'));
    manifest['explosion'] = { file: 'explosion.png', width: 192, height: 32, frameCount: 6, frameWidth: 32, frameHeight: 32 };
    console.log('Copied explosion.png');
  } catch (e) {
    console.warn('Could not copy explosion.png:', e.message);
  }

  // ─── Atlas Packing ───
  // Pack all manifest sprites into a single texture atlas for fast PixiJS loading.
  // Individual PNGs are still kept for React <img> usage (spriteUrl/statusSpriteUrl).
  const atlasSprites = [];
  for (const [name, info] of Object.entries(manifest)) {
    const filePath = join(outDir, info.file);
    try {
      const buffer = await readFile(filePath);
      atlasSprites.push({ name, buffer, width: info.width, height: info.height });
    } catch {
      console.warn(`Atlas: could not read ${filePath}, skipping`);
    }
  }

  // Shelf bin-packer: sort by height desc, pack left-to-right into rows
  const MAX_ATLAS_WIDTH = 2048;
  atlasSprites.sort((a, b) => b.height - a.height || b.width - a.width);
  let shelfY = 0, shelfHeight = 0, cursorX = 0;
  for (const s of atlasSprites) {
    if (cursorX + s.width > MAX_ATLAS_WIDTH) {
      shelfY += shelfHeight;
      shelfHeight = 0;
      cursorX = 0;
    }
    s.ax = cursorX;
    s.ay = shelfY;
    cursorX += s.width;
    shelfHeight = Math.max(shelfHeight, s.height);
  }
  const atlasHeight = nextPow2(shelfY + shelfHeight);

  // Add atlas coords to manifest
  for (const s of atlasSprites) {
    manifest[s.name].ax = s.ax;
    manifest[s.name].ay = s.ay;
  }

  // Composite atlas PNG
  const atlasPath = join(outDir, 'atlas.png');
  await sharp({
    create: { width: MAX_ATLAS_WIDTH, height: atlasHeight, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } },
  })
    .composite(atlasSprites.map(s => ({ input: s.buffer, left: s.ax, top: s.ay })))
    .png()
    .toFile(atlasPath);

  console.log(`Atlas: ${atlasSprites.length} sprites packed into ${MAX_ATLAS_WIDTH}×${atlasHeight} (${atlasPath})`);

  await writeFile(
    join(outDir, 'manifest.json'),
    JSON.stringify(manifest, null, 2),
  );

  console.log(`Copied ${copied} sprites to public/sprites/`);
  console.log(`Extracted ${extracted} atlas sprites, ${targetedExtracted} targeted sprites`);
  console.log(`Manifest: ${Object.keys(manifest).length} entries`);
}

function nextPow2(n) { let v = 1; while (v < n) v <<= 1; return v; }

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
