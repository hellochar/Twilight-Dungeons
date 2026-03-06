import { WeightedRandomBag } from './WeightedRandomBag';
import type { Floor } from '../model/Floor';
import type { Room } from './Room';

/** An encounter is a function that populates a floor region */
export type Encounter = (floor: Floor, room: Room | null) => void;

/** Weighted bag of encounters — extends WeightedRandomBag with encounter-specific typing */
export class EncounterBag extends WeightedRandomBag<Encounter> {}

export function mult(fn: Encounter, times: number): Encounter {
  return (floor, room) => {
    for (var i = 0; i < times; i++) {
      fn(floor, room);
    }
  };
}


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

function makeShared(E: Record<string, Encounter>): EncounterGroup {
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

  return shared;
}

function makeBasicEncounterGroup(E: Record<string, Encounter>): EncounterGroup {
  const basic = new EncounterGroup();
  basic.assignShared(makeShared(E));

  basic.mobs = new EncounterBag();
  basic.mobs.add(1.0, E.addWallflowers);
  basic.mobs.add(1.0, E.addBird);
  basic.mobs.add(1.0, E.addSnake);
  basic.mobs.add(1.0, E.aFewBlobs);
  basic.mobs.add(1.0, E.jackalPile);
  basic.mobs.add(1.0, E.addSkullys);
  basic.mobs.add(1.0, E.addOctopus);
  basic.mobs.add(1.0, E.aFewSnails);
  basic.mobs.add(1.0, E.addSpiders);

  basic.grasses = new EncounterBag();
  basic.grasses.add(1, E.addSoftGrass);
  basic.grasses.add(1, E.addLlaora);
  basic.grasses.add(1, E.addGuardleaf);
  basic.grasses.add(1, E.addBladegrass);
  basic.grasses.add(1, E.scatteredBoombugs);
  basic.grasses.add(1, E.addEveningBells);
  basic.grasses.add(1, E.addDeathbloom);
  // basic.grasses.add(0.5, E.addWebs);
  // earlyGame.grasses.add(0.4, E.addAgave);
  // basic.grasses.add(0.35, E.addHangingVines);
  basic.grasses.add(1, E.addViolets);
  // earlyGame.grasses.add(0.2, E.fillWithFerns);

  basic.spice = new EncounterBag();
  basic.spice.add(20, E.empty);
  // basic.spice.add(0.5, E.addFruitingBodies);
  // basic.spice.add(0.5, E.addScuttlers);
  // earlyGame.spice.add(0.25, E.addSoftGrass);
  basic.spice.add(1, E.fillWithBladegrass);
  basic.spice.add(1, E.scatteredBoombugs4x);
  // earlyGame.spice.add(0.2, E.addWater);
  // earlyGame.spice.add(0.1, E.addOldDude);
  // earlyGame.spice.add(0.1, E.addDeathbloom);
  basic.spice.add(1, E.addGuardleaf4x);
  // earlyGame.spice.add(0.5, E.addSpore);
  basic.spice.add(1, E.addEveningBells);
  basic.spice.add(1, E.addPoisonmoss);
  // basic.spice.add(0.05, E.fillWithFerns);
  // earlyGame.spice.add(0.01, E.addNecroroot);
  // earlyGame.spice.add(0.01, E.addFaegrass);
  return basic;
}

function makeMediumEncounterGroup(E: Record<string, Encounter>): EncounterGroup {
  const medium = new EncounterGroup();
  medium.assignShared(makeShared(E));

  medium.mobs = new EncounterBag();
  medium.mobs.add(1.0, E.addWallflowers);
  medium.mobs.add(1.0, E.addBird);
  medium.mobs.add(1.0, E.addSnake);
  medium.mobs.add(1.0, E.aFewBlobs);
  medium.mobs.add(1.0, E.jackalPile);
  medium.mobs.add(1.0, E.addSkullys);
  medium.mobs.add(1.0, E.addOctopus);
  medium.mobs.add(1.0, E.aFewSnails);
  medium.mobs.add(1.0, E.addSpiders);
  medium.mobs.add(1.0, E.addBats);
  medium.mobs.add(1.0, E.addGolems);

  medium.grasses = new EncounterBag();
  medium.grasses.add(0.5, E.addSoftGrass);
  medium.grasses.add(1, E.addLlaora);
  medium.grasses.add(1, E.addGuardleaf);
  medium.grasses.add(1, E.addBladegrass);
  medium.grasses.add(1, E.scatteredBoombugs);
  medium.grasses.add(1, E.addEveningBells);
  medium.grasses.add(1, E.addDeathbloom);
  // earlyGame.grasses.add(0.4, E.addAgave);
  // basic.grasses.add(0.35, E.addHangingVines);
  medium.grasses.add(1, E.addViolets);
  medium.grasses.add(1, E.addBrambles);
  // earlyGame.grasses.add(0.2, E.fillWithFerns);

  medium.spice = new EncounterBag();
  // medium.spice.add(100, E.empty);
  medium.spice.add(1, E.addWebs);
  medium.spice.add(1, E.addFruitingBodies);
  medium.spice.add(1, E.addScuttlers);
  // earlyGame.spice.add(0.25, E.addSoftGrass);
  // earlyGame.spice.add(0.25, E.addBladegrass);
  medium.spice.add(1, E.scatteredBoombugs4x);
  // earlyGame.spice.add(0.2, E.addWater);
  // earlyGame.spice.add(0.1, E.addOldDude);
  medium.spice.add(1, mult(E.addDeathbloom, 4));
  // earlyGame.spice.add(0.1, E.addGuardleaf);
  // earlyGame.spice.add(0.5, E.addSpore);
  medium.spice.add(1, E.addEveningBells);
  medium.spice.add(1, E.addPoisonmoss);
  medium.spice.add(1, E.fillWithFerns);
  // earlyGame.spice.add(0.01, E.addNecroroot);
  // earlyGame.spice.add(0.01, E.addFaegrass);
  return medium;
}

