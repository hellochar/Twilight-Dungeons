import gsap from 'gsap';
import { Container, Sprite, Text, TextStyle } from 'pixi.js';
import { Vector2Int } from '../core/Vector2Int';
import { Camera } from './Camera';
import { GameRenderer } from './GameRenderer';

const DAMAGE_STYLE = new TextStyle({
  fontFamily: 'monospace',
  fontSize: 14,
  fontWeight: 'bold',
  fill: 0xff3333,
  stroke: { color: 0x000000, width: 3 },
});

const HEAL_STYLE = new TextStyle({
  fontFamily: 'monospace',
  fontSize: 14,
  fontWeight: 'bold',
  fill: 0x33ff66,
  stroke: { color: 0x000000, width: 3 },
});

// ─── Unity BumpAndReturn constants (BumpAndReturn.cs) ───
const BUMP_DURATION = 0.25;
const BUMP_INTENSITY = 0.75;

/** Unity's exact parabolic bump-and-return easing: pow(cos(PI/2 + PI*sqrt(t)), 4) * 0.75 */
function bumpAndReturnEasing(t: number): number {
  return Math.pow(Math.cos(Math.PI / 2 + Math.PI * Math.sqrt(t)), 4) * BUMP_INTENSITY;
}

/** Describes an event that happened during a turn step, for animation. */
export interface GameEvent {
  type: 'move' | 'attack' | 'attackGround' | 'damage' | 'heal' | 'death' | 'spawn';
  entityGuid: string;
  from?: Vector2Int;
  to?: Vector2Int;
  targetGuid?: string;
  amount?: number;
}

/**
 * Plays animations for game events using GSAP tweens on PixiJS sprites.
 * Each event type maps to a specific tween pattern.
 */
export class AnimationPlayer {
  private renderer: GameRenderer;
  private camera: Camera;
  private timeline: gsap.core.Timeline | null = null;

  constructor(renderer: GameRenderer, camera: Camera) {
    this.renderer = renderer;
    this.camera = camera;
  }

  /** Play a sequence of game events as animations. Returns a promise that resolves when done. */
  async play(events: GameEvent[]): Promise<void> {
    if (events.length === 0) return;

    // Kill any running animation
    this.timeline?.kill();
    this.timeline = gsap.timeline();

    for (const event of events) {
      this.addEventToTimeline(event, this.timeline);
    }

    return new Promise((resolve) => {
      this.timeline!.eventCallback('onComplete', resolve);
      // Safety timeout — if timeline is empty or instant, resolve immediately
      if (this.timeline!.duration() === 0) resolve();
    });
  }

  /** Play a batch of events on a fresh timeline (for per-step animation). */
  async playBatch(events: GameEvent[]): Promise<void> {
    if (events.length === 0) return;

    this.timeline?.kill();
    this.timeline = gsap.timeline();

    for (const event of events) {
      this.addEventToTimeline(event, this.timeline);
    }

    return new Promise((resolve) => {
      this.timeline!.eventCallback('onComplete', resolve);
      if (this.timeline!.duration() === 0) resolve();
    });
  }

  /** Skip/cancel any running animation. */
  skip(): void {
    if (this.timeline) {
      this.timeline.progress(1);
      this.timeline.kill();
      this.timeline = null;
    }
  }

  /** Whether an animation is currently playing. */
  get isPlaying(): boolean {
    return this.timeline?.isActive() ?? false;
  }

  private addEventToTimeline(event: GameEvent, tl: gsap.core.Timeline): void {
    const node = this.renderer.getEntitySprite(event.entityGuid);
    if (!node) return;
    const visual = this.renderer.getEntityVisual(event.entityGuid);

    switch (event.type) {
      case 'move':
        this.animateMove(node, event, tl);
        break;
      case 'attack':
        this.animateAttack(node, event, tl);
        break;
      case 'attackGround':
        this.animateAttackGround(node, event, tl);
        break;
      case 'damage':
        this.animateDamage(node, visual, event, tl);
        break;
      case 'heal':
        this.animateHeal(visual, event, tl);
        break;
      case 'death':
        this.animateDeath(node, event, tl);
        break;
      case 'spawn':
        this.animateSpawn(node, event, tl);
        break;
    }
  }

  /** Slide to new position (from wherever the sprite currently is). */
  private animateMove(node: Container, event: GameEvent, tl: gsap.core.Timeline): void {
    if (!event.to) return;
    const toPx = this.camera.tileToPixel(event.to);

    tl.to(node.position, {
      x: toPx.x,
      y: toPx.y,
      duration: 0.12,
      ease: 'power2.out',
    }, '<');
  }

