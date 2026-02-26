import { WeightedRandomBag } from './WeightedRandomBag';
import type { Floor } from '../model/Floor';
import type { Room } from './Room';

/** An encounter is a function that populates a floor region */
export type Encounter = (floor: Floor, room: Room | null) => void;

/** Weighted bag of encounters — extends WeightedRandomBag with encounter-specific typing */
export class EncounterBag extends WeightedRandomBag<Encounter> {}

/**
 * Encounter groups organize encounters by category and difficulty tier.
 * Port of C# EncounterGroup.cs + EncounterGroupShared.
 */
export class EncounterGroup {
  mobs: EncounterBag;
  spice: EncounterBag;
  grasses: EncounterBag;
  walls: EncounterBag;
  chasms: EncounterBag;
  rewards: EncounterBag;
  plants: EncounterBag;
  rests: EncounterBag;

  constructor() {
    this.mobs = new EncounterBag();
    this.spice = new EncounterBag();
    this.grasses = new EncounterBag();
    this.walls = new EncounterBag();
    this.chasms = new EncounterBag();
    this.rewards = new EncounterBag();
    this.plants = new EncounterBag();
    this.rests = new EncounterBag();
  }

  /** Copy shared bags (walls, chasms, rewards, plants, rests) from a source group */
  assignShared(source: EncounterGroup): this {
    this.walls = source.walls;
    this.chasms = source.chasms;
    this.rewards = source.rewards;
    this.plants = source.plants;
    this.rests = source.rests;
    return this;
  }
}

// Encounters are defined in Encounters.ts, but we need to create groups here
// that reference them. We use a lazy initialization pattern to break the circular
// dependency: groups are populated when createEncounterGroups() is called.

export interface EncounterGroupSet {
  shared: EncounterGroup;
  earlyGame: EncounterGroup;
  everything: EncounterGroup;
  midGame: EncounterGroup;
}

/**
 * Create all encounter groups with properly weighted bags.
 * Called by FloorGenerator constructor after Encounters module is loaded.
 */
