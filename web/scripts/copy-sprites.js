/**
 * Copies sprite PNGs from Assets/Textures/ to web/public/sprites/
 * and generates a manifest.json with sprite metadata.
 *
 * Run: node scripts/copy-sprites.js
 */
import { readdir, copyFile, mkdir, writeFile, stat } from 'fs/promises';
import { join, basename, extname, resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { execSync } from 'child_process';

const __dirname = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(__dirname, '..', '..');
const outDir = resolve(__dirname, '..', 'public', 'sprites');

const SPRITE_SOURCES = [
  join(projectRoot, 'Assets', 'Textures', 'Resources'),
  join(projectRoot, 'Assets', 'Textures', 'Plants'),
];

/** Get PNG dimensions by reading the IHDR chunk (bytes 16-23). */
async function getPngDimensions(filePath) {
  const { readFile } = await import('fs/promises');
  const buf = await readFile(filePath);
  // PNG IHDR starts at byte 16: 4 bytes width, 4 bytes height (big-endian)
  if (buf[0] !== 0x89 || buf[1] !== 0x50) return null;
  const width = buf.readUInt32BE(16);
  const height = buf.readUInt32BE(16 + 4);
  return { width, height };
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

  await writeFile(
    join(outDir, 'manifest.json'),
    JSON.stringify(manifest, null, 2),
  );

  console.log(`Copied ${copied} sprites to public/sprites/`);
  console.log(`Manifest: ${Object.keys(manifest).length} entries`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
