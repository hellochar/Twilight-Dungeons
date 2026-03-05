import { Application, Container, Sprite, Graphics, Texture } from 'pixi.js';
import { Floor } from '../model/Floor';
import { Entity } from '../model/Entity';
import { Vector2Int } from '../core/Vector2Int';
import { Camera, isMobile } from './Camera';
import { SpriteManager } from './SpriteManager';
import { SPRITE_TINTS, SPRITE_ALPHAS } from './spriteTints';
import { TelegraphedTask } from '../model/tasks/TelegraphedTask';
import { AttackGroundTask } from '../model/tasks/AttackGroundTask';
import { ExplodeTask } from '../model/tasks/ExplodeTask';
import { RunAwayTask } from '../model/tasks/RunAwayTask';
import { WaitTask } from '../model/tasks/WaitTask';
import { AttackBaseAction, AttackGroundBaseAction } from '../model/BaseAction';
import { Actor } from '../model/Actor';
import {
  type EntityRenderState,
  type RenderCtx,
  getEntityRenderHooks,
} from './entityRenderers';
import { TileRenderer } from './TileRenderer';
import { HpLabelRenderer } from './HpLabelRenderer';
import {
  DEEP_SLEEP_TINT,
  SPAWN_ANIMATION_DURATION,
  FADE_DURATION_MS,
  FADE_END_SCALE,
  TELEGRAPH_FADE_DURATION,
} from './animationConstants';

/**
 * Grasses whose Unity prefab overrides Shadow rotation to (0,0,0) — flat shadow
 * instead of the default angled (60°,20°,0°). Matched by displayName lowercase.
 */
const FLAT_SHADOW_ENTITIES = new Set([
  'blob slime', 'bloodwort', 'cheshire weed sprout', 'coralmoss',
  'dandypuff', 'necroroot', 'poisonmoss', 'vibrant ivy', 'web',
]);

/**
 * Per-status visual config matching Unity prefab positioning.
 * Only statuses with prefabs in Assets/Prefabs/Resources/Statuses/ get visuals.
 */
interface StatusVisualConfig {
  spriteKey: string;
  /** X offset in tile units from entity center. */
  offsetX: number;
  /** Y offset in tile units from entity center (Unity Y-up convention). */
  offsetY: number;
  /** Scale relative to 1 tile. */
  scale: number;
  /** Hide when actor is sleeping. */
  hideWhenSleeping?: boolean;
  /** Optional tint color (Unity SpriteRenderer.color). */
  tint?: number;
  /** Second sprite for dual-sprite prefabs (e.g. ConstrictedStatus). */
  secondSprite?: { offsetX: number; offsetY: number; flipX?: boolean };
}

/**
 * Status visuals from Unity prefabs. Each status has individual positioning —
 * some render overhead, some on body, some below. No generic row layout.
 */
const STATUS_VISUALS: Record<string, StatusVisualConfig> = {
  // Overhead sprite icons
  PoisonedStatus:  { spriteKey: 'poisoned-status', offsetX: 0, offsetY: 0.65, scale: 0.75 },
  CharmedStatus:   { spriteKey: 'charmed',         offsetX: 0, offsetY: 0.65, scale: 0.75 },
  ConfusedStatus:  { spriteKey: 'confused',         offsetX: 0, offsetY: 0.65, scale: 0.75 },
  SurprisedStatus: { spriteKey: 'colored_transparent_packed_658', offsetX: 0, offsetY: 0.65, scale: 0.75 },
  PacifiedStatus:  { spriteKey: 'peace',            offsetX: 0, offsetY: 0.5,  scale: 0.4 },
  DandyStatus:     { spriteKey: 'dandypuff',        offsetX: 0, offsetY: 0.4,  scale: 0.75 },
  // Body-level sprites
  WebbedStatus:    { spriteKey: 'web-bottom',       offsetX: 0, offsetY: 0,    scale: 1.0 },
  ParasiteStatus:  { spriteKey: 'parasite',         offsetX: 0, offsetY: 0,    scale: 0.35 },
  // Particle-based (static sprite fallback — TODO: implement PixiJS particles)
  SlimedStatus:    { spriteKey: 'slimed',           offsetX: 0, offsetY: 0,     scale: 1.0 },
  VulnerableStatus:{ spriteKey: 'vulnerable',       offsetX: 0, offsetY: 0,     scale: 0.5 },
  WeaknessStatus:  { spriteKey: 'weakness',         offsetX: 0, offsetY: 0.5,   scale: 1.0 },
  // FreeMoveStatus: particles-only at y=-0.415 (ground level, hidden under entity) → status bar only
  // Sprint 14
  SporedStatus:    { spriteKey: 'spored-status',    offsetX: 0, offsetY: 0,     scale: 0.75 },
  ConstrictedStatus: { spriteKey: 'constricted-particle', offsetX: 0, offsetY: 0, scale: 1.0, tint: 0xC8A92A, secondSprite: { offsetX: -0.039, offsetY: -0.059, flipX: true } },
  // FrenziedStatus: no Unity status prefab exists → status bar only
  // Sprint 15
  InfectedStatus:  { spriteKey: 'infected',         offsetX: 0, offsetY: 0,     scale: 0.5 },
  ThirdEyeStatus:  { spriteKey: 'third-eye',        offsetX: 0, offsetY: 0.65,  scale: 0.75 },
};

/** SleepTask visual (from Assets/Prefabs/Resources/Tasks/SleepTask.prefab). */
const SLEEP_VISUAL: StatusVisualConfig = {
  spriteKey: 'sleep', offsetX: 0, offsetY: 0.5, scale: 0.5,
};

/** RunAwayTask visual (from Assets/Prefabs/Resources/Tasks/RunAwayTask.prefab). */
const RUN_AWAY_VISUAL: StatusVisualConfig = {
  spriteKey: 'colored_transparent_packed_659', offsetX: 0, offsetY: 0.5, scale: 0.5,
};
const RUN_AWAY_TINT = 0xFF4000;

/** WaitTask visual (from Assets/Prefabs/Resources/Tasks/WaitTask.prefab). */
const WAIT_TASK_VISUAL: StatusVisualConfig = {
  spriteKey: 'clock', offsetX: 0, offsetY: 0.5, scale: 0.5,
};
const WAIT_TASK_TINT = 0xCFC6B8;

