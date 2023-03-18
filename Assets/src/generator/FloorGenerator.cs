using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using Random = System.Random;

[Serializable]
public abstract class FloorGenerator {
  public EncounterGroupShared shared;
  public EncounterGroup EncounterGroup;
  public List<int> floorSeeds;
  public EncounterGroup earlyGame, everything, midGame;

  [NonSerialized] /// these are hard-coded and reinstantiated when the program runs
  public List<Func<Floor>> floorGenerators;

  [OnDeserialized]
  void HandleDeserialized() {
    InitFloorGenerators();
    // 1.10.0 32 -> 36 levels
    while (floorSeeds.Count() < floorGenerators.Count()) {
      floorSeeds.Add(MyRandom.Next());
    }
  }

  public FloorGenerator(List<int> floorSeeds) {
    this.floorSeeds = floorSeeds;
#if experimental_singleitemplants
    shared = new SingleItemPlant.CustomEncounterGroupShared();
#else
    shared = new EncounterGroupShared();
#endif

    earlyGame = EncounterGroup.EarlyGame().AssignShared(shared);
    everything = EncounterGroup.EarlyMidMixed().AssignShared(shared);
    midGame = EncounterGroup.MidGame().AssignShared(shared);
    InitFloorGenerators();
  }

  protected abstract void InitFloorGenerators(); 

  internal HomeFloor generateHomeFloor() {
    EncounterGroup = earlyGame;
    HomeFloor floor;
#if experimental_mistyhome
      floor = MistsHomeFloor.generate(floorSeeds.Count());
#elif experimental_expandinghome
      floor = ExpandingHomeFloor.generate(floorSeeds.Count());
#elif experimental_multiroomhome
      floor = MultiRoomHomeFloor.generate();
#elif experimental_cavenetwork
      floor = Generate.CaveNetworkHomeFloor(EncounterGroup);
#elif experimental_actionpoints
      floor = generateGardeningActionPointsFloor0();
#else
      floor = Generate.Floor0(EncounterGroup);
#endif
    PostProcessFloor(floor);
    return floor;
  }

  protected abstract EncounterGroup GetEncounterGroup(int depth);

  public Floor generateFloor(FloorGenerationParams parameters) {
    Floor f = parameters.generate();
    PostProcessFloor(f);
    return f;
  }

  // public Floor generateCaveFloor(int depth) {
  //   /// The generators rely on the following state:
  //   /// (1) encounter group
  //   /// (2) MyRandom seed

  //   /// configure the EncounterGroup
  //   EncounterGroup = GetEncounterGroup(depth);

  //   /// set the seed
  //   Debug.Log("Depth " + depth + " seed " + floorSeeds[depth].ToString("x"));
  //   MyRandom.SetSeed(floorSeeds[depth]);

  //   // pick the generator
  //   var generator = floorGenerators[depth];
  //   Floor floor = null;

  //   int guard = 0;
  //   while (floor == null && guard++ < 20) {
  //     #if !UNITY_EDITOR
  //     try {
  //       floor = generator();
  //       PostProcessFloor(floor);
  //     } catch (Exception e) {
  //       Debug.LogError(e);
  //       GameModel.main.turnManager.latestException = e;
  //       MyRandom.Next();
  //     }
  //     #else
  //     floor = generator();
  //     PostProcessFloor(floor);
  //     #endif
  //   }
  //   if (floor == null) {
  //     throw GameModel.main.turnManager.latestException;
  //   }
  //   if (floor.depth != depth) {
  //     throw new Exception("floorGenerator depth " + depth + " is marked as depth " + floor.depth);
  //   }
  //   return floor;
  // }

  public void PostProcessFloor(Floor floor) {
#if experimental_chainfloors
    PostProcessPushEnemiesBack(floor);
#endif

    // PostProcessReduceEnemyAndGrassCount(floor);

#if UNITY_EDITOR
    var depth = floor.depth;
    floor.depth = 20;
    // put stuff here
    // Encounters.OneButterfly(floor, floor.root);
    // Encounters.AddSoftMoss(floor, floor.root);
    floor.depth = depth;
#endif

    PostProcessAddSignpost(floor);
    floor.ComputeEntityTypes();
  }

