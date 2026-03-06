import type { CSSProperties } from 'react';
import { buttonBase, buttonPaddingCompact } from './theme';

interface CloseButtonProps {
  onClick: () => void;
  style?: CSSProperties;
}

export function CloseButton({ onClick, style }: CloseButtonProps) {
  return (
    <button
      onClick={onClick}
      style={{
        ...buttonBase,
        ...buttonPaddingCompact,
        background: 'transparent',
        border: 'none',
        color: '#888',
        lineHeight: 1,
        ...style,
      }}
    >
      ✕
    </button>
  );
}
