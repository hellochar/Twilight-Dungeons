import gsap from 'gsap';
import { Container, Sprite, Text, TextStyle } from 'pixi.js';
import { Vector2Int } from '../core/Vector2Int';
import { Camera } from './Camera';
import { GameRenderer } from './GameRenderer';

// CodersCrux cap height ≈ 65% of point size; scale up so glyphs appear ~1 tile tall
const HP_TEXT_FONT_SCALE = 1.00;

function makeDamageStyle(tileSize: number): TextStyle {
  const sz = Math.round(tileSize * HP_TEXT_FONT_SCALE);
  return new TextStyle({
    fontFamily: 'CodersCrux, monospace',
    fontSize: sz,
    fill: 0xff3333,
    stroke: { color: 0x000000, width: Math.max(2, Math.round(sz * 0.12)) },
  });
}

function makeHealStyle(tileSize: number): TextStyle {
  const sz = Math.round(tileSize * HP_TEXT_FONT_SCALE);
  return new TextStyle({
    fontFamily: 'CodersCrux, monospace',
    fontSize: sz,
    fill: 0x33ff66,
    stroke: { color: 0x000000, width: Math.max(2, Math.round(sz * 0.12)) },
  });
}

// ─── Unity BumpAndReturn constants (BumpAndReturn.cs) ───
const BUMP_DURATION = 0.25;
const BUMP_INTENSITY = 0.75;
// Time when bump reaches peak displacement: t_peak=0.25, so 0.25*BUMP_DURATION
const BUMP_IMPACT_TIME = BUMP_DURATION * 0.25;

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
  private floatingTexts: Text[] = [];

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
    for (const t of this.floatingTexts) {
      gsap.killTweensOf(t);
      t.parent?.removeChild(t);
      t.destroy();
    }
    this.floatingTexts = [];
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
        // No tween — movement is handled by GameRenderer.lerpPositions()
        // (matching Unity's ActorController.Update() constant-speed lerp).
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
  private animateAttackGround(_node: Container, event: GameEvent, tl: gsap.core.Timeline): void {
    const scaleRoot = this.renderer.getEntityScaleRoot(event.entityGuid);
    if (!scaleRoot) return;
    const label = `atkground-${event.entityGuid}`;
    tl.to(scaleRoot.scale, {
      x: 1.3,
      y: 1.3,
      duration: 0.06,
      ease: 'power2.in',
    }, label);
    tl.to(scaleRoot.scale, {
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
    // Start at bump impact time so damage overlaps the attack hit
    const pos = BUMP_IMPACT_TIME;

    if (visual) {
      const origTint = visual.tint;
      tl.to(visual, {
        tint: 0xff3333,
        duration: 0.05,
        onComplete: () => { visual.tint = origTint; },
      }, pos);
    }

    // Shake the whole node — relative tween so it's independent of setup-time position changes
    tl.to(node.position, {
      x: '+=3',
      duration: 0.03,
      yoyo: true,
      repeat: 2,
    }, pos);

    if (event.amount !== undefined && event.amount > 0) {
      this.spawnFloatingText(`-${event.amount}`, event, makeDamageStyle(this.camera.tileSize));
    }
  }

  /** Brief green flash on heal — tint targets the visual Sprite only. */
  private animateHeal(
    visual: Sprite | undefined, event: GameEvent, tl: gsap.core.Timeline,
  ): void {
    if (!visual) return;
    const pos = BUMP_IMPACT_TIME;
    const origTint = visual.tint;
    tl.to(visual, {
      tint: 0x33ff66,
      duration: 0.08,
      onComplete: () => { visual.tint = origTint; },
    }, pos);

    if (event.amount !== undefined && event.amount > 0) {
      this.spawnFloatingText(`+${event.amount}`, event, makeHealStyle(this.camera.tileSize));
    }
  }

  /**
   * Fade + shrink on death — matches Unity FadeThenDestroy.cs:
   * fadeTime=0.5, shrink=0.5, linear easing.
   * Targets scaleRoot (center-pivoted inner Container) so shrink animates from tile center.
   */
  private animateDeath(_node: Container, event: GameEvent, tl: gsap.core.Timeline): void {
    const scaleRoot = this.renderer.getEntityScaleRoot(event.entityGuid);
    if (!scaleRoot) return;
    const pos = BUMP_IMPACT_TIME;
    tl.to(scaleRoot, { alpha: 0, duration: 0.5, ease: 'none' }, pos);
    tl.to(scaleRoot.scale, { x: 0.5, y: 0.5, duration: 0.5, ease: 'none' }, pos);
  }

  /** Pop in from zero scale on spawn — targets scaleRoot for center-pivot animation. */
  private animateSpawn(_node: Container, event: GameEvent, tl: gsap.core.Timeline): void {
    const scaleRoot = this.renderer.getEntityScaleRoot(event.entityGuid);
    if (!scaleRoot) return;
    scaleRoot.scale.set(0, 0);
    scaleRoot.alpha = 0;
    tl.to(scaleRoot.scale, {
      x: 1,
      y: 1,
      duration: 0.2,
      ease: 'back.out(1.7)',
    }, '<');
    tl.to(scaleRoot, {
      alpha: 1,
      duration: 0.15,
    }, '<');
  }

  /**
   * Spawn floating HP change text matching Unity's "HP Change Text" prefab animation.
   * Runs independently (not on master timeline) so it doesn't block turn processing.
   * Total duration: 2s. Y: power2.out over full 2s (immediate pop, smooth deceleration).
   * Alpha: slow fade (0→1.083s) then fast fade (1.083→2s).
   * RandomizeStartPosition: x ±0.3 tiles, y ±0.1 tiles.
   */
  private spawnFloatingText(
    text: string, event: GameEvent, style: TextStyle,
  ): void {
    if (!event.to && !event.from) return;
    const pos = event.to ?? event.from!;
    const ts = this.camera.tileSize;
    const px = this.camera.tileToCenterPixel(pos);

    // RandomizeStartPosition: Random.insideUnitSphere scaled by amount {x:0.3, y:0.1}
    const randX = (Math.random() * 2 - 1) * 0.3 * ts;
    const randY = (Math.random() * 2 - 1) * 0.1 * ts;

    const t = new Text({ text, style });
    t.anchor.set(0.5, 0.5);
    // Unity anim: y starts at -0.1 (0.1 tiles below center); PixiJS Y-down so +0.1*ts
    t.position.set(px.x + randX, px.y + 0.1 * ts + randY);

    const layer = this.renderer.getEffectLayer();
    layer.addChild(t);
    this.floatingTexts.push(t);

    const cleanup = () => {
      const idx = this.floatingTexts.indexOf(t);
      if (idx >= 0) this.floatingTexts.splice(idx, 1);
      t.parent?.removeChild(t);
      t.destroy();
    };

    // Y: single ease-out over 2s. Immediate pop (no pause), smooth deceleration into drift.
    // power2.out matches Unity's quick-rise-then-slow-drift feel without the phase-boundary jerk.
    gsap.to(t, { y: px.y - 0.4 * ts + randY, duration: 2, ease: 'power2.out' });
    // Alpha: stays near full opacity, then fades. Two phases match Unity .anim keyframes.
    // Phase 1 (0→1.083s): 1→0.749, accelerating fade → power2.in
    gsap.to(t, { alpha: 0.749, duration: 1.083, ease: 'power2.in' });
    // Phase 2 (1.083→2s): 0.749→0, decelerating fade → power2.out
    gsap.to(t, { alpha: 0, duration: 0.917, delay: 1.083, ease: 'power2.out', onComplete: cleanup });
  }
}
