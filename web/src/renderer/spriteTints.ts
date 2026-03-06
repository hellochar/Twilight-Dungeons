/**
 * Per-entity sprite tint colors, extracted from Unity prefab SpriteRenderer.m_Color.
 * PixiJS applies tints as multiplicative RGB (same as Unity).
 * Only entities with non-white tints need entries here.
 */
export const SPRITE_ALPHAS: Record<string, number> = {
  'soft grass': 0.902, // m_Color.a = 0.9019608
};
export const SPRITE_TINTS: Record<string, number> = {
  // Monochrome atlas sprites (white base, need tinting)
  'blob': 0xe845c1,        // rgb(0.906, 0.272, 0.757)
  'mini blob': 0xff91f2,   // rgb(1.0, 0.568, 0.948)

  // Colored atlas sprites with non-white tints
  'bat': 0xdef327,          // rgb(0.869, 0.953, 0.155)
  'jackal': 0xff954e,       // rgb(1.0, 0.584, 0.307)
  'jackal boss': 0xff954e,  // rgb(1.0, 0.584, 0.307) — same as Jackal
  'gambler': 0xa7ffef,      // rgb(0.655, 1.0, 0.934)
  'mercenary': 0xeebfae,    // rgb(0.934, 0.749, 0.682)
  'octopus': 0x5affd1,      // rgb(0.354, 1.0, 0.818)
  'moss man': 0xe9ffe3,     // rgb(0.912, 1.0, 0.891)
  'scorpion': 0xff605c,     // rgb(1.0, 0.375, 0.362)
  'snail': 0xe4ff98,        // rgb(0.892, 1.0, 0.596)

  'grasper': 0x6DBC3F,      // rgb(0.427, 0.737, 0.247)
  'tendril': 0x6DBC3F,      // rgb(0.428, 0.736, 0.247) — Tendril.prefab m_Color
  'wildekin': 0xFFB300,     // rgb(1.0, 0.703, 0.0)
  'hard shell': 0x00DBFF,   // rgb(0.0, 0.861, 1.0)

  // Individual PNG sprites with tints
  'snake': 0x9922ec,        // rgb(0.598, 0.134, 0.925)

  // Grasses with non-white tints
  'soft grass': 0x3BAA62,   // rgb(0.231, 0.667, 0.383) alpha=0.902
};
