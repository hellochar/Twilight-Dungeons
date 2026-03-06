import gsap from 'gsap';
import { Container, Graphics, Sprite, Text, TextStyle, Texture } from 'pixi.js';
import { Vector2Int } from '../core/Vector2Int';
import { Camera } from './Camera';
import { GameRenderer } from './GameRenderer';
import type { SoundManager } from '../audio/SoundManager';
import { FONT_FAMILY } from '../ui/fonts';
import { BUMP_DURATION, BUMP_INTENSITY, BUMP_IMPACT_TIME, MOVE_LERP_S, DEATH_FADE_S, DAMAGE_FLASH_S, DAMAGE_TEXT_FADE_S, MOVE_SFX_VOLUME } from '../constants';

// CodersCrux cap height ≈ 65% of point size; scale up so glyphs appear ~1 tile tall
const HP_TEXT_FONT_SCALE = 1.00;

function makeDamageStyle(tileSize: number): TextStyle {
  const sz = Math.round(tileSize * HP_TEXT_FONT_SCALE);
  return new TextStyle({
    fontFamily: FONT_FAMILY,
    fontSize: sz,
    fill: 0xff3333,
    stroke: { color: 0x000000, width: Math.max(2, Math.round(sz * 0.12)) },
  });
}

function makeHealStyle(tileSize: number): TextStyle {
  const sz = Math.round(tileSize * HP_TEXT_FONT_SCALE);
  return new TextStyle({
    fontFamily: FONT_FAMILY,
    fontSize: sz,
    fill: 0x33ff66,
    stroke: { color: 0x000000, width: Math.max(2, Math.round(sz * 0.12)) },
  });
}

function makeNeutralStyle(tileSize: number): TextStyle {
  const sz = Math.round(tileSize * HP_TEXT_FONT_SCALE);
  return new TextStyle({
    fontFamily: FONT_FAMILY,
    fontSize: sz,
    fill: 0xCCCCCC,
    stroke: { color: 0x000000, width: Math.max(2, Math.round(sz * 0.12)) },
  });
}

// BumpAndReturn constants and MOVE_LERP_S imported from constants.ts

/** Unity's exact parabolic bump-and-return easing: pow(cos(PI/2 + PI*sqrt(t)), 4) * 0.75 */
function bumpAndReturnEasing(t: number): number {
  return Math.pow(Math.cos(Math.PI / 2 + Math.PI * Math.sqrt(t)), 4) * BUMP_INTENSITY;
}

/** Describes an event that happened during a turn step, for animation. */
export interface GameEvent {
  type: 'move' | 'jump' | 'attack' | 'attackGround' | 'damage' | 'heal' | 'death' | 'squishDeath' | 'quickDeath' | 'spawn' | 'pulse' | 'struggle' | 'wait' | 'attackGroundHit' | 'disperse' | 'explosion' | 'spray';
  entityGuid: string;
  from?: Vector2Int;
  to?: Vector2Int;
  targetGuid?: string;
  amount?: number;
  isBoss?: boolean;
  pulseScale?: number;
  color?: number;
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
      this.addEventToTimeline(event, this.timeline, events);
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
      this.addEventToTimeline(event, this.timeline, events);
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