/** Enemies with hideWaitTask=true in Unity — do NOT show the clock icon. */
const HIDE_WAIT_TASK_NAMES = new Set(['Leecher', 'FungalColony', 'FruitingBody', 'Crab']);

/**
 * PixiJS-based renderer for the game floor.
 * Layered containers: tiles → grasses → items → bodies → above-entity → effects → fog.
 * Tile rendering is delegated to TileRenderer.
 */
export class GameRenderer {
  readonly app: Application;
  readonly camera: Camera;
  readonly sprites: SpriteManager;

  private tileRenderer: TileRenderer;

  // Entity layer containers (back to front, between tile and fog layers)
  private grassLayer = new Container();
  private itemLayer = new Container();
  private attackGroundLayer = new Container();
  private bodyLayer = new Container();
  private aboveEntityLayer = new Container();
  private effectLayer = new Container();
  private hpLabelRenderer: HpLabelRenderer;

  // Active targeting highlights on the effect layer
  private targetHighlights: Graphics[] = [];
  // Proposed path dots (PathDot sprites) and reticle — FollowPathUI port
  private pathDotSprites: Sprite[] = [];
  private reticleSprite: Sprite | null = null;

  // Entity guids currently mid-animation — skip position snapping for these
  readonly animatingGuids = new Set<string>();

  // All entity render state, keyed by guid.
  // Active entities: state with no `fade`.
  // Fading entities: state with `fade` set — removed by updateEntityAnimations when complete.
  private entityStates = new Map<string, EntityRenderState>();

  private floor: Floor | null = null;

  constructor(app: Application, camera: Camera, sprites: SpriteManager) {
    this.app = app;
    this.camera = camera;
    this.sprites = sprites;
    this.tileRenderer = new TileRenderer(camera, sprites);

    // Add layers in draw order
    app.stage.addChild(this.tileRenderer.tileLayer);
    app.stage.addChild(this.grassLayer);
    app.stage.addChild(this.itemLayer);
    app.stage.addChild(this.attackGroundLayer);
    app.stage.addChild(this.bodyLayer);
    app.stage.addChild(this.aboveEntityLayer);
    app.stage.addChild(this.effectLayer);
    this.hpLabelRenderer = new HpLabelRenderer(camera);
    app.stage.addChild(this.hpLabelRenderer.layer);
    app.stage.addChild(this.tileRenderer.dimLayer);
  }

  /** Set the floor to render and do full rebuild. */
  setFloor(floor: Floor): void {
    this.floor = floor;
    this.camera.resize(
      this.app.screen.width,
      this.app.screen.height,
      floor.width,
      floor.height,
      isMobile() ? -0.5 : 0.5,
    );
    this.rebuildAll();
  }

  /** Handle viewport resize. */
  resize(): void {
    if (!this.floor) return;
    this.camera.resize(
      this.app.screen.width,
      this.app.screen.height,
      this.floor.width,
      this.floor.height,
      isMobile() ? -0.5 : 0.5,
    );
    this.rebuildAll();
  }

  /** Full rebuild of all visual elements from model state. */
  rebuildAll(): void {
    if (!this.floor) return;
    this.clearAll();
    this.tileRenderer.build(this.floor);
    this.buildEntities();
    this.tileRenderer.syncVisibility(this.floor);
  }

  /** Sync visuals to current model state (call after each turn step). */
  syncToModel(): void {
    if (!this.floor) return;
    this.tileRenderer.sync(this.floor);
    this.syncEntities();
    this.tileRenderer.syncVisibility(this.floor);
    this.hpLabelRenderer.sync();
  }

  /** Get the entity node Container (for position/scale/alpha animations). */
  getEntitySprite(guid: string): Container | undefined {
    return this.entityStates.get(guid)?.node;
  }

  /** Get the visual Sprite child (for tint animations). */
  getEntityVisual(guid: string): Sprite | undefined {
    return this.entityStates.get(guid)?.visual;
  }

  /** Get the scale/alpha inner Container (center-pivoted; for death/spawn animations). */
  getEntityScaleRoot(guid: string): Container | undefined {
    return this.entityStates.get(guid)?.scaleRoot;
  }

  /** Stop the idle bob for an entity (e.g. during squish death animation). */
  disableEntityBob(guid: string): void {
    const state = this.entityStates.get(guid);
    if (state) state.bob = undefined;
  }

  /** Mark entity to skip FadeThenDestroy (AnimationPlayer will handle cleanup). */
  suppressEntityFade(guid: string): void {
    const state = this.entityStates.get(guid);
    if (state) state.suppressFade = true;
  }

  /** Immediately destroy entity state (called by AnimationPlayer after quickDeath etc.). */
  destroyEntityState(guid: string): void {
    const state = this.entityStates.get(guid);
    if (!state) return;
    state.node.destroy({ children: true });
    if (state.detachedShadow) state.detachedShadow.destroy({ children: true });
    if (state.statusIndicator) state.statusIndicator.destroy();
    this.entityStates.delete(guid);
  }

  /** Stop the vibrate animation and snap position.x back to center. */
  disableEntityVibrate(guid: string): void {
    const state = this.entityStates.get(guid);
    if (!state) return;
    state.vibrate = undefined;
    state.scaleRoot.position.x = this.camera.tileSize / 2;
  }

  /** Get the effect layer container (for animation overlays). */
  getEffectLayer(): Container {
    return this.effectLayer;
  }

  /** Show green highlight rectangles on valid target positions. */
  showTargetHighlights(positions: Vector2Int[]): void {
    this.clearTargetHighlights();
    const ts = this.camera.tileSize;
    for (const pos of positions) {
      const px = this.camera.tileToPixel(pos);
      const g = new Graphics();
      g.rect(0, 0, ts, ts).fill({ color: 0x44ff44, alpha: 0.25 });
      g.rect(0, 0, ts, ts).stroke({ color: 0x44ff44, alpha: 0.6, width: 2 });
      g.position.set(px.x, px.y);
      this.effectLayer.addChild(g);
      this.targetHighlights.push(g);
    }
  }

  /** Remove all target highlight graphics. */
  clearTargetHighlights(): void {
    for (const g of this.targetHighlights) g.destroy();
    this.targetHighlights = [];
  }

