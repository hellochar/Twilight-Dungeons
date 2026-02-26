## Context

Twilight Dungeons is a Unity 2D C# roguelike (~24,500 lines across 305 files) with a clean model-view separation. The goal is to create a web-native "daily puzzle" version that presents one procedurally-generated level per day, seeded by date. All existing content (35+ enemies, 24 grasses, 25+ status effects, 12 items, etc.) should work. Scoring is by turns taken. The site deploys to GitHub Pages as a static site. High score histograms can come later.

Key architectural insight: The C# model layer (/Assets/src/model/) is pure game logic with minimal Unity dependencies (just Vector2Int, Mathf, Debug.Log, [Serializable]). It translates nearly 1:1 to TypeScript.

Asset note: All PNG sprites (200 files) are stored in Git LFS, which isn't available in this environment. We'll build the spritesheet infrastructure with colored-rectangle fallback placeholders. Real pixel art drops in when the user pulls LFS locally and runs the atlas-generation script.

## Tech Stack

| Layer | Choice | Rationale |
|-|-|-|
| Language | TypeScript (strict) | Near 1:1 mapping to C# classes, interfaces, generics |
| UI Framework | React 19 | State-driven HUD, inventory panel, menus, overlays — DOM is better than Canvas for interactive UI |
| Rendering | PixiJS v8 (WebGL) | Purpose-built 2D WebGL renderer. Automatic sprite batching (1 draw call for entire grid). Built-in spritesheet/atlas support. ~100KB gzipped. 60fps guaranteed on mobile. |
| Animation | GSAP (GreenSock) | Industry-standard tween library. Works on any JS object — perfect for PixiJS sprite props (x, y, alpha, scale). Tiny (~25KB). |
| Particles | @pixi/particle-emitter | Built for PixiJS. Poison bubbles, spore puffs, attack sparks, death fade effects. |
| Sprites | Spritesheet atlas | Single PNG + JSON manifest. One WebGL texture bind → all sprites batched in one draw call. Generated at build time from individual PNGs via free-tex-packer-core. |
| Build | Vite | Zero-config TS + React, fast HMR, single static bundle, base config for GitHub Pages |
| Deploy | GitHub Pages via gh-pages | npm run deploy pushes dist/ |
| RNG | Custom seeded PRNG (mulberry32) | 10 lines, no deps, deterministic per-day seeding |
| State | Single GameModel instance + React context | Game logic in model, React reads snapshot for UI |

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    React App Shell                       │
│  ┌──────────────┐  ┌─────────────┐  ┌───────────────┐  │
│  │  HUD Panel   │  │  Inventory  │  │  Game Over /  │  │
│  │  (HP, turns, │  │  (drag/tap) │  │  Daily Screen │  │
│  │   enemies)   │  │             │  │               │  │
│  └──────────────┘  └─────────────┘  └───────────────┘  │
│  ┌──────────────────────────────────────────────────┐   │
│  │             PixiJS Canvas (WebGL)                │   │
│  │  ┌────────────────────────────────────────────┐  │   │
│  │  │  Tile Layer (Ground, Wall, Chasm, Water)   │  │   │
│  │  │  Grass Layer (Bladegrass, Web, etc.)       │  │   │
│  │  │  Item Layer (dropped items)                │  │   │
│  │  │  Body Layer (Player, Enemies)              │  │   │
│  │  │  Effect Layer (Particles, Damage numbers)  │  │   │
│  │  │  Fog Layer (Unexplored / Explored overlay) │  │   │
│  │  └────────────────────────────────────────────┘  │   │
│  └──────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────┐   │
│  │       Input Handler (Click/Tap/KB/Swipe)         │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                            │
                    playerAction(Move/Attack/Wait/Use)
                            │