  private addEventToTimeline(event: GameEvent, tl: gsap.core.Timeline, batch: GameEvent[] = []): void {
    // Position-based effects don't need the entity sprite (which may already be gone).
    if (event.type === 'disperse') { this.animateParticleBurst(tl, event.from!); return; }
    if (event.type === 'spray') { this.animateParticleBurst(tl, event.from!, { color: event.color, speed: 6.5 }); return; }

    const node = this.renderer.getEntitySprite(event.entityGuid);
    if (!node) return;
    const visual = this.renderer.getEntityVisual(event.entityGuid);

    // When an entity moves and dies in the same step, lerpPositions skips it (dead entities
    // are excluded). We detect this and add a GSAP tween for the move, then delay the
    // death animation so the creature visually arrives at the new tile before dying.
    const entityMovedAndDies = (guid: string) =>
      batch.some(e => e.type === 'move' && e.entityGuid === guid) &&
      batch.some(e => e.entityGuid === guid && (e.type === 'death' || e.type === 'squishDeath'));

    switch (event.type) {
      case 'move': {
        // Normal case: movement driven by GameRenderer.lerpPositions() (matching Unity).
        // Special case: if this entity dies in the same batch, lerpPositions won't run (it
        // skips dead entities), so we add a GSAP tween to animate the position directly.
        if (entityMovedAndDies(event.entityGuid) && event.from && event.to) {
          const fromPx = this.camera.tileToPixel(event.from);
          const toPx = this.camera.tileToPixel(event.to);
          node.position.set(fromPx.x, fromPx.y);
          tl.to(node.position, { x: toPx.x, y: toPx.y, duration: MOVE_LERP_S, ease: 'none' }, 0);
        }
        if (event.entityGuid === this.playerGuid && this.sound) {
          const s = this.sound;
          tl.call(() => s.play('move', MOVE_SFX_VOLUME), [], '<');
        }
        break;
      }
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
        if (batch.some(e => e.entityGuid === event.entityGuid && (e.type === 'death' || e.type === 'squishDeath'))) {
          // Skip visual effects (flash, shake) for dying entities, but still show floating text
          this.spawnDamageText(event);
        } else {
          this.animateDamage(node, visual, event, tl);
        }
        break;
      case 'heal':
        this.animateHeal(visual, event, tl);
        break;
      case 'death':
        this.animateDeath(node, event, tl, entityMovedAndDies(event.entityGuid) ? MOVE_LERP_S : BUMP_IMPACT_TIME);
        break;
      case 'squishDeath':
        this.animateSquishDeath(event, tl, entityMovedAndDies(event.entityGuid) ? MOVE_LERP_S : BUMP_IMPACT_TIME);
        break;
      case 'quickDeath':
        this.animateQuickDeath(event, tl);
        break;
      case 'spawn':
        this.animateSpawn(node, event, tl);
        break;
      case 'pulse':
        if (!batch.some(e => e.entityGuid === event.entityGuid && (e.type === 'death' || e.type === 'squishDeath' || e.type === 'quickDeath'))) {
          this.animatePulse(event, tl);
        }
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
      case 'explosion':
        this.animateExplosion(event, tl);
        break;
    }
  }

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