  /**
   * Show proposed path dots and reticle for FollowPathUI (two-click move preview).
   * PathDot: 0.32 × tileSize, centered on tile, alpha = 0.5^i.
   * Reticle: full tile at target position.
   */
  showProposedPath(target: Vector2Int, path: Vector2Int[]): void {
    this.clearProposedPath();
    const ts = this.camera.tileSize;
    const dotSize = 0.32 * ts;
    const dotTex = this.sprites.getTexture('path dot');
    for (let i = 0; i < path.length; i++) {
      const px = this.camera.tileToPixel(path[i]);
      const dot = new Sprite(dotTex ?? Texture.WHITE);
      dot.width = dotSize;
      dot.height = dotSize;
      dot.position.set(px.x + ts / 2 - dotSize / 2, px.y + ts / 2 - dotSize / 2);
      dot.alpha = Math.pow(0.5, i);
      this.effectLayer.addChild(dot);
      this.pathDotSprites.push(dot);
    }
    const reticleTex = this.sprites.getTexture('colored_transparent_packed_697');
    const reticle = new Sprite(reticleTex ?? Texture.WHITE);
    const rPx = this.camera.tileToPixel(target);
    reticle.width = ts;
    reticle.height = ts;
    reticle.position.set(rPx.x, rPx.y);
    this.effectLayer.addChild(reticle);
    this.reticleSprite = reticle;
  }

  /** Remove proposed path dots and reticle. */
  clearProposedPath(): void {
    for (const dot of this.pathDotSprites) dot.destroy();
    this.pathDotSprites = [];
    if (this.reticleSprite) { this.reticleSprite.destroy(); this.reticleSprite = null; }
  }

  /**
   * Lerp non-animated entity positions toward their model positions.
   * Call from app.ticker each frame.
   * Matches Unity ActorController.Update() lines 82-89:
   *   speed = 16 / actionCost, snap if distance > 3 tiles.
   */
  lerpPositions(dt: number): void {
    const floor = this.floor;
    if (!floor) return;
    const ts = this.camera.tileSize;

    for (const body of floor.bodies) {
      if (body.isDead) continue;
      if (this.animatingGuids.has(body.guid)) continue;
      const state = this.entityStates.get(body.guid);
      if (!state) continue;
      const node = state.node;

      const target = this.camera.tileToPixel(body.pos);
      const dx = target.x - node.position.x;
      const dy = target.y - node.position.y;
      const dist = Math.sqrt(dx * dx + dy * dy);

      if (dist < 0.5) {
        node.position.set(target.x, target.y);
        continue;
      }

      // Snap if distance > 3 tiles (matching Unity ActorController line 85-86)
      if (dist > ts * 3) {
        node.position.set(target.x, target.y);
        continue;
      }

      // Lerp speed: 16 tiles per second (Unity ActorController line 82: 16 / actionCost)
      const speed = 16 * ts * dt;
      const ratio = Math.min(speed / dist, 1);
      node.position.set(
        node.position.x + dx * ratio,
        node.position.y + dy * ratio,
      );
    }
  }

  // ─── Private ───

  private clearAll(): void {
    this.clearProposedPath();
    this.tileRenderer.clear();
    this.grassLayer.removeChildren();
    this.itemLayer.removeChildren();
    this.bodyLayer.removeChildren();
    this.aboveEntityLayer.removeChildren();
    this.effectLayer.removeChildren();
    for (const state of this.entityStates.values()) {
      if (state.telegraph) state.telegraph.container.destroy({ children: true });
      if (state.explodeAOE) state.explodeAOE.sprite.destroy();
      if (state.attackGround) { state.attackGround.line.destroy(); state.attackGround.reticle.destroy(); }
    }
    this.entityStates.clear();
  }

  private buildEntities(): void {
    const floor = this.floor!;

    // Grasses
    for (const pos of floor.enumerateFloor()) {
      const grass = floor.grasses.get(pos);
      if (grass) this.addEntitySprite(grass, this.grassLayerFor(grass));
    }

    // Items
    for (const pos of floor.enumerateFloor()) {
      const item = floor.items.get(pos);
      if (item) this.addEntitySprite(item, this.itemLayer);
    }

    // Bodies (no spawn animation — AnimationPlayer handles their events)
    for (const body of floor.bodies) {
      this.addEntitySprite(body, this.bodyLayer);
    }
  }

  /** Returns aboveEntityLayer for entities declaring renderLayer='above-entity', else grassLayer. */
  private grassLayerFor(entity: Entity): Container {
    return (entity as any).renderLayer === 'above-entity'
      ? this.aboveEntityLayer
      : this.grassLayer;
  }

