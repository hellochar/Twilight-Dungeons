import { useState, useCallback } from 'react';
import type { StatusSnapshot } from '../hooks/useGameLoop';
import { statusSpriteUrl } from './spriteUrl';
import { getObjectInfo } from '../model/ObjectInfo';
import { EntityInfoPopup, type EntityInfoData } from './EntityInfoPopup';

/**
 * Status effect icons matching Unity StatusIconController:
 * - Sprite icon for each active status
 * - Red outline/border for debuffs
 * - Stack count overlay for StackingStatus
 * - Click/tap opens ObjectInfo popup
 */

const ICON_SIZE = 24;

interface StatusBarProps {
  statuses: StatusSnapshot[];
}

export function StatusBar({ statuses }: StatusBarProps) {
  const [popup, setPopup] = useState<EntityInfoData | null>(null);

  const openPopup = useCallback((status: StatusSnapshot, x: number, y: number) => {
    const info = getObjectInfo(status.className);
    const stacks = status.stacks ?? 0;
    const rawDesc = info?.description ?? '';
    // Substitute {stacks*N} first, then plain {stacks}
    const desc = rawDesc
      .replace(/\{stacks\*(\d+)\}/g, (_, n) => String(stacks * Number(n)))
      .replace(/\{stacks\}/g, String(stacks));
    setPopup({
      name: status.displayName,
      typeName: status.className,
      spriteSrc: statusSpriteUrl(status.className),
      stats: desc || undefined,
      x,
      y,
    });
  }, []);

  if (statuses.length === 0) return null;

  return (
    <>
      <div style={{
        padding: '0 10px',
        display: 'flex',
        gap: 4,
        flexWrap: 'wrap',
        pointerEvents: 'auto',
      }}>
        {statuses.map((s, i) => (
          <StatusIcon key={`${s.className}-${i}`} status={s} onOpen={openPopup} />
        ))}
      </div>
      {popup && <EntityInfoPopup data={popup} onClose={() => setPopup(null)} />}
    </>
  );
}

function StatusIcon({
  status,
  onOpen,
}: {
  status: StatusSnapshot;
  onOpen: (s: StatusSnapshot, x: number, y: number) => void;
}) {
  const imgSrc = statusSpriteUrl(status.className);

  const handleClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    onOpen(status, e.clientX, e.clientY);
  };

  const handleTouch = (e: React.TouchEvent) => {
    e.preventDefault();
    const t = e.changedTouches[0];
    onOpen(status, t.clientX, t.clientY);
  };

  return (
    <div
      onClick={handleClick}
      onTouchEnd={handleTouch}
      style={{
        width: ICON_SIZE,
        height: ICON_SIZE,
        position: 'relative',
        border: status.isDebuff ? '1.5px solid #c44' : '1.5px solid rgba(255,255,255,0.15)',
        borderRadius: 3,
        background: 'rgba(0,0,0,0.5)',
        imageRendering: 'pixelated',
        cursor: 'pointer',
      }}
    >
      <img
        src={imgSrc}
        alt={status.displayName}
        style={{
          width: '100%',
          height: '100%',
          objectFit: 'contain',
          imageRendering: 'pixelated',
        }}
        onError={e => { (e.target as HTMLImageElement).style.display = 'none'; }}
      />
      {status.stacks != null && status.stacks > 0 && (
        <div style={{
          position: 'absolute',
          bottom: -2,
          right: -2,
          background: '#000',
          color: '#fff',
          fontSize: 14,
          fontFamily: 'CodersCrux, monospace',
          fontWeight: 'bold',
          lineHeight: '1',
          padding: '0 2px',
          borderRadius: 2,
          minWidth: 10,
          textAlign: 'center',
        }}>
          {status.stacks}
        </div>
      )}
    </div>
  );
}
