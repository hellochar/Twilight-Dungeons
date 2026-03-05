import { Vector2Int } from '../core/Vector2Int';
import { Camera } from '../renderer/Camera';
import { EventEmitter } from '../core/EventEmitter';

export type PlayerIntent =
  | { type: 'move'; direction: Vector2Int }
  | { type: 'click'; tilePos: Vector2Int }
  | { type: 'wait' }
  | { type: 'cancel' };

/**
 * Captures keyboard + mouse/touch input and emits PlayerIntent events.
 * Translate raw browser events into tile-relative game actions.
 */
/** Right-click / long-press info about a tile position + screen coords. */
export interface TileContextEvent {
  tilePos: Vector2Int;
  screenX: number;
  screenY: number;
}

export class InputHandler {
  readonly onIntent = new EventEmitter<[PlayerIntent]>();
  readonly onContextMenu = new EventEmitter<[TileContextEvent]>();
  readonly onTileHover = new EventEmitter<[TileContextEvent]>();
  private camera: Camera;
  private canvas: HTMLCanvasElement;
  private enabled = true;
  private longPressTimer: ReturnType<typeof setTimeout> | null = null;
  /** Tile where the current touch started (null when no touch active). */
  private touchStartTile: Vector2Int | null = null;
  /** Set true if finger drags to a different tile — suppresses click intent on release. */
  private touchDragged = false;

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
    q: new Vector2Int(-1, 1),
    e: new Vector2Int(1, 1),
    z: new Vector2Int(-1, -1),
    c: new Vector2Int(1, -1),
    Q: new Vector2Int(-1, 1),
    E: new Vector2Int(1, 1),
    Z: new Vector2Int(-1, -1),
    C: new Vector2Int(1, -1),
  };

  constructor(camera: Camera, canvas: HTMLCanvasElement) {
    this.camera = camera;
    this.canvas = canvas;
    this.onKeyDown = this.onKeyDown.bind(this);
    this.onClick = this.onClick.bind(this);
    this.onContextMenuEvent = this.onContextMenuEvent.bind(this);
    this.onMouseMove = this.onMouseMove.bind(this);
    this.onTouchStart = this.onTouchStart.bind(this);
    this.onTouchMove = this.onTouchMove.bind(this);
    this.onTouchEnd = this.onTouchEnd.bind(this);
  }

  attach(): void {
    window.addEventListener('keydown', this.onKeyDown);
    this.canvas.addEventListener('pointerdown', this.onClick);
    this.canvas.addEventListener('contextmenu', this.onContextMenuEvent);
    this.canvas.addEventListener('mousemove', this.onMouseMove);
    this.canvas.addEventListener('touchstart', this.onTouchStart, { passive: false });
    this.canvas.addEventListener('touchmove', this.onTouchMove, { passive: true });
    this.canvas.addEventListener('touchend', this.onTouchEnd);
    this.canvas.addEventListener('touchcancel', this.onTouchEnd);
  }

  detach(): void {
    window.removeEventListener('keydown', this.onKeyDown);
    this.canvas.removeEventListener('pointerdown', this.onClick);
    this.canvas.removeEventListener('contextmenu', this.onContextMenuEvent);
    this.canvas.removeEventListener('mousemove', this.onMouseMove);
    this.canvas.removeEventListener('touchstart', this.onTouchStart);
    this.canvas.removeEventListener('touchmove', this.onTouchMove);
    this.canvas.removeEventListener('touchend', this.onTouchEnd);
    this.canvas.removeEventListener('touchcancel', this.onTouchEnd);
    this.clearLongPress();
  }

  setEnabled(enabled: boolean): void {
    this.enabled = enabled;
  }

  private onKeyDown(e: KeyboardEvent): void {
    if (!this.enabled) return;

    if (e.key === 'Escape') {
      this.onIntent.emit({ type: 'cancel' });
      return;
    }

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
    if (e.button !== 0) return; // left-click only
    // If the finger dragged to a different tile, suppress the click — the drag
    // already updated hover/reticle and the user didn't intend to tap-confirm.
    if (this.touchDragged) return;

    const rect = this.canvas.getBoundingClientRect();
    const px = e.clientX - rect.left;
    const py = e.clientY - rect.top;

    const tilePos = this.camera.pixelToTile(px, py);
    if (this.camera.isInBounds(tilePos)) {
      this.onIntent.emit({ type: 'click', tilePos });
    }
  }

  private onMouseMove(e: MouseEvent): void {
    const rect = this.canvas.getBoundingClientRect();
    const px = e.clientX - rect.left;
    const py = e.clientY - rect.top;
    const tilePos = this.camera.pixelToTile(px, py);
    if (this.camera.isInBounds(tilePos)) {
      this.onTileHover.emit({ tilePos, screenX: e.clientX, screenY: e.clientY });
    }
  }

  private onContextMenuEvent(e: MouseEvent): void {
    e.preventDefault();
    if (!this.enabled) return;

    const rect = this.canvas.getBoundingClientRect();
    const px = e.clientX - rect.left;
    const py = e.clientY - rect.top;

    const tilePos = this.camera.pixelToTile(px, py);
    if (this.camera.isInBounds(tilePos)) {
      this.onContextMenu.emit({ tilePos, screenX: e.clientX, screenY: e.clientY });
    }
  }

  /** Long-press on mobile (500ms) triggers context menu. */
  private onTouchStart(e: TouchEvent): void {
    if (!this.enabled || e.touches.length !== 1) return;
    const touch = e.touches[0];
    const screenX = touch.clientX;
    const screenY = touch.clientY;

    this.touchDragged = false;

    // Immediately emit hover so the info panel updates on tap
    const rect = this.canvas.getBoundingClientRect();
    const px = screenX - rect.left;
    const py = screenY - rect.top;
    const tilePos = this.camera.pixelToTile(px, py);
    this.touchStartTile = this.camera.isInBounds(tilePos) ? tilePos : null;
    if (this.touchStartTile) {
      this.onTileHover.emit({ tilePos, screenX, screenY });
    }

    this.longPressTimer = setTimeout(() => {
      this.longPressTimer = null;
      const rect = this.canvas.getBoundingClientRect();
      const px = screenX - rect.left;
      const py = screenY - rect.top;
      const tilePos = this.camera.pixelToTile(px, py);
      if (this.camera.isInBounds(tilePos)) {
        this.onContextMenu.emit({ tilePos, screenX, screenY });
      }
    }, 500);
  }

  /** Dragging finger updates hover/reticle to the tile under the finger. */
  private onTouchMove(e: TouchEvent): void {
    if (!this.enabled || e.touches.length !== 1) return;
    const touch = e.touches[0];
    const rect = this.canvas.getBoundingClientRect();
    const px = touch.clientX - rect.left;
    const py = touch.clientY - rect.top;
    const tilePos = this.camera.pixelToTile(px, py);
    if (!this.camera.isInBounds(tilePos)) return;

    // Only emit when the tile actually changed
    if (this.touchStartTile && Vector2Int.equals(tilePos, this.touchStartTile) && !this.touchDragged) return;

    this.touchDragged = true;
    this.clearLongPress();
    this.onTileHover.emit({ tilePos, screenX: touch.clientX, screenY: touch.clientY });
  }

  private onTouchEnd(): void {
    this.clearLongPress();
    this.touchStartTile = null;
    // Reset touchDragged after a microtask so onClick (which fires after touchend) can still read it
    Promise.resolve().then(() => { this.touchDragged = false; });
  }

  private clearLongPress(): void {
    if (this.longPressTimer != null) {
      clearTimeout(this.longPressTimer);
      this.longPressTimer = null;
    }
  }
}
