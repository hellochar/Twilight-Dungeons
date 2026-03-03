/**
 * SoundManager — Web Audio API singleton for SFX + background music.
 * Mirrors Unity's AudioClipStore + PlayerController audio logic.
 */

export type SoundKey =
  | 'move' | 'attack' | 'attackNoDamage' | 'bossDeath' | 'death'
  | 'plantHarvest' | 'playerChangeWater' | 'playerEquip' | 'playerEquipmentBreak'
  | 'playerHeal' | 'playerGeneric' | 'playerGetDebuff' | 'playerPickupItem'
  | 'playerTakeStairs' | 'playerWait' | 'summon' | 'uiError';

type MusicTrack = 'normal' | 'boss' | 'none';

/** Maps canonical key → public/audio filename. */
const SFX_FILES: Record<SoundKey, string> = {
  move:                 'footstep04.ogg',
  attack:               'impactPlank_medium_002.ogg',
  attackNoDamage:       'muted-impact.ogg',
  bossDeath:            'boss-defeated.ogg',
  death:                'death.ogg',
  plantHarvest:         'plant-harvest.ogg',
  playerChangeWater:    'water.mp3',
  playerEquip:          'cloth3.ogg',
  playerEquipmentBreak: 'item-breaking.ogg',
  playerHeal:           'heal.ogg',
  playerGeneric:        'short-tone.ogg',
  playerGetDebuff:      'debuff.ogg',
  playerPickupItem:     'cloth4.ogg',
  playerTakeStairs:     'floor-change.ogg',
  playerWait:           'little-noise.ogg',
  summon:               'summon.ogg',
  uiError:              'error.ogg',
};

const HURT_FILES = ['hurt1.ogg', 'hurt2.ogg', 'hurt3.ogg'];
const MUSIC_FILES: Record<'normal', string> = {
  normal: 'background-music.ogg',
};

export class SoundManager {
  private ctx: AudioContext | null = null;
  private sfxBuffers = new Map<string, AudioBuffer>();
  private hurtBuffers: AudioBuffer[] = [];
  private musicBuffers = new Map<string, AudioBuffer>();

  // Music state
  private currentTrack: MusicTrack = 'none';
  private musicSource: AudioBufferSourceNode | null = null;
  private musicGain: GainNode | null = null;
  private stopTimer: ReturnType<typeof setTimeout> | null = null;

  /** Load SFX and hurt clips. Call this before starting the game. */
  async loadSFX(): Promise<void> {
    this.ctx = new AudioContext();

    // iOS/mobile: AudioContext only unlocks when resume() is called directly inside
    // a user gesture handler. GSAP timeline callbacks run in RAF — outside gesture scope.
    // This listener fires resume() at the earliest possible gesture, before any RAF tick.
    const unlockCtx = () => {
      this.ctx?.resume();
      document.removeEventListener('touchstart', unlockCtx, true);
      document.removeEventListener('pointerdown', unlockCtx, true);
      document.removeEventListener('keydown', unlockCtx, true);
    };
    document.addEventListener('touchstart', unlockCtx, true);
    document.addEventListener('pointerdown', unlockCtx, true);
    document.addEventListener('keydown', unlockCtx, true);

    const base = import.meta.env.BASE_URL;
    const load = (file: string) => this.loadBuffer(`${base}audio/${file}`);

    await Promise.all([
      ...Object.entries(SFX_FILES).map(async ([key, file]) => {
        const buf = await load(file);
        if (buf) this.sfxBuffers.set(key, buf);
      }),
      ...HURT_FILES.map(async (file) => {
        const buf = await load(file);
        if (buf) this.hurtBuffers.push(buf);
      }),
    ]);
  }

  /** Load background music tracks. Safe to fire-and-forget — setMusic() no-ops if not ready. */
  async loadMusic(): Promise<void> {
    const base = import.meta.env.BASE_URL;
    const load = (file: string) => this.loadBuffer(`${base}audio/${file}`);

    await Promise.all(
      Object.entries(MUSIC_FILES).map(async ([key, file]) => {
        const buf = await load(file);
        if (buf) this.musicBuffers.set(key, buf);
      }),
    );
  }

  /** Play a named SFX. pitchVariation applies random 0.75–1.25 playbackRate (for death). */
  play(key: SoundKey, volume = 1.0, pitchVariation = false): void {
    const ctx = this.ctx;
    if (!ctx) return;
    ctx.resume();
    const buffer = this.sfxBuffers.get(key);
    if (!buffer) return;
    this.playBuffer(buffer, volume, pitchVariation);
  }

  /** Play a random hurt clip at volume 3.0 (matches Unity vol 3f). */
  playHurt(): void {
    const ctx = this.ctx;
    if (!ctx || this.hurtBuffers.length === 0) return;
    ctx.resume();
    const buf = this.hurtBuffers[Math.floor(Math.random() * this.hurtBuffers.length)];
    this.playBuffer(buf, 3.0, false);
  }

  /**
   * Switch background music track.
   * Fades out current in 1s, fades in new over 2s (matches FadeInAudio.cs).
   */
  setMusic(track: MusicTrack): void {
    if (track === this.currentTrack) return;
    const ctx = this.ctx;
    if (!ctx) return;
    ctx.resume();

    // Fade out + stop current
    if (this.musicSource && this.musicGain) {
      const gain = this.musicGain;
      const src = this.musicSource;
      this.musicSource = null;
      this.musicGain = null;
      if (this.stopTimer !== null) clearTimeout(this.stopTimer);
      gain.gain.setValueAtTime(gain.gain.value, ctx.currentTime);
      gain.gain.linearRampToValueAtTime(0, ctx.currentTime + 1);
      this.stopTimer = setTimeout(() => { try { src.stop(); } catch { /* ignore */ } }, 1100);
    }

    this.currentTrack = track;

    if (track === 'none') return;

    const buffer = this.musicBuffers.get(track === 'boss' ? 'normal' : track);
    if (!buffer) return;

    const gain = ctx.createGain();
    gain.gain.setValueAtTime(0, ctx.currentTime);
    gain.gain.linearRampToValueAtTime(1.0, ctx.currentTime + 2);
    gain.connect(ctx.destination);

    const source = ctx.createBufferSource();
    source.buffer = buffer;
    source.loop = true;
    source.connect(gain);
    source.start(0);

    this.musicSource = source;
    this.musicGain = gain;
  }

  // ─── Private ───

  private async loadBuffer(url: string): Promise<AudioBuffer | null> {
    try {
      const resp = await fetch(url);
      if (!resp.ok) { console.warn(`Audio load failed: ${url}`); return null; }
      const arr = await resp.arrayBuffer();
      return await this.ctx!.decodeAudioData(arr);
    } catch (e) {
      console.warn(`Audio decode failed: ${url}`, e);
      return null;
    }
  }

  private playBuffer(buffer: AudioBuffer, volume: number, pitchVariation: boolean): void {
    const ctx = this.ctx!;
    const gain = ctx.createGain();
    gain.gain.value = volume;
    gain.connect(ctx.destination);

    const source = ctx.createBufferSource();
    source.buffer = buffer;
    if (pitchVariation) {
      source.playbackRate.value = 0.75 + Math.random() * 0.5;
    }
    source.connect(gain);
    source.start(0);
  }
}
