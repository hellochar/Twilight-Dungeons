import { useState } from 'react';
import { getLocalScore } from '../services/ScoreService';

const DAY_ONE = '2026-02-04';

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
  onSelectDate: (dateSeed: string) => void;
}

export function DateSelectorPanel({ currentDateSeed, onSelectDate }: Props) {
  const [open, setOpen] = useState(false);

  const days = getAllDays();
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
          minWidth: 360,
        }}>
          {days.map(({ dateStr }) => {
            const score = getLocalScore(dateStr);
            const isActive = dateStr === currentDateSeed;

            return (
              <div
                key={dateStr}
                onClick={() => { onSelectDate(dateStr); setOpen(false); }}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 12,
                  padding: '6px 12px',
                  cursor: 'pointer',
                  background: isActive ? '#1e1e30' : 'transparent',
                  borderLeft: isActive ? '3px solid #88f' : '3px solid transparent',
                  fontFamily: 'CodersCrux, monospace',
                }}
              >
                <span style={{ flex: 1, fontSize: 26, color: isActive ? '#ccf' : '#666' }}>{dateStr}</span>
                {score
                  ? <span style={{ fontSize: 26, color: '#4a8', whiteSpace: 'nowrap' }}>won in {score.turnsTaken} turns</span>
                  : <span style={{ fontSize: 26, color: '#333' }}>—</span>
                }
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
