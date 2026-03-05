import { useState } from 'react';
import { getLocalScore } from '../services/ScoreService';
import { type Difficulty, DIFFICULTY_LABEL } from '../model/GameModel';

const DAY_ONE = '2026-02-04';
const DIFFICULTIES: Difficulty[] = ['basic', 'medium', 'complex'];

function localDateStr(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

interface DayEntry {
  dateStr: string;
  dayNum: number;
}

function getAllDays(): DayEntry[] {
  const days: DayEntry[] = [];
  const start = new Date(DAY_ONE + 'T00:00:00');
  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const cursor = new Date(start);
  let dayNum = 1;
  while (cursor <= today) {
    days.push({ dateStr: localDateStr(cursor), dayNum });
    cursor.setDate(cursor.getDate() + 1);
    dayNum++;
  }
  return days.reverse(); // newest first
}

function diffUrl(dateStr: string, diff: Difficulty): string {
  return `?date=${dateStr}&difficulty=${diff}`;
}

interface Props {
  currentDateSeed: string;
  currentDifficulty: Difficulty;
  currentTurn: number;
}

export function DateSelectorPanel({ currentDateSeed, currentDifficulty, currentTurn }: Props) {
  const [open, setOpen] = useState(false);

  const days = getAllDays();

  function handleChipClick(e: React.MouseEvent<HTMLAnchorElement>, dateStr: string, diff: Difficulty) {
    if (dateStr === currentDateSeed && diff === currentDifficulty) {
      e.preventDefault();
      setOpen(false);
      return;
    }
    if (currentTurn > 0 && !confirm("You'll lose your current progress. Continue?")) {
      e.preventDefault();
    } else {
      setOpen(false);
    }
  }

  return (
    <div style={{ position: 'absolute', bottom: 8, left: 8, zIndex: 20, pointerEvents: 'auto' }}>
      <button
        onClick={() => setOpen(o => !o)}
        style={{
          background: 'rgba(16, 16, 24, 0.88)',
          color: '#aaa',
          border: '1px solid #445',
          borderRadius: 4,
          padding: '3px 10px',
          fontFamily: 'CodersCrux, monospace',
          fontSize: 32,
          cursor: 'pointer',
        }}
      >
        {currentDateSeed || days[0]?.dateStr}
      </button>

      {open && (
        <div style={{
          position: 'absolute',
          bottom: '100%',
          left: 0,
          marginBottom: 4,
          background: 'rgba(12, 12, 20, 0.97)',
          border: '1px solid #445',
          borderRadius: 6,
          padding: '4px 0',
          maxHeight: 360,
          overflowY: 'auto',
          minWidth: 480,
        }}>
          {days.map(({ dateStr }) => {
            const isActiveDate = dateStr === currentDateSeed;

            return (
              <div
                key={dateStr}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 10,
                  padding: '5px 12px',
                  borderLeft: isActiveDate ? '3px solid #88f' : '3px solid transparent',
                  background: isActiveDate ? '#1a1a2c' : 'transparent',
                  fontFamily: 'CodersCrux, monospace',
                }}
              >
                <span style={{ fontSize: 26, color: isActiveDate ? '#ccf' : '#666', minWidth: 140 }}>{dateStr}</span>
                <div style={{ display: 'flex', gap: 6 }}>
                  {DIFFICULTIES.map(diff => {
                    const score = getLocalScore(`${dateStr}-${diff}`);
                    const isActiveChip = isActiveDate && diff === currentDifficulty;
                    return (
                      <a
                        key={diff}
                        href={diffUrl(dateStr, diff)}
                        onClick={e => handleChipClick(e, dateStr, diff)}
                        style={{
                          display: 'inline-block',
                          padding: '2px 8px',
                          textDecoration: 'none',
                          borderRadius: 3,
                          border: isActiveChip ? '1px solid #88f' : '1px solid #334',
                          background: isActiveChip ? '#252540' : 'transparent',
                          fontSize: 20,
                          color: score ? '#4a8' : '#444',
                          whiteSpace: 'nowrap',
                        }}
                      >
                        {DIFFICULTY_LABEL[diff]}{score ? ` ${score.turnsTaken} turns` : ''}
                      </a>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
