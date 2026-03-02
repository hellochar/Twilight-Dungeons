import type { GameOverInfo } from '../hooks/useGameLoop';

interface GameOverOverlayProps {
  info: GameOverInfo;
  onPlayAgain: () => void;
}

export function GameOverOverlay({ info, onPlayAgain }: GameOverOverlayProps) {
  const title = info.won
    ? 'Floor Cleared!'
    : info.killedBy
      ? `You perished to ${info.killedBy}...`
      : 'You perished...';

  const borderColor = info.won ? '#4f4' : '#f44';

  // Non-blocking banner at the bottom of the screen
  return (
    <div style={{
      position: 'absolute',
      top: 12,
      left: '50%',
      transform: 'translateX(-50%)',
      zIndex: 100,
      pointerEvents: 'auto',
    }}>
      <div style={{
        background: 'rgba(16, 16, 24, 0.92)',
        border: `1px solid ${borderColor}`,
        borderRadius: 8,
        padding: '12px 24px',
        display: 'flex',
        alignItems: 'center',
        gap: 20,
        fontFamily: 'CodersCrux, monospace',
        color: '#ccc',
        whiteSpace: 'nowrap',
      }}>
        <span style={{ color: borderColor, fontWeight: 'bold', fontSize: 15 }}>
          {title}
        </span>

        <span style={{ fontSize: 12, color: '#888' }}>
          T{info.turnsTaken}
          {' \u00b7 '}
          {info.enemiesDefeated} killed
          {' \u00b7 '}
          {info.damageDealt} dmg
        </span>

        <button
          onClick={onPlayAgain}
          style={{
            background: '#335',
            color: '#ccc',
            border: '1px solid #556',
            borderRadius: 4,
            padding: '4px 14px',
            fontFamily: 'CodersCrux, monospace',
            fontSize: 12,
            cursor: 'pointer',
          }}
        >
          New Game
        </button>
      </div>
    </div>
  );
}
