import { Vector2Int } from '../core/Vector2Int';
import { Camera } from '../renderer/Camera';
import { EventEmitter } from '../core/EventEmitter';

export type PlayerIntent =
  | { type: 'move'; direction: Vector2Int }
  | { type: 'click'; tilePos: Vector2Int }
  | { type: 'wait' };

/**
 * Captures keyboard + mouse/touch input and emits PlayerIntent events.
 * Translate raw browser events into tile-relative game actions.
 */
export class InputHandler {
  readonly onIntent = new EventEmitter<[PlayerIntent]>();
  private camera: Camera;
  private canvas: HTMLCanvasElement;
  private enabled = true;

  // Key → direction mapping (arrows, WASD, numpad)
  private static KEY_MAP: Record<string, Vector2Int> = {
    ArrowUp:    Vector2Int.up,
    ArrowDown:  Vector2Int.down,
    ArrowLeft:  Vector2Int.left,
    ArrowRight: Vector2Int.right,
    w: Vector2Int.up,
    s: Vector2Int.down,
    a: Vector2Int.left,
    d: Vector2Int.right,
    W: Vector2Int.up,
    S: Vector2Int.down,
    A: Vector2Int.left,
    D: Vector2Int.right,
    // Numpad
    Numpad8: Vector2Int.up,
    Numpad2: Vector2Int.down,
    Numpad4: Vector2Int.left,
    Numpad6: Vector2Int.right,
    Numpad7: new Vector2Int(-1, -1),
    Numpad9: new Vector2Int(1, -1),
    Numpad1: new Vector2Int(-1, 1),
    Numpad3: new Vector2Int(1, 1),
    // Diagonal with QEZC
    q: new Vector2Int(-1, -1),
    e: new Vector2Int(1, -1),
    z: new Vector2Int(-1, 1),
    c: new Vector2Int(1, 1),
    Q: new Vector2Int(-1, -1),
    E: new Vector2Int(1, -1),
    Z: new Vector2Int(-1, 1),
    C: new Vector2Int(1, 1),
  };

  constructor(camera: Camera, canvas: HTMLCanvasElement) {
    this.camera = camera;
    this.canvas = canvas;
    this.onKeyDown = this.onKeyDown.bind(this);
    this.onClick = this.onClick.bind(this);
  }

  attach(): void {
    window.addEventListener('keydown', this.onKeyDown);
    this.canvas.addEventListener('pointerdown', this.onClick);
  }

  detach(): void {
    window.removeEventListener('keydown', this.onKeyDown);
    this.canvas.removeEventListener('pointerdown', this.onClick);
  }

  setEnabled(enabled: boolean): void {
    this.enabled = enabled;
  }

  private onKeyDown(e: KeyboardEvent): void {
    if (!this.enabled) return;

    // Wait: space or numpad5 or period
    if (e.key === ' ' || e.key === 'Numpad5' || e.key === '.') {
      e.preventDefault();
      this.onIntent.emit({ type: 'wait' });
      return;
    }

    const dir = InputHandler.KEY_MAP[e.code] ?? InputHandler.KEY_MAP[e.key];
    if (dir) {
      e.preventDefault();
      this.onIntent.emit({ type: 'move', direction: dir });
    }
  }

  private onClick(e: PointerEvent): void {
    if (!this.enabled) return;

    const rect = this.canvas.getBoundingClientRect();
    const px = e.clientX - rect.left;
    const py = e.clientY - rect.top;

    const tilePos = this.camera.pixelToTile(px, py);
    if (this.camera.isInBounds(tilePos)) {
      this.onIntent.emit({ type: 'click', tilePos });
    }
  }
}
