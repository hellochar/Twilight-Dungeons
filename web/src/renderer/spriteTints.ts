/**
 * Per-entity sprite tint colors, extracted from Unity prefab SpriteRenderer.m_Color.
 * PixiJS applies tints as multiplicative RGB (same as Unity).
 * Only entities with non-white tints need entries here.
 */
export const SPRITE_TINTS: Record<string, number> = {
  // Monochrome atlas sprites (white base, need tinting)
  'blob': 0xe845c1,        // rgb(0.906, 0.272, 0.757)

  // Colored atlas sprites with non-white tints
  'bat': 0xdef327,          // rgb(0.869, 0.953, 0.155)
  'jackal': 0xc7954e,       // rgb(0.78, 0.584, 0.307)
  'jackal boss': 0xc7954e,  // rgb(0.78, 0.584, 0.307) — same as Jackal
  'gambler': 0xa7ffef,      // rgb(0.655, 1.0, 0.934)
  'mercenary': 0xeebfae,    // rgb(0.934, 0.749, 0.682)
  'octopus': 0x5affd1,      // rgb(0.354, 1.0, 0.818)
  'moss man': 0xe9ffe3,     // rgb(0.912, 1.0, 0.891)
  'scorpion': 0xc7605c,     // rgb(0.78, 0.375, 0.362)

  // Individual PNG sprites with tints
  'snake': 0x9922ec,        // rgb(0.598, 0.134, 0.925)
};