export function createEncounterGroups(E: Record<string, Encounter>): EncounterGroupSet {
  // Shared encounters (walls, chasms, rewards, plants, rests)
  const shared = new EncounterGroup();

  shared.walls = new EncounterBag();
  shared.walls.add(8.5, E.empty);
  shared.walls.add(1, E.wallPillars);
  shared.walls.add(1, E.concavity);
  shared.walls.add(1, E.chunkInMiddle);
  shared.walls.add(1, E.lineWithOpening);
  shared.walls.add(1, E.insetLayerWithOpening);
  shared.walls.add(1, E.addStalk);
  shared.walls.add(1, E.rubbleCluster);
  shared.walls.add(0.5, E.chasmsAwayFromWalls2);

  shared.chasms = new EncounterBag();
  shared.chasms.add(19, E.empty);
  shared.chasms.add(2, E.chasmBridge);
  shared.chasms.add(1, E.chasmGrowths);

  shared.rewards = new EncounterBag();
  shared.rewards.add(20, E.empty);
  shared.rewards.add(1, E.addMushroom);
  shared.rewards.add(1, E.addPumpkin);
  shared.rewards.add(1, E.addGambler);
  shared.rewards.add(1, E.addSpiderSandals);
  shared.rewards.add(1, E.oneButterfly);

  shared.plants = new EncounterBag();
  shared.plants.add(1, E.matureBerryBush);
  shared.plants.add(1, E.matureThornleaf);
  shared.plants.add(1, E.matureWildWood);
  shared.plants.add(1, E.matureWeirdwood);
  shared.plants.add(1, E.matureKingshroom);
  shared.plants.add(1, E.matureFrizzlefen);
  shared.plants.add(1, E.matureChangErsWillow);
  shared.plants.add(1, E.matureStoutShrub);
  shared.plants.add(1, E.matureBroodpuff);

  shared.rests = new EncounterBag();

  // Early game encounters
  const earlyGame = new EncounterGroup();
  earlyGame.assignShared(shared);

  earlyGame.mobs = new EncounterBag();
  earlyGame.mobs.add(1.0, E.addWallflowers);
  earlyGame.mobs.add(1.0, E.addBird);
  earlyGame.mobs.add(1.0, E.addSnake);
  earlyGame.mobs.add(1.0, E.aFewBlobs);
  earlyGame.mobs.add(1.0, E.jackalPile);
  earlyGame.mobs.add(1.0, E.addSkullys);
  earlyGame.mobs.add(1.0, E.addOctopus);
  earlyGame.mobs.add(1.0, E.aFewSnails);
  earlyGame.mobs.add(0.33, E.addSpiders);

  earlyGame.grasses = new EncounterBag();
  earlyGame.grasses.add(1, E.addSoftGrass);
  earlyGame.grasses.add(0.6, E.addLlaora);
  earlyGame.grasses.add(0.6, E.addGuardleaf);
  earlyGame.grasses.add(0.6, E.addBladegrass);
  earlyGame.grasses.add(0.5, E.scatteredBoombugs);
  earlyGame.grasses.add(0.5, E.addEveningBells);
  earlyGame.grasses.add(0.4, E.addDeathbloom);
  earlyGame.grasses.add(0.4, E.addWebs);
  earlyGame.grasses.add(0.4, E.addAgave);
  earlyGame.grasses.add(0.35, E.addHangingVines);
  earlyGame.grasses.add(0.2, E.addViolets);
  earlyGame.grasses.add(0.2, E.fillWithFerns);

  earlyGame.spice = new EncounterBag();
  earlyGame.spice.add(5, E.empty);
  earlyGame.spice.add(0.5, E.addFruitingBodies);
  earlyGame.spice.add(0.5, E.addScuttlers);
  earlyGame.spice.add(0.25, E.addSoftGrass);
  earlyGame.spice.add(0.25, E.addBladegrass);
  earlyGame.spice.add(0.2, E.scatteredBoombugs);
  earlyGame.spice.add(0.2, E.addWater);
  earlyGame.spice.add(0.1, E.addOldDude);
  earlyGame.spice.add(0.1, E.addDeathbloom);
  earlyGame.spice.add(0.1, E.addGuardleaf);
  earlyGame.spice.add(0.1, E.addSpore);
  earlyGame.spice.add(0.05, E.addEveningBells);
  earlyGame.spice.add(0.05, E.addPoisonmoss);
  earlyGame.spice.add(0.05, E.fillWithFerns);
  earlyGame.spice.add(0.01, E.addNecroroot);
  earlyGame.spice.add(0.01, E.addFaegrass);

  // Everything (early-mid mixed) encounters
  const everything = new EncounterGroup();
  everything.assignShared(shared);

  everything.mobs = new EncounterBag();
  everything.mobs.add(1, E.addFungalBreeder);
  everything.mobs.add(1, E.addFungalSentinel);
  everything.mobs.add(1, E.addBats);
  everything.mobs.add(1, E.addSpiders);
  everything.mobs.add(1, E.addScorpions);
  everything.mobs.add(1, E.addThistlebog);
  everything.mobs.add(1, E.addGolems);
  everything.mobs.add(0.5, E.addClumpshroom);
  everything.mobs.add(0.5, E.addGrasper);
  everything.mobs.add(0.5, E.addParasite);
  everything.mobs.add(0.2, E.addHydra);

  everything.grasses = new EncounterBag();
  everything.grasses.add(1, E.addVibrantIvy);
  everything.grasses.add(1, E.addSpore);
  everything.grasses.add(1, E.addBloodwort);
  everything.grasses.add(0.75, E.addBladegrass);
  everything.grasses.add(0.5, E.addTunnelroot);
  everything.grasses.add(0.5, E.addBrambles);
  everything.grasses.add(0.5, E.addSoftGrass);
  everything.grasses.add(0.5, E.scatteredBoombugs);
  everything.grasses.add(0.5, E.addDeathbloom);
  everything.grasses.add(0.5, E.addGuardleaf);
  everything.grasses.add(0.4, E.addPoisonmoss);
  everything.grasses.add(0.4, E.addWebs);
  everything.grasses.add(0.4, E.addViolets);
  everything.grasses.add(0.4, E.addHangingVines);
  everything.grasses.add(0.35, E.addEveningBells);
  everything.grasses.add(0.2, E.addAgave);
  everything.grasses.add(0.05, E.fillWithFerns);

  everything.spice = new EncounterBag();
  everything.spice.add(5, E.empty);
  everything.spice.add(0.25, E.aFewBlobs);
  everything.spice.add(0.25, E.jackalPile);
  everything.spice.add(0.25, E.aFewSnails);
  everything.spice.add(0.25, E.addScuttlers);
  everything.spice.add(0.25, E.addFruitingBodies);
  everything.spice.add(0.25, E.addBloodstone);
  everything.spice.add(0.1, E.addWater);
  everything.spice.add(0.05, E.addEveningBells);
  everything.spice.add(0.05, E.addPoisonmoss);
  everything.spice.add(0.05, E.addTunnelroot);
  everything.spice.add(0.02, E.addFaegrass);
  everything.spice.add(0.02, E.addNecroroot);
  everything.spice.add(0.01, E.addHydra);

  // Mid game encounters
  const midGame = new EncounterGroup();
  midGame.assignShared(shared);

  midGame.mobs = new EncounterBag();
  midGame.mobs.add(1, E.addHoppers);
  midGame.mobs.add(1, E.addScorpions);
  midGame.mobs.add(1, E.addWildekins);
  midGame.mobs.add(1, E.addDizapper);
  midGame.mobs.add(1, E.addGoo);
  midGame.mobs.add(1, E.addHardShell);
  midGame.mobs.add(0.5, E.addHealer);
  midGame.mobs.add(0.5, E.addPoisoner);
  midGame.mobs.add(0.5, E.addMuckola);
  midGame.mobs.add(0.5, E.addGolems);
  midGame.mobs.add(0.5, E.addHydra);

  midGame.grasses = new EncounterBag();
  midGame.grasses.add(0.75, E.addCrabs);
  midGame.grasses.add(0.75, E.addViolets);
  midGame.grasses.add(0.75, E.addGoldGrass);
  midGame.grasses.add(0.75, E.addRedcaps);
  midGame.grasses.add(0.5, E.addVibrantIvy);
  midGame.grasses.add(0.5, E.addHangingVines2x);
  midGame.grasses.add(0.5, E.addDeathbloom);
  midGame.grasses.add(0.5, E.addPoisonmoss);
  midGame.grasses.add(0.5, E.addSpore);
  midGame.grasses.add(0.3, E.addEveningBells);
  midGame.grasses.add(0.2, E.addTunnelroot);
  midGame.grasses.add(0.2, E.scatteredBoombugs4x);
  midGame.grasses.add(0.2, E.addGuardleaf4x);
  midGame.grasses.add(0.2, E.addBrambles);
  midGame.grasses.add(0.05, E.addNecroroot);
  midGame.grasses.add(0.05, E.fillWithFerns);

  midGame.spice = new EncounterBag();
  midGame.spice.add(5, E.empty);
  midGame.spice.add(0.5, E.addIronJelly);
  midGame.spice.add(0.25, E.addTunnelroot4x);
  midGame.spice.add(0.25, E.addBloodstone);
  midGame.spice.add(0.25, E.addScuttlers4x);
  midGame.spice.add(0.25, E.addMercenary);
  midGame.spice.add(0.25, E.addSpore8x);
  midGame.spice.add(0.25, E.fillWithBladegrass);
  midGame.spice.add(0.25, E.fillWithGuardleaf);
  midGame.spice.add(0.25, E.fillWithViolets);
  midGame.spice.add(0.25, E.fillWithFerns);
  midGame.spice.add(0.05, E.addParasite8x);
  midGame.spice.add(0.02, E.addFaegrass);
  midGame.spice.add(0.02, E.addNecroroot);

  return { shared, earlyGame, everything, midGame };
}
