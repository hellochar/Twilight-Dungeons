/**
 * SoundManager — Web Audio API singleton for SFX + background music.
 * Mirrors Unity's AudioClipStore + PlayerController audio logic.
 */

import { MASTER_VOLUME, SFX_VOLUME, HURT_VOLUME, MUSIC_VOLUME, MUSIC_FADE_IN_S, MUSIC_FADE_OUT_S } from '../constants';

export type SoundKey =
  | 'move' | 'attack' | 'attackNoDamage' | 'bossDeath' | 'death' | 'explosion'
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
  explosion:            'boombug-explode.ogg',
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

const MUTE_KEY = 'twilight-dungeons-muted';

export class SoundManager {
  private ctx: AudioContext | null = null;
  private masterGain: GainNode | null = null;
  private sfxBuffers = new Map<string, AudioBuffer>();
  private hurtBuffers: AudioBuffer[] = [];
  private musicBuffers = new Map<string, AudioBuffer>();
  private _muted: boolean = localStorage.getItem(MUTE_KEY) === '1';
  private _muteListeners: Array<(muted: boolean) => void> = [];

  // Music state
  private currentTrack: MusicTrack = 'none';
  private pendingTrack: MusicTrack = 'none';
  private musicSource: AudioBufferSourceNode | null = null;
  private musicGain: GainNode | null = null;
  private stopTimer: ReturnType<typeof setTimeout> | null = null;

  get muted(): boolean { return this._muted; }

  toggleMute(): void {
    this._muted = !this._muted;
    localStorage.setItem(MUTE_KEY, this._muted ? '1' : '0');
    if (this.masterGain) {
      this.masterGain.gain.value = this._muted ? 0 : MASTER_VOLUME;
    }
    for (const fn of this._muteListeners) fn(this._muted);
  }

  onMuteChange(fn: (muted: boolean) => void): () => void {
    this._muteListeners.push(fn);
    return () => { this._muteListeners = this._muteListeners.filter(f => f !== fn); };
  }

  /** Ensure AudioContext exists, creating it on demand. */
  private ensureContext(): AudioContext {
    if (!this.ctx) {
      this.ctx = new AudioContext();
      this.masterGain = this.ctx.createGain();
      this.masterGain.gain.value = this._muted ? 0 : MASTER_VOLUME;
      this.masterGain.connect(this.ctx.destination);
    }
    return this.ctx;
  }

  /** Initialize audio system. Registers gesture-based unlock for AudioContext. */
  init(): void {
    const unlockCtx = () => {
      if (!this.ctx) {
        this.ensureContext();
      }
      this.ctx!.resume();
      document.removeEventListener('touchstart', unlockCtx, true);
      document.removeEventListener('pointerdown', unlockCtx, true);
      document.removeEventListener('keydown', unlockCtx, true);
    };
    document.addEventListener('touchstart', unlockCtx, true);
    document.addEventListener('pointerdown', unlockCtx, true);
    document.addEventListener('keydown', unlockCtx, true);
  }

  /** Load SFX and hurt clips. Call this before starting the game. */
  async loadSFX(): Promise<void> {
    this.ensureContext();

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

  /** Load background music tracks. After loading, starts any pending track. */
  async loadMusic(): Promise<void> {
    const base = import.meta.env.BASE_URL;
    const load = (file: string) => this.loadBuffer(`${base}audio/${file}`);

    await Promise.all(
      Object.entries(MUSIC_FILES).map(async ([key, file]) => {
        const buf = await load(file);
        if (buf) this.musicBuffers.set(key, buf);
      }),
    );

    // If setMusic was called before buffers were ready, replay now
    if (this.pendingTrack !== 'none') {
      const track = this.pendingTrack;
      this.currentTrack = 'none'; // Reset so setMusic doesn't early-return
      this.setMusic(track);
    }
  }

  /** Play a named SFX. pitchVariation applies random 0.75–1.25 playbackRate (for death). */
  play(key: SoundKey, volume = 1.0, pitchVariation = false): void {
    const ctx = this.ctx;
    if (!ctx) return;
    ctx.resume();
    const buffer = this.sfxBuffers.get(key);
    if (!buffer) return;
    this.playBuffer(buffer, volume * SFX_VOLUME, pitchVariation);
  }

  /** Play a random hurt clip at volume 3.0 (matches Unity vol 3f). */
  playHurt(): void {
    const ctx = this.ctx;
    if (!ctx || this.hurtBuffers.length === 0) return;
    ctx.resume();
    const buf = this.hurtBuffers[Math.floor(Math.random() * this.hurtBuffers.length)];
    this.playBuffer(buf, HURT_VOLUME * SFX_VOLUME, false);
  }

  /**
   * Switch background music track.
   * Fades out current in 1s, fades in new over 2s (matches FadeInAudio.cs).
   */
  setMusic(track: MusicTrack): void {
    if (track === this.currentTrack) return;
    this.pendingTrack = track;
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
      gain.gain.linearRampToValueAtTime(0, ctx.currentTime + MUSIC_FADE_OUT_S);
      this.stopTimer = setTimeout(() => { try { src.stop(); } catch { /* ignore */ } }, (MUSIC_FADE_OUT_S + 0.1) * 1000);
    }

    this.currentTrack = track;

    if (track === 'none') return;

    const buffer = this.musicBuffers.get(track === 'boss' ? 'normal' : track);
    if (!buffer) return;

    const gain = ctx.createGain();
    gain.gain.setValueAtTime(0, ctx.currentTime);
    gain.gain.linearRampToValueAtTime(MUSIC_VOLUME, ctx.currentTime + MUSIC_FADE_IN_S);
    gain.connect(this.masterGain!);

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
    gain.connect(this.masterGain!);

    const source = ctx.createBufferSource();
    source.buffer = buffer;
    if (pitchVariation) {
      source.playbackRate.value = 0.75 + Math.random() * 0.5;
    }
    source.connect(gain);
    source.start(0);
  }
}

/** Singleton instance — created once, shared across the app. */
export const soundManager = new SoundManager();
