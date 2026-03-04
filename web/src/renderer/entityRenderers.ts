import { Sprite, Texture, Container, Graphics } from 'pixi.js';
import type { Entity } from '../model/Entity';
import type { SpriteManager } from './SpriteManager';
import { VibrantIvy } from '../model/grasses/VibrantIvy';
import { Violets } from '../model/grasses/Violets';
import { Wall } from '../model/Tile';
import { Vector2Int } from '../core/Vector2Int';
import { Snail } from '../model/enemies/Snail';
import { InShellStatus } from '../model/statuses/InShellStatus';

// ─── EntityRenderState ───

/** All per-entity renderer state, consolidated into a single object keyed by guid. */
export interface EntityRenderState {
  node: Container;
  visual: Sprite;
  scaleRoot: Container;
  /** True for floor.bodies entries — exempt from FadeThenDestroy on removal. */
  isBody: boolean;
  statusIndicator?: Container;
  /** GrowAtStart spawn animation progress. */
  spawn?: { elapsed: number; scale: number };
  /** FadeThenDestroy exit animation. Set when entity leaves the floor; drives updateEntityAnimations. */
  fade?: { startScale: number; startTime: number };
  /** Detached shadow Container on grassLayer for above-entity grasses (e.g. Guardleaf). */
  detachedShadow?: Container;
  /** Shadow sprite (for entities whose shadow texture must update dynamically). */
  shadow?: Sprite;
  /** Idle bob state for Actor bodies. */
  bob?: { timer: number; entity: Entity };
  /** VibrantIvy directional sprites and last known stack count. */
  ivy?: { directionalSprites: Sprite[]; lastStacks: number };
  /** Violets animated flower sprite. */
  violetFlower?: Sprite;
  /** TelegraphedTask charging particle effect. */
  telegraph?: {
    container: Container;
    particles: Array<{ g: Graphics; age: number; angle: number }>;
    spawnAccum: number;
    fadingOut: boolean;
    reticle?: Graphics;
    reticleAge: number;
    reticleTilePos?: Vector2Int;
  };
  /** AttackGroundTask line + reticle effects. */
  attackGround?: {
    line: Graphics;
    reticle: Sprite;
    reticleAge: number;
  };
}

// ─── Entity Renderer Hooks ───

export interface RenderCtx {
  sprites: SpriteManager;
  ts: number;
}

export interface EntityRenderHooks {
  /**
   * If true, `init` is called INSTEAD of the default shadow+sprite setup.
   * The hook is responsible for creating all visuals and setting state.visual.
   */
  overridesDefaultSprite?: boolean;
  /** Called once when the entity's visual is first created. */
  init?: (entity: Entity, state: EntityRenderState, ctx: RenderCtx) => void;
  /** Called each sync step while the entity is alive on the floor. */
  sync?: (entity: Entity, state: EntityRenderState, ctx: RenderCtx) => void;
}

const REGISTRY = new Map<Function, EntityRenderHooks>();

export function registerEntityRenderer(ctor: Function, hooks: EntityRenderHooks): void {
  REGISTRY.set(ctor, hooks);
}

export function getEntityRenderHooks(entity: Entity): EntityRenderHooks | undefined {
  return REGISTRY.get(entity.constructor as Function);
}

// ─── Violets helper ───

/**
 * Port of VioletsController.Update(): selects the flower stage sprite key and
 * PixiJS anchor based on violets.countUp and violets.isOpen.
 * flowerStages = [purple_1, purple_2, purple_3, purple_4], open = purple_5.
 * Anchors from Unity .meta pivots (y flipped: pixiAnchorY = 1 - unityPivotY).
 */
function violetFlowerStage(v: Violets): { key: string; anchorX: number; anchorY: number } {
  const FLOWER_STAGES = ['purple_1', 'purple_2', 'purple_3', 'purple_4'];
  const ANCHORS: Array<[number, number]> = [
    [0.5,     0.5    ], // purple_1: pivot (0.5, 0.5)
    [0.5,     0.5    ], // purple_2: pivot (0.5, 0.5)
    [0.46875, 0.5    ], // purple_3: pivot (0.46875, 0.5)
    [0.46875, 0.5    ], // purple_4: pivot (0.46875, 0.5)
  ];
  if (v.isOpen) {
    // purple_5: pivot (0.46875, 0.53125) → anchorY = 1 - 0.53125 = 0.46875
    return { key: 'purple_5', anchorX: 0.46875, anchorY: 0.46875 };
  }
  const stage0Count = Violets.turnsToChange - FLOWER_STAGES.length; // 8
  const idx = v.countUp >= stage0Count ? v.countUp - stage0Count : 0;
  const [anchorX, anchorY] = ANCHORS[idx];
  return { key: FLOWER_STAGES[idx], anchorX, anchorY };
}

