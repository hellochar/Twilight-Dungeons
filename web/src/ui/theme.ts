import type { CSSProperties } from 'react';
import { FONT_FAMILY, FontSize } from './fonts';

/** Shared base style for all interactive buttons. */
export const buttonBase: CSSProperties = {
  fontFamily: FONT_FAMILY,
  fontSize: FontSize.xl,
  borderRadius: 8,
  cursor: 'pointer',
};

/** Standard padding for text buttons. */
export const buttonPadding: CSSProperties = {
  padding: '12px 32px',
};

/** Compact padding for icon-only buttons (×, ✕, ?). */
export const buttonPaddingCompact: CSSProperties = {
  padding: '12px 24px',
};
