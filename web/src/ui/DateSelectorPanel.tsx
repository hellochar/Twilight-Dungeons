import { useState } from 'react';
import { getLocalScore } from '../services/ScoreService';
import { type Difficulty, DIFFICULTY_LABEL } from '../model/GameModel';
import { FONT_FAMILY, FontSize } from './fonts';
import { DAY_ONE } from '../constants';

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

interface Props {
  currentDateSeed: string;
  currentDifficulty: Difficulty;
  currentTurn: number;
  gameOver: boolean;
  onNavigate: (dateSeed: string, difficulty: Difficulty) => void;
}

export function DateSelectorPanel({ currentDateSeed, currentDifficulty, currentTurn, gameOver, onNavigate }: Props) {
  const [open, setOpen] = useState(false);

  const days = getAllDays();

  function handleChipClick(dateStr: string, diff: Difficulty) {
    if (dateStr === currentDateSeed && diff === currentDifficulty) {
      setOpen(false);
      return;
    }
    if (currentTurn > 0 && !gameOver && !confirm("You'll lose your current progress. Continue?")) {
      return;
    }
    setOpen(false);
    onNavigate(dateStr, diff);
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
          fontFamily: FONT_FAMILY,
          fontSize: FontSize.xl,
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
                  fontFamily: FONT_FAMILY,
                }}
              >
                <span style={{ fontSize: FontSize.lg, color: isActiveDate ? '#ccf' : '#666', minWidth: 140 }}>{dateStr}</span>
                <div style={{ display: 'flex', gap: 6 }}>
                  {DIFFICULTIES.map(diff => {
                    const score = getLocalScore(`${dateStr}-${diff}`);
                    const isActiveChip = isActiveDate && diff === currentDifficulty;
                    return (
                      <button
                        key={diff}
                        onClick={() => handleChipClick(dateStr, diff)}
                        style={{
                          display: 'inline-block',
                          width: 72,
                          padding: '2px 0',
                          textAlign: 'center',
                          borderRadius: 3,
                          border: isActiveChip ? '1px solid #88f' : '1px solid #334',
                          background: isActiveChip ? '#252540' : 'transparent',
                          fontSize: FontSize.lg,
                          fontFamily: FONT_FAMILY,
                          color: score ? '#4a8' : '#444',
                          whiteSpace: 'nowrap',
                          cursor: 'pointer',
                        }}
                      >
                        {score ? `turn ${score.turnsTaken}` : DIFFICULTY_LABEL[diff]}
                      </button>
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