┌─────────────────────────────────────────────────────────┐
│                   Game Model (Pure TS)                   │
│  GameModel → TurnManager → stepUntilPlayerChoice()      │
│  Floor → Tiles[x,y], Grasses[x,y], Bodies[], Items[x,y] │
│  Player → Inventory, Equipment, Statuses, HP            │
│  AIActor → GetNextTask() → Execute → Events             │
│  Modifier Chain → Of<T> → Process → Final value         │
│  EventQueue → drain → AnimationQueue (for renderer)     │
└─────────────────────────────────────────────────────────┘
```

Data flow: User input → Player action → TurnManager steps all entities → emits game events → AnimationQueue records visual events → PixiJS plays them as tweened animations → React re-renders HUD from model snapshot.

## Animation System Design

The turn-based model steps synchronously (all logic resolves instantly). But we want animated playback. Solution: record game events, replay as animations.

```typescript
// During TurnManager.step(), the model emits events:
interface GameEvent {
  type: 'move' | 'attack' | 'damage' | 'death' | 'heal' | 'statusAdd' | 'statusRemove' | 'spawn' | 'pickup';
  entityId: string;
  data: any;
  simultaneous?: boolean; // can play at same time as previous event
}

// AnimationQueue collects these, then plays them:
class AnimationPlayer {
  async playEvents(events: GameEvent[]) {
    for (const group of groupSimultaneous(events)) {
      await Promise.all(group.map(e => this.animateEvent(e)));
    }
  }

  animateEvent(event: GameEvent): Promise<void> {
    switch (event.type) {
      case 'move':
        return gsap.to(sprite, { x: newX, y: newY, duration: 0.15, ease: 'power2.out' });
      case 'attack':
        return this.bumpAnimation(attacker, target, 0.12);
      case 'damage':
        return this.flashAndShake(target, 0.1);
      case 'death':
        return gsap.to(sprite, { alpha: 0, scale: 0.5, duration: 0.3 });
      case 'statusAdd':
        return this.emitParticles(entity, statusType);
    }
  }
}
```

**Key animations:**

| Event | Animation | Duration |
|-|-|-|
| Move | Slide sprite to new tile | 150ms ease-out |
| Attack | Bump toward target + recoil | 120ms |
| Take damage | Flash white + screen shake | 100ms |
| Death | Shrink + fade out + particles | 300ms |
| Heal | Green glow pulse | 200ms |
| Status effect | Particle emitter (poison bubbles, web strands, etc.) | Ongoing |
| Level clear | Celebration particle burst | 500ms |
| Grass enter | Small puff/rustle particles | 100ms |

**All animations are optional for gameplay correctness** — the model is already resolved. If the user wants to skip animations, just snap sprites to final positions.

---

## Spritesheet Pipeline

```
Individual PNGs (from Git LFS)         Build-time script
     sprites/bat.png         ──→   ┌─────────────────────┐
     sprites/goo.png         ──→   │ free-tex-packer-core │
     sprites/player.png      ──→   │   (Node.js script)   │
     sprites/ground.png      ──→   └──────────┬──────────┘
     ...200 files...                           │
                                               ▼
                              public/atlas.png (512x512, ~30KB)
                              public/atlas.json (frame data)

Runtime: PixiJS loads atlas.json + atlas.png → Spritesheet object
         getSprite('bat') → returns Sprite from atlas frame
