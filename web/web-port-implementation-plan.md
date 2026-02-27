# Implementation Plan: Twilight Dungeons Web Puzzle Port

## Context

Port a Unity 2D C# roguelike (~20.6K lines of game logic across ~90 core files + ~200 sprite PNGs) to a TypeScript/React/PixiJS daily puzzle web game. The C# model layer is largely pure game logic with minimal Unity coupling (mainly `Vector2Int`, `Mathf`, `Debug`, `IEnumerator` coroutines). No `web/` directory exists yet — build from scratch.

**Why the hybrid approach:** The task has two distinct shapes:
- **Foundation + patterns** (Phases 1-2): deeply interdependent files where every interface decision affects everything downstream. Must be sequential, careful, verified.
- **Bulk content** (Phase 3): 29 remaining enemies, 20 remaining grasses, 10+ remaining items — structurally identical, embarrassingly parallel once patterns are established.

---

## Pre-Implementation: Conventions & Contract Documents

Before any code, create two reference documents that persist across all sessions:

### `web/PORTING_CONVENTIONS.md`
Written after Phase 1, refined after Phase 2. Contains:
- Unity → TS mapping table (Vector2Int → Vec2, Mathf → Math, Debug.Log → console.log, [Serializable] → remove, IEnumerator → synchronous)
- Import conventions, file naming, export patterns
- How to register modifiers (symbol tags)
- How enemies/grasses/items export and register themselves
- Multi-class C# file splitting rules (Spider.cs → Spider.ts + Web.ts + WebbedStatus.ts + PoisonedStatus.ts + ItemSpiderSandals.ts)

### `web/claude-progress.txt`
Updated every session. Contains: completed files checklist, current session scope, design decisions made, known issues, next steps.

---

## Phase 1: Foundation (Sessions 1-6)

### Session 1 — Project Scaffold + Core Types
- `npm create vite@latest web -- --template react-ts`
- Install deps: `pixijs@^8`, `gsap`, `@pixi/particle-emitter`
- Port `core/Vector2Int.ts` — static methods (add, sub, distance, equals, manhattanDistance), directional constants. Source: Vector2Int used ubiquitously as `{x,y}` with operator overloading → static methods.
- Port `core/MyRandom.ts` — mulberry32 seeded PRNG. Source: `model/MyRandom.cs` (~50 lines)
- Port `core/Modifiers.ts` — symbol-tagged modifier chain. Source: `model/IModifier.cs` (95 lines). Define all 8 modifier symbols: ATTACK_DAMAGE, DAMAGE_TAKEN, ACTION_COST, BASE_ACTION, STEP, MAX_HP, MOVEMENT_LAYER, ANY_DAMAGE_TAKEN
- Port `core/types.ts` — enums: Faction, TileVisibility, CollisionLayer (flags), ActionType
- Port `core/EventEmitter.ts` — typed event emitter replacing C# `event` delegates
- **Verify:** `tsc --noEmit` passes

### Session 2 — Entity + Tile + Floor
- Port `model/Entity.ts` from `Entity.cs` (145 lines) — guid, floor ref, pos (abstract), isDead, timedEvents, MyModifiers (IModifierProvider)
- Port `model/Tile.ts` from `Tile.cs` (300 lines) — abstract Tile + Ground, Wall, HardGround, FancyGround, Chasm, Water, Soil, Signpost. Remove Upstairs/Downstairs (single floor).
- Port `model/Floor.ts` from `Floor.cs` (486) + EntityStore.cs (170) + FloorEnumeratorExtensions.cs (164) + PathfindingManager.cs (131) — inline EntityStore into Floor. Tile grid, body list, BFS pathfinding, line-of-sight visibility, Put/Remove entity dispatch.
- **Key decision:** Floor tile storage — use `Map<string, Tile>` keyed by `"x,y"` or 2D array `Tile[][]`. Array is faster for grid access, matches C# `StaticEntityGrid`.
- **Verify:** `tsc --noEmit`, write a small test: create Floor, place tiles, test pathfinding

