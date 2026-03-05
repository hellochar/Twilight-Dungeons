import type { GameState, OnTopActionSnapshot } from '../hooks/useGameLoop';
import { DIFFICULTY_LABEL } from '../model/GameModel';
import { StatusBar } from './StatusBar';

interface HUDProps {
  state: GameState;
  onTopAction: OnTopActionSnapshot | null;
  onExecuteOnTopAction: () => void;
  onWait: () => void;
}

/**
 * Top HUD matching Unity layout:
 * - Top-left: Hearts (4 HP per heart, 5 fill states)
 * - Top-left below hearts: Status icons
 * - Top-center: Depth + Turn banner
 * - Below banner: Enemy counter text
 * - Bottom-right: Wait button; OnTopAction button above it when present
 */
export function HUD({ state, onTopAction, onExecuteOnTopAction, onWait }: HUDProps) {
  const showButtons = !state.isPlayerDead && !state.isCleared;
  return (
    <div style={{ position: 'absolute', top: 0, left: 0, right: 0, bottom: 0, pointerEvents: 'none' }}>
      {/* Top row: hearts left, info center */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'flex-start',
        padding: '6px 10px',
      }}>
        {/* Hearts */}
        <Hearts hp={state.hp} maxHp={state.maxHp} />

        {/* Center: date · difficulty · turn */}
        <div style={{ position: 'absolute', left: 0, right: 0, top: 6, display: 'flex', justifyContent: 'center', pointerEvents: 'none' }}>
          <Banner dateSeed={state.dateSeed} difficulty={state.difficulty} turn={state.turn} isCleared={state.isCleared} clearedOnTurn={state.clearedOnTurn} />
        </div>
      </div>

      {/* Status icons below hearts */}
      <StatusBar statuses={state.statuses} />

      {/* Bottom-right: OnTopAction above Wait button */}
      {showButtons && onTopAction && (
        <OnTopActionButton action={onTopAction} onClick={onExecuteOnTopAction} bottom={64} />
      )}
      {showButtons && (
        <WaitButton onClick={onWait} />
      )}
    </div>
  );
}

// ─── Hearts ───
// Unity: 4 HP per heart, 5 pre-baked sprite states from heart_animated_2.png
// Sheet order: heart-0.png = empty ... heart-4.png = full
// Unity scene reverses: sprites[0]=full, sprites[4]=empty → index as (4 - hpForThis)

const HP_PER_HEART = 1;
const HEART_SIZE = 48;

function Hearts({ hp, maxHp }: { hp: number; maxHp: number }) {
  const numHearts = Math.ceil(maxHp / HP_PER_HEART);
  const hearts = [];
  for (let i = 0; i < numHearts; i++) {
    // const hpForThis = Math.max(0, Math.min(HP_PER_HEART, hp - i * HP_PER_HEART));
    const hpForThis = i < hp ? 4 : 0;
    hearts.push(<Heart key={i} state={hpForThis} />);
  }
  return <div style={{ display: 'flex', gap: 2 }}>{hearts}</div>;
}

function Heart({ state }: { state: number }) {
  return (
    <img
      src={`${import.meta.env.BASE_URL}sprites/hearts/heart-${4 - state}.png`}
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

type BannerProps = { dateSeed: string; difficulty: import('../model/GameModel').Difficulty; turn: number; isCleared: boolean; clearedOnTurn: number | null };

function Banner({ dateSeed, difficulty, turn, isCleared, clearedOnTurn }: BannerProps) {
  const parts: string[] = [dateSeed, DIFFICULTY_LABEL[difficulty]];
  if (isCleared) {
    parts.push(`Cleared on turn ${clearedOnTurn ?? turn}`);
  } else {
    parts.push(`Turn ${turn}`);
  }

  return (
    <div style={{
      fontFamily: 'CodersCrux, monospace',
      fontSize: 20,
      color: '#ccc',
      textShadow: '1px 1px 2px #000',
    }}>
      {parts.join(' ')}
    </div>
  );
}

// ─── On-Top Action Button ───

const BUTTON_STYLE = {
  display: 'flex',
  alignItems: 'center',
  gap: 6,
  padding: '6px 14px',
  background: 'rgba(20, 20, 30, 0.85)',
  border: '1px solid rgba(255, 255, 255, 0.25)',
  borderRadius: 6,
  color: '#eee',
  fontFamily: 'CodersCrux, monospace',
  fontSize: 20,
  cursor: 'pointer',
} as const;

function WaitButton({ onClick }: { onClick: () => void }) {
  return (
    <div style={{ position: 'absolute', bottom: 16, right: 16, pointerEvents: 'auto' }}>
      <button onClick={onClick} style={BUTTON_STYLE}>
        <img
          src={`${import.meta.env.BASE_URL}sprites/clock.png`}
          alt=""
          style={{ width: 20, height: 20, imageRendering: 'pixelated' }}
          onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
        />
        Wait
      </button>
    </div>
  );
}

function OnTopActionButton({ action, onClick, bottom }: { action: OnTopActionSnapshot; onClick: () => void; bottom: number }) {
  return (
    <div style={{
      position: 'absolute',
      bottom,
      right: 16,
      pointerEvents: 'auto',
    }}>
      <button onClick={onClick} style={BUTTON_STYLE}>
        <img
          src={`${import.meta.env.BASE_URL}sprites/${action.spriteName}.png`}
          alt=""
          style={{ width: 20, height: 20, imageRendering: 'pixelated' }}
          onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
        />
        {action.name}
      </button>
    </div>
  );
}
