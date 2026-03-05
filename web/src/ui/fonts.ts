/** Primary game font stack. */
export const FONT_FAMILY = 'CodersCrux, monospace';

/** Semantic font size tokens (pixels). */
export const FontSize = {
  sm:  12,  // labels, slot counts, score rows, minor text
  md:  17,  // body text, descriptions, buttons, status bar
  lg:  22,  // headings, HUD, entity names, panel subtitles
  xl:  40,  // hero text (game over title, panel headers)
} as const;