### Session 3 — Body + Actor + Player + Actions
- Port `model/Body.ts` from `Body.cs` (212 lines) — HP, pos with collision, TakeDamage/TakeAttackDamage/Heal, movement
- Port `model/Actor.ts` from `Actor.cs` (292 lines) — task/taskQueue, StatusList, faction, Step() logic, modifier pipeline integration
- Port `model/Player.ts` from `Player.cs` (170 lines) — 12 HP, inventory ref, equipment ref, visibility recalc on move. Remove: water, garden, tutorial hooks.
- Port `model/BaseAction.ts` from `BaseAction.cs` (112 lines) — Move, Attack, Wait, Use actions + ActionCosts
- **Verify:** `tsc --noEmit`

### Session 4 — Tasks + TurnManager + GameModel
- Port `model/tasks/ActorTask.ts` — base class + DoOnceTask (from tasks/, 12 files, ~491 lines total)
- Port first tasks: WaitTask, AttackTask, FollowPathTask, ChaseTargetTask, MoveRandomlyTask, MoveToTargetTask
- Port `model/AIActor.ts` from `enemies/AIActor.cs` (57 lines) — abstract GetNextTask(), HandleDeath()
- Port `model/TurnManager.ts` from `TurnManager.cs` (182 lines) — **critical transformation:** IEnumerator coroutine → synchronous `stepUntilPlayerChoice()` that returns `GameEvent[]`. No yields, no WaitForSeconds. Record events instead of waiting.
- Port `model/GameModel.ts` from `GameModel.cs` (313 lines) — **simplify:** single floor only, remove home/cave/depth/checkpoint/tutorial/water meter/garden. Keep: player, floor, time, turnManager, eventQueue, timedEvents.
- Port remaining tasks: SleepTask, RunAwayTask, ChaseDynamicTargetTask, MoveNextToTargetTask, GenericTask, TelegraphedTask, AttackGroundTask
- **Verify:** `tsc --noEmit`, instantiate GameModel with a small floor + player, call stepUntilPlayerChoice

### Session 5 — Rendering Layer
- Build `scripts/generate-atlas.ts` — free-tex-packer-core: combine ~200 PNGs from `Assets/Textures/Resources/` + `Assets/Textures/Plants/` into atlas.png + atlas.json. Git LFS sprites are available — no fallback placeholders needed.
- Build `renderer/SpriteManager.ts` — load atlas.json/atlas.png via PixiJS Spritesheet, entity displayName → sprite frame mapping
- Build `renderer/GameRenderer.ts` — PixiJS Application, layered containers (tiles, grasses, items, bodies, effects, fog), syncToModel() method
- Build `renderer/FogOverlay.ts` — alpha overlay for Unexplored/Explored/Visible
- Build `renderer/Camera.ts` — tile size calc, viewport centering
- Build `renderer/AnimationPlayer.ts` — GameEvent[] → GSAP tweens on sprites. Events: move (slide 150ms), attack (bump 120ms), damage (flash 100ms), death (shrink+fade 300ms)
- **Verify:** `npm run dev`, see a rendered grid with real sprites

### Session 6 — Input + HUD + Wire-Up
- Build `input/InputHandler.ts` — click/tap tile → player action mapping, keyboard (arrows/WASD/numpad), swipe detection for mobile
- Build `ui/PixiCanvas.tsx` — React component wrapping PixiJS lifecycle (mount/unmount)
- Build `ui/HUD.tsx` — HP bar, turn counter, enemy count
- Build `ui/App.tsx` — wire GameModel + GameRenderer + InputHandler + HUD
- Build `hooks/useGameModel.ts` — React context + forceUpdate on model step
- Build `hooks/usePixiApp.ts` — PixiJS Application create/destroy lifecycle
- **Verify:** `npm run dev` — player walks around a hardcoded small grid, fog works, movement animates, can bump walls
- **Write `PORTING_CONVENTIONS.md`** — document all patterns established so far

### Phase 1 Gate
- `npm run build` succeeds with zero errors
- Manual playtest: player moves, fog reveals, walls block, animations play at 60fps
- Freeze all core type interfaces (Entity, Body, Actor, Floor, GameModel, TurnManager, Tile, BaseAction, ActorTask, Modifiers)

---

## Phase 2: Combat + First Content (Sessions 7-10)

