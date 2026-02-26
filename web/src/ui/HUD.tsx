import type { GameState } from '../hooks/useGameLoop';

interface HUDProps {
  state: GameState;
}

export function HUD({ state }: HUDProps) {
  const hpPct = state.maxHp > 0 ? (state.hp / state.maxHp) * 100 : 0;
  const hpColor = hpPct > 50 ? '#4a4' : hpPct > 25 ? '#aa4' : '#a44';

  return (
    <div style={{
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      padding: '8px 12px',
      display: 'flex',
      alignItems: 'center',
      gap: 16,
      fontFamily: 'monospace',
      fontSize: 13,
      color: '#ccc',
      pointerEvents: 'none',
      background: 'linear-gradient(to bottom, rgba(0,0,0,0.6) 0%, transparent 100%)',
    }}>
      {/* HP bar */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
        <span>HP</span>
        <div style={{
          width: 100,
          height: 10,
          background: '#333',
          borderRadius: 3,
          overflow: 'hidden',
        }}>
          <div style={{
            width: `${hpPct}%`,
            height: '100%',
            background: hpColor,
            transition: 'width 0.2s, background 0.2s',
          }} />
        </div>
        <span>{state.hp}/{state.maxHp}</span>
      </div>

      {/* Turn counter */}
      <span>Turn {state.turn}</span>

      {/* Enemy count */}
      <span>Enemies: {state.enemyCount}</span>

      {/* Status messages */}
      {state.isPlayerDead && (
        <span style={{ color: '#f44', fontWeight: 'bold' }}>DEAD</span>
      )}
      {state.isCleared && (
        <span style={{ color: '#4f4', fontWeight: 'bold' }}>CLEARED!</span>
      )}
    </div>
  );
}