  /**
   * Create a Container node with a scaleRoot inner Container and a visual Sprite child.
   * Structure: node (position/lerp) → scaleRoot (center pivot, scale/alpha) → shadow + sprite.
   * Tint goes on the Sprite so sibling children (status indicators) don't inherit it.
   * scaleRoot pivot=(ts/2,ts/2) + position=(ts/2,ts/2) keeps visual origin at node(0,0)
   * while scaling from tile center.
   *
   * For entities with EntityRenderHooks.overridesDefaultSprite, the hook handles all visual
   * creation (no shadow or default sprite). For others, the hook's init() augments after
   * the default setup (e.g. Violets adds a flower sprite on top).
   */
  private addEntitySprite(entity: Entity, layer: Container): Container {
    const ts = this.camera.tileSize;
    const px = this.camera.tileToPixel(entity.pos);

    const node = new Container();
    node.position.set(px.x, px.y);

    // scaleRoot: center-pivoted inner Container so scale/alpha animations use tile center
    const scaleRoot = new Container();
    scaleRoot.pivot.set(ts / 2, ts / 2);
    scaleRoot.position.set(ts / 2, ts / 2);
    node.addChild(scaleRoot);

    const hooks = getEntityRenderHooks(entity);
    const ctx: RenderCtx = { sprites: this.sprites, ts };

    // Start building state — visual is a placeholder until assigned below
    const state: EntityRenderState = {
      node,
      visual: new Sprite(Texture.EMPTY),
      scaleRoot,
      isBody: layer === this.bodyLayer,
    };

    if (hooks?.overridesDefaultSprite) {
      // Entity renderer hook controls all visual creation (e.g. VibrantIvy: 4 directional sprites)
      hooks.init!(entity, state, ctx);
    } else {
      // Default setup: shadow, main sprite, tint, alpha, rotation
      const spriteKeyOverride = 'spriteKey' in entity ? (entity as any).spriteKey as string : null;
      const tex = spriteKeyOverride
        ? this.sprites.getTextureByKey(spriteKeyOverride)
        : this.sprites.getTexture(entity.displayName);

      // Shadow — bodies and grasses only (items have no shadow in Unity).
      // Matches Unity Shadow.prefab: color rgba(0.055, 0.059, 0.075, 0.4).
      const hasShadow = layer === this.bodyLayer || layer === this.grassLayer || layer === this.aboveEntityLayer;
      if (hasShadow) {
        const bottomPad = this.sprites.getBottomPadding(entity.displayName) * ts;
        const isFlat = FLAT_SHADOW_ENTITIES.has(entity.displayName.toLowerCase());

        const buildShadow = () => {
          const shadow = new Sprite(tex ?? Texture.WHITE);
          shadow.tint = 0x0E0F13;
          shadow.alpha = 0.4;
          shadow.width = ts;
          shadow.height = ts;
          if (isFlat) {
            shadow.position.set(ts * 0.05, -ts * 0.05 - bottomPad);
          } else {
            shadow.anchor.set(0.5, 1.0);
            shadow.position.set(ts / 2, ts - bottomPad);
            shadow.scale.y *= 0.5;
            shadow.skew.x = -0.35;
          }
          return shadow;
        };

        if (layer === this.aboveEntityLayer) {
          // Shadow renders on grassLayer (below bodies) — mirrors Unity Guardleaf.
          const shadowNode = new Container();
          shadowNode.position.set(px.x, px.y);
          shadowNode.addChild(buildShadow());
          this.grassLayer.addChild(shadowNode);
          state.detachedShadow = shadowNode;
        } else {
          const shadowSprite = buildShadow();
          scaleRoot.addChild(shadowSprite);
          state.shadow = shadowSprite;
        }
      }

      const sprite = new Sprite(tex ?? Texture.WHITE);
      const lowerName = entity.displayName.toLowerCase();

      // HangingVines: 2-tile height (Unity m_Size.y=2), hanging down from wall into floor below
      const isHangingVines = lowerName === 'hanging vines';
      sprite.width = ts;
      sprite.height = isHangingVines ? 2 * ts : ts;

      // Tint on sprite only — siblings (status indicators) won't inherit
      const tint = SPRITE_TINTS[lowerName];
      if (tint !== undefined) {
        sprite.tint = tint;
      } else if (!tex) {
        sprite.tint = this.fallbackColor(entity.displayName);
      }

      // Alpha from Unity SpriteRenderer.m_Color.a
      const alpha = SPRITE_ALPHAS[lowerName];
      if (alpha !== undefined) sprite.alpha = alpha;

      // Rotation for entities with angle property (e.g. EveningBells)
      if ('angle' in entity && typeof (entity as any).angle === 'number') {
        const angleDeg = (entity as any).angle as number;
        sprite.anchor.set(0.5, 0.5);
        sprite.position.set(ts / 2, ts / 2);
        sprite.rotation = angleDeg * (Math.PI / 180);
      }

      scaleRoot.addChild(sprite);
      state.visual = sprite;

      // Idle bob: Actor instances only, not structurally stationary enemies
      // (Grasper/Tendril/HydraHeart/HydraHead/Clumpshroom implement IBaseActionModifier directly)
      const isStationary = (entity as any)[Symbol.for('IBaseActionModifier')] === true;
      if (layer === this.bodyLayer && entity instanceof Actor && !isStationary) {
        state.bob = { timer: Math.random(), entity };
      }

      // Entity-specific augmentation (e.g. Violets adds flower sprite on top)
      if (hooks?.init) hooks.init(entity, state, ctx);
    }

    layer.addChild(node);
    this.entityStates.set(entity.guid, state);
    return node;
  }

  /**
   * Sync entity positions/visibility. Add new entities, remove dead ones.
   * This is a lightweight update — doesn't rebuild tiles.
   */
  private syncEntities(): void {
    const floor = this.floor!;
    const ts = this.camera.tileSize;
    const seenGuids = new Set<string>();
    const ctx: RenderCtx = { sprites: this.sprites, ts };

    // Sync bodies
    for (const body of floor.bodies) {
      seenGuids.add(body.guid);
      let state = this.entityStates.get(body.guid);
      if (!state) {
        this.addEntitySprite(body, this.bodyLayer);
        state = this.entityStates.get(body.guid)!;
        // No initSpawnAnimation — AnimationPlayer handles body spawn/death events
      }
      state.node.visible = !body.isDead;

      // Update sprite variant + rotation for entities with dynamic spriteKey (e.g. Tendril).
      // TendrilController.UpdateSprite() is called after every Grasper action; we mirror that here.
      if (!body.isDead && 'spriteKey' in body) {
        const visual = state.visual;
        const key = (body as any).spriteKey as string;
        const newTex = this.sprites.getTextureByKey(key);
        if (newTex) visual.texture = newTex;
        const angleDeg = (body as any).angle as number;
        visual.anchor.set(0.5, 0.5);
        visual.position.set(ts / 2, ts / 2);
        visual.rotation = angleDeg * (Math.PI / 180);
      }

      const hooks = getEntityRenderHooks(body);
      if (hooks?.sync) hooks.sync(body, state, ctx);
    }

    // Sync grasses
    for (const pos of floor.enumerateFloor()) {
      const grass = floor.grasses.get(pos);
      if (grass) {
        seenGuids.add(grass.guid);
        let state = this.entityStates.get(grass.guid);
        if (!state) {
          this.addEntitySprite(grass, this.grassLayerFor(grass));
          state = this.entityStates.get(grass.guid)!;
          this.initSpawnAnimation(grass.guid);
        }
        state.node.visible = !grass.isDead;

        const hooks = getEntityRenderHooks(grass);
        if (hooks?.sync) hooks.sync(grass, state, ctx);
      }
    }

    // Sync items
    for (const pos of floor.enumerateFloor()) {
      const item = floor.items.get(pos);
      if (item) {
        seenGuids.add(item.guid);
        let state = this.entityStates.get(item.guid);
        if (!state) {
          this.addEntitySprite(item, this.itemLayer);
          state = this.entityStates.get(item.guid)!;
          this.initSpawnAnimation(item.guid);
        }
        state.node.visible = !item.isDead;
      }
    }

    // Remove nodes for dead/removed entities
    for (const [guid, state] of this.entityStates) {
      if (seenGuids.has(guid) || state.fade || state.suppressFade) continue;
      state.spawn = undefined;
      if (state.isBody) {
        // Bodies: AnimationPlayer already ran death animation — just destroy
        state.node.destroy({ children: true });
        if (state.telegraph) state.telegraph.container.destroy({ children: true });
        if (state.explodeAOE) state.explodeAOE.sprite.destroy();
        if (state.attackGround) { state.attackGround.line.destroy(); state.attackGround.reticle.destroy(); }
        this.entityStates.delete(guid);
      } else {
        // Grasses + items: FadeThenDestroy (Unity FloorController behavior)
        const now = performance.now();
        state.fade = { startScale: state.scaleRoot.scale.x, startTime: now };
        if (state.statusIndicator) {
          state.statusIndicator.destroy();
          state.statusIndicator = undefined;
        }
      }
    }

    // Sync status indicators on bodies
    this.syncStatusIndicators(floor);
    // Sync telegraph charging effects
    this.syncTelegraphEffects(floor);
    // Sync AttackGroundTask line + reticle
    this.syncAttackGroundEffects(floor);
    // Sync ExplodeTask AoE markers
    this.syncExplodeEffects(floor);
  }