### Session 7 — Status System + Inventory + Equipment + Items
- Port `model/Status.ts` + `model/StatusList.ts` from `actors/Status.cs` (172 lines) — Status base, StackingStatus, StackingMode enum, StatusList management
- Port `model/Inventory.ts` from `Inventory.cs` (157 lines) — fixed-size storage, stacking
- Port `model/Equipment.ts` from `Equipment.cs` (57 lines) — 5-slot equip system
- Port `model/Item.ts` from `items/Item.cs` (61 lines) + interfaces: IWeapon (3), IDurable (19), IStackable (40), IUsable, IEdible
- Port `model/Grass.ts` base class — pos, OnNoteworthyAction, IActorEnterHandler pattern
- Port first items: ItemHands, ItemStick, ItemRedberry
- **Verify:** `tsc --noEmit`

### Session 8 — First 6 Enemies + Associated Content
- Port enemies: Bat (96), Goo (~80), Spider (241 — split into Spider.ts + WebbedStatus.ts + PoisonedStatus.ts), Scorpion (32), Snail (~60), Crab (40)
- Port associated statuses: WebbedStatus, PoisonedStatus, SurprisedStatus (from SleepTask), InShellStatus, SlimedStatus
- Port first grasses: Web (from Spider.cs), Bladegrass, SoftGrass, Guardleaf
- **Verify:** `tsc --noEmit`, spawn enemies on test floor, step model, verify AI behaviors

### Session 9 — Combat Animations + Particles
- Enhance AnimationPlayer: attack bump+recoil, damage flash+shake, death shrink+fade+particles, heal glow
- Build `renderer/ParticleEffects.ts` — particle configs for poison bubbles, web strands, grass enter puffs
- Add status effect visual indicators on sprites
- **Verify:** `npm run dev` — fight enemies, see all animations play correctly

### Session 10 — Inventory UI + Game Over + Phase 2 Gate
- Build `ui/InventoryPanel.tsx` — tap-to-use/equip items, equipment display
- Build `ui/StatusBar.tsx` — active status effect icons
- Implement floor clearing logic (`floor.enemiesLeft() === 0`)
- Implement basic game over detection (player death / all enemies dead)
- **Verify:** Full combat loop works — fight enemies, use items, status effects apply with particles, clear level triggers win
- **Update `PORTING_CONVENTIONS.md`** with enemy/grass/item porting patterns + concrete examples

### Phase 2 Gate
- Manual playtest: fight 6 enemy types, use 3 item types, statuses work, level clears
- Document the porting template: "here's Bat.cs, here's Bat.ts, follow this pattern"

---

## Phase 3: Level Gen + All Content (Sessions 11-16)

### Session 11 — Floor Generator
- Port `generator/Room.ts` from `Room.cs` (236 lines) — BSP tree, split, shrink, traverse, connections
- Port `generator/FloorUtils.ts` from `FloorUtils.cs` (146 lines) — CarveGround, NaturalizeEdges, EmptyTilesInRoom, etc.
- Port `generator/TileSection.ts` (211) + `TileSectionConcavity.ts` (112) + `ShapeTransform.ts` (94)
- Port `generator/FloorGenerator.ts` from `FloorGenerator.cs` (777 lines) — focus on `generateSingleRoomFloor`. Remove multi-floor/boss-floor/cave generation.
- **Verify:** Generate a floor, inspect tile layout, ensure connectedness

### Session 12 — Encounter System
- Port `generator/Encounters.ts` from `Encounters.cs` (933) + `EnemyEncounters.cs` (327) — all encounter delegates. These reference all enemy/grass/item constructors, so stub any not-yet-ported as TODOs.
- Port `generator/EncounterGroup.ts` (131) + EarlyGame/MidGame/Everything encounter groups
- Port `daily/DailyPuzzle.ts` — date seed → difficulty config → floor generation
- **Verify:** Generate daily-seeded floor with encounters populated

### Sessions 13-15 — Parallel Content Porting (3 batches)

Each session spawns 4-6 parallel Task agents in worktrees. Each agent gets:
- `PORTING_CONVENTIONS.md`
- The core type interfaces
- One C# source file
- Instruction: "Port to TypeScript following conventions. Split multi-class files. Run tsc --noEmit."

**Batch 1 (~12 enemies):** Bird, Blob, Bloodstone, Boombug, Butterfly, CheshireWeed, Clumpshroom, Dizapper, FruitingBody, Golem, Grasper, HardShell

**Batch 2 (~12 enemies + grasses):** Healer, Hopper, HydraHeart, IronJelly, Jackal, Octopus, Parasite, Scuttler, Shielder, Skully, Snake, Wallflower, Wildekin + Blobmother, FungalColony. Also: Agave, Astoria, Bloodwort, Brambles, Dandypuff, Deathbloom