function makeComplexEncounterGroup(E: Record<string, Encounter>): EncounterGroup {
  const complex = new EncounterGroup();
  complex.assignShared(makeShared(E));
  complex.walls.remove(E.empty);

  complex.mobs = new EncounterBag();
  // complex.mobs.add(1.0, E.addWallflowers);
  // complex.mobs.add(1.0, E.addBird);
  // complex.mobs.add(1.0, E.aFewSnails);

  // complex.mobs.add(1.0, mult(E.addSkullys, 2));
  complex.mobs.add(1.0, mult(E.addSnake, 2));
  // complex.mobs.add(1.0, E.aFewBlobs);
  // complex.mobs.add(1.0, E.jackalPile);
  complex.mobs.add(1.0, mult(E.addOctopus, 1));
  complex.mobs.add(1.0, mult(E.addSpiders, 2));
  complex.mobs.add(1.0, mult(E.addBats, 1));

  complex.mobs.add(1.0, E.addScorpions);
  complex.mobs.add(1.0, E.addGolems);
  complex.mobs.add(1.0, E.addHoppers);
  complex.mobs.add(1.0, E.addHydra);
  complex.mobs.add(1.0, E.addGrasper);
  complex.mobs.add(1.0, E.addPoisoner);
  complex.mobs.add(1.0, E.addGoo);
  complex.mobs.add(0.5, E.addClumpshroom);

  // complex.mobs.add(1.0, E.addMuckola);
  // complex.mobs.add(1.0, E.addDizapper);
  // complex.mobs.add(1.0, E.addWildekins);
  // complex.mobs.add(1.0, E.addThistlebog);


  complex.grasses = new EncounterBag();
  complex.grasses.add(1, E.addViolets);
  complex.grasses.add(1, E.addGoldGrass);
  // complex.grasses.add(0.75, E.addRedcaps);
  complex.grasses.add(1, E.addVibrantIvy);
  complex.grasses.add(1, E.addCrabs);
  // complex.grasses.add(0.5, E.addHangingVines2x);
  complex.grasses.add(1, E.addDeathbloom);
  complex.grasses.add(1, E.addPoisonmoss);
  // complex.grasses.add(1, E.addSpore);
  complex.grasses.add(1, E.addEveningBells);
  complex.grasses.add(1, E.addTunnelroot);
  complex.grasses.add(1, E.addGuardleaf);
  complex.grasses.add(1, E.addBrambles);
  // complex.grasses.add(0.05, E.addNecroroot);

  complex.spice = new EncounterBag();
  complex.spice.add(1, E.fillWithViolets);
  complex.spice.add(1, E.addCrabs);
  // complex.spice.add(1.0, mult(E.jackalPile, 4));
  complex.spice.add(1, mult(E.addCrabs, 4));
  complex.spice.add(1, E.addIronJelly);
  complex.spice.add(1, E.addBloodstone);
  // complex.spice.add(1, E.addTunnelroot4x);
  complex.spice.add(1, E.addScuttlers4x);
  complex.spice.add(1, E.scatteredBoombugs4x);
  complex.spice.add(0.2, E.fillWithFerns);
  complex.spice.add(0.2, E.fillWithGuardleaf);
  complex.spice.add(0.25, E.fillWithBladegrass);
  complex.spice.add(0.05, E.fillWithViolets);
  return complex;
}

/**
 * Create all encounter groups with properly weighted bags.
 * Called by FloorGenerator constructor after Encounters module is loaded.
 */
export function createEncounterGroups(E: Record<string, Encounter>): EncounterGroupSet {
  var earlyGame = makeBasicEncounterGroup(E);
  const everything = makeMediumEncounterGroup(E);
  const midGame = makeComplexEncounterGroup(E);
  return { shared: makeShared(E), earlyGame, everything, midGame };
}

export function createEncounterGroupsOld_donotdelete(E: Record<string, Encounter>) {
  // medium difficulty
  const everything = makeMediumEncounterGroup(E);

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

  // complex difficulty
  const midGame = new EncounterGroup();
  // midGame.assignShared(shared);

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
  midGame.spice.add(0.25, E.addBloodstone);
  midGame.spice.add(0.25, E.addTunnelroot4x);
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
}