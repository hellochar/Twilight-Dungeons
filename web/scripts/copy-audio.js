/**
 * Copies/transcodes audio files from Assets/Audio/ to web/public/audio/.
 * - .ogg and .mp3 sources: copied as-is
 * - .wav sources: transcoded to .ogg via ffmpeg (libopus 96kbps)
 *   Output skipped if destination is already newer than source.
 * Run: node scripts/copy-audio.js
 */
import { copyFile, mkdir, readdir, rm, stat } from 'fs/promises';
import { spawn } from 'child_process';
import { join, resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(__dirname, '..', '..');
const audioSrc = join(projectRoot, 'Assets', 'Audio');
const outDir = resolve(__dirname, '..', 'public', 'audio');

/**
 * Map of destination filename → source path relative to Assets/Audio/.
 * .wav sources are transcoded to .ogg; all others are copied directly.
 */
const AUDIO_FILES = {
  // SFX (already compressed — copy as-is)
  'footstep04.ogg':            'kenney_rpgaudio/footstep04.ogg',
  'impactPlank_medium_002.ogg':'kenney_impactsounds/impactPlank_medium_002.ogg',
  'footstep_concrete_000.ogg': 'kenney_impactsounds/footstep_concrete_000.ogg',
  'cloth3.ogg':                'kenney_rpgaudio/cloth3.ogg',
  'cloth4.ogg':                'kenney_rpgaudio/cloth4.ogg',
  'bookOpen.ogg':              'kenney_rpgaudio/bookOpen.ogg',
  'plant-harvest.ogg':         'self/plant-harvest.ogg',
  'water.mp3':                 'self/water.mp3',
  'heal.ogg':                  'self/heal.ogg',
  'short-tone.ogg':            'self/short-tone.ogg',
  'little-noise.ogg':          'self/little-noise.ogg',
  // SFX (WAV sources — transcoded to .ogg)
  'muted-impact.ogg':          'self/muted-impact.wav',
  'boss-defeated.ogg':         'self/boss-defeated.wav',
  'death.ogg':                 'self/death.wav',
  'item-breaking.ogg':         'self/item-breaking.wav',
  'debuff.ogg':                'self/debuff.wav',
  'hurt1.ogg':                 'self/hurt1.wav',
  'hurt2.ogg':                 'self/hurt2.wav',
  'hurt3.ogg':                 'self/hurt3.wav',
  'floor-change.ogg':          'self/floor change.wav',
  'summon.ogg':                'self/summon.wav',
  'error.ogg':                 'self/error.wav',
  // Music (WAV sources — transcoded to .ogg)
  'background-music.ogg':      'self/background-music.wav',
};

/** Transcode src WAV → dest OGG using ffmpeg libopus 96kbps. Skip if dest is up to date. */
async function transcodeWav(srcPath, destPath) {
  try {
    const [srcStat, destStat] = await Promise.all([stat(srcPath), stat(destPath)]);
    if (destStat.mtimeMs >= srcStat.mtimeMs) return 'skipped';
  } catch { /* dest missing — proceed */ }

  return new Promise((resolve, reject) => {
    const ff = spawn('ffmpeg', ['-y', '-i', srcPath, '-c:a', 'libopus', '-b:a', '96k', destPath]);
    ff.on('close', code => code === 0 ? resolve('transcoded') : reject(new Error(`ffmpeg exited ${code} for ${srcPath}`)));
    ff.on('error', reject);
  });
}

async function main() {
  await mkdir(outDir, { recursive: true });

  // Remove any files in outDir that are no longer in AUDIO_FILES (stale artifacts).
  const expected = new Set(Object.keys(AUDIO_FILES));
  try {
    const existing = await readdir(outDir);
    await Promise.all(existing.filter(f => !expected.has(f)).map(f => rm(join(outDir, f))));
  } catch { /* outDir may not exist yet */ }

  const results = await Promise.all(
    Object.entries(AUDIO_FILES).map(async ([dest, src]) => {
      const srcPath = join(audioSrc, src);
      const destPath = join(outDir, dest);
      try {
        if (src.endsWith('.wav')) {
          const result = await transcodeWav(srcPath, destPath);
          return { dest, result };
        } else {
          await copyFile(srcPath, destPath);
          return { dest, result: 'copied' };
        }
      } catch (e) {
        console.warn(`Skipping ${src}: ${e.message}`);
        return { dest, result: 'failed' };
      }
    })
  );

  const counts = { transcoded: 0, copied: 0, skipped: 0, failed: 0 };
  for (const { result } of results) counts[result]++;
  console.log(`Audio: ${counts.transcoded} transcoded, ${counts.copied} copied, ${counts.skipped} skipped, ${counts.failed} failed → public/audio/`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
