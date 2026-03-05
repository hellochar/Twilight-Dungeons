import { Container, Text, TextStyle } from 'pixi.js';
import { Camera } from './Camera';
import { GameModelRef } from '../model/GameModelRef';
import { Actor } from '../model/Actor';

/** Toggle to show/hide HP labels for all visible actors. */
export const SHOW_HP_LABELS = true;

// Unity: actorController.transform.position + Vector3(0, -0.30, 0)
const HP_LABEL_Y_OFFSET = 0.30;
// ~10px at 48px tile size
const HP_FONT_SCALE = 0.22;

/**
 * Renders "{hp}/{maxHp}" labels below all visible actors (ThirdEye-style).
 * Self-contained — GameRenderer only needs to addChild(layer) and call sync().
 */
export class HpLabelRenderer {
  /** Add this layer to the scene between effectLayer and dimLayer. */
  readonly layer = new Container();

  private labels = new Map<string, Text>();

  constructor(private camera: Camera) {}

  sync(): void {
    if (!SHOW_HP_LABELS) {
      this.layer.visible = false;
      return;
    }
    this.layer.visible = true;

    const player = GameModelRef.main?.player;
    if (!player || !player.floor) {
      this.clear();
      return;
    }

    const visible = player.floor.bodies.where((e) => e instanceof Actor) as Actor[];
    const visibleGuids = new Set(visible.map((a) => a.guid));

    // Remove stale labels
    for (const [guid, label] of this.labels) {
      if (!visibleGuids.has(guid)) {
        this.layer.removeChild(label);
        label.destroy();
        this.labels.delete(guid);
      }
    }

    const ts = this.camera.tileSize;
    const fontSize = Math.round(ts * HP_FONT_SCALE);
    for (const actor of visible) {
      let label = this.labels.get(actor.guid);
      if (!label) {
        label = new Text({
          text: '',
          style: new TextStyle({
            fontFamily: 'CodersCrux, monospace',
            fontSize,
            fill: 0xffffff,
            stroke: { color: 0x000000, width: Math.max(2, Math.round(fontSize * 0.12)) },
            align: 'center',
          }),
        });
        // anchor (0.5, 1) = bottom-center, matching Unity pivot (0.5, 0) + VerticalAlign Bottom
        label.anchor.set(0.5, 1);
        this.layer.addChild(label);
        this.labels.set(actor.guid, label);
      }

      label.text = `${actor.hp}/${actor.maxHp}`;
    }
  }

  /** Call every frame (after lerpPositions) to keep labels glued to their sprites. */
  syncPositions(getSprite: (guid: string) => Container | undefined): void {
    if (!this.layer.visible) return;
    const ts = this.camera.tileSize;
    for (const [guid, label] of this.labels) {
      const node = getSprite(guid);
      if (node) {
        label.x = node.x + ts / 2;
        label.y = node.y + ts / 2 + HP_LABEL_Y_OFFSET * ts;
      }
    }
  }

  private clear(): void {
    for (const label of this.labels.values()) label.destroy();
    this.labels.clear();
  }
}
