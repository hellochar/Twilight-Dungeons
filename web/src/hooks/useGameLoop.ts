import { useCallback, useEffect, useRef, useState } from 'react';
import { Application, TextureSource } from 'pixi.js';
import { GameModel } from '../model/GameModel';
import { Player } from '../model/Player';
import { Floor } from '../model/Floor';
import { Vector2Int } from '../core/Vector2Int';
import { Faction } from '../core/types';
import { Camera, SpriteManager, GameRenderer, AnimationPlayer } from '../renderer';
import { InputHandler, type PlayerIntent } from '../input/InputHandler';
import { FollowPathTask } from '../model/tasks/FollowPathTask';
import { AttackTask } from '../model/tasks/AttackTask';
import { WaitTask } from '../model/tasks/WaitTask';
import { MoveNextToTargetTask } from '../model/tasks/MoveNextToTargetTask';

export interface GameState {
  hp: number;
  maxHp: number;
  turn: number;
  enemyCount: number;
  isPlayerDead: boolean;
  isCleared: boolean;
}

const EMPTY_STATE: GameState = {
  hp: 0, maxHp: 0, turn: 0, enemyCount: 0,
  isPlayerDead: false, isCleared: false,
};

/**
 * Main game loop hook. Creates model, renderer, input; orchestrates turn cycle.
 * Returns a ref to attach to the canvas container div + reactive game state.
 */
export function useGameLoop() {
  const containerRef = useRef<HTMLDivElement>(null);
  const [gameState, setGameState] = useState<GameState>(EMPTY_STATE);
  const [ready, setReady] = useState(false);

  // Refs for mutable objects that persist across renders
  const modelRef = useRef<GameModel | null>(null);
  const rendererRef = useRef<GameRenderer | null>(null);
  const animatorRef = useRef<AnimationPlayer | null>(null);
  const inputRef = useRef<InputHandler | null>(null);
  const processingRef = useRef(false);

  const readState = useCallback((): GameState => {
    const model = modelRef.current;
    if (!model) return EMPTY_STATE;
    const player = model.player;
    const floor = model.currentFloor;
    return {
      hp: player.hp,
      maxHp: player.maxHp,
      turn: Math.floor(model.time),
      enemyCount: countEnemies(floor),
      isPlayerDead: player.isDead,
      isCleared: floor.isCleared,
    };
  }, []);

  const syncAndUpdate = useCallback(() => {
    rendererRef.current?.syncToModel();
    setGameState(readState());
  }, [readState]);

  /** Process a player intent: assign task to player, step model, animate, sync. */
  const processIntent = useCallback(async (intent: PlayerIntent) => {
    const model = modelRef.current;
    const renderer = rendererRef.current;
    const animator = animatorRef.current;
    const input = inputRef.current;
    if (!model || !renderer || !animator || !input) return;
    if (processingRef.current) return;
    if (model.player.isDead) return;

    const player = model.player;
    const floor = model.currentFloor;

    // Translate intent to task
    const task = resolveIntent(intent, player, floor);
    if (!task) return;

    processingRef.current = true;
    input.setEnabled(false);

    // Assign task and step
    player.setTasks(task);
    model.turnManager.stepUntilPlayerChoice();

    // Collect and play animation events
    const events = model.consumeAnimationEvents();
    if (events.length > 0) {
      // Allow skipping animations with any key or click
      const skipHandler = () => animator.skip();
      window.addEventListener('keydown', skipHandler, { once: true });
      window.addEventListener('pointerdown', skipHandler, { once: true });
      try {
        await animator.play(events);
      } finally {
        window.removeEventListener('keydown', skipHandler);
        window.removeEventListener('pointerdown', skipHandler);
      }
    }

    // Snap to final model state
    renderer.syncToModel();
    setGameState(readState());

    processingRef.current = false;
    input.setEnabled(true);
  }, [readState]);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    let input: InputHandler | null = null;
    let resizeHandler: (() => void) | null = null;
    let keyHandler: ((e: KeyboardEvent) => void) | null = null;
    let destroyed = false;
    // Track whether init completed so cleanup knows if app is safe to destroy
    let appReady = false;
    let pixiApp: Application | null = null;

    async function init() {
      if (destroyed) return;

      // Pixel art: nearest-neighbor filtering globally
      TextureSource.defaultOptions.scaleMode = 'nearest';

      const app = new Application();
      pixiApp = app;
      await app.init({
        width: container!.clientWidth,
        height: container!.clientHeight,
        backgroundColor: 0x111118,
      });
      if (destroyed) { app.destroy(true, { children: true }); return; }
      appReady = true;
      container!.appendChild(app.canvas);

      // Load sprites
      const sprites = new SpriteManager();
      await sprites.load();
      if (destroyed) { app.destroy(true, { children: true }); return; }

      // Create model — use procedural floor generation
      const model = GameModel.createDailyGame();
      modelRef.current = model;

      // Renderer
      const camera = new Camera();
      const renderer = new GameRenderer(app, camera, sprites);
      rendererRef.current = renderer;

      const animator = new AnimationPlayer(renderer, camera);
      animatorRef.current = animator;

      // Initial render
      renderer.setFloor(model.currentFloor);
      renderer.syncToModel();

      // Input
      input = new InputHandler(camera, app.canvas);
      inputRef.current = input;
      input.onIntent.on(processIntent);
      input.attach();

      // Resize handler
      resizeHandler = () => {
        camera.resize(app.screen.width, app.screen.height, model.currentFloor.width, model.currentFloor.height);
        renderer.rebuildAll();
      };
      window.addEventListener('resize', resizeHandler);

      // Debug: press R to regenerate floor with a random seed
      if (import.meta.env.DEV) {
        keyHandler = (e: KeyboardEvent) => {
          if (e.key === 'r' || e.key === 'R') {
            if (processingRef.current) return;
            const newModel = GameModel.createDailyGame(String(Date.now()));
            modelRef.current = newModel;
            renderer.setFloor(newModel.currentFloor);
            renderer.syncToModel();
            camera.resize(app.screen.width, app.screen.height, newModel.currentFloor.width, newModel.currentFloor.height);
            setGameState(readState());
          }
        };
        window.addEventListener('keydown', keyHandler);
      }

      setGameState(readState());
      setReady(true);
    }

    init();

    return () => {
      destroyed = true;
      input?.detach();
      if (resizeHandler) window.removeEventListener('resize', resizeHandler);
      if (keyHandler) window.removeEventListener('keydown', keyHandler);
      if (pixiApp && appReady) {
        pixiApp.destroy(true, { children: true });
      }
      modelRef.current = null;
      rendererRef.current = null;
      animatorRef.current = null;
      inputRef.current = null;
    };
  }, [processIntent, readState]);

  return { containerRef, gameState, ready };
}

