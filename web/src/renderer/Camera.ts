import { Vector2Int } from '../core/Vector2Int';
import { DEFAULT_TILE_SIZE, MIN_TILE_SIZE, TILE_PADDING } from '../constants';

/** Returns true on touch/mobile devices. */
export function isMobile(): boolean {
  return navigator.maxTouchPoints > 0 || window.innerWidth < 768;
}

/**
 * Converts between tile coordinates and pixel coordinates.
 * Handles viewport sizing and centering.
 */
export class Camera {
  /** Pixels per tile. */
  tileSize: number;

  /** Pixel offset to center the floor in the viewport. */
  offsetX: number;
  offsetY: number;

  /** Floor dimensions in tiles. */
  floorWidth: number;
  floorHeight: number;

  constructor() {
    this.tileSize = DEFAULT_TILE_SIZE;
    this.offsetX = 0;
    this.offsetY = 0;
    this.floorWidth = 0;
    this.floorHeight = 0;
  }

  /**
   * Recalculate tile size and offset to fit the floor in the viewport.
   * Keeps square tiles and centers the grid.
   * @param tilePadding - empty tile padding on each side (symmetric). Negative = zoom in past edges. Default 0.5.
   */
  resize(
    viewportWidth: number,
    viewportHeight: number,
    floorWidth: number,
    floorHeight: number,
    tilePadding: number = TILE_PADDING,
  ): void {
    this.floorWidth = floorWidth;
    this.floorHeight = floorHeight;

    this.tileSize = Math.floor(Math.min(
      viewportWidth / (floorWidth + 2 * tilePadding),
      viewportHeight / (floorHeight + 2 * tilePadding),
    ));
    this.tileSize = Math.max(this.tileSize, MIN_TILE_SIZE);

    const gridW = floorWidth * this.tileSize;
    const gridH = floorHeight * this.tileSize;
    this.offsetX = Math.floor((viewportWidth - gridW) / 2);
    this.offsetY = Math.floor((viewportHeight - gridH) / 2);
  }

  /**
   * Tile position → pixel position (top-left corner of tile).
   * Y is flipped: game Y increases upward (Unity convention),
   * but screen Y increases downward. So game y=0 renders at bottom.
   */
  tileToPixel(pos: Vector2Int): { x: number; y: number } {
    return {
      x: this.offsetX + pos.x * this.tileSize,
      y: this.offsetY + (this.floorHeight - 1 - pos.y) * this.tileSize,
    };
  }

  /** Tile position → pixel position (center of tile). */
  tileToCenterPixel(pos: Vector2Int): { x: number; y: number } {
    return {
      x: this.offsetX + pos.x * this.tileSize + this.tileSize / 2,
      y: this.offsetY + (this.floorHeight - 1 - pos.y) * this.tileSize + this.tileSize / 2,
    };
  }

  /** Pixel position → tile position (floor'd), with Y flip. */
  pixelToTile(px: number, py: number): Vector2Int {
    const tx = Math.floor((px - this.offsetX) / this.tileSize);
    const screenTy = Math.floor((py - this.offsetY) / this.tileSize);
    const ty = this.floorHeight - 1 - screenTy;
    return new Vector2Int(tx, ty);
  }

  /** Check if a tile coordinate is within the floor bounds. */
  isInBounds(pos: Vector2Int): boolean {
    return pos.x >= 0 && pos.x < this.floorWidth && pos.y >= 0 && pos.y < this.floorHeight;
  }
}
