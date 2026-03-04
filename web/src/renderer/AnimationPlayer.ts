import gsap from 'gsap';
import { Container, Graphics, Sprite, Text, TextStyle, Texture } from 'pixi.js';
import { Vector2Int } from '../core/Vector2Int';
import { Camera } from './Camera';
import { GameRenderer } from './GameRenderer';
import type { SoundManager } from '../audio/SoundManager';

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

function makeNeutralStyle(tileSize: number): TextStyle {
  const sz = Math.round(tileSize * HP_TEXT_FONT_SCALE);
  return new TextStyle({
    fontFamily: 'CodersCrux, monospace',
    fontSize: sz,
    fill: 0xCCCCCC,
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
  type: 'move' | 'jump' | 'attack' | 'attackGround' | 'damage' | 'heal' | 'death' | 'spawn' | 'pulse' | 'struggle' | 'wait' | 'attackGroundHit' | 'disperse' | 'explosion';
  entityGuid: string;
  from?: Vector2Int;
  to?: Vector2Int;
  targetGuid?: string;
  amount?: number;
  isBoss?: boolean;
  pulseScale?: number;
}

/**
 * Plays animations for game events using GSAP tweens on PixiJS sprites.
 * Each event type maps to a specific tween pattern.
 */
export class AnimationPlayer {
  private renderer: GameRenderer;
  private camera: Camera;
  private playerGuid: string;
  private sound: SoundManager | null;
  private timeline: gsap.core.Timeline | null = null;
  private floatingTexts: Text[] = [];

  constructor(renderer: GameRenderer, camera: Camera, playerGuid: string, sound: SoundManager | null = null) {
    this.renderer = renderer;
    this.camera = camera;
    this.playerGuid = playerGuid;
    this.sound = sound;
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
        if (event.entityGuid === this.playerGuid && this.sound) {
          const s = this.sound;
          tl.call(() => s.play('move', 0.25), [], '<');
        }
        break;
      case 'jump':
        this.animateJump(node, event, tl);
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
      case 'pulse':
        this.animatePulse(event, tl);
        break;
      case 'struggle':
        this.animateStruggle(node, event, tl);
        break;
      case 'wait':
        this.animatePlayerWait(event);
        break;
      case 'attackGroundHit':
        this.animateAttackGroundHit(event, tl);
        break;
  /**
   * Parabolic arc jump animation.
   * Unity PlayJumpAnimation: duration 0.5s, H = D * 0.5, quadratic arc a=-4H/D², b=4H/D.
   * PixiJS Y-down: subtract arc offset to move upward on screen.
   */
  private animateJump(node: Container, event: GameEvent, tl: gsap.core.Timeline): void {
    if (!event.from || !event.to) return;
    const fromPx = this.camera.tileToPixel(event.from);
    const toPx = this.camera.tileToPixel(event.to);
    const D = Math.sqrt((toPx.x - fromPx.x) ** 2 + (toPx.y - fromPx.y) ** 2);
    if (D < 0.5) return;
    const H = D * 0.5;
    const a = -4 * H / (D * D);
    const b = 4 * H / D;
    const progress = { tNorm: 0 };
    tl.to(progress, {
      tNorm: 1,
      duration: 0.5,
      ease: 'sine.inOut',
      onUpdate: () => {
        const t = progress.tNorm * D;
        const arcY = a * t * t + b * t;
        const wx = fromPx.x + (toPx.x - fromPx.x) * progress.tNorm;
        const wy = fromPx.y + (toPx.y - fromPx.y) * progress.tNorm;
        node.position.set(wx, wy - arcY);
      },
      onComplete: () => {
        node.position.set(toPx.x, toPx.y);
      },
    }, `jump-${event.entityGuid}`);
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

    if (event.entityGuid === this.playerGuid && this.sound) {
      const s = this.sound;
      const key = (event.amount !== undefined && event.amount > 0) ? 'attack' as const : 'attackNoDamage' as const;
      tl.call(() => s.play(key), [], `<+=${BUMP_IMPACT_TIME}`);
    }
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

    if (event.amount !== 0) {
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

      if (event.entityGuid === this.playerGuid && this.sound) {
        const s = this.sound;
        tl.call(() => s.playHurt(), [], pos);
      }
    }

    if (event.amount !== undefined && event.amount > 0) {
      this.spawnFloatingText(`-${event.amount}`, event, makeDamageStyle(this.camera.tileSize));
    } else if (event.amount === 0) {
      this.spawnFloatingText('0', event, makeNeutralStyle(this.camera.tileSize));
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

    if (event.entityGuid === this.playerGuid && this.sound) {
      const s = this.sound;
      tl.call(() => s.play('playerHeal'), [], pos);
    }

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

    if (event.entityGuid !== this.playerGuid && this.sound) {
      const s = this.sound;
      if (event.isBoss) {
        tl.call(() => s.play('bossDeath'), [], pos);
      } else {
        tl.call(() => s.play('death', 1, true), [], pos);
      }
    }
  }

  /** Pop in from zero scale on spawn — targets scaleRoot for center-pivot animation. */
  private animateSpawn(_node: Container, event: GameEvent, tl: gsap.core.Timeline): void {
    const scaleRoot = this.renderer.getEntityScaleRoot(event.entityGuid);
    if (!scaleRoot) return;
    scaleRoot.scale.set(0, 0);
    scaleRoot.alpha = 0;
    if (this.sound) {
      const s = this.sound;
      tl.call(() => s.play('summon'), [], '<');
    }
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
   * Shrink/grow pulse — Unity PulseAnimation.cs: 0.33s, easing cos(t*PI)^4 from 1→pulseScale→1.
   * Default pulseScale 0.75 (shrink). Values >1 give a grow pulse.
   */
  private animatePulse(event: GameEvent, tl: gsap.core.Timeline): void {
    const scaleRoot = this.renderer.getEntityScaleRoot(event.entityGuid);
    if (!scaleRoot) return;
    const pulseScale = event.pulseScale ?? 0.75;
    const progress = { t: 0 };
    tl.to(progress, {
      t: 1,
      duration: 0.33,
      ease: 'none',
      onUpdate: () => {
        const cosVal = Math.pow(Math.cos(progress.t * Math.PI), 4);
        const s = 1 + (pulseScale - 1) * (1 - cosVal);
        scaleRoot.scale.set(s, s);
      },
      onComplete: () => { scaleRoot.scale.set(1, 1); },
    }, '<');
  }

  /**
   * X-axis shake matching Struggle.anim keyframes (tile units):
   * 0→−0.2, 0.083→+0.2, 0.167→−0.2, 0.25→+0.2, 0.333→−0.2, 0.417→+0.2, 0.5→−0.1, 0.583→0
   */
  private animateStruggle(node: Container, event: GameEvent, tl: gsap.core.Timeline): void {
    if (!event.from) return;
    const px = this.camera.tileToPixel(event.from);
    const d = 0.2 * this.camera.tileSize;
    const label = `struggle-${event.entityGuid}`;
    tl.set(node.position, { x: px.x - d }, label);
    tl.to(node.position, { x: px.x + d, duration: 0.083, ease: 'none' }, '>');
    tl.to(node.position, { x: px.x - d, duration: 0.083, ease: 'none' }, '>');
    tl.to(node.position, { x: px.x + d, duration: 0.083, ease: 'none' }, '>');
    tl.to(node.position, { x: px.x - d, duration: 0.083, ease: 'none' }, '>');
    tl.to(node.position, { x: px.x + d, duration: 0.083, ease: 'none' }, '>');
    tl.to(node.position, { x: px.x - d * 0.5, duration: 0.083, ease: 'none' }, '>');
    tl.to(node.position, { x: px.x, duration: 0.083, ease: 'none', onComplete: () => { node.position.x = px.x; } }, '>');
  }

  /**
   * Floating clock sprite for player wait — Unity Wait.prefab FadeUp animation.
   * Clock rises 0.1 tiles, alpha 1→0 over 0.5s from player.pos.y + 0.9 tiles.
   * Runs independently (not on master timeline).
   */
  private animatePlayerWait(event: GameEvent): void {
    if (!event.from) return;
    const ts = this.camera.tileSize;
    const px = this.camera.tileToCenterPixel(event.from);
    const tex = this.renderer.sprites.getTextureByKey('clock') ?? Texture.WHITE;
    const sprite = new Sprite(tex);
    sprite.anchor.set(0.5, 0.5);
    sprite.width = ts;
    sprite.height = ts;
    sprite.tint = 0xCFC6B8;
    sprite.position.set(px.x, px.y - 0.9 * ts);

    const layer = this.renderer.getEffectLayer();
    layer.addChild(sprite);

    const cleanup = () => {
      sprite.parent?.removeChild(sprite);
      sprite.destroy();
    };

    gsap.to(sprite, { y: sprite.y - 0.1 * ts, alpha: 0, duration: 0.5, ease: 'linear', onComplete: cleanup });
  }
  /**
   * Attack Sprite hit animation — Unity Attack Sprite.prefab Swipe.anim (0.717s).
   * Spawned at target tile. Y: +0.2→−0.12 tiles, scale: 0→1.2→1→0.
   */
  private animateAttackGroundHit(event: GameEvent, tl: gsap.core.Timeline): void {
    if (!event.to) return;
    const ts = this.camera.tileSize;
    const px = this.camera.tileToCenterPixel(event.to);
    const tex = this.renderer.sprites.getTextureByKey('colored_transparent_packed_553') ?? Texture.WHITE;
    const sprite = new Sprite(tex);
    sprite.anchor.set(0.5, 0.5);
    sprite.width = ts;
    sprite.height = ts;
    sprite.scale.set(0, 0);
    sprite.position.set(px.x, px.y + 0.2 * ts);

    const layer = this.renderer.getEffectLayer();
    layer.addChild(sprite);

    const cleanup = () => {
      sprite.parent?.removeChild(sprite);
      sprite.destroy();
    };

    const label = `atkgroundhit-${event.entityGuid}`;
    // Scale up 0→1.2 over first ~35%, 1.2→1 middle, 1→0 end; Y moves 0.2→-0.12 total
    tl.to(sprite.scale, { x: 1.2, y: 1.2, duration: 0.25, ease: 'power2.out' }, label);
    tl.to(sprite, { y: px.y - 0.12 * ts, duration: 0.717, ease: 'power2.out' }, label);
    tl.to(sprite.scale, { x: 1, y: 1, duration: 0.1, ease: 'none' }, '>');
    tl.to(sprite.scale, { x: 0, y: 0, duration: 0.367, ease: 'power2.in', onComplete: cleanup }, '>');
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