/** Translate a PlayerIntent into an ActorTask for the player. */
function resolveIntent(
  intent: PlayerIntent,
  player: Player,
  floor: Floor,
): import('../model/ActorTask').ActorTask | null {
  switch (intent.type) {
    case 'wait':
      return new WaitTask(player, 1);

    case 'move': {
      const target = Vector2Int.add(player.pos, intent.direction);
      // If there's an enemy body at target, attack it
      const bodyAtTarget = floor.bodies.get(target);
      if (bodyAtTarget && bodyAtTarget !== player && 'hp' in bodyAtTarget) {
        return new AttackTask(player, bodyAtTarget as any);
      }
      // Otherwise try to move there
      const tile = floor.tiles.get(target);
      if (tile && tile.canBeOccupiedBy(player)) {
        return new FollowPathTask(player, target, [target]);
      }
      return null; // Can't move there
    }

    case 'click': {
      const { tilePos } = intent;
      // Same tile = wait
      if (Vector2Int.equals(tilePos, player.pos)) {
        return new WaitTask(player, 1);
      }
      // Adjacent tile with enemy = attack
      if (Vector2Int.chebyshevDistance(player.pos, tilePos) === 1) {
        const bodyAtTarget = floor.bodies.get(tilePos);
        if (bodyAtTarget && bodyAtTarget !== player && 'hp' in bodyAtTarget) {
          return new AttackTask(player, bodyAtTarget as any);
        }
      }
      // Non-adjacent enemy = move next to then attack
      const bodyAtTarget = floor.bodies.get(tilePos);
      if (bodyAtTarget && bodyAtTarget !== player && 'hp' in bodyAtTarget) {
        return new MoveNextToTargetTask(player, bodyAtTarget.pos);
      }
      // Pathfind to tile
      const path = floor.findPath(player.pos, tilePos, false, player);
      if (path.length > 0) {
        return new FollowPathTask(player, tilePos, path);
      }
      return null;
    }
  }
}

function countEnemies(floor: Floor): number {
  let count = 0;
  for (const body of floor.bodies) {
    if ('faction' in body && (body as any).faction === Faction.Enemy) {
      count++;
    }
  }
  return count;
}