  private static void PostProcessAddSignpost(Floor floor) {
    /// add a signpost onto the floor
    if (Tips.tipMap.ContainsKey(floor.depth)) {
      /// put it near the upstairs
      var signpostSearchStartPos = floor.startPos;
      var signpostPos = floor.BreadthFirstSearch(signpostSearchStartPos, (tile) => true).Skip(5).Where(t => t is Ground && t.CanBeOccupied() && t.grass == null).FirstOrDefault();
      if (signpostPos != null) {
        floor.Put(new Signpost(signpostPos.pos, Tips.tipMap[floor.depth]));
      }
    }
  }

  public void PostProcessReduceEnemyAndGrassCount(Floor floor, float amountScalar = 0.67f) {
    // shrinking hack; reduce number of bodies and grasses
    var enemies = floor.Enemies().Where(a => !(a is Boss)).ToList();
    var originalEnemyNum = enemies.Count();
    var newEnemyNum = Mathf.Max(1, Mathf.RoundToInt(enemies.Count() * amountScalar));
    while(enemies.Count > newEnemyNum) {
      var choice = Util.RandomPick(enemies);
      enemies.Remove(choice);
      floor.Remove(choice);
    }

    var nonEnemyBodies = floor.bodies.Except(enemies).ToList();
    var originalNonEnemyBodyNum = nonEnemyBodies.Count();
    var newNonEnemyBodyNum = Mathf.Max(1, Mathf.RoundToInt(nonEnemyBodies.Count() * amountScalar));
    while (nonEnemyBodies.Count > newNonEnemyBodyNum) {
      var choice = Util.RandomPick(nonEnemyBodies);
      nonEnemyBodies.Remove(choice);
      floor.Remove(choice);
    }

    var grasses = floor.grasses.ToList();
    var originalGrassesNum = grasses.Count();
    var newGrassNum = Mathf.Max(1, Mathf.RoundToInt(grasses.Count() * 0.5f));
    while(grasses.Count > newGrassNum) {
      var choice = Util.RandomPick(grasses);
      grasses.Remove(choice);
      floor.Remove(choice);
    }

    Debug.LogFormat("Enemies {0} -> {1}, non-enemy bodies {2} -> {3}, grasses {4} -> {5}",
    originalEnemyNum, newEnemyNum,
    originalNonEnemyBodyNum, newNonEnemyBodyNum,
    originalGrassesNum, newGrassNum
    );
  }


  void PostProcessPushEnemiesBack(Floor floor) {
    // push all enemies back so the player has a few turns to "prepare"
    var enemies = floor.Enemies().Where(a => !(a is Boss)).ToList();
    foreach(var enemy in enemies) {
      var enemyRoom = enemy.room;
      if (enemyRoom == null) {
        continue;
      }
      var canOccupyMethod = enemy.GetType().GetMethod("CanOccupy", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
      bool canOccupy(Tile t) {
        if (!t.CanBeOccupied()) {
          return false;
        }
        if (canOccupyMethod != null) {
          return (bool) canOccupyMethod.Invoke(null, new object[] { t });
        }
        return true;
      }
      var newTile = floor
        .EnumerateRoomTiles(enemyRoom)
        .Where(canOccupy)
        .OrderByDescending(t => t.pos.x)
        .Skip(MyRandom.Range(0, 10))
        .FirstOrDefault();
      // var xRel = enemy.pos.x - enemyRoom.min.x;
      // // var pushBackChance = Util.MapLinear(xRel, 1, enemyRoom.width - 1, 0.9f, 0.0f);
      // var pushBackChance = 1;
      // if (MyRandom.value < pushBackChance) {
      //   List<Tile> possibleLaterTiles = new List<Tile>();
      //   var xPlusOneRel = xRel + 1;
      //   var x1 = xPlusOneRel + enemyRoom.min.x;
      //   for(int y = 0; y < floor.height; y++) {
      //     if (floor.tiles[x1, y].CanBeOccupied()) {
      //       possibleLaterTiles.Add(floor.tiles[x1, y]);
      //     }
      //   }
      //   var newTile = Util.RandomPick(possibleLaterTiles);
        if (newTile != null) {
          // enemy.ForceSet(newTile.pos);
          enemy.pos = newTile.pos;
        }
      // }
    }
  }


  public static FloorGenerator Create(List<int> floorSeeds) {
#if experimental_chainfloors
    return new FloorGeneratorChainFloors(floorSeeds);
#else
    return new FloorGenerator200Start(floorSeeds);
#endif
  }
}
