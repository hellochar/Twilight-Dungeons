/**
 * Centralized tunable constants for the game.
 * Adjust values here to control audio, animation, camera, and UI globally.
 */

// ─── Audio ───

/** Master volume multiplier applied to all audio output. */
export const MASTER_VOLUME = 0.8;
/** Default volume for SFX when no explicit volume is passed. */
export const SFX_VOLUME = 0.75;
/** Music fade-in target volume. */
export const MUSIC_VOLUME = 0.4;
/** Volume for hurt sound effects (matches Unity vol 3f). */
export const HURT_VOLUME = 3.0;
/** Volume for footstep/move SFX. */
export const MOVE_SFX_VOLUME = 0.25;
/** Volume for water-change SFX. */
export const WATER_SFX_VOLUME = 0.2;
/** Music fade-in duration in seconds (matches FadeInAudio.cs). */
export const MUSIC_FADE_IN_S = 2.0;
/** Music fade-out duration in seconds. */
export const MUSIC_FADE_OUT_S = 1.0;

// ─── Animation ───

export const TIME_GAP_DELAY = .2; // a one turn delay turns into a 0.2 second wait

/** BumpAndReturn total duration in seconds. */
export const BUMP_DURATION = 0.25;
/** BumpAndReturn peak displacement scale. */
export const BUMP_INTENSITY = 0.75;
/** Time when bump reaches peak displacement: 0.25 * BUMP_DURATION. */
export const BUMP_IMPACT_TIME = BUMP_DURATION * 0.25;
/** Entity movement speed in tiles per second (Unity ActorController: 16 / actionCost). */
export const MOVE_SPEED = 12;
/** Time for a 1-tile move lerp in seconds, derived from MOVE_SPEED. */
export const MOVE_LERP_S = 1 / MOVE_SPEED;
/** Move lerp delay in ms between entity steps (MOVE_LERP_S + small buffer). */
export const MOVE_LERP_MS = Math.ceil(MOVE_LERP_S * 1000) + 100;
/** Death fade-out duration in seconds. */
export const DEATH_FADE_S = 0.25;
/** Damage flash duration in seconds. */
export const DAMAGE_FLASH_S = 0.25;
/** Damage/heal text float-up fade duration in seconds. */
export const DAMAGE_TEXT_FADE_S = 0.5;
/** GrowAtStart total spawn scale-in animation duration in seconds. */
export const SPAWN_ANIMATION_DURATION = 3.0;
/** FadeThenDestroy duration in milliseconds. */
export const FADE_DURATION_MS = 500;
/** FadeThenDestroy final scale factor (50%). */
export const FADE_END_SCALE = 0.5;
/** TelegraphedTask prefab fade-out duration in seconds. */
export const TELEGRAPH_FADE_DURATION = 0.25;
/** Deep sleep tint color (Unity SleepTaskController). */
export const DEEP_SLEEP_TINT = 0x5DABFF;

// ─── Camera ───

/** Default pixels per tile. */
export const DEFAULT_TILE_SIZE = 32;
/** Minimum pixels per tile. */
export const MIN_TILE_SIZE = 8;
/** Empty tile padding on each side for camera fit. */
export const TILE_PADDING = 0.5;

// ─── UI ───

/** Heart icon size in pixels. */
export const HEART_SIZE = 48;
/** Status effect icon size in pixels. */
export const STATUS_ICON_SIZE = 48;
/** Mute button icon size in pixels. */
export const MUTE_ICON_SIZE = 40;

export const DAY_ONE = '2026-03-01';