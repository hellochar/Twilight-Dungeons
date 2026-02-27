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
];

const ATLAS_SOURCES = [
  join(projectRoot, 'Assets', '3rd Party', '1bitpack_kenney_1.1', 'Tilesheet', 'monochrome_transparent_packed.png'),
  join(projectRoot, 'Assets', '3rd Party', '1bitpack_kenney_1.1', 'Tilesheet', 'colored_transparent_packed.png'),
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
        const frameHeight = 16; // Standard sprite height
        const frameWidth = 16;  // Standard sprite width
        const frameCount = Math.floor(dims.width / frameWidth);
        manifest[name] = {
          file: `${name}.png`,
          width: dims.width,
          height: dims.height,
          frameCount: frameCount > 1 ? frameCount : 1,
          frameWidth,
          frameHeight,
        };
      }
    }
  }

  // Extract sprites from Unity atlas spritesheets
  const extracted = await extractAtlasSprites(manifest);

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

  await writeFile(
    join(outDir, 'manifest.json'),
    JSON.stringify(manifest, null, 2),
  );

  console.log(`Copied ${copied} sprites to public/sprites/`);
  console.log(`Extracted ${extracted} atlas sprites`);
  console.log(`Manifest: ${Object.keys(manifest).length} entries`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
