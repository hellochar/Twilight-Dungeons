/**
 * Holds a reference to the active GameModel instance.
 * Breaks the circular dependency: Entity → GameModel → Floor → Entity.
 * GameModel sets this on construction. Other modules read from here.
 */

export interface IGameModelRef {
  time: number;
  currentFloor: any;
  timedEvents: { register(evt: any): void };
  player: any;
  enqueuEvent(action: () => void): void;
  emitAnimation(event: object): void;
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
