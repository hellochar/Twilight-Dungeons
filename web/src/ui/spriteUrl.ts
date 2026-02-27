/**
 * Maps entity/item/status display names to sprite URLs for React DOM <img> tags.
 * Mirrors the NAME_MAP logic from SpriteManager but returns file paths instead of PixiJS textures.
 */

/** Entity displayName → sprite filename overrides (same as SpriteManager.NAME_MAP). */
const NAME_MAP: Record<string, string> = {
  'player': 'player',
  'hands': 'hands',
  'stick': 'stick',
  'chasm': 'chasm',
  'soil': 'soil',
  'water': 'water',
  'signpost': 'sign',
  'mini blob': 'miniblob',
  'blob': 'monochrome-blob',
  'bat': 'colored_transparent_packed_409',
  'scorpion': 'colored_transparent_packed_263',
  'soft grass': 'softgrass',
  'guardleaf': 'guardroot',
  'bat tooth': 'bat-tooth',
  'spider sandals': 'spider-silk-shoes',
  'snail shell': 'snail-shell',
  'redberry': 'redberry',
};

/** Status className → sprite filename. */
const STATUS_SPRITE_MAP: Record<string, string> = {
  PoisonedStatus: 'poisoned-status',
  WebbedStatus: 'web',
  WeaknessStatus: 'weakness',
  InShellStatus: 'snail-shell',
  SlimedStatus: 'slimed',
  SurprisedStatus: 'colored_transparent_packed_658',
  GuardedStatus: 'guardroot',
  FreeMoveStatus: 'free-move',
  SoftGrassStatus: 'colored_transparent_packed_95',
};

/** Known debuff status class names. */
const DEBUFF_STATUSES = new Set([
  'PoisonedStatus', 'WebbedStatus', 'WeaknessStatus', 'InShellStatus', 'SlimedStatus',
]);

/** Resolve an entity/item display name to a sprite PNG URL. */
export function spriteUrl(displayName: string): string {
  const lower = displayName.toLowerCase();
  const key = NAME_MAP[lower] ?? lower;
  return `/sprites/${key}.png`;
}

/** Resolve a status className to a sprite PNG URL. */
export function statusSpriteUrl(className: string): string {
  const key = STATUS_SPRITE_MAP[className];
  if (key) return `/sprites/${key}.png`;
  return `/sprites/${className.toLowerCase().replace(/status$/, '')}.png`;
}

/** Check if a status className represents a debuff. */
export function isDebuffStatus(className: string): boolean {
  return DEBUFF_STATUSES.has(className);
}