**Batch 3 (remaining grasses + items):** DeathlyCreeper, EveningBells, Fern, HangingVines, Llaora, Mushroom, Necroroot, Ninetails, Poisonmoss, Redcap, Spores, Tunnelroot, VibrantIvy, Violets. Items: ItemPumpkinHelmet, ItemWoodShield, ItemMushroom, ItemCharmBerry, ItemPumpkin, ItemSeed, ItemWildwoodLeaf, ItemWildwoodWreath, ItemBatTooth, ItemGloopShoes, ItemJackalHide, ItemSnailShell, ItemBoombugCorpse

After each batch: merge, run `tsc --noEmit`, fix integration issues.

### Session 16 — Integration + Encounter Wiring
- Wire all ported content into Encounters.ts (replace stubs)
- Register all enemies/grasses/items in sprite map
- Verify all encounter groups can spawn all their content
- **Verify:** `npm run build` succeeds, generate floors with all content types

### Phase 3 Gate
- Generate 10 different daily seeds, spot-check enemy diversity
- `npm run build` zero errors

---

## Phase 4: Polish + Deploy (Sessions 17-18)

### Session 17 — Daily Puzzle UI + Scoring
- Build `ui/DailyScreen.tsx` — landing page: today's date, "Play" button, past scores
- Build `ui/GameOverScreen.tsx` — win/loss with turn score, share-result button (copy to clipboard)
- Build `daily/Scoring.ts` — localStorage persistence, streak tracking
- Build `ui/EntityInfo.tsx` — long-press/right-click entity details popup
- Mobile polish: responsive tile sizing, touch-friendly inventory, swipe tuning

### Session 18 — Deployment + Final Polish
- Configure Vite: `base: '/Twilight-Dungeons/'` for GitHub Pages
- Add gh-pages package, `npm run deploy` script
- `<meta>` tags for sharing (OG image, description, title)
- Optional: "speed mode" toggle (skip animations)
- **Verify:** `npm run build && npx serve dist/`, full playtest on desktop + mobile
- Deploy to GitHub Pages

---

## Per-Session Protocol

1. Read `web/claude-progress.txt` + `git log --oneline -10`
2. Read relevant C# source files for this session's scope
3. Write TS ports, run `tsc --noEmit` after each file
4. Update `web/claude-progress.txt` — checked-off list of completed files, decisions, issues
5. Commit with descriptive message
6. `/clear` if context is heavy before next session

## Anti-Drift Safeguards

- Every session starts by re-reading `PORTING_CONVENTIONS.md`
- Scope-lock: each session prompt says "you are ONLY working on [X]. Do not refactor existing code."
- `tsc --noEmit` after every file write catches interface mismatches immediately
- Parallel workers get the conventions doc + a concrete template file (Bat.ts) as reference
- Progress file uses explicit per-file checklists, not prose descriptions

## Key Porting Gotchas (Reference)

| C# Pattern | TypeScript Equivalent |
|-|-|
| `Vector2Int` operator overloading | `Vec2.add(a, b)` static methods |
| `IEnumerator` coroutine yields | Synchronous step, return `GameEvent[]` |
| `[Serializable]` | Remove (no save/load in daily puzzle) |
| `IModifier<T>` runtime discovery | Symbol-tagged interfaces + `collectModifiers()` |
| C# `event` delegates | Custom `EventEmitter<T>` |
| `Mathf.Clamp/Min/Max` | `Math.min/max`, custom `clamp()` |
| `Debug.Log/Assert` | `console.log/assert` |
| Multi-class files (Spider.cs) | Split to one-class-per-file |
| `GameModel.main` singleton | Module-level `let gameModel: GameModel` or React context |
| `EnqueueEvent(Action)` | `eventQueue.push(() => ...)` + `drainQueue()` |
| `AddTimedEvent(float, Action)` | `timedEvents.add(gameTime, callback)` |
| C# `(int, int)` tuples | `[number, number]` |
| `CollisionLayer` flags enum | TypeScript numeric enum with bitwise ops |

## Estimated Effort
~18 sessions. Critical path: Phases 1-2 (10 sessions, sequential). Phase 3 content porting can be heavily parallelized. Phase 4 is 2 sessions.
