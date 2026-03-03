/**
 * Copies audio files from Assets/Audio/ to web/public/audio/.
 * Run: node scripts/copy-audio.js
 */
import { copyFile, mkdir } from 'fs/promises';
import { join, resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(__dirname, '..', '..');
const audioSrc = join(projectRoot, 'Assets', 'Audio');
const outDir = resolve(__dirname, '..', 'public', 'audio');

/** Map of destination filename → source path relative to Assets/Audio/ */
const AUDIO_FILES = {
  // SFX
  'footstep04.ogg':            'kenney_rpgaudio/footstep04.ogg',
  'impactPlank_medium_002.ogg':'kenney_impactsounds/impactPlank_medium_002.ogg',
  'footstep_concrete_000.ogg': 'kenney_impactsounds/footstep_concrete_000.ogg',
  'cloth3.ogg':                'kenney_rpgaudio/cloth3.ogg',
  'cloth4.ogg':                'kenney_rpgaudio/cloth4.ogg',
  'bookOpen.ogg':              'kenney_rpgaudio/bookOpen.ogg',
  'muted-impact.wav':          'self/muted-impact.wav',
  'boss-defeated.wav':         'self/boss-defeated.wav',
  'death.wav':                 'self/death.wav',
  'plant-harvest.ogg':         'self/plant-harvest.ogg',
  'water.mp3':                 'self/water.mp3',
  'item-breaking.wav':         'self/item-breaking.wav',
  'heal.ogg':                  'self/heal.ogg',
  'short-tone.ogg':            'self/short-tone.ogg',
  'debuff.wav':                'self/debuff.wav',
  'hurt1.wav':                 'self/hurt1.wav',
  'hurt2.wav':                 'self/hurt2.wav',
  'hurt3.wav':                 'self/hurt3.wav',
  'floor-change.wav':          'self/floor change.wav',
  'little-noise.ogg':          'self/little-noise.ogg',
  'summon.wav':                'self/summon.wav',
  'error.wav':                 'self/error.wav',
  // Music
  'background-music.wav':      'self/background-music.wav',
  'boss.wav':                  'self/boss.wav',
};

async function main() {
  await mkdir(outDir, { recursive: true });

  let copied = 0;
  for (const [dest, src] of Object.entries(AUDIO_FILES)) {
    const srcPath = join(audioSrc, src);
    const destPath = join(outDir, dest);
    try {
      await copyFile(srcPath, destPath);
      copied++;
    } catch (e) {
      console.warn(`Skipping ${src}: ${e.message}`);
    }
  }
  console.log(`Copied ${copied}/${Object.keys(AUDIO_FILES).length} audio files to public/audio/`);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