  /**
   * Sync status/task visuals as siblings of the visual sprite inside the
   * entity's Container node (mirrors Unity's Actor → Statuses child).
   * Each status has its own position/scale from its Unity prefab — no generic row.
   */
  private syncStatusIndicators(floor: Floor): void {
    const ts = this.camera.tileSize;

    for (const body of floor.bodies) {
      const state = this.entityStates.get(body.guid);
      if (!state || body.isDead || !('statuses' in body)) {
        if (state?.statusIndicator) {
          state.statusIndicator.destroy();
          state.statusIndicator = undefined;
        }
        continue;
      }

      const actor = body as any;
      const isSleeping = actor.task?.constructor?.name === 'SleepTask';
      const statuses = actor.statuses as { list: any[] };

      // Collect visuals to render
      const visuals: Array<{ config: StatusVisualConfig; status?: any; tint?: number }> = [];

      // SleepTask visual
      if (isSleeping) {
        visuals.push({
          config: SLEEP_VISUAL,
          tint: actor.task.isDeepSleep ? DEEP_SLEEP_TINT : undefined,
        });
      }

      // RunAwayTask visual
      if (actor.task instanceof RunAwayTask) {
        visuals.push({ config: RUN_AWAY_VISUAL, tint: RUN_AWAY_TINT });
      }

      // WaitTask visual — all non-player actors unless hideWaitTask=true in Unity
      const isPlayer = body.constructor.name === 'Player';
      const hideWaitTask = HIDE_WAIT_TASK_NAMES.has(body.constructor.name);
      if (!isPlayer && !hideWaitTask && actor.task instanceof WaitTask) {
        visuals.push({ config: WAIT_TASK_VISUAL, tint: WAIT_TASK_TINT });
      }

      // Status visuals — only statuses with Unity prefabs
      for (const s of statuses.list) {
        const config = STATUS_VISUALS[s.constructor.name];
        if (!config) continue;
        if (config.hideWhenSleeping && isSleeping) continue;
        visuals.push({ config, status: s });
      }

      if (visuals.length === 0) {
        if (state.statusIndicator) {
          state.statusIndicator.destroy();
          state.statusIndicator = undefined;
        }
        continue;
      }

      let container = state.statusIndicator;
      if (container) {
        container.removeChildren();
        if (container.parent !== state.scaleRoot) {
          container.removeFromParent();
          state.scaleRoot.addChild(container);
        }
      } else {
        container = new Container();
        state.scaleRoot.addChild(container);
        state.statusIndicator = container;
      }

      for (const { config, status, tint } of visuals) {
        // PoisonedStatus: select frame from spritesheet based on stack count
        let tex: Texture | null = null;
        if (status?.constructor?.name === 'PoisonedStatus') {
          const frames = this.sprites.getFrames('poisoned');
          if (frames && frames.length > 0) {
            const idx = Math.min(Math.max((status.stacks ?? 1) - 1, 0), frames.length - 1);
            tex = frames[idx];
          }
        }
        if (!tex) {
          tex = this.sprites.getTexture(config.spriteKey);
        }
        if (!tex) continue;

        const icon = new Sprite(tex);
        icon.anchor.set(0.5, 0.5);
        const size = config.scale * ts;
        icon.width = size;
        icon.height = size;

        // Convert Unity coords to PixiJS node-local coords
        // Node center = (ts/2, ts/2), Y-flip for Unity Y-up → PixiJS Y-down
        let cx = ts / 2 + config.offsetX * ts;
        let cy = ts / 2 - config.offsetY * ts;

        // ParasiteStatus: pseudo-random offset based on stacks (simplified wiggle)
        if (status?.constructor?.name === 'ParasiteStatus') {
          const seed = ((status.stacks ?? 1) * 7919) | 0;
          cx += ((seed % 80) - 40) / 100 * ts;
          cy += (((seed * 13) % 80) - 40) / 100 * ts;
        }

        icon.position.set(cx, cy);
        if (config.tint != null) icon.tint = config.tint;
        if (tint != null) icon.tint = tint;
        container.addChild(icon);

        // Second sprite for dual-sprite prefabs (e.g. ConstrictedStatus)
        if (config.secondSprite) {
          const icon2 = new Sprite(tex);
          icon2.anchor.set(0.5, 0.5);
          icon2.width = size;
          icon2.height = size;
          icon2.position.set(
            ts / 2 + config.secondSprite.offsetX * ts,
            ts / 2 - config.secondSprite.offsetY * ts,
          );
          if (config.secondSprite.flipX) icon2.scale.x *= -1;
          if (config.tint != null) icon2.tint = config.tint;
          if (tint != null) icon2.tint = tint;
          container.addChild(icon2);
        }
      }
    }
  }

