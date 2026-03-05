import { useState, useEffect } from 'react';
import type { GameOverInfo } from '../hooks/useGameLoop';
import { NEXT_DIFFICULTY, DIFFICULTY_LABEL, type Difficulty } from '../model/GameModel';
import {
  getLocalScore, saveLocalScore, markSubmitted,
  submitScore, fetchHistogram, type HistogramBucket,
} from '../services/ScoreService';

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
      }}>
        <button
          onClick={onPlayAgain}
          style={{
            background: '#bbbbbb',
            color: '#000',
            border: '2px solid #888',
            borderRadius: 8,
            padding: '16px 64px',
            fontFamily: 'CodersCrux, monospace',
            fontSize: 48,
            cursor: 'pointer',
          }}
        >
          Retry
        </button>
      </div>
    );
  }

  const title = `${DIFFICULTY_LABEL[difficulty]} Cleared!`;
  const next = NEXT_DIFFICULTY[difficulty];
  const nextUrl = next ? (() => { const p = new URLSearchParams(window.location.search); p.set('difficulty', next); return `?${p.toString()}`; })() : null;
  const scoreKey = dateSeed ? `${dateSeed}-${difficulty}` : '';
  const borderColor = '#4f4';

  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [histogram, setHistogram] = useState<HistogramBucket[] | null>(null);
  const [loadingHistogram, setLoadingHistogram] = useState(false);

  // On mount: save local score and check if already submitted
  useEffect(() => {
    if (scoreKey) {
      saveLocalScore(scoreKey, info.turnsTaken);
      const local = getLocalScore(scoreKey);
      if (local?.submitted) {
        loadHistogram();
      }
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
        fontFamily: 'CodersCrux, monospace',
        color: '#ccc',
        minWidth: 360,
      }}>
        {/* Top row: title + stats + new game */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 20, whiteSpace: 'nowrap' }}>
          <span style={{ color: borderColor, fontWeight: 'bold', fontSize: 23 }}>
            {title}
          </span>

          <span style={{ fontSize: 18, color: '#888' }}>
            {info.turnsTaken} turns
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
              fontSize: 18,
              cursor: 'pointer',
            }}
          >
            New Game
          </button>
        </div>

        {/* Score section — wins only */}
        {info.won && dateSeed && (
          <div style={{ borderTop: '1px solid #334', paddingTop: 8 }}>
            {histogram ? (
              <Histogram buckets={histogram} playerTurns={info.turnsTaken} />
            ) : alreadySubmitted ? (
              loadingHistogram
                ? <span style={{ fontSize: 14, color: '#666' }}>Loading scores…</span>
                : null
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
                    fontFamily: 'CodersCrux, monospace',
                    fontSize: 16,
                    cursor: submitting ? 'default' : 'pointer',
                    opacity: submitting ? 0.6 : 1,
                  }}
                >
                  {submitting ? 'Submitting…' : 'Submit Score'}
                </button>
                {submitError && (
                  <span style={{ fontSize: 13, color: '#f88' }}>{submitError}</span>
                )}
              </div>
            )}
          </div>
        )}
        {/* Next difficulty link — shown after histogram section */}
        {info.won && next && (
          <div style={{ borderTop: '1px solid #334', paddingTop: 8 }}>
            <a
              href={nextUrl!}
              style={{
                display: 'inline-block',
                background: '#335',
                color: '#ccc',
                border: '1px solid #556',
                borderRadius: 4,
                padding: '4px 14px',
                fontFamily: 'CodersCrux, monospace',
                fontSize: 18,
                textDecoration: 'none',
                cursor: 'pointer',
              }}
            >
              Play {DIFFICULTY_LABEL[next]}
            </a>
          </div>
        )}
      </div>
    </div>
  );
}

// ---- Histogram component ----

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
      <div style={{ fontSize: 13, color: '#888', marginBottom: 6 }}>
        {totalPlayers} {totalPlayers === 1 ? 'score' : 'scores'} today
        {' \u00b7 '}
        top {percentile}% with {playerTurns} turns
      </div>
      <div style={{ display: 'flex', alignItems: 'flex-end', gap: 2, height: 40 }}>
        {buckets.map(b => {
          const isPlayer = b.turns_taken === playerTurns;
          const heightPct = (b.count / maxCount) * 100;
          return (
            <div key={b.turns_taken} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 1 }}>
              <div
                title={`${b.turns_taken} turns: ${b.count} player${b.count !== 1 ? 's' : ''}`}
                style={{
                  width: 10,
                  height: `${heightPct}%`,
                  minHeight: 2,
                  background: isPlayer ? '#4f4' : '#446',
                  borderRadius: 1,
                }}
              />
            </div>
          );
        })}
      </div>
      <div style={{ display: 'flex', gap: 2, marginTop: 2 }}>
        {buckets.map(b => (
          <div key={b.turns_taken} style={{
            width: 10,
            fontSize: 8,
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
