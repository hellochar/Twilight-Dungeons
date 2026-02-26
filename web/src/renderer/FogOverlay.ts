import { Container, Graphics } from 'pixi.js';
import { Floor } from '../model/Floor';
import { TileVisibility } from '../core/types';
import { Vector2Int } from '../core/Vector2Int';
import { Camera } from './Camera';

/**
 * Renders fog of war as semi-transparent overlays on tiles.
 * Unexplored = solid black, Explored = 60% black, Visible = transparent.
 */
export class FogOverlay {
  readonly container = new Container();
  private camera: Camera;
  private cells = new Map<string, Graphics>();

  constructor(camera: Camera) {
    this.camera = camera;
  }

  /** Full rebuild of fog overlay from floor visibility state. */
  rebuild(floor: Floor): void {
    this.container.removeChildren();
    this.cells.clear();

    const ts = this.camera.tileSize;

    for (const pos of floor.enumerateFloor()) {
      const tile = floor.tiles.get(pos);
      if (!tile) continue;

      const key = Vector2Int.key(pos);
      const px = this.camera.tileToPixel(pos);
      const g = new Graphics();
      g.rect(0, 0, ts, ts).fill(0x000000);
      g.position.set(px.x, px.y);
      g.alpha = this.alphaForVisibility(tile.visibility);
      this.container.addChild(g);
      this.cells.set(key, g);
    }
  }

  /** Update fog alpha based on current tile visibility. */
  sync(floor: Floor): void {
    for (const pos of floor.enumerateFloor()) {
      const tile = floor.tiles.get(pos);
      if (!tile) continue;

      const key = Vector2Int.key(pos);
      const g = this.cells.get(key);
      if (g) {
        g.alpha = this.alphaForVisibility(tile.visibility);
      }
    }
  }

  private alphaForVisibility(vis: TileVisibility): number {
    switch (vis) {
      case TileVisibility.Unexplored: return 1.0;
      case TileVisibility.Explored: return 0.6;
      case TileVisibility.Visible: return 0.0;
    }
  }
}