```

Fallback for missing sprites: SpriteLoader generates a colored rectangle (entity-type color) with the first letter of the entity name. This works even with zero sprite assets — the game is playable immediately.

Sprite mapping: `spriteMap.ts` maps entity class names → atlas frame names:

```typescript
const SPRITE_MAP: Record<string, string> = {
  Player: 'player',
  Bat: 'bat',
  Ground: 'ground',
  Wall: 'wall',
  Bladegrass: 'bladegrass',
  // ... all entities
};
```

---

## Project Structure

```
web/                              # New directory in this repo
├── index.html                    # Entry point (Vite template)
├── package.json                  # React, PixiJS, GSAP, Vite, etc.
├── tsconfig.json                 # Strict TS
├── vite.config.ts                # base: '/Twilight-Dungeons/' for GH Pages
├── scripts/
│   ├── generate-atlas.ts         # Build-time: PNGs → spritesheet atlas
│   └── generate-placeholders.ts  # Generate colored-rect placeholder sprites
├── public/
│   ├── atlas.png                 # Generated spritesheet (or placeholder)
│   └── atlas.json                # Atlas frame metadata
├── src/
│   ├── main.tsx                  # React root mount
│   ├── App.tsx                   # Top-level app: routing between Daily/Game/GameOver screens
│   │
│   ├── core/                     # Foundational types (no game logic, no UI)
│   │   ├── Vector2Int.ts         # {x,y} with static methods (add, distance, equals)
│   │   ├── MyRandom.ts           # Seeded PRNG (mulberry32)
│   │   ├── Modifiers.ts          # Modifier chain system (Of<T>, Process)
│   │   ├── EventEmitter.ts       # Simple typed event emitter (replaces C# events)
│   │   └── types.ts              # Shared enums: Faction, TileVisibility, CollisionLayer, ActionType
│   │
│   ├── model/                    # Direct port of /Assets/src/model/ — PURE TYPESCRIPT, NO UI
│   │   ├── Entity.ts
│   │   ├── GameModel.ts          # Simplified: single floor, no home/cave
│   │   ├── TurnManager.ts        # Synchronous step loop (no coroutines)
│   │   ├── Tile.ts               # Ground, Wall, Chasm, Water, HardGround, FancyGround, Signpost
│   │   ├── Body.ts               # HP, movement, damage pipeline
│   │   ├── Actor.ts              # Tasks, statuses, attack, step
│   │   ├── Player.ts             # 12 HP, inventory, equipment
│   │   ├── Status.ts             # Status + StackingStatus base classes
│   │   ├── Inventory.ts          # Fixed-size item storage with stacking
│   │   ├── Equipment.ts          # 5-slot equip system
│   │   ├── Item.ts               # Base item + interfaces (IWeapon, IDurable, IStackable, etc.)
│   │   ├── AIActor.ts            # AI base class
│   │   ├── Grass.ts              # Base grass class
│   │   ├── Floor.ts              # Tile grid, entity stores, pathfinding, visibility, BFS
│   │   ├── tasks/                # All 13 task types (one file each)
│   │   │   ├── ActorTask.ts
│   │   │   ├── AttackTask.ts
│   │   │   ├── ChaseTargetTask.ts
│   │   │   └── ... (all tasks)
│   │   ├── enemies/              # All 35+ enemy types (one file each)
│   │   │   ├── Bat.ts
│   │   │   ├── Goo.ts
│   │   │   └── ... (all enemies)
│   │   ├── grasses/              # All 24 grass types (one file each)
│   │   │   ├── Bladegrass.ts
│   │   │   ├── Web.ts
│   │   │   └── ... (all grasses)
│   │   ├── items/                # All item types
│   │   │   ├── ItemStick.ts
│   │   │   └── ... (all items)
│   │   └── statuses/             # Standalone status effects
│   │       └── index.ts
│   │
│   ├── generator/                # Direct port of /Assets/src/generator/
│   │   ├── FloorGenerator.ts     # generateSingleRoomFloor + supporting methods
│   │   ├── Encounters.ts         # All encounter definitions
│   │   ├── EncounterGroup.ts     # Weighted random bags of encounters
│   │   ├── Room.ts               # BSP room splitting
│   │   └── FloorUtils.ts        # CarveGround, NaturalizeEdges, Line3x3, etc.
│   │
│   ├── daily/                    # Daily puzzle specific logic
│   │   ├── DailyPuzzle.ts        # Date seed → puzzle config → generate level + equipment
│   │   └── Scoring.ts            # Turn counting, localStorage persistence
│   │
│   ├── renderer/                 # PixiJS rendering layer
│   │   ├── GameRenderer.ts       # Main renderer: creates PixiJS Application, manages layers
│   │   ├── SpriteManager.ts      # Load atlas, create/pool/recycle PixiJS Sprites
│   │   ├── AnimationPlayer.ts    # Game event queue → GSAP tweened animations on sprites
│   │   ├── ParticleEffects.ts    # Particle emitter configs for status effects, death, etc.
│   │   ├── FogOverlay.ts         # Fog of war: alpha overlay on unexplored/explored tiles
│   │   └── Camera.ts             # Viewport: tile size calc, centering, optional smooth follow
│   │
│   ├── ui/                       # React components
│   │   ├── GameScreen.tsx        # Main game screen: PixiJS canvas + HUD overlay
│   │   ├── PixiCanvas.tsx        # React component wrapping PixiJS Application
│   │   ├── HUD.tsx               # HP bar, turn counter, enemy count
│   │   ├── InventoryPanel.tsx    # Inventory + equipment UI with tap-to-use
│   │   ├── EntityInfo.tsx        # Popup: entity details on long-press
│   │   ├── GameOverScreen.tsx    # Win/loss: turns taken, share button
│   │   ├── DailyScreen.tsx       # Landing: date, play button, streak, past scores
│   │   └── StatusBar.tsx         # Active status effect icons
│   │
│   ├── input/                    # Input handling
│   │   └── InputHandler.ts       # Click/tap tile mapping, keyboard (arrows/WASD/numpad), swipe detection
│   │
│   └── hooks/                    # React hooks
│       ├── useGameModel.ts       # React context + re-render on model changes
│       └── usePixiApp.ts         # Create/destroy PixiJS Application lifecycle
```

## Key Porting Decisions

### 1. Modifier Chain System → Symbol-Tagged Interfaces

The C# code uses `IModifier<T>` interfaces discovered at runtime via `Modifiers.Of<T>()`. TypeScript equivalent using unique symbols for zero-cost type narrowing:

```typescript
// Symbol tags for each modifier type
const ATTACK_DAMAGE_MOD = Symbol('AttackDamageModifier');
const DAMAGE_TAKEN_MOD = Symbol('DamageTakenModifier');
const ACTION_COST_MOD = Symbol('ActionCostModifier');
// ... etc