  /**
   * Bump toward target then return — same BumpAndReturn as melee attack.
   * Unity ActorController.HandleActionPerformed: AttackGroundBaseAction calls PlayAttackAnimation(targetPosition).
   */
  private animateAttackGround(node: Container, event: GameEvent, tl: gsap.core.Timeline): void {
    if (!event.from || !event.to) return;
    const fromPx = this.camera.tileToPixel(event.from);
    const toPx = this.camera.tileToPixel(event.to);

    const dx = (toPx.x - fromPx.x) * BUMP_INTENSITY;
    const dy = (toPx.y - fromPx.y) * BUMP_INTENSITY;

    const bumpLabel = `atkground-${event.entityGuid}`;
    const impactLabel = `atkground-impact-${event.entityGuid}`;
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
    }, bumpLabel);
    // Label at impact time — used by animateAttackGroundHit to synchronize
    tl.addLabel(impactLabel, `${bumpLabel}+=${BUMP_IMPACT_TIME}`);
    // Hide line+reticle at impact
    tl.call(() => this.renderer.hideAttackGroundEffect(event.entityGuid), [], impactLabel);
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
          duration: DAMAGE_FLASH_S,
          onComplete: () => { visual.tint = origTint; },
        }, pos);
      }

      // Shake the whole node — relative tween so it's independent of setup-time position changes
      tl.to(node.position, {
        x: '+=30',
        duration: 0.1,
        yoyo: true,
        repeat: 2,
      }, pos);

      if (event.entityGuid === this.playerGuid && this.sound) {
        const s = this.sound;
        tl.call(() => s.playHurt(), [], pos);
      }
    }

    this.spawnDamageText(event);
  }

  /** Spawn floating damage/block text without any visual effects on the sprite. */
  private spawnDamageText(event: GameEvent): void {
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
   * Fade + shrink on death — based on Unity FadeThenDestroy.cs.
   * Targets scaleRoot (center-pivoted inner Container) so shrink animates from tile center.
   */
  private animateDeath(_node: Container, event: GameEvent, tl: gsap.core.Timeline, startTime = BUMP_IMPACT_TIME): void {
    const scaleRoot = this.renderer.getEntityScaleRoot(event.entityGuid);
    if (!scaleRoot) return;
    const pos = startTime;
    tl.to(scaleRoot, { alpha: 0, duration: DEATH_FADE_S, ease: 'power3.out' }, pos);
    tl.to(scaleRoot.scale, { x: 0.5, y: 0.5, duration: DEATH_FADE_S, ease: 'power3.out' }, pos);

    if (event.entityGuid !== this.playerGuid && this.sound) {
      const s = this.sound;
      if (event.isBoss) {
        tl.call(() => s.play('bossDeath'), [], pos);
      } else {
        tl.call(() => s.play('death', 1, true), [], pos);
      }
    }
  }

  /**
   * Squish-to-flat death: scaleY 1→0 pinned at tile bottom over 0.2s.
   * Used for Skully → Muck transform.
   * Keeps center pivot; position.y is derived from scale.y each frame to pin the bottom edge.
   * Formula: position.y = ts - (ts/2) * scale.y  (ts/2 at scale=1, ts at scale=0).
   */
  private animateSquishDeath(event: GameEvent, tl: gsap.core.Timeline, startTime = BUMP_IMPACT_TIME): void {
    this.renderer.disableEntityBob(event.entityGuid);
    const scaleRoot = this.renderer.getEntityScaleRoot(event.entityGuid);
    if (!scaleRoot) return;
    const ts = this.camera.tileSize;
    const pos = startTime;
    tl.to(scaleRoot.scale, {
      y: 0.1,
      duration: 0.2,
      ease: 'power1.out',
      onUpdate: () => { scaleRoot.position.y = ts - (ts / 2) * scaleRoot.scale.y; },
      // onComplete: () => { scaleRoot.position.y = ts; },
    }, pos);
    if (this.sound) {
      const s = this.sound;
      tl.call(() => s.play('death', 1, true), [], pos);
    }
  }

  /** Muck → Skully transform: instant (0.1s) fade-out. Stops vibrate immediately. */
  private animateQuickDeath(event: GameEvent, tl: gsap.core.Timeline): void {
    this.renderer.disableEntityVibrate(event.entityGuid);
    const scaleRoot = this.renderer.getEntityScaleRoot(event.entityGuid);
    if (!scaleRoot) return;
    // Suppress FadeThenDestroy — we handle cleanup ourselves on complete.
    this.renderer.suppressEntityFade(event.entityGuid);
    const guid = event.entityGuid;
    tl.to(scaleRoot, {
      alpha: 0,
      duration: 0.1,
      ease: 'power1.out',
      onComplete: () => this.renderer.destroyEntityState(guid),
    }, 0);
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

    gsap.to(sprite, { y: sprite.y - 0.1 * ts, alpha: 0, duration: DAMAGE_TEXT_FADE_S, ease: 'linear', onComplete: cleanup });
  }

  /**
   * Reusable radial particle burst. Defaults match Llaora's cyan poof.
   * Motion: deceleration curve dist(t) = speed*(1-(1-t)^6)/6.
   * Alpha: holds 1 until t=0.69, then fades to 0.
   */
  private animateParticleBurst(
    tl: gsap.core.Timeline,
    from: Vector2Int,
    opts?: { color?: number; count?: number; speed?: number; lifetime?: number },
  ): void {
    const ts = this.camera.tileSize;
    const px = this.camera.tileToCenterPixel(from);

    const P_RADIUS_BASE = 0.05 * ts;
    const BASE_SPEED = (opts?.speed ?? 12.0) * ts;
    const COLOR = opts?.color ?? 0x00acd8;
    const COUNT = opts?.count ?? 120;
    const LIFETIME = opts?.lifetime ?? 1.0;

    const container = new Container();
    container.position.set(px.x, px.y);
    this.renderer.getEffectLayer().addChild(container);

    const particles: { g: Graphics; angle: number; speed: number }[] = [];
    for (let i = 0; i < COUNT; i++) {
      const angle = Math.random() * Math.PI * 2;
      const speed = BASE_SPEED * (0.5 + Math.random() * 0.5);
      const g = new Graphics();
      g.circle(0, 0, P_RADIUS_BASE).fill({ color: COLOR });
      g.position.set(0, 0);
      container.addChild(g);
      particles.push({ g, angle, speed });
    }

    const progress = { t: 0 };
    tl.to(progress, {
      t: 1,
      duration: LIFETIME,
      ease: 'none',
      onUpdate: () => {
        const t = progress.t;
        const alpha = t < 0.69 ? 1 : (1 - t) / 0.31;
        for (const p of particles) {
          const d = p.speed * (1 - Math.pow(1 - t, 6)) / 6;
          p.g.position.set(Math.cos(p.angle) * d, Math.sin(p.angle) * d);
          p.g.alpha = alpha;
        }
      },
      onComplete: () => {
        container.parent?.removeChild(container);
        container.destroy({ children: true });
      },
    }, '<');
  }

  /**
   * 6-frame explosion spritesheet animation at 3×3 tile scale.
   * Port of Unity Boombug Explosion.prefab: scale (3,3,1), Animator cycles explosion_0..5.
   * Frame duration ~12fps so total ~0.5s.
   */
  private animateExplosion(event: GameEvent, tl: gsap.core.Timeline): void {
    if (!event.from) return;
    // Unity Boombug Explosion.prefab: AudioSource volume 0.5, pitch randomized 0.9–1.111
    this.sound?.play('explosion', 0.5);
    const ts = this.camera.tileSize;
    const px = this.camera.tileToPixel(event.from);
    const frames = this.renderer.sprites.getFrames('explosion');
    if (!frames || frames.length === 0) return;

    const s = new Sprite(frames[0]);
    s.width = 3 * ts;
    s.height = 3 * ts;
    s.position.set(px.x - ts, px.y - ts);
    this.renderer.getEffectLayer().addChild(s);

    const FRAME_DURATION = 0.083; // ~12fps
    const progress = { t: 0 };
    tl.to(progress, {
      t: 1,
      duration: FRAME_DURATION * frames.length,
      ease: 'none',
      onUpdate: () => {
        const idx = Math.min(Math.floor(progress.t * frames.length), frames.length - 1);
        s.texture = frames[idx];
      },
      onComplete: () => {
        s.parent?.removeChild(s);
        s.destroy();
      },
    }, '<');
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
    sprite.width = 1;
    sprite.height = 1;
    sprite.position.set(px.x, px.y + 0.1 * ts);

    const layer = this.renderer.getEffectLayer();
    layer.addChild(sprite);

    const cleanup = () => {
      sprite.parent?.removeChild(sprite);
      sprite.destroy();
    };

    // Run the full 0.717s swipe animation on a standalone timeline so it doesn't block
    // the main animation chain. The main timeline only reserves 0.25s for this effect.
    const impactLabel = `atkground-impact-${event.entityGuid}`;
    const standalone = gsap.timeline();
    standalone.to(sprite, { width: ts * 0.9, height: ts * 0.9, duration: 0.2, ease: 'power2.out' }, 0);
    standalone.to(sprite, { y: px.y - 0.12 * ts, duration: 0.45, ease: 'power3.out' }, 0);
    standalone.to(sprite, { width: 0, height: 0, duration: 0.27, ease: 'power2.in', onComplete: cleanup }, 0.5);

    // Reserve only 0.25s on the main timeline so the chain continues while the effect plays
    tl.call(() => { standalone.play(0); }, [], impactLabel);
    tl.set({}, {}, `${impactLabel}+=0.25`);
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