  /**
   * Detect which actors have TelegraphedTask and create/fade effects.
   * Called from syncEntities each model sync.
   */
  private syncTelegraphEffects(floor: Floor): void {
    const activeGuids = new Set<string>();

    for (const body of floor.bodies) {
      if (body.isDead) continue;
      const actor = body as any;
      // AttackGroundTask has its own line+reticle; skip generic telegraph particles for it.
      if (actor.task instanceof TelegraphedTask && !(actor.task instanceof AttackGroundTask)) {
        activeGuids.add(body.guid);
        const state = this.entityStates.get(body.guid);
        if (state && !state.telegraph) {
          const container = new Container();
          this.effectLayer.addChild(container);

          const then = actor.task.then;
          let reticleTilePos: Vector2Int | undefined;
          if (then instanceof AttackBaseAction && !then.target.isDead) {
            const tp = then.target.pos;
            if (!(tp.x === body.pos.x && tp.y === body.pos.y)) reticleTilePos = tp;
          } else if (then instanceof AttackGroundBaseAction) {
            const tp = then.targetPosition;
            if (!(tp.x === body.pos.x && tp.y === body.pos.y)) reticleTilePos = tp;
          }

          state.telegraph = {
            container,
            particles: [],
            spawnAccum: 0,
            fadingOut: false,
            reticleAge: 0,
            reticleTilePos,
          };
        }
      }
    }

    // Start fade-out for effects whose actor no longer has TelegraphedTask
    for (const [guid, state] of this.entityStates) {
      if (state.telegraph && !activeGuids.has(guid) && !state.telegraph.fadingOut) {
        state.telegraph.fadingOut = true;
      }
    }
  }

  /**
   * Per-frame update for telegraph charging particle effects.
   * Matches Unity TelegraphedTask.prefab:
   * - 30 white particles/sec from circle rim (radiusThickness 0), radius 0.5 tiles
   * - Particle life 0.4s, startSpeed 0
   * - VelocityModule radial curve 0→-1 (scalar 1, speedModifier 5): particles
   *   accelerate inward, reaching center exactly at end of life
   * - Fades out over 0.25s when task ends
   * @param dt Delta time in seconds.
   */
  updateTelegraphEffects(dt: number): void {
    const ts = this.camera.tileSize;
    const SPAWN_RATE = 30;
    const PARTICLE_LIFE = 0.4;
    const RADIUS = 0.5 * ts;
    const PARTICLE_RADIUS = Math.max(1.5, 0.05 * ts);
    // Radial displacement coefficient: particles travel from RADIUS to 0 over lifetime.
    // v(a) = -6.25*ts*a, displacement = -3.125*ts*a² (reaches -RADIUS at a=0.4)
    const RADIAL_COEFF = 3.125 * ts;

    for (const [, state] of this.entityStates) {
      const effect = state.telegraph;
      if (!effect) continue;

      // Position at entity center
      effect.container.position.set(
        state.node.position.x + ts / 2,
        state.node.position.y + ts / 2,
      );

      // Spawn new particles at random angles on the circle rim
      if (!effect.fadingOut) {
        effect.spawnAccum += dt;
        const interval = 1 / SPAWN_RATE;
        while (effect.spawnAccum >= interval) {
          effect.spawnAccum -= interval;
          const angle = Math.random() * Math.PI * 2;
          const g = new Graphics();
          g.circle(0, 0, PARTICLE_RADIUS).fill({ color: 0xffffff, alpha: 0.5 });
          g.position.set(Math.cos(angle) * RADIUS, Math.sin(angle) * RADIUS);
          g.alpha = 0;
          effect.container.addChild(g);
          effect.particles.push({ g, age: 0, angle });
        }
      }

      // Age particles: accelerate inward along their spawn angle
      for (let i = effect.particles.length - 1; i >= 0; i--) {
        const p = effect.particles[i];
        p.age += dt;
        if (p.age >= PARTICLE_LIFE) {
          p.g.destroy();
          effect.particles.splice(i, 1);
        } else {
          const r = RADIUS - RADIAL_COEFF * p.age * p.age;
          p.g.position.set(Math.cos(p.angle) * r, Math.sin(p.angle) * r);
          // Opacity: 0→1 over first 10%, hold at 1, then 1→0 over last 10%
          const t = p.age / PARTICLE_LIFE;
          p.g.alpha = t < 0.1 ? t / 0.1 : t > 0.9 ? (1 - t) / 0.1 : 1;
        }
      }

      // Reticle: flash at attack target (orange rectangle, sin-wave alpha)
      if (effect.reticleTilePos) {
        effect.reticleAge += dt;
        if (!effect.reticle) {
          const g = new Graphics();
          this.effectLayer.addChild(g);
          effect.reticle = g;
        }
        const rPx = this.camera.tileToPixel(effect.reticleTilePos);
        effect.reticle.clear();
        if (!effect.fadingOut) {
          const alpha = 0.3 + 0.5 * (0.5 + 0.5 * Math.sin(effect.reticleAge * Math.PI * 2 * 2));
          effect.reticle.rect(rPx.x, rPx.y, ts, ts).stroke({ color: 0xff6633, alpha, width: 2 });
        }
      }

      // Fade out container
      if (effect.fadingOut) {
        effect.container.alpha = Math.max(0, effect.container.alpha - dt / TELEGRAPH_FADE_DURATION);
        if (effect.container.alpha <= 0) {
          if (effect.reticle) { effect.reticle.destroy(); }
          effect.container.destroy({ children: true });
          state.telegraph = undefined;
        }
      }
    }

    // AttackGroundTask reticle flash animation (Flashing.anim: white→reddish 0xff1a2f, 0.5s loop)
    for (const [, state] of this.entityStates) {
      const ag = state.attackGround;
      if (!ag) continue;
      ag.reticleAge += dt;
      // sin oscillates 0→1→0 at period 0.5s
      const t = 0.5 + 0.5 * Math.sin(ag.reticleAge * Math.PI * 4);
      const r = 255;
      const g = Math.round(255 * (1 - t * (1 - 0.102))); // 255 → 26 (0.1 * 255)
      const b = Math.round(255 * (1 - t * (1 - 0.184))); // 255 → 47
      ag.reticle.tint = (r << 16) | (g << 8) | b;
    }
  }

