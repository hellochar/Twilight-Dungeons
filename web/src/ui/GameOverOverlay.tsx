import type { GameOverInfo } from '../hooks/useGameLoop';

interface GameOverOverlayProps {
  info: GameOverInfo;
  onPlayAgain: () => void;
}

export function GameOverOverlay({ info, onPlayAgain }: GameOverOverlayProps) {
  // Unity: "You perished to {killedBy}..." on loss
  const titleColor = info.won ? '#4f4' : '#f44';
  const title = info.won
    ? 'Floor Cleared!'
    : info.killedBy
      ? `You perished to ${info.killedBy}...`
      : 'You perished...';

  return (
    <div style={{
      position: 'absolute',
      inset: 0,
      background: 'rgba(0,0,0,0.7)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      zIndex: 100,
      pointerEvents: 'auto',
    }}>
      <div style={{
        background: '#1a1a24',
        border: '1px solid #444',
        borderRadius: 8,
        padding: '24px 32px',
        minWidth: 260,
        textAlign: 'center',
        fontFamily: 'monospace',
        color: '#ccc',
      }}>
        <h2 style={{ color: titleColor, margin: '0 0 16px', fontSize: 20 }}>
          {title}
        </h2>

        <table style={{
          margin: '0 auto 20px',
          borderCollapse: 'collapse',
          fontSize: 13,
          textAlign: 'left',
        }}>
          <tbody>
            <StatRow label="Turns" value={info.turnsTaken} />
            <StatRow label="Enemies defeated" value={info.enemiesDefeated} />
            <StatRow label="Damage dealt" value={info.damageDealt} />
            <StatRow label="Damage taken" value={info.damageTaken} />
          </tbody>
        </table>

        <button
          onClick={onPlayAgain}
          style={{
            background: info.won ? '#335' : '#ddd',
            color: info.won ? '#ccc' : '#111',
            border: info.won ? '1px solid #556' : '1px solid #999',
            borderRadius: 4,
            padding: '8px 24px',
            fontFamily: 'monospace',
            fontSize: 14,
            cursor: 'pointer',
          }}
        >
          {info.won ? 'Play Again' : 'New Game'}
        </button>
      </div>
    </div>
  );
}

function StatRow({ label, value }: { label: string; value: number }) {
  return (
    <tr>
      <td style={{ padding: '2px 12px 2px 0', color: '#888' }}>{label}</td>
      <td style={{ padding: '2px 0', fontWeight: 'bold' }}>{value}</td>
    </tr>
  );
}
