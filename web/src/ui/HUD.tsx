import { useState } from 'react';
import type { GameState, OnTopActionSnapshot } from '../hooks/useGameLoop';
import { DIFFICULTY_LABEL } from '../model/GameModel';
import { soundManager } from '../audio/SoundManager';
import { StatusBar } from './StatusBar';
import { FONT_FAMILY, FONT_FAMILY_SERIF, FontSize } from './fonts';
import { buttonBase, buttonPadding } from './theme';
import { HEART_SIZE, MUTE_ICON_SIZE } from '../constants';

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
      {/* Top-center: date · difficulty · turn */}
      <div style={{ display: 'flex', justifyContent: 'center', padding: '6px 10px' }}>
        <Banner dateSeed={state.dateSeed} difficulty={state.difficulty} turn={state.turn} isCleared={state.isCleared} clearedOnTurn={state.clearedOnTurn} />
      </div>

      {/* Top-right: mute button */}
      <div style={{ position: 'absolute', top: 6, right: 10, pointerEvents: 'auto' }}>
        <MuteButton />
      </div>

      {/* Bottom-center: status icons above hearts, with action buttons inline */}
      <div style={{
        position: 'absolute',
        bottom: '5%',
        left: 0,
        right: 0,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: 16,
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <StatusBar statuses={state.statuses} />
          {showButtons && onTopAction && (
            <OnTopActionButton action={onTopAction} onClick={onExecuteOnTopAction} />
          )}
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 32 }}>
          <Hearts hp={state.hp} maxHp={state.maxHp} />
          {showButtons && (
            <WaitButton onClick={onWait} />
          )}
        </div>
      </div>
    </div>
  );
}

// ─── Hearts ───
// Unity: 4 HP per heart, 5 pre-baked sprite states from heart_animated_2.png
// Sheet order: heart-0.png = empty ... heart-4.png = full
// Unity scene reverses: sprites[0]=full, sprites[4]=empty → index as (4 - hpForThis)

const HP_PER_HEART = 1;

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
      fontFamily: FONT_FAMILY,
      fontSize: FontSize.lg,
      color: '#ccc',
      textShadow: '1px 1px 2px #000',
    }}>
      {parts.join(' ')}
    </div>
  );
}

// ─── On-Top Action Button ───

const BUTTON_STYLE = {
  ...buttonBase,
  ...buttonPadding,
  display: 'flex',
  alignItems: 'center',
  gap: 6,
  background: 'rgba(20, 20, 30, 0.85)',
  border: '1px solid rgba(255, 255, 255, 0.25)',
  color: '#eee',
} as const;

