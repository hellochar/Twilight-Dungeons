import type { StatusSnapshot } from '../hooks/useGameLoop';
import { statusSpriteUrl } from './spriteUrl';

/**
 * Status effect icons matching Unity StatusIconController:
 * - Sprite icon for each active status
 * - Red outline/border for debuffs
 * - Stack count overlay for StackingStatus
 */

const ICON_SIZE = 24;

interface StatusBarProps {
  statuses: StatusSnapshot[];
}

export function StatusBar({ statuses }: StatusBarProps) {
  if (statuses.length === 0) return null;

  return (
    <div style={{
      padding: '0 10px',
      display: 'flex',
      gap: 4,
      flexWrap: 'wrap',
      pointerEvents: 'none',
    }}>
      {statuses.map((s, i) => (
        <StatusIcon key={`${s.className}-${i}`} status={s} />
      ))}
    </div>
  );
}

function StatusIcon({ status }: { status: StatusSnapshot }) {
  const imgSrc = statusSpriteUrl(status.className);

  return (
    <div style={{
      width: ICON_SIZE,
      height: ICON_SIZE,
      position: 'relative',
      border: status.isDebuff ? '1.5px solid #c44' : '1.5px solid rgba(255,255,255,0.15)',
      borderRadius: 3,
      background: 'rgba(0,0,0,0.5)',
      imageRendering: 'pixelated',
    }}>
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
      {/* Stack count */}
      {status.stacks != null && status.stacks > 0 && (
        <div style={{
          position: 'absolute',
          bottom: -2,
          right: -2,
          background: '#000',
          color: '#fff',
          fontSize: 9,
          fontFamily: 'monospace',
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