  /**
   * Bump toward target then return (melee attack feel).
   * Unity BumpAndReturn.cs: duration 0.25s, intensity 0.75,
   * easing = pow(cos(PI/2 + PI * sqrt(t)), 4) * 0.75
   */
  private animateAttack(node: Container, event: GameEvent, tl: gsap.core.Timeline): void {
    if (!event.from || !event.to) return;
    const fromPx = this.camera.tileToPixel(event.from);
    const toPx = this.camera.tileToPixel(event.to);

    const dx = (toPx.x - fromPx.x) * BUMP_INTENSITY;
    const dy = (toPx.y - fromPx.y) * BUMP_INTENSITY;

    const progress = { t: 0 };
    tl.to(progress, {
      t: 1,
      duration: BUMP_DURATION,
      ease: 'none',
      onUpdate: () => {
        const e = bumpAndReturnEasing(progress.t);
        node.position.set(fromPx.x + dx * e, fromPx.y + dy * e);
      },
      onComplete: () => {
        node.position.set(fromPx.x, fromPx.y);
      },
    }, `attack-${event.entityGuid}`);
  }

  /** Quick scale pulse for ground attack. */
  private animateAttackGround(node: Container, event: GameEvent, tl: gsap.core.Timeline): void {
    const label = `atkground-${event.entityGuid}`;
    tl.to(node.scale, {
      x: 1.3,
      y: 1.3,
      duration: 0.06,
      ease: 'power2.in',
    }, label);
    tl.to(node.scale, {
      x: 1,
      y: 1,
      duration: 0.06,
      ease: 'power2.out',
    }, '>');
  }

  /** Flash red on damage — tint targets the visual Sprite, shake targets the node. */
  private animateDamage(
    node: Container, visual: Sprite | undefined,
    event: GameEvent, tl: gsap.core.Timeline,
  ): void {
    const label = `dmg-${event.entityGuid}`;

    if (visual) {
      const origTint = visual.tint;
      tl.to(visual, {
        tint: 0xff3333,
        duration: 0.05,
        onComplete: () => { visual.tint = origTint; },
      }, label);
    }

    // Shake the whole node
    const startX = node.position.x;
    tl.to(node.position, {
      x: startX + 3,
      duration: 0.03,
      yoyo: true,
      repeat: 2,
    }, label);

    if (event.amount !== undefined && event.amount > 0) {
      this.spawnFloatingText(`-${event.amount}`, event, tl, label, DAMAGE_STYLE);
    }
  }

  /** Brief green flash on heal — tint targets the visual Sprite only. */
  private animateHeal(
    visual: Sprite | undefined, event: GameEvent, tl: gsap.core.Timeline,
  ): void {
    if (!visual) return;
    const label = `heal-${event.entityGuid}`;
    const origTint = visual.tint;
    tl.to(visual, {
      tint: 0x33ff66,
      duration: 0.08,
      onComplete: () => { visual.tint = origTint; },
    }, label);

    if (event.amount !== undefined && event.amount > 0) {
      this.spawnFloatingText(`+${event.amount}`, event, tl, label, HEAL_STYLE);
    }
  }

  /** Shrink + fade out on death — targets the whole node. */
  private animateDeath(node: Container, event: GameEvent, tl: gsap.core.Timeline): void {
    const label = `death-${event.entityGuid}`;
    tl.to(node, {
      alpha: 0,
      duration: 0.25,
      ease: 'power2.in',
    }, label);
    tl.to(node.scale, {
      x: 0,
      y: 0,
      duration: 0.25,
      ease: 'power2.in',
    }, label);
  }

  /** Pop in from zero scale on spawn — targets the whole node. */
  private animateSpawn(node: Container, _event: GameEvent, tl: gsap.core.Timeline): void {
    node.scale.set(0, 0);
    node.alpha = 0;
    tl.to(node.scale, {
      x: 1,
      y: 1,
      duration: 0.2,
      ease: 'back.out(1.7)',
    }, '<');
    tl.to(node, {
      alpha: 1,
      duration: 0.15,
    }, '<');
  }

  /** Spawn floating text (e.g. "-3" or "+2") that drifts up and fades. */
  private spawnFloatingText(
    text: string, event: GameEvent, tl: gsap.core.Timeline,
    label: string, style: TextStyle,
  ): void {
    if (!event.to && !event.from) return;
    const pos = event.to ?? event.from!;
    const px = this.camera.tileToCenterPixel(pos);

    const t = new Text({ text, style });
    t.anchor.set(0.5, 1);
    t.position.set(px.x, px.y - this.camera.tileSize * 0.1);

    const layer = this.renderer.getEffectLayer();
    layer.addChild(t);

    tl.to(t, {
      y: t.y - 24,
      alpha: 0,
      duration: 0.6,
      ease: 'power2.out',
      onComplete: () => { t.destroy(); },
    }, label);
  }
}
