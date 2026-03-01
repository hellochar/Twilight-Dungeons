import type { GameState, OnTopActionSnapshot } from '../hooks/useGameLoop';
import { StatusBar } from './StatusBar';

interface HUDProps {
  state: GameState;
  onTopAction: OnTopActionSnapshot | null;
  onExecuteOnTopAction: () => void;
}

/**
 * Top HUD matching Unity layout:
 * - Top-left: Hearts (4 HP per heart, 5 fill states)
 * - Top-left below hearts: Status icons
 * - Top-right: Depth + Turn banner
 * - Below banner: Enemy counter text
 */
export function HUD({ state, onTopAction, onExecuteOnTopAction }: HUDProps) {
  return (
    <div style={{ position: 'absolute', top: 0, left: 0, right: 0, bottom: 0, pointerEvents: 'none' }}>
      {/* Top row: hearts left, banner right */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'flex-start',
        padding: '6px 10px',
      }}>
        {/* Hearts */}
        <Hearts hp={state.hp} maxHp={state.maxHp} />

        {/* Right side: banner + enemies */}
        <div style={{ textAlign: 'right' }}>
          <Banner depth={state.depth} turn={state.turn} isCleared={state.isCleared} />
          <EnemiesLeft count={state.enemyCount} isCleared={state.isCleared} />
        </div>
      </div>

      {/* Status icons below hearts */}
      <StatusBar statuses={state.statuses} />

      {/* On-top action button */}
      {onTopAction && (
        <OnTopActionButton action={onTopAction} onClick={onExecuteOnTopAction} />
      )}
    </div>
  );
}

// ─── Hearts ───
// Unity: 4 HP per heart, 5 pre-baked sprite states from heart_animated_2.png
// Sheet order: heart-0.png = empty ... heart-4.png = full
// Unity scene reverses: sprites[0]=full, sprites[4]=empty → index as (4 - hpForThis)

const HP_PER_HEART = 4;
const HEART_SIZE = 20;

function Hearts({ hp, maxHp }: { hp: number; maxHp: number }) {
  const numHearts = Math.ceil(maxHp / HP_PER_HEART);
  const hearts = [];
  for (let i = 0; i < numHearts; i++) {
    const hpForThis = Math.max(0, Math.min(HP_PER_HEART, hp - i * HP_PER_HEART));
    hearts.push(<Heart key={i} state={hpForThis} />);
  }
  return <div style={{ display: 'flex', gap: 2 }}>{hearts}</div>;
}

function Heart({ state }: { state: number }) {
  return (
    <img
      src={`/sprites/hearts/heart-${4 - state}.png`}
      alt=""
      style={{
        width: HEART_SIZE,
        height: HEART_SIZE,
        imageRendering: 'pixelated',
      }}
    />
  );
}

// ─── Banner ───
// Unity: "Depth X   Turn Y" or "Depth X   Cleared!"

function Banner({ depth, turn, isCleared }: { depth: number; turn: number; isCleared: boolean }) {
  const parts: string[] = [`Depth ${depth}`];
  if (isCleared) {
    parts.push('Cleared!');
  } else if (turn > 0) {
    parts.push(`Turn ${turn}`);
  }

  return (
    <div style={{
      fontFamily: 'monospace',
      fontSize: 13,
      color: '#ccc',
      textShadow: '1px 1px 2px #000',
    }}>
      {parts.join('   ')}
    </div>
  );
}

// ─── Enemies Left ───
// Unity: "Defeat all enemies." (>3), "X enemies left." (1-3), "Cleared!" (0)

function EnemiesLeft({ count, isCleared }: { count: number; isCleared: boolean }) {
  let text = '';
  if (isCleared) {
    text = 'Cleared!';
  } else if (count > 3) {
    text = 'Defeat all enemies.';
  } else if (count === 1) {
    text = '1 enemy left.';
  } else if (count > 0) {
    text = `${count} enemies left.`;
  }

  if (!text) return null;

  return (
    <div style={{
      fontFamily: 'monospace',
      fontSize: 11,
      color: isCleared ? '#4f4' : '#aaa',
      textShadow: '1px 1px 2px #000',
      marginTop: 2,
    }}>
      {text}
    </div>
  );
}

// ─── On-Top Action Button ───

function OnTopActionButton({ action, onClick }: { action: OnTopActionSnapshot; onClick: () => void }) {
  return (
    <div style={{
      position: 'absolute',
      bottom: 16,
      right: 16,
      pointerEvents: 'auto',
    }}>
      <button
        onClick={onClick}
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 6,
          padding: '6px 14px',
          background: 'rgba(20, 20, 30, 0.85)',
          border: '1px solid rgba(255, 255, 255, 0.25)',
          borderRadius: 6,
          color: '#eee',
          fontFamily: 'monospace',
          fontSize: 13,
          cursor: 'pointer',
        }}
      >
        <img
          src={`/sprites/${action.spriteName}.png`}
          alt=""
          style={{ width: 20, height: 20, imageRendering: 'pixelated' }}
          onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
        />
        {action.name}
      </button>
    </div>
  );
}
