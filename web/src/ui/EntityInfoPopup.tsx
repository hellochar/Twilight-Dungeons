import { useEffect } from 'react';
import { EntityCard } from './ObjectInfoList';
import { CloseButton } from './CloseButton';
import { isMobile } from '../renderer';

export interface EntityInfoData {
  name: string;
  /** Constructor name for ObjectInfo lookup. */
  typeName: string;
  hp?: number;
  maxHp?: number;
  /** Pre-formatted description (e.g. status with stacks substituted). */
  stats?: string;
  /** Override sprite URL (e.g. for statuses that use a different URL scheme). */
  spriteSrc?: string;
  /** Screen position to anchor popup near. */
  x: number;
  y: number;
}

interface EntityInfoPopupProps {
  data: EntityInfoData;
  onClose: () => void;
}

export function EntityInfoPopup({ data, onClose }: EntityInfoPopupProps) {
  // Close on Escape
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [onClose]);

  // Position: on mobile use centered bottom sheet; on desktop anchor near click
  const mobile = isMobile();
  const popupWidth = 240;
  const popupMaxHeight = 300;
  const margin = 8;
  let left = data.x + margin;
  let top = data.y + margin;

  if (!mobile) {
    if (left + popupWidth > window.innerWidth - margin) {
      left = data.x - popupWidth - margin;
    }
    if (top + popupMaxHeight > window.innerHeight - margin) {
      top = Math.max(margin, window.innerHeight - popupMaxHeight - margin);
    }
  }

  const mobileStyle: React.CSSProperties = {
    position: 'absolute',
    left: '50%',
    transform: 'translateX(-50%)',
    bottom: 80,
    maxWidth: 330,
    width: 'calc(100% - 32px)',
    maxHeight: popupMaxHeight,
    overflow: 'auto',
    zIndex: 100,
  };

  const desktopStyle: React.CSSProperties = {
    position: 'fixed',
    left,
    top,
    width: popupWidth,
    maxHeight: popupMaxHeight,
    overflow: 'auto',
    zIndex: 100,
  };

  return (
    /* Fullscreen backdrop absorbs clicks so they don't reach the canvas */
    <div
      style={{ position: 'fixed', inset: 0, zIndex: 99, pointerEvents: 'auto' }}
      onMouseDown={onClose}
      onTouchEnd={onClose}
    >
      <div
        style={mobile ? mobileStyle : desktopStyle}
        onMouseDown={(e) => e.stopPropagation()}
        onTouchEnd={(e) => e.stopPropagation()}
      >
        <CloseButton
          onClick={onClose}
          style={{ position: 'absolute', top: 4, right: 4, zIndex: 101 }}
        />
        <EntityCard
          data={{
            displayName: data.name,
            typeName: data.typeName,
            hp: data.hp,
            maxHp: data.maxHp,
          }}
          horizontal={false}
          description={data.stats}
          spriteSrc={data.spriteSrc}
        />
      </div>
    </div>
  );
}
