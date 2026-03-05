import posthog from 'posthog-js';
import { getPlayerId } from './ScoreService';
import type { PlayStats, Difficulty } from '../model/GameModel';

const key = import.meta.env.VITE_POSTHOG_KEY as string | undefined;
const deployTarget = (import.meta.env.VITE_DEPLOY_TARGET as string) || 'local';

if (key) {
  posthog.init(key, {
    api_host: 'https://us.i.posthog.com',
    autocapture: false,
    capture_pageview: false,
  });
  posthog.identify(getPlayerId());
  posthog.register({ deploy_target: deployTarget });
} else {
  console.warn('[Analytics] VITE_POSTHOG_KEY not set — analytics disabled.');
}

function capture(event: string, properties: Record<string, unknown>): void {
  if (key) posthog.capture(event, properties);
}

export function trackSessionStart(isReturning: boolean, difficulty: Difficulty): void {
  capture('session_start', { is_returning: isReturning, difficulty });
}

export function trackGameOver(
  stats: PlayStats,
  difficulty: Difficulty,
  dateSeed: string,
  playDurationMs: number,
  retryNumber: number,
): void {
  capture('game_over', {
    won: stats.won,
    turns_taken: stats.turnsTaken,
    killed_by: stats.killedBy,
    enemies_defeated: stats.enemiesDefeated,
    damage_dealt: stats.damageDealt,
    damage_taken: stats.damageTaken,
    difficulty,
    date_seed: dateSeed,
    play_duration_ms: playDurationMs,
    retry_number: retryNumber,
  });
}

export function trackRetry(retryNumber: number, prevStats: PlayStats): void {
  capture('retry', {
    retry_number: retryNumber,
    prev_won: prevStats.won,
    prev_turns: prevStats.turnsTaken,
  });
}
