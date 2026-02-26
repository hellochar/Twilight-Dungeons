import gsap from 'gsap';
import { Sprite, Graphics } from 'pixi.js';
import { Vector2Int } from '../core/Vector2Int';
import { Camera } from './Camera';
import { GameRenderer } from './GameRenderer';

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
    const sprite = this.renderer.getEntitySprite(event.entityGuid);
    if (!sprite) return;

    switch (event.type) {
      case 'move':
        this.animateMove(sprite, event, tl);
        break;
      case 'attack':
        this.animateAttack(sprite, event, tl);
        break;
      case 'attackGround':
        this.animateAttackGround(sprite, event, tl);
        break;
      case 'damage':
        this.animateDamage(sprite, event, tl);
        break;
      case 'heal':
        this.animateHeal(sprite, event, tl);
        break;
      case 'death':
        this.animateDeath(sprite, event, tl);
        break;
      case 'spawn':
        this.animateSpawn(sprite, event, tl);
        break;
    }
  }

  /** Slide from old position to new position. */
  private animateMove(sprite: Sprite, event: GameEvent, tl: gsap.core.Timeline): void {
    if (!event.from || !event.to) return;
    const fromPx = this.camera.tileToPixel(event.from);
    const toPx = this.camera.tileToPixel(event.to);

    // Set to old position, tween to new
    sprite.position.set(fromPx.x, fromPx.y);
    tl.to(sprite.position, {
      x: toPx.x,
      y: toPx.y,
      duration: 0.12,
      ease: 'power2.out',
    }, '<');
  }

  /** Bump toward target then return (melee attack feel). */
  private animateAttack(sprite: Sprite, event: GameEvent, tl: gsap.core.Timeline): void {
    if (!event.from || !event.to) return;
    const fromPx = this.camera.tileToPixel(event.from);
    const toPx = this.camera.tileToPixel(event.to);

    // Bump halfway toward target
    const midX = fromPx.x + (toPx.x - fromPx.x) * 0.4;
    const midY = fromPx.y + (toPx.y - fromPx.y) * 0.4;

    const label = `attack-${event.entityGuid}`;
    tl.to(sprite.position, {
      x: midX,
      y: midY,
      duration: 0.06,
      ease: 'power2.in',
    }, label);
    tl.to(sprite.position, {
      x: fromPx.x,
      y: fromPx.y,
      duration: 0.06,
      ease: 'power2.out',
    }, '>');
  }

  /** Quick scale pulse for ground attack. */
  private animateAttackGround(sprite: Sprite, event: GameEvent, tl: gsap.core.Timeline): void {
    const label = `atkground-${event.entityGuid}`;
    tl.to(sprite.scale, {
      x: 1.3,
      y: 1.3,
      duration: 0.06,
      ease: 'power2.in',
    }, label);
    tl.to(sprite.scale, {
      x: 1,
      y: 1,
      duration: 0.06,
      ease: 'power2.out',
    }, '>');
  }

  /** Flash red on damage. */
  private animateDamage(sprite: Sprite, event: GameEvent, tl: gsap.core.Timeline): void {
    const origTint = sprite.tint;
    const label = `dmg-${event.entityGuid}`;
    tl.to(sprite, {
      tint: 0xff3333,
      duration: 0.05,
      onComplete: () => { sprite.tint = origTint; },
    }, label);

    // Shake
    const startX = sprite.position.x;
    tl.to(sprite.position, {
      x: startX + 3,
      duration: 0.03,
      yoyo: true,
      repeat: 2,
    }, label);

    // Spawn floating damage number
    if (event.amount !== undefined) {
      this.spawnDamageNumber(event, tl, label);
    }
  }

  /** Brief green flash on heal. */
  private animateHeal(sprite: Sprite, event: GameEvent, tl: gsap.core.Timeline): void {
    const origTint = sprite.tint;
    tl.to(sprite, {
      tint: 0x33ff66,
      duration: 0.08,
      onComplete: () => { sprite.tint = origTint; },
    }, `heal-${event.entityGuid}`);
  }

  /** Shrink + fade out on death. */
  private animateDeath(sprite: Sprite, event: GameEvent, tl: gsap.core.Timeline): void {
    const label = `death-${event.entityGuid}`;
    tl.to(sprite, {
      alpha: 0,
      duration: 0.25,
      ease: 'power2.in',
    }, label);
    tl.to(sprite.scale, {
      x: 0,
      y: 0,
      duration: 0.25,
      ease: 'power2.in',
    }, label);
  }

  /** Pop in from zero scale on spawn. */
  private animateSpawn(sprite: Sprite, _event: GameEvent, tl: gsap.core.Timeline): void {
    sprite.scale.set(0, 0);
    sprite.alpha = 0;
    tl.to(sprite.scale, {
      x: 1,
      y: 1,
      duration: 0.2,
      ease: 'back.out(1.7)',
    }, '<');
    tl.to(sprite, {
      alpha: 1,
      duration: 0.15,
    }, '<');
  }

  /** Spawn a floating "-N" text that drifts up and fades. */
  private spawnDamageNumber(event: GameEvent, tl: gsap.core.Timeline, label: string): void {
    if (!event.to && !event.from) return;
    const pos = event.to ?? event.from!;
    const px = this.camera.tileToCenterPixel(pos);

    const g = new Graphics();
    // Draw damage text as a simple red circle with number
    // (PixiJS v8 text is heavy; use a simple visual indicator)
    g.circle(0, 0, 6).fill(0xff3333);
    g.position.set(px.x, px.y - this.camera.tileSize * 0.3);
    g.alpha = 1;

    const layer = this.renderer.getEffectLayer();
    layer.addChild(g);

    tl.to(g, {
      y: g.y - 20,
      alpha: 0,
      duration: 0.5,
      ease: 'power2.out',
      onComplete: () => {
        g.destroy();
      },
    }, label);
  }
}
