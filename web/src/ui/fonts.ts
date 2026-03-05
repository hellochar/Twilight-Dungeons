/** Serif font for flavor text, lore, etc. */
export const FONT_FAMILY_SERIF = "'Libre Baskerville', serif";

/** Primary game font stack. */
export const FONT_FAMILY = 'CodersCrux, monospace';
// export const FONT_FAMILY = FONT_FAMILY_SERIF;

/** Semantic font size tokens (pixels). */
export const FontSize = {
  serifSm: 14,
  serifLg: 20,
  sm:  16,  // labels, slot counts, score rows, minor text
  md:  20,  // body text, descriptions, buttons, status bar
  lg:  24,  // headings, HUD, entity names, panel subtitles
  xl:  40,  // hero text (game over title, panel headers)
} as const;
