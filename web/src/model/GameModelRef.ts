/**
 * Holds a reference to the active GameModel instance.
 * Breaks the circular dependency: Entity → GameModel → Floor → Entity.
 * GameModel sets this on construction. Other modules read from here.
 */

import type { Player } from "./Player";

export interface IGameModelRef {
  time: number;
  currentFloor: any;
  timedEvents: { register(evt: any): void };
  player: Player;
  stats: { damageDealt: number; damageTaken: number; enemiesDefeated: number };
  enqueuEvent(action: () => void): void;
  drainEventQueue(): void;
  emitAnimation(event: object): void;
  gameOver(won: boolean, deathSource?: { displayName: string }): void;
  floorCleared(floor: any): void;
  onFloorCleared: { on(listener: (floor: any) => void): () => void };
}

let _main: IGameModelRef | null = null;

export const GameModelRef = {
  get main(): IGameModelRef {
    return _main!;
  },
  get mainOrNull(): IGameModelRef | null {
    return _main;
  },
  set main(value: IGameModelRef | null) {
    _main = value;
  },
};
