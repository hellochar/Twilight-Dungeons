import { Vector2Int } from '../core/Vector2Int';

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
    this.tileSize = 32;
    this.offsetX = 0;
    this.offsetY = 0;
    this.floorWidth = 0;
    this.floorHeight = 0;
  }

  /**
   * Recalculate tile size and offset to fit the floor in the viewport.
   * Keeps square tiles and centers the grid.
   */
  resize(viewportWidth: number, viewportHeight: number, floorWidth: number, floorHeight: number): void {
    this.floorWidth = floorWidth;
    this.floorHeight = floorHeight;

    // Fit floor into viewport with some padding
    const padding = 16;
    const availW = viewportWidth - padding * 2;
    const availH = viewportHeight - padding * 2;

    this.tileSize = Math.floor(Math.min(availW / floorWidth, availH / floorHeight));
    this.tileSize = Math.max(this.tileSize, 8); // minimum 8px tiles

    const gridW = floorWidth * this.tileSize;
    const gridH = floorHeight * this.tileSize;
    this.offsetX = Math.floor((viewportWidth - gridW) / 2);
    this.offsetY = Math.floor((viewportHeight - gridH) / 2);
  }

  /** Tile position → pixel position (top-left corner of tile). */
  tileToPixel(pos: Vector2Int): { x: number; y: number } {
    return {
      x: this.offsetX + pos.x * this.tileSize,
      y: this.offsetY + pos.y * this.tileSize,
    };
  }

  /** Tile position → pixel position (center of tile). */
  tileToCenterPixel(pos: Vector2Int): { x: number; y: number } {
    return {
      x: this.offsetX + pos.x * this.tileSize + this.tileSize / 2,
      y: this.offsetY + pos.y * this.tileSize + this.tileSize / 2,
    };
  }

  /** Pixel position → tile position (floor'd). */
  pixelToTile(px: number, py: number): Vector2Int {
    const tx = Math.floor((px - this.offsetX) / this.tileSize);
    const ty = Math.floor((py - this.offsetY) / this.tileSize);
    return new Vector2Int(tx, ty);
  }

  /** Check if a tile coordinate is within the floor bounds. */
  isInBounds(pos: Vector2Int): boolean {
    return pos.x >= 0 && pos.x < this.floorWidth && pos.y >= 0 && pos.y < this.floorHeight;
  }
}
