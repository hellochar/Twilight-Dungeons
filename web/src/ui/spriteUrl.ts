import type { CSSProperties } from 'react';
import { SPRITE_NAME_MAP } from '../renderer/spriteNameMap';
import { SPRITE_TINTS, SPRITE_ALPHAS } from '../renderer/spriteTints';

/** Status className → sprite filename. Matches spriteKey values from GameRenderer STATUS_VISUALS. */
const STATUS_SPRITE_MAP: Record<string, string> = {
  PoisonedStatus:    'poisoned-status',
  WebbedStatus:      'web',
  WeaknessStatus:    'weakness',
  InShellStatus:     'snail-shell',
  SlimedStatus:      'slimed',
  SurprisedStatus:   'colored_transparent_packed_658',
  GuardedStatus:     'guardroot',
  FreeMoveStatus:    'free-move',
  SoftGrassStatus:   'colored_transparent_packed_95',
  CharmedStatus:     'charmed',
  ConfusedStatus:    'confused',
  PacifiedStatus:    'peace',
  ParasiteStatus:    'parasite',
  VulnerableStatus:  'vulnerable',
  SporedStatus:      'spored-status',
  ConstrictedStatus: 'hanging-vines',
  FrenziedStatus:    '3red',
  InfectedStatus:    'infected',
  ThirdEyeStatus:    'third-eye',
};

/** Known debuff status class names. */
const DEBUFF_STATUSES = new Set([
  'PoisonedStatus', 'WebbedStatus', 'WeaknessStatus', 'InShellStatus', 'SlimedStatus',
]);

const BASE = import.meta.env.BASE_URL;

/** Resolve an entity/item display name to a sprite PNG URL. */
export function spriteUrl(displayName: string): string {
  const lower = displayName.toLowerCase();
  const key = SPRITE_NAME_MAP[lower] ?? lower;
  return `${BASE}sprites/${key}.png`;
}

/** Resolve a status className to a sprite PNG URL. */
export function statusSpriteUrl(className: string): string {
  const key = STATUS_SPRITE_MAP[className];
  if (key) return `${BASE}sprites/${key}.png`;
  return `${BASE}sprites/${className.toLowerCase().replace(/status$/, '')}.png`;
}

/**
 * Builds an SVG feColorMatrix filter that exactly replicates PixiJS multiplicative tinting.
 * The matrix multiplies each channel independently: output_R = input_R * r_t, etc.
 */
function hexTintToFilter(hex: number): string {
  const r = ((hex >> 16) & 0xff) / 255;
  const g = ((hex >> 8) & 0xff) / 255;
  const b = (hex & 0xff) / 255;
  const matrix = `${r} 0 0 0 0  0 ${g} 0 0 0  0 0 ${b} 0 0  0 0 0 1 0`;
  const svg = `<svg xmlns='http://www.w3.org/2000/svg'><filter id='tint'><feColorMatrix type='matrix' values='${matrix}'/></filter></svg>`;
  return `url("data:image/svg+xml,${encodeURIComponent(svg)}#tint")`;
}

/** Returns CSS style for a sprite img that applies the correct tint and opacity from spriteTints. */
export function getSpriteImgStyle(displayName: string): CSSProperties {
  const lower = displayName.toLowerCase();
  const tint = SPRITE_TINTS[lower];
  const alpha = SPRITE_ALPHAS[lower];
  const style: CSSProperties = {};
  if (tint != null) style.filter = hexTintToFilter(tint);
  if (alpha != null) style.opacity = alpha;
  return style;
}

/** Check if a status className represents a debuff. */
export function isDebuffStatus(className: string): boolean {
  return DEBUFF_STATUSES.has(className);
}