  /**
   * Sync AttackGroundTask visuals: line from actor to target + flashing reticle sprite.
   * Unity AttackGroundTaskController.cs: LineRenderer (tan→transparent) + colored_transparent_packed_613 reticle.
   * Line width: 0.08 tiles normally, 0.03 if diamond distance > 1.
   * Reticle flash animation (Flashing.anim) is driven per-frame inside updateTelegraphEffects.
   */
  private syncAttackGroundEffects(floor: Floor): void {
    const ts = this.camera.tileSize;
    const activeGuids = new Set<string>();

    for (const body of floor.bodies) {
      if (body.isDead) continue;
      const actor = body as any;
      if (!(actor.task instanceof AttackGroundTask)) continue;
      activeGuids.add(body.guid);
      const state = this.entityStates.get(body.guid);
      if (!state) continue;

      const targetPos = (actor.task as AttackGroundTask).targetPosition;
      const fromPx = this.camera.tileToCenterPixel(body.pos);
      const toPx = this.camera.tileToCenterPixel(targetPos);
      const diamondDist = Math.abs(targetPos.x - body.pos.x) + Math.abs(targetPos.y - body.pos.y);
      const lineWidth = (diamondDist > 1 ? 0.03 : 0.08) * ts;

      if (!state.attackGround) {
        const line = new Graphics();
        this.attackGroundLayer.addChild(line);
        const tex = this.sprites.getTextureByKey('colored_transparent_packed_613') ?? Texture.WHITE;
        const reticle = new Sprite(tex);
        reticle.anchor.set(0.5, 0.5);
        reticle.width = ts;
        reticle.height = ts;
        this.attackGroundLayer.addChild(reticle);
        state.attackGround = { line, reticle, reticleAge: 0 };
      }

      const { line, reticle } = state.attackGround;
      line.clear();
      line.moveTo(fromPx.x, fromPx.y)
        .lineTo(toPx.x, toPx.y)
        .stroke({ color: 0xCFC6B8, alpha: 0.8, width: lineWidth });
      reticle.position.set(toPx.x, toPx.y);
    }

    for (const [guid, state] of this.entityStates) {
      if (state.attackGround && !activeGuids.has(guid)) {
        state.attackGround.line.destroy();
        state.attackGround.reticle.destroy();
        state.attackGround = undefined;
      }
    }
  }

  /** Hide the attack-ground line+reticle for a given entity guid (called from AnimationPlayer at bump impact). */
  hideAttackGroundEffect(guid: string): void {
    const state = this.entityStates.get(guid);
    if (state?.attackGround) {
      state.attackGround.line.visible = false;
      state.attackGround.reticle.visible = false;
    }
  }

  /**
   * Show 3×3 AoE reticle (selection-48x48) while entity has ExplodeTask.
   * On removal, marks fadingOut for updateExplodeEffects to fade out.
   */
  private syncExplodeEffects(floor: Floor): void {
    const ts = this.camera.tileSize;
    const activeGuids = new Set<string>();

    for (const body of floor.bodies) {
      if (body.isDead) continue;
      if ((body as any).task instanceof ExplodeTask) {
        activeGuids.add(body.guid);
        const state = this.entityStates.get(body.guid);
        if (!state) continue;
        const px = this.camera.tileToPixel(body.pos);
        if (!state.explodeAOE) {
          const tex = this.sprites.getTextureByKey('selection-48x48') ?? Texture.WHITE;
          const s = new Sprite(tex);
          s.width = 3 * ts;
          s.height = 3 * ts;
          s.alpha = 0.85;
          this.effectLayer.addChild(s);
          state.explodeAOE = { sprite: s, elapsed: 0, fadingOut: false };
        }
        if (!state.explodeAOE.fadingOut) {
          state.explodeAOE.sprite.position.set(px.x - ts, px.y - ts);
        }
      }
    }

    for (const [guid, state] of this.entityStates) {
      if (state.explodeAOE && !activeGuids.has(guid) && !state.explodeAOE.fadingOut) {
        state.explodeAOE.fadingOut = true;
        state.explodeAOE.elapsed = 0;
        state.explodeAOE.sprite.alpha = 0.85;
      }
    }
  }

  /**
   * Per-frame update for ExplodeTask AoE markers.
   * Pulses tint white→red at 2Hz while active, then fades out on removal.
   */
  updateExplodeEffects(dt: number): void {
    const FADE_DUR = 0.3;
    const PULSE_HZ = 2.0;
    for (const [, state] of this.entityStates) {
      const aoe = state.explodeAOE;
      if (!aoe) continue;
      aoe.elapsed += dt;
      if (aoe.fadingOut) {
        const t = Math.min(aoe.elapsed / FADE_DUR, 1);
        aoe.sprite.alpha = 0.85 * (1 - t);
        if (t >= 1) {
          aoe.sprite.destroy();
          state.explodeAOE = undefined;
        }
      } else {
        // Lerp tint: white (0xFFFFFF) → red (0xFF0000) oscillating with sine
        const pulse = (Math.sin(aoe.elapsed * PULSE_HZ * Math.PI * 2) + 1) / 2; // 0..1
        const g = Math.round(255 * (1 - pulse));
        const b = Math.round(255 * (1 - pulse));
        aoe.sprite.tint = (0xFF << 16) | (g << 8) | b;
      }
    }
  }

  /** Create sprites for floor bodies not yet in entityStates. Call before animation playback. */
  addNewBodySprites(): void {
    if (!this.floor) return;
    for (const body of this.floor.bodies) {
      if (!this.entityStates.has(body.guid)) {
        this.addEntitySprite(body, this.bodyLayer);
      }
    }
  }

  /** Initialize GrowAtStart spawn animation. Skips if the renderer's init hook already set state.spawn. */
  private initSpawnAnimation(guid: string): void {
    const state = this.entityStates.get(guid);
    if (!state || state.spawn) return;
    state.scaleRoot.scale.set(0.01, 0.01);
    state.scaleRoot.alpha = 0.01;
    state.spawn = { elapsed: 0, scale: 0.01, duration: SPAWN_ANIMATION_DURATION };
  }