interface ModifierProvider {
  getModifiers(): Iterable<object>;
}

// Collect handlers by checking for symbol property
function collectModifiers<T>(provider: ModifierProvider, tag: symbol): T[] {
  const result: T[] = [];
  for (const mod of provider.getModifiers()) {
    if (mod && typeof mod === 'object' && tag in mod) {
      result.push(mod as T);
    }
    // recurse into sub-providers
    if (mod && typeof (mod as any).getModifiers === 'function' && mod !== provider) {
      result.push(...collectModifiers(mod as ModifierProvider, tag));
    }
  }
  return result;
}

// Process: fold modifiers over initial value (same as C#)
function processModifiers<T>(modifiers: { modify(value: T): T }[], initial: T): T {
  return modifiers.reduce((val, mod) => mod.modify(val), initial);
}
```

Source: `/Assets/src/model/IModifier.cs` (lines 1-96)

### 2. Turn Manager → Synchronous Step + Animation Queue

The C# `TurnManager.StepUntilPlayersChoice()` is an IEnumerator coroutine that yields WaitForSeconds. In the web version:

1. Model step runs synchronously — all entities take turns until it's the player's choice again
2. During stepping, game events are recorded into an AnimationQueue
3. After model resolves, the AnimationPlayer plays back events as GSAP-tweened animations on PixiJS sprites
4. Player input is blocked during animation playback
5. After animations finish, React re-renders HUD from final model state

```typescript
async function handlePlayerAction(action: BaseAction) {
  // 1. Set player action on model
  model.player.setAction(action);

  // 2. Step model synchronously (records events)
  const events = model.stepUntilPlayerChoice();

  // 3. Animate events (async, takes ~200-500ms)
  await animationPlayer.playEvents(events);

  // 4. Sync renderer to final model state + re-render React HUD
  renderer.syncToModel(model);
  forceRerender();
}
```

Source: `/Assets/src/model/TurnManager.cs` (lines 71-182)

### 3. GameModel → Simplified Single-Floor

Remove: home floor, cave multi-floor, save/load, checkpoint, tutorial, attempts system, water meter, garden.
Keep: single floor, player, time, turnManager, eventQueue, timedEvents.

Source: `/Assets/src/model/GameModel.cs` (lines 1-270)

### 4. Daily Puzzle Seeding

```typescript
function getDailySeed(date: Date): number {
  const str = `${date.getFullYear()}-${String(date.getMonth()+1).padStart(2,'0')}-${String(date.getDate()).padStart(2,'0')}`;
  // djb2 hash
  let hash = 5381;
  for (const ch of str) hash = ((hash << 5) + hash) + ch.charCodeAt(0);
  return hash >>> 0;
}
```

The seed determines: floor dimensions (9-14 x 7-9), number of enemies (2-8), number of grasses (1-5), encounter group tier, and starting equipment. Uses existing `FloorGenerator.generateSingleRoomFloor()` which already accepts these params.

### 5. Win Condition & Scoring

- **Win:** All enemies dead (`floor.enemiesLeft() === 0`)
- **Score:** `Math.floor(GameModel.main.time)` (total turns taken, lower = better)
- **Stored:** localStorage key `td-daily-{YYYY-MM-DD}` → `{ turns, won, date }`

### 6. Vector2Int

C# uses operator overloading (`a + b`). TypeScript can't. Use static methods:

```typescript
class Vector2Int {
  constructor(public readonly x: number, public readonly y: number) {}
  static add(a: Vector2Int, b: Vector2Int): Vector2Int { return new Vector2Int(a.x + b.x, a.y + b.y); }
  static distance(a: Vector2Int, b: Vector2Int): number { ... }
  static equals(a: Vector2Int, b: Vector2Int): boolean { return a.x === b.x && a.y === b.y; }
  static readonly zero = new Vector2Int(0, 0);
  static readonly up = new Vector2Int(0, 1);
  static readonly down = new Vector2Int(0, -1);
  static readonly left = new Vector2Int(-1, 0);
  static readonly right = new Vector2Int(1, 0);
}
```

## Content to Port (ALL)

**Enemies (35 types — port all)**
Bat, Bird, Blob, Bloodstone, Boombug, Butterfly, CheshireWeed, Clumpshroom, Crab, Dizapper, FruitingBody, Golem, Goo, Grasper, HardShell, Healer, Hopper, HydraHeart, IronJelly, Jackal, Octopus, Parasite, Scorpion, Scuttler, Shielder, Skully, Snail, Snake, Spider, Thistlebog, Wallflower, Wildekin + bosses (Blobmother, FungalColony)

**Grasses (24 types — port all)**
Agave, Astoria, Bladegrass, Bloodwort, Brambles, Dandypuff, Deathbloom, DeathlyCreeper, EveningBells, Fern, Guardleaf, HangingVines, Llaora, Mushroom, Necroroot, Ninetails, Poisonmoss, Redcap, SoftGrass, Spores, Tunnelroot, VibrantIvy, Violets + Web (from Spider)

**Items (16+ types — port all)**
ItemHands, ItemStick, ItemRedberry, ItemPumpkinHelmet, ItemWoodShield, ItemMushroom, ItemCharmBerry, ItemPumpkin, ItemSeed, ItemWildwoodLeaf, ItemWildwoodWreath, ItemBatTooth + enemy drops (ItemGloopShoes, ItemJackalHide, ItemSnailShell, ItemBoombugCorpse)

**Status Effects (30+ — all that are referenced by the above content)**

**Tasks (13 types — port all)**
AttackTask, AttackGroundTask, ChaseTargetTask, ChaseDynamicTargetTask, MoveToTargetTask, MoveNextToTargetTask, MoveRandomlyTask, FollowPathTask, RunAwayTask, WaitTask, SleepTask, GenericTask, TelegraphedTask

**Tiles (8 types)**
Ground, Wall, Chasm, Water, HardGround, FancyGround, Soil, Signpost

## Implementation Phases

### Phase 1: Foundation — Grid, Movement, Rendering (React + PixiJS scaffold)

1. Set up Vite project: `npm create vite@latest web -- --template react-ts`
2. Install deps: pixijs, gsap, @pixi/particle-emitter
3. Port Vector2Int, MyRandom (seeded mulberry32 PRNG), EventEmitter
4. Port Modifiers.ts (symbol-tagged modifier chain + Process fold)
5. Port core types.ts (Faction, TileVisibility, CollisionLayer, ActionType enums)
6. Port Entity, Tile (Ground + Wall only), Floor (tile grid, enumeration, BFS pathfinding, line-of-sight visibility)
7. Port Body, Actor, Player (HP, basic movement, basic attack)
8. Port BaseAction (Move, Attack, Wait) + ActionCosts
9. Port ActorTask base + WaitTask + AttackTask + FollowPathTask
10. Port TurnManager (synchronous step loop, emits game events)
11. Port GameModel (simplified single-floor daily version)
12. Build SpriteManager — load spritesheet atlas with colored-rectangle fallbacks
13. Build GameRenderer — PixiJS Application with layered containers (tiles, grasses, bodies, effects, fog)
14. Build PixiCanvas.tsx — React component wrapping PixiJS lifecycle
15. Build AnimationPlayer — basic move/attack tweens via GSAP
16. Build InputHandler — click/tap on tile → player action, keyboard arrows/WASD/numpad
17. Build HUD.tsx — HP bar, turn counter
18. Build App.tsx — wire it all together

**Deliverable:** Player walks around a hardcoded small grid, fog of war works, movement animates smoothly at 60fps, can bump walls.

### Phase 2: Combat + First Enemies

1. Port AIActor base, StatusList, Status, StackingStatus
2. Port Inventory, Equipment, Item, EquippableItem + interfaces (IWeapon, IDurable, IStackable, IUsable, IEdible)
3. Port remaining task types (ChaseTarget, MoveRandomly, MoveToTarget, ChaseDynamic, Sleep, RunAway, etc.)
4. Port first wave of enemies: Bat, Goo, Spider, Scorpion, Snail, Crab (6 diverse types testing different AI patterns)
5. Port associated statuses: WebbedStatus, PoisonedStatus, SurprisedStatus, InShellStatus, SlimedStatus
6. Port first grasses: Web, Bladegrass, SoftGrass, Guardleaf
7. Port first items: ItemHands, ItemStick, ItemRedberry
8. Port Grass base class + grass BodyModifier system
9. Add combat animations: attack bump, damage flash, death shrink+fade, heal glow
10. Add particle effects: poison bubbles, web strands
11. Build InventoryPanel.tsx — tap-to-use/equip items
12. Build StatusBar.tsx — active status effect icons
13. Implement floor clearing logic + basic game-over detection

**Deliverable:** Can fight enemies, use items, status effects work with particles, clear a level.

### Phase 3: Level Gen + All Content

1. Port Room, FloorUtils, FloorGenerator.generateSingleRoomFloor()
2. Port Encounters, EncounterGroup, WeightedRandomBag
3. Port ensureConnectedness algorithm
4. Port all remaining enemies (29 more types)
5. Port all remaining grasses (20 more types)
6. Port all remaining items + status effects
7. Port CollisionLayer flags properly (Flying movement for Bat, etc.)
8. Port boss encounters (Blobmother, FungalColony) for special days
9. Build DailyPuzzle.ts — date seeding, difficulty config, starting equipment selection
10. Build DailyScreen.tsx — landing page: today's date, "Play" button, past scores, streak

**Deliverable:** Full content, daily-seeded procedural levels, all enemies/grasses/items functional.

### Phase 4: Polish + Deploy

1. Build EntityInfo.tsx — long-press/right-click to see entity details
2. Build GameOverScreen.tsx — win/loss with turn score, share-result button (copy to clipboard)
3. Mobile polish: swipe-to-move, responsive tile sizing (CSS dvh), touch-friendly inventory
4. Add particle effects for all remaining content (grass enter, status start/end, floor clear celebration)
5. Add smooth camera follow if needed (PixiJS container offset)
6. localStorage: streak tracking, personal bests per day, completion calendar
7. Spritesheet generation script: `scripts/generate-atlas.ts` using free-tex-packer-core
8. GitHub Pages deployment: Vite `base: '/Twilight-Dungeons/'`, gh-pages package, `npm run deploy` script
9. `<meta>` tags for sharing (OG image, description, title)
10. Optional: "speed mode" toggle — skip animations, instant turns

**Deliverable:** Live on GitHub Pages, playable daily puzzle with all content, animations, particles, 60fps mobile.

## Performance Budget

| Metric | Target | How |
|-|-|-|
| Initial load | < 500KB gzipped | Vite tree-shaking, single atlas PNG, code-split React lazily |
| First paint | < 1s | Atlas preload, skeleton HUD render |
| Frame rate | 60fps on mobile | WebGL via PixiJS, single draw call, sprite pooling |
| Turn response | < 50ms model + ~300ms animation | Sync model step, async tween playback |
| Atlas size | < 100KB | 200 sprites x 16x16px in 512x512 atlas |

## Key C# Source Files → TypeScript Mapping

| C# Source | TS Target | Lines | Notes |
|-|-|-|-|
| model/IModifier.cs | core/Modifiers.ts | 96 | Symbol-tagged modifier pattern |
| model/Entity.cs | model/Entity.ts | 146 | Remove Unity deps |
| model/GameModel.cs | model/GameModel.ts | 270 | Simplify to single-floor |
| model/TurnManager.cs | model/TurnManager.ts | 183 | Sync loop + event recording |
| model/Tile.cs | model/Tile.ts | ~300 | Direct port |
| model/actors/Body.cs | model/Body.ts | 213 | Direct port |
| model/actors/Actor.cs | model/Actor.ts | ~300 | Direct port |
| model/actors/Player.cs | model/Player.ts | ~200 | Remove water, garden logic |
| model/actors/Status.cs | model/Status.ts | ~100 | Direct port |
| model/Inventory.cs | model/Inventory.ts | ~150 | Direct port |
| model/Equipment.cs | model/Equipment.ts | ~100 | Direct port |
| model/floors/Floor.cs | model/Floor.ts | 486 | Direct port |
| model/floors/EntityStore.cs | model/Floor.ts | ~150 | Inline into Floor |
| model/floors/FloorEnumeratorExtensions.cs | model/Floor.ts | ~200 | Methods on Floor class |
| model/floors/PathfindingManager.cs | model/Floor.ts | ~100 | BFS pathfinding |
| model/MyRandom.cs | core/MyRandom.ts | ~50 | mulberry32 PRNG |
| generator/FloorGenerator.cs | generator/FloorGenerator.ts | 777 | Port generateSingleRoomFloor focus |
| generator/Encounters.cs | generator/Encounters.ts | 933 | Port all encounters |
| generator/Room.cs | generator/Room.ts | ~150 | BSP splitting |

## Verification Plan

1. **Manual play test:** Open localhost:5173, verify daily puzzle generates, player can move/attack/use items, enemies act correctly, level clears
2. **60fps test:** Open Chrome DevTools Performance tab on mobile — verify consistent 60fps during animations
3. **Determinism test:** Open the same day's puzzle in two tabs — verify identical level layout (same seed, same RNG sequence)
4. **Mobile test:** Open on phone browser — verify touch input, swipe, responsive tile sizing, no horizontal scroll
5. **Content spot-check:** Verify at least 5 different enemy types behave correctly (Bat flies + sleeps, Goo splits, Spider webs, Boombug explodes, Snail shells)
6. **Animation test:** Verify move/attack/death/status animations play correctly and don't block input afterward
7. **Deploy test:** Run `npm run build`, serve `dist/` statically, verify it works at correct base path
8. **Cross-day test:** Change system date, verify new puzzle generates with different layout
