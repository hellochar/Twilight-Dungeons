import { useState, useEffect } from 'react';
import type { GameOverInfo } from '../hooks/useGameLoop';
import { NEXT_DIFFICULTY, DIFFICULTY_LABEL, type Difficulty } from '../model/GameModel';
import {
  getLocalScore, saveLocalScore, markSubmitted,
  submitScore, fetchHistogram, type HistogramBucket,
} from '../services/ScoreService';
import { FONT_FAMILY, FONT_FAMILY_SERIF, FontSize } from './fonts';

const AUTO_SUBMIT = true;

interface GameOverOverlayProps {
  info: GameOverInfo;
  dateSeed: string;
  difficulty: Difficulty;
  onPlayAgain: () => void;
}

export function GameOverOverlay({ info, dateSeed, difficulty, onPlayAgain }: GameOverOverlayProps) {
  if (!info.won) {
    return (
      <div style={{
        position: 'absolute',
        bottom: '16%',
        left: '50%',
        transform: 'translateX(-50%)',
        zIndex: 100,
        pointerEvents: 'auto',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
      }}>
        {info.killedBy && (
          <div style={{
            fontFamily: FONT_FAMILY_SERIF,
            fontSize: FontSize.xl,
            color: 'white',
            textAlign: 'center',
            marginBottom: 8,
            whiteSpace: 'nowrap',
          }}>
            You perished to a {info.killedBy}...
          </div>
        )}
        <button
          onClick={onPlayAgain}
          style={{
            background: '#bbbbbb',
            color: '#000',
            border: '2px solid #888',
            borderRadius: 8,
            padding: '16px 64px',
            fontFamily: FONT_FAMILY,
            fontSize: FontSize.xl,
            cursor: 'pointer',
          }}
        >
          Retry
        </button>
      </div>
    );
  }

  const title = `Cleared ${dateSeed} ${DIFFICULTY_LABEL[difficulty]} on turn ${info.turnsTaken}!`;
  const next = NEXT_DIFFICULTY[difficulty];
  const nextUrl = next ? (() => { const p = new URLSearchParams(window.location.search); p.set('difficulty', next); return `?${p.toString()}`; })() : null;
  const scoreKey = dateSeed ? `${dateSeed}-${difficulty}` : '';
  const borderColor = '#4f4';

  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [histogram, setHistogram] = useState<HistogramBucket[] | null>(null);
  const [loadingHistogram, setLoadingHistogram] = useState(false);

  // On mount: save local score, auto-submit if enabled, or load histogram if already submitted
  useEffect(() => {
    if (!scoreKey) return;
    saveLocalScore(scoreKey, info.turnsTaken);
    const local = getLocalScore(scoreKey);
    if (local?.submitted) {
      loadHistogram();
    } else if (AUTO_SUBMIT) {
      handleSubmit();
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function loadHistogram() {
    setLoadingHistogram(true);
    try {
      const data = await fetchHistogram(scoreKey);
      setHistogram(data);
    } catch { /* silent */ }
    setLoadingHistogram(false);
  }

  async function handleSubmit() {
    setSubmitting(true);
    setSubmitError(null);
    try {
      await submitScore(scoreKey, info.turnsTaken);
      markSubmitted(scoreKey);
      await loadHistogram();
    } catch (e) {
      setSubmitError(e instanceof Error ? e.message : 'Submission failed');
    }
    setSubmitting(false);
  }

  const alreadySubmitted = scoreKey ? (getLocalScore(scoreKey)?.submitted ?? false) : false;
  const showNextDifficulty = histogram !== null && next;

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
        flexDirection: 'column',
        gap: 10,
        fontFamily: FONT_FAMILY,
        color: '#ccc',
        minWidth: 360,
      }}>
        {/* Title row */}
        <span style={{ color: borderColor, fontSize: FontSize.lg, whiteSpace: 'nowrap' }}>
          {title}
        </span>

        {/* Score section */}
        {dateSeed && (
          <div style={{ borderTop: '1px solid #334', paddingTop: 8 }}>
            {histogram ? (
              <>
                <Histogram buckets={histogram} playerTurns={info.turnsTaken} />
                <div style={{ marginTop: 8, display: 'flex', justifyContent: 'center', gap: 10 }}>
                  <button onClick={onPlayAgain} style={secondaryButtonStyle}>Retry</button>
                  {showNextDifficulty && (
                    <a href={nextUrl!} style={{ ...secondaryButtonStyle, textDecoration: 'none', display: 'inline-block' }}>
                      Play {DIFFICULTY_LABEL[next!]}
                    </a>
                  )}
                </div>
              </>
            ) : alreadySubmitted ? (
              loadingHistogram
                ? <span style={{ fontSize: FontSize.lg, color: '#666' }}>Loading scores…</span>
                : (
                  <button onClick={onPlayAgain} style={secondaryButtonStyle}>Retry</button>
                )
            ) : (
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <button
                  onClick={handleSubmit}
                  disabled={submitting}
                  style={{
                    background: '#253',
                    color: '#ccc',
                    border: '1px solid #4f4',
                    borderRadius: 4,
                    padding: '3px 12px',
                    fontFamily: FONT_FAMILY,
                    fontSize: FontSize.md,
                    cursor: submitting ? 'default' : 'pointer',
                    opacity: submitting ? 0.6 : 1,
                  }}
                >
                  {submitting ? 'Submitting…' : 'Submit Score'}
                </button>
                <button onClick={onPlayAgain} style={secondaryButtonStyle}>Retry</button>
                {submitError && (
                  <span style={{ fontSize: FontSize.sm, color: '#f88' }}>{submitError}</span>
                )}
              </div>
            )}
          </div>
        )}

      </div>
    </div>
  );
}