  /**
   * Update GrowAtStart spawn and FadeThenDestroy despawn animations each frame.
   *
   * GrowAtStart (Unity _Grass.prefab): iterative lerp each frame from current scale toward 1.
   *   lerpAmount = 1 - 1/exp(t * PI * 2)  where t = elapsed / ANIMATION_TIME (3s).
   *   scale = lerp(scale, 1, lerpAmount)  — same formula applied each frame, not direct.
   *   This causes rapid early growth; visually done in ~0.25s despite the 3s window.
   *   Alpha mirrors scale for a fade-in effect. Scaling is center-pivoted.
   *
   * FadeThenDestroy (Unity FloorController): alpha 1→0, scale shrinks to 50% over 0.5s.
   *
   * @param dt Delta time in seconds.
   */
  updateEntityAnimations(dt: number): void {
    // GrowAtStart: iterative lerp matching Unity's frame-by-frame accumulation
    for (const [, state] of this.entityStates) {
      if (!state.spawn) continue;
      state.spawn.elapsed += dt;
      const t = state.spawn.elapsed / state.spawn.duration;
      if (t >= 1) {
        state.scaleRoot.scale.set(1, 1);
        state.scaleRoot.alpha = 1;
        state.spawn = undefined;
      } else {
        const la = 1 - 1 / Math.exp(t * Math.PI * 2);
        state.spawn.scale = state.spawn.scale + la * (1 - state.spawn.scale); // lerp(current, 1, la)
        state.scaleRoot.scale.set(state.spawn.scale, state.spawn.scale);
        state.scaleRoot.alpha = state.spawn.scale;
      }
    }

    // FadeThenDestroy: alpha 1→0, scale shrinks to 50% over 0.5s
    const now = performance.now();
    for (const [guid, state] of this.entityStates) {
      if (!state.fade) continue;
      const t = Math.min((now - state.fade.startTime) / FADE_DURATION_MS, 1);
      if (t >= 1) {
        state.node.destroy({ children: true });
        if (state.detachedShadow) state.detachedShadow.destroy({ children: true });
        if (state.telegraph) state.telegraph.container.destroy({ children: true });
        if (state.explodeAOE) state.explodeAOE.sprite.destroy();
        this.entityStates.delete(guid);
      } else {
        state.scaleRoot.alpha = 1 - t;
        const s = state.fade.startScale * (1 - FADE_END_SCALE * t);
        state.scaleRoot.scale.set(s, s);
        if (state.detachedShadow) {
          state.detachedShadow.alpha = 1 - t;
          const ss = 1 - FADE_END_SCALE * t;
          state.detachedShadow.scale.set(ss, ss);
        }
      }
    }

    // Bladegrass Sharpen animation: frames 0→1→2→3 over 0.35s (keyframes at t=0,0.083,0.167,0.25)
    for (const [, state] of this.entityStates) {
      const anim = state.bladegrassAnim;
      if (!anim) continue;
      const { frames, sharpenStart } = anim;
      if (sharpenStart === null) {
        state.visual.texture = frames[0];
      } else {
        const elapsed = (performance.now() - sharpenStart) / 1000;
        const idx = elapsed < 0.083 ? 0 : elapsed < 0.167 ? 1 : elapsed < 0.250 ? 2 : 3;
        state.visual.texture = frames[idx];
      }
    }

    // Idle bob (Unity _Actor.prefab Idle.anim: step up 0.1 units at phase 0.5, speed = 1/baseActionCost)
    const bobTs = this.camera.tileSize;
    for (const [, state] of this.entityStates) {
      if (!state.bob) continue;
      const scaleRoot = state.scaleRoot;
      const actor = state.bob.entity as any;
      // Unity SleepTaskController disables the Animator when sleeping — suppress bob.
      // Any status with blocksMovement() true (Webbed, Constricted, InShell, etc.) — suppress bob.
      const isSleeping = actor.task?.constructor?.name === 'SleepTask';
      const isMovementBlocked = actor.statuses?.list?.some(
        (s: any) => s.blocksMovement?.() === true
      );
      if (isSleeping || isMovementBlocked) {
        scaleRoot.position.y = bobTs / 2;
        continue;
      }
      const baseActionCost: number = actor.baseActionCost ?? 1;
      state.bob.timer = (state.bob.timer + dt / baseActionCost) % 1.0;
      const bobPx = state.bob.timer >= 0.5 ? 0.1 * bobTs : 0;
      scaleRoot.position.y = bobTs / 2 - bobPx;
    }

    const VIBRATE_PERIOD = 4.60;
    const ts = this.camera.tileSize;
    for (const [, state] of this.entityStates) {
      if (!state.vibrate) continue;
      // Stop vibrating once the fade-out (disappear) animation starts
      if (state.fade) {
        state.scaleRoot.position.x = ts / 2;
        continue;
      }
      state.vibrate.timer += dt;
      const t = state.vibrate.timer % VIBRATE_PERIOD;
      let amplitude: number = 0.07;
      // 20Hz alternating sign matching Vibrate.anim's 0.05s keyframe intervals
      const sign = (Math.floor(t / 0.05) % 2 === 0) ? -1 : 1;
      state.scaleRoot.position.x = ts / 2 + sign * amplitude * ts;
    }

    // Skully squish-spawn (unsquish from bottom): scaleY 0→1 over 0.3s, bottom-pivot.
    const SQUISH_SPAWN_DURATION = 0.3;
    for (const [, state] of this.entityStates) {
      if (!state.squishSpawn) continue;
      state.squishSpawn.elapsed += dt;
      const tSq = Math.min(state.squishSpawn.elapsed / SQUISH_SPAWN_DURATION, 1);
      const eased = 1 - Math.pow(1 - tSq, 2); // power2.out
      state.scaleRoot.scale.y = eased;
      state.scaleRoot.position.y = ts * (1 - eased / 2); // ts→ts/2 as eased 0→1
      if (tSq >= 1) {
        state.scaleRoot.scale.set(1, 1);
        state.scaleRoot.position.y = ts / 2;
        state.squishSpawn = undefined;
      }
    }

    // Deathbloom bloom animation: scale 0.25→0.7836857, alpha 0.251→1.0 over 1.5s (ease-out quadratic)
    for (const [, state] of this.entityStates) {
      if (!state.deathbloom || state.deathbloom.done) continue;
      const db = state.deathbloom;
      db.elapsed += dt;
      const t = Math.min(db.elapsed / 1.5, 1.0);
      const ease = t * (2 - t);
      const scaleRatio = (0.25 / 0.7836857) + ease * (1 - 0.25 / 0.7836857);
      db.flower.scale.set(db.targetScale * scaleRatio);
      db.flower.alpha = 0.251 + ease * (1.0 - 0.251);
      if (t >= 1.0) db.done = true;
    }
  }

  /** Generate a deterministic fallback color from a name string. */
  private fallbackColor(name: string): number {
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = ((hash << 5) - hash + name.charCodeAt(i)) | 0;
    }
    // Ensure decent saturation/brightness
    const h = Math.abs(hash) % 360;
    const s = 50 + (Math.abs(hash >> 8) % 30);
    const l = 40 + (Math.abs(hash >> 16) % 20);
    return hslToHex(h, s, l);
  }
}

function hslToHex(h: number, s: number, l: number): number {
  s /= 100;
  l /= 100;
  const a = s * Math.min(l, 1 - l);
  const f = (n: number) => {
    const k = (n + h / 30) % 12;
    const color = l - a * Math.max(Math.min(k - 3, 9 - k, 1), -1);
    return Math.round(255 * color);
  };
  return (f(0) << 16) | (f(8) << 8) | f(4);
}