function WaitButton({ onClick }: { onClick: () => void }) {
  return (
    <div style={{ pointerEvents: 'auto' }}>
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

const ACTION_BUTTON_STYLE = {
  ...BUTTON_STYLE,
  background: '#e8c820',
  color: '#1a1a00',
  border: '2px solid #ffd700',
  // fontWeight: 'bold',
} as const;

function OnTopActionButton({ action, onClick }: { action: OnTopActionSnapshot; onClick: () => void }) {
  return (
    <div style={{ pointerEvents: 'auto' }}>
      <button onClick={onClick} style={ACTION_BUTTON_STYLE}>
        <img
          src={`${import.meta.env.BASE_URL}sprites/${action.spriteName}.png`}
          alt=""
          style={{ width: 28, height: 28, imageRendering: 'pixelated' }}
          onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
        />
        {action.name}
      </button>
    </div>
  );
}

// ─── Mute Button ───

const MUTE_BUTTON_STYLE: React.CSSProperties = {
  ...buttonBase,
  padding: '6px 6px 0 6px',
  background: 'rgba(20, 20, 30, 0.85)',
  border: 'none',
  borderRadius: 0,
  // border: '1px solid rgba(255, 255, 255, 0.25)',
  color: '#ccc',
  fontSize: FontSize.xl,
  lineHeight: 1,
};

const ICON_STYLE: React.CSSProperties = { width: MUTE_ICON_SIZE, height: MUTE_ICON_SIZE, fill: 'currentColor' };

function MuteButton() {
  const [muted, setMuted] = useState(() => soundManager.muted);

  const handleClick = () => {
    soundManager.toggleMute();
    setMuted(soundManager.muted);
  };

  return (
    <button
      onClick={handleClick}
      style={MUTE_BUTTON_STYLE}
      title={muted ? 'Unmute' : 'Mute'}
    >
      {muted ? <VolumeXMarkIcon /> : <VolumeHighIcon />}
    </button>
  );
}

/** FA solid volume-high (Font Awesome Free 6.x, CC BY 4.0) */
function VolumeHighIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 640 512" style={ICON_STYLE}>
      <path d="M412.6 182c-10.28-8.334-25.41-6.867-33.75 3.402c-8.406 10.24-6.906 25.35 3.375 33.74C393.5 228.4 400 241.8 400 255.1c0 14.17-6.5 27.59-17.81 36.83c-10.28 8.396-11.78 23.5-3.375 33.74c4.719 5.806 11.62 8.802 18.56 8.802c5.344 0 10.75-1.779 15.19-5.399C435.1 311.5 448 284.6 448 255.1S435.1 200.4 412.6 182zM473.1 108.2c-10.22-8.334-25.34-6.898-33.78 3.34c-8.406 10.24-6.906 25.35 3.344 33.74C476.6 172.1 496 213.3 496 255.1s-19.44 82.1-53.31 110.7c-10.25 8.396-11.75 23.5-3.344 33.74c4.75 5.775 11.62 8.771 18.56 8.771c5.375 0 10.75-1.779 15.22-5.431C518.2 366.9 544 313 544 255.1S518.2 145 473.1 108.2zM534.4 33.4c-10.22-8.334-25.34-6.867-33.78 3.34c-8.406 10.24-6.906 25.35 3.344 33.74C559.9 116.3 592 183.9 592 255.1s-32.09 139.7-88.06 185.5c-10.25 8.396-11.75 23.5-3.344 33.74C505.3 481 512.2 484 519.2 484c5.375 0 10.75-1.779 15.22-5.431C601.5 423.6 640 342.5 640 255.1S601.5 88.34 534.4 33.4zM301.2 34.98c-11.5-5.181-25.01-3.076-34.43 5.29L131.8 160.1H48c-26.51 0-48 21.48-48 47.96v95.92c0 26.48 21.49 47.96 48 47.96h83.84l134.9 119.8C272.7 477 280.3 479.8 288 479.8c4.438 0 8.959-.9314 13.16-2.835C312.7 471.8 320 460.4 320 447.9V64.12C320 51.55 312.7 40.13 301.2 34.98z"/>
    </svg>
  );
}

/** FA solid volume-xmark (Font Awesome Free 6.x, CC BY 4.0) */
function VolumeXMarkIcon() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 576 512" style={ICON_STYLE}>
      <path d="M301.2 34.85c-11.5-5.188-25.02-3.122-34.44 5.253L131.8 160H48c-26.51 0-48 21.49-48 47.1v95.1c0 26.51 21.49 47.1 48 47.1h83.84l134.9 119.9c5.984 5.312 13.58 8.094 21.26 8.094c4.438 0 8.972-.9375 13.17-2.844c11.5-5.156 18.82-16.56 18.82-29.16V64C319.1 51.41 312.7 40 301.2 34.85zM513.9 255.1l47.03-47.03c9.375-9.375 9.375-24.56 0-33.94s-24.56-9.375-33.94 0L480 222.1L432.1 175c-9.375-9.375-24.56-9.375-33.94 0s-9.375 24.56 0 33.94l47.03 47.03l-47.03 47.03c-9.375 9.375-9.375 24.56 0 33.94c9.373 9.373 24.56 9.381 33.94 0L480 289.9l47.03 47.03c9.373 9.373 24.56 9.381 33.94 0c9.375-9.375 9.375-24.56 0-33.94L513.9 255.1z"/>
    </svg>
  );
}