// ─── VibrantIvy renderer ───

/**
 * VibrantIvyController port: 4 directional sprites (up/right/down/left), one per adjacent
 * cardinal wall. No centered main sprite, no shadow. Offset: 0.07 tile units from center.
 */
registerEntityRenderer(VibrantIvy, {
  overridesDefaultSprite: true,
  init(entity: Entity, state: EntityRenderState, ctx: RenderCtx): void {
    const ivy = entity as VibrantIvy;
    const floor = ivy.floor!;
    const { sprites, ts } = ctx;
    const tex = sprites.getTexture(entity.displayName);
    const OFFSET = 0.07;
    // [gameDx, gameDy, pixiOffsetX, pixiOffsetY, rotation]
    // game Y+1 = screen up (PixiJS -Y); game Y-1 = screen down (PixiJS +Y)
    const dirs: [number, number, number, number, number][] = [
      [ 0,  1,       0, -OFFSET,           0], // up
      [ 1,  0,  OFFSET,       0,  Math.PI / 2], // right
      [ 0, -1,       0,  OFFSET,      Math.PI], // down
      [-1,  0, -OFFSET,       0, -Math.PI / 2], // left
    ];
    const ivySprites: Sprite[] = [];
    for (const [gdx, gdy, odx, ody, rot] of dirs) {
      const n = floor.tiles.get(new Vector2Int(ivy.pos.x + gdx, ivy.pos.y + gdy));
      if (!(n instanceof Wall)) continue;
      const s = new Sprite(tex ?? Texture.WHITE);
      s.width = ts;
      s.height = ts;
      s.anchor.set(0.5, 0.5);
      s.position.set(ts / 2 + odx * ts, ts / 2 + ody * ts);
      s.rotation = rot;
      state.scaleRoot.addChild(s);
      ivySprites.push(s);
    }
    state.visual = ivySprites[0] ?? new Sprite(Texture.EMPTY);
    state.ivy = { directionalSprites: ivySprites, lastStacks: ivy.stacks };
  },
  sync(entity: Entity, state: EntityRenderState, _ctx: RenderCtx): void {
    const ivy = entity as VibrantIvy;
    if (!state.ivy) return;
    const last = state.ivy.lastStacks;
    if (ivy.stacks < last && ivy.stacks > 0) {
      for (let i = 0; i < last - ivy.stacks; i++) {
        if (state.ivy.directionalSprites.length > 0) {
          state.ivy.directionalSprites.shift()!.visible = false;
        }
      }
    }
    state.ivy.lastStacks = ivy.stacks;
  },
});

// ─── Violets renderer ───

/**
 * VioletsController port: animated "Flower" child sprite (purple_1..5) on top of stem.
 * Scale 0.65, centered on tile. Texture and anchor update each sync based on countUp/isOpen.
 */
registerEntityRenderer(Violets, {
  init(_entity: Entity, state: EntityRenderState, ctx: RenderCtx): void {
    const { sprites, ts } = ctx;
    const flowerTex = sprites.getTextureByKey('purple_1') ?? Texture.WHITE;
    const flowerSprite = new Sprite(flowerTex);
    flowerSprite.width = ts * 0.65;
    flowerSprite.height = ts * 0.65;
    flowerSprite.anchor.set(0.5, 0.5);
    flowerSprite.position.set(ts / 2, ts / 2);
    state.scaleRoot.addChild(flowerSprite);
    state.violetFlower = flowerSprite;
  },
  sync(entity: Entity, state: EntityRenderState, ctx: RenderCtx): void {
    if (!state.violetFlower) return;
    const { sprites, ts } = ctx;
    const { key, anchorX, anchorY } = violetFlowerStage(entity as Violets);
    const tex = sprites.getTextureByKey(key);
    if (tex) state.violetFlower.texture = tex;
    state.violetFlower.width = ts * 0.65;
    state.violetFlower.height = ts * 0.65;
    state.violetFlower.anchor.set(anchorX, anchorY);
    state.violetFlower.position.set(ts / 2, ts / 2);
  },
});
// ─── Snail renderer ───

registerEntityRenderer(Snail, {
  sync(entity: Entity, state: EntityRenderState, ctx: RenderCtx): void {
    const frames = ctx.sprites.getFrames('snail');
    if (!frames) return;
    const inShell = (entity as Snail).statuses.get(InShellStatus);
    const stacks = inShell?.stacks ?? 0;
    const idx = Math.min(stacks, frames.length - 1);
    const tex = frames[idx];
    state.visual.texture = tex;
    if (state.shadow) state.shadow.texture = tex;
    // Suppress idle bob while in shell
  },
});
