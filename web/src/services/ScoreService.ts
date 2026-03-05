import { createClient } from '@supabase/supabase-js';

// ---- Supabase client (lazy, no-op if env vars absent) ----

const supabaseUrl = import.meta.env.VITE_SUPABASE_URL as string | undefined;
const supabaseKey = import.meta.env.VITE_SUPABASE_ANON_KEY as string | undefined;

const supabase = supabaseUrl && supabaseKey
  ? createClient(supabaseUrl, supabaseKey)
  : null;

if (!supabase) {
  console.warn('[ScoreService] Supabase env vars not set — score submission disabled.');
}

// ---- localStorage schema ----

interface DayEntry {
  turnsTaken: number;
  submitted: boolean;
}

interface LocalStore {
  playerId: string;
  scores: Record<string, DayEntry>;
}

const STORAGE_KEY = 'twilight-dungeons';

function loadStore(): LocalStore {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) return JSON.parse(raw) as LocalStore;
  } catch { /* ignore */ }
  return { playerId: crypto.randomUUID(), scores: {} };
}

function saveStore(store: LocalStore): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(store));
}

// ---- Public API ----

export function getPlayerId(): string {
  const store = loadStore();
  return store.playerId;
}

export interface DayScore {
  turnsTaken: number;
  submitted: boolean;
}

export function getLocalScore(day: string): DayScore | null {
  return loadStore().scores[day] ?? null;
}

export function saveLocalScore(day: string, turnsTaken: number): void {
  const store = loadStore();
  const existing = store.scores[day];
  if (!existing || turnsTaken < existing.turnsTaken) {
    // New best score — reset submitted so it gets re-submitted
    store.scores[day] = { turnsTaken, submitted: false };
    saveStore(store);
  }
}

export function markSubmitted(day: string): void {
  const store = loadStore();
  if (store.scores[day]) {
    store.scores[day].submitted = true;
    saveStore(store);
  }
}

export async function submitScore(day: string, turnsTaken: number): Promise<void> {
  if (!supabase) return;
  const playerId = getPlayerId();
  const { error } = await supabase.rpc('submit_score_if_better', {
    p_day: day,
    p_turns_taken: turnsTaken,
    p_player_id: playerId,
  });
  if (error) throw error;
}

export interface HistogramBucket {
  turns_taken: number;
  count: number;
}

export async function fetchHistogram(day: string): Promise<HistogramBucket[]> {
  if (!supabase) return [];
  const { data, error } = await supabase
    .from('daily_scores')
    .select('turns_taken')
    .eq('day', day);
  if (error) throw error;

  // Aggregate client-side (Supabase free tier doesn't expose RPC easily)
  const counts = new Map<number, number>();
  for (const row of data ?? []) {
    const t = row.turns_taken as number;
    counts.set(t, (counts.get(t) ?? 0) + 1);
  }
  return Array.from(counts.entries())
    .map(([turns_taken, count]) => ({ turns_taken, count }))
    .sort((a, b) => a.turns_taken - b.turns_taken);
}