const secondaryButtonStyle: React.CSSProperties = {
  background: '#335',
  color: '#ccc',
  border: '1px solid #556',
  borderRadius: 4,
  padding: '4px 14px',
  fontFamily: FONT_FAMILY,
  fontSize: FontSize.md,
  cursor: 'pointer',
};

// ---- Histogram component ----

const BAR_WIDTH = 18;
const BAR_GAP = 3;

function Histogram({ buckets, playerTurns }: { buckets: HistogramBucket[]; playerTurns: number }) {
  if (buckets.length === 0) return null;

  const maxCount = Math.max(...buckets.map(b => b.count));
  const totalPlayers = buckets.reduce((s, b) => s + b.count, 0);
  const betterCount = buckets
    .filter(b => b.turns_taken < playerTurns)
    .reduce((s, b) => s + b.count, 0);
  const percentile = totalPlayers > 1
    ? Math.round((1 - betterCount / totalPlayers) * 100)
    : 100;

  return (
    <div>
      <div style={{ fontSize: FontSize.lg, color: '#888', marginBottom: 8 }}>
        {totalPlayers} {totalPlayers === 1 ? 'score' : 'scores'} today
        {' \u00b7 '}
        top {percentile}% with {playerTurns} turns
      </div>
      <div style={{ display: 'flex', alignItems: 'flex-end', gap: BAR_GAP, height: 80 }}>
        {buckets.map(b => {
          const isPlayer = b.turns_taken === playerTurns;
          const heightPct = (b.count / maxCount) * 100;
          return (
            <div key={b.turns_taken} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2 }}>
              <span style={{
                fontSize: FontSize.sm,
                color: isPlayer ? '#4f4' : '#666',
                lineHeight: 1,
              }}>
                {b.count}
              </span>
              <div
                style={{
                  width: BAR_WIDTH,
                  height: `${heightPct}%`,
                  minHeight: 3,
                  background: isPlayer ? '#4f4' : '#446',
                  borderRadius: 2,
                }}
              />
            </div>
          );
        })}
      </div>
      <div style={{ display: 'flex', gap: BAR_GAP, marginTop: 3 }}>
        {buckets.map(b => (
          <div key={b.turns_taken} style={{
            width: BAR_WIDTH,
            fontSize: 12,
            color: b.turns_taken === playerTurns ? '#4f4' : '#555',
            textAlign: 'center',
            overflow: 'hidden',
          }}>
            {b.turns_taken}
          </div>
        ))}
      </div>
    </div>
  );
}
