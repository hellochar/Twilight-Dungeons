using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using Random = System.Random;

[Serializable]
public class FloorGenerator {
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

  private void InitFloorGenerators() {
    floorGenerators = new List<Func<Floor>>() {
      // early game
      () => generateHomeFloor(),
#if experimental_chainfloors
      () => generateChainFloor(1, 3, 7, 5, 2, 1),
      () => generateChainFloor(2, 3, 7, 5, 2, 1),
      () => generateChainFloor(3, 3, 7, 5, 2, 2),
      () => generateChainFloor(4, 3, 7, 5, 3, 2),
      () => generateChainFloor(5, 3, 7, 5, 3, 3),
      () => generateChainFloor(6, 3, 7, 5, 3, 3),
#else
      () => generateSingleRoomFloor(1, 9, 7, 1, 1),
      () => generateSingleRoomFloor(2, 9, 7, 1, 1, extraEncounters: Encounters.OneAstoria),
      () => generateSingleRoomFloor(3, 9, 7, 1, 1),
      () => generateSingleRoomFloor(4, 9, 7, 2, 1, true, extraEncounters: Encounters.OneAstoria),
      () => generateSingleRoomFloor(5, 9, 7, 2, 1),
      () => generateSingleRoomFloor(6, 9, 7, 2, 1),
#endif
      () => generateSingleRoomFloor(7, 9, 7, 2, 1),
      () => generateRewardFloor(8, shared.Plants.GetRandomAndDiscount(1f), Encounters.OneAstoria),
      () => generateSingleRoomFloor(9, 10, 7, 3, 2),
      () => generateSingleRoomFloor(10, 10, 7, 3, 2),
      () => generateSingleRoomFloor(11, 10, 7, 3, 2, true, null, Encounters.AddDownstairsInRoomCenter),
      () => generateBlobBossFloor(12),

      // midgame
      () => generateSingleRoomFloor(13, 11, 8, 2, 1),
      () => generateSingleRoomFloor(14, 11, 8, 2, 1),
      () => generateSingleRoomFloor(15, 11, 8, 2, 1),
      () => generateRewardFloor(16, shared.Plants.GetRandomAndDiscount(1f), Encounters.OneAstoria),
      () => generateSingleRoomFloor(17, 12, 8, 3, 2),
      () => generateSingleRoomFloor(18, 12, 8, 3, 2),
      () => generateSingleRoomFloor(19, 12, 8, 4, 3, true),
      () => generateSingleRoomFloor(20, 12, 8, 5, 2),
      () => generateSingleRoomFloor(21, 12, 8, 6, 3),
      () => generateSingleRoomFloor(22, 12, 8, 7, 4, true, null, Encounters.AddDownstairsInRoomCenter, Encounters.FungalColonyAnticipation),
      () => generateFungalColonyBossFloor(23),
      () => generateRewardFloor(24, shared.Plants.GetRandomAndDiscount(1f), Encounters.AddWater),

      // endgame
      () => generateSingleRoomFloor(25, 12, 8, 2, 2),
      () => generateSingleRoomFloor(26, 12, 8, 2, 2),
      () => generateSingleRoomFloor(27, 13, 9, 3, 3, false, null, Encounters.AddWater),
      () => generateSingleRoomFloor(28, 13, 9, 4, 3, false, new Encounter[] { Encounters.LineWithOpening, Encounters.ChasmsAwayFromWalls2 }),
      () => generateSingleRoomFloor(29, 13, 9, 5, 3),
      () => generateSingleRoomFloor(30, 13, 9, 6, 3),
      () => generateSingleRoomFloor(31, 14, 9, 7, 4),
      () => generateRewardFloor(32, shared.Plants.GetRandomAndDiscount(1f), Encounters.AddWater, Encounters.ThreeAstoriasInCorner),
      () => generateSingleRoomFloor(33, 14, 9, 8, 5),
      () => generateSingleRoomFloor(34, 14, 9, 9, 6),
      () => generateSingleRoomFloor(35, 14, 9, 10, 7),
      () => generateEndBossFloor(36),
      () => generateEndFloor(37),
    };
  }

  internal HomeFloor generateHomeFloor() {
    EncounterGroup = earlyGame;
    HomeFloor floor;
#if experimental_actionpoints
      floor = generateGardeningActionPointsFloor0();
#elif experimental_grasscovering
      floor = generateGardeningV1Floor0();
#else
      floor = generateFloor0();
#endif
    PostProcessFloor(floor);
    return floor;
  }

  public Floor generateCaveFloor(int depth) {
    /// The generators rely on the following state:
    /// (1) encounter group
    /// (2) MyRandom seed

    /// configure the EncounterGroup
    if (depth <= 12) {
      EncounterGroup = earlyGame;
    } else if (depth <= 24) {
      EncounterGroup = everything;
    } else {
      EncounterGroup = midGame;
    }

    /// set the seed
    Debug.Log("Depth " + depth + " seed " + floorSeeds[depth].ToString("x"));
    MyRandom.SetSeed(floorSeeds[depth]);

    // pick the generator
    var generator = floorGenerators[depth];
    Floor floor = null;

    int guard = 0;
    while (floor == null && guard++ < 20) {
      #if !UNITY_EDITOR
      try {
        floor = generator();
      } catch (Exception e) {
        Debug.LogError(e);
        GameModel.main.turnManager.latestException = e;
        MyRandom.Next();
      }
      #else
      floor = generator();
      #endif
    }
    if (floor == null) {
      throw GameModel.main.turnManager.latestException;
    }
    if (floor.depth != depth) {
      throw new Exception("floorGenerator depth " + depth + " is marked as depth " + floor.depth);
    }
    PostProcessFloor(floor);
    return floor;
  }

  public void PostProcessFloor(Floor floor) {
    var enemies = floor.Enemies().Where(a => !(a is Boss)).ToList();
    foreach(var enemy in enemies) {
      var x = enemy.pos.x;
      var pushBackChance = Util.MapLinear(x, 1, floor.width - 1, 0.9f, 0.0f);
      if (MyRandom.value < pushBackChance) {
        List<Tile> possibleLaterTiles = new List<Tile>();
        for(int y = 0; y < floor.height; y++) {
          var x1 = x + 1;
          if (floor.tiles[x1, y].CanBeOccupied()) {
            possibleLaterTiles.Add(floor.tiles[x1, y]);
          }
        }
        var newTile = Util.RandomPick(possibleLaterTiles);
        if (newTile != null) {
          // enemy.ForceSet(newTile.pos);
          enemy.pos = newTile.pos;
        }
      }
    }

    // var amountScalar = 0.67f;

    // // shrinking hack; reduce number of bodies and grasses
    // var originalEnemyNum = enemies.Count();
    // var newEnemyNum = Mathf.Max(1, Mathf.RoundToInt(enemies.Count() * amountScalar));
    // while(enemies.Count > newEnemyNum) {
    //   var choice = Util.RandomPick(enemies);
    //   enemies.Remove(choice);
    //   floor.Remove(choice);
    // }

    // var nonEnemyBodies = floor.bodies.Except(enemies).ToList();
    // var originalNonEnemyBodyNum = nonEnemyBodies.Count();
    // var newNonEnemyBodyNum = Mathf.Max(1, Mathf.RoundToInt(nonEnemyBodies.Count() * amountScalar));
    // while (nonEnemyBodies.Count > newNonEnemyBodyNum) {
    //   var choice = Util.RandomPick(nonEnemyBodies);
    //   nonEnemyBodies.Remove(choice);
    //   floor.Remove(choice);
    // }

    // var grasses = floor.grasses.ToList();
    // var originalGrassesNum = grasses.Count();
    // var newGrassNum = Mathf.Max(1, Mathf.RoundToInt(grasses.Count() * 0.5f));
    // while(grasses.Count > newGrassNum) {
    //   var choice = Util.RandomPick(grasses);
    //   grasses.Remove(choice);
    //   floor.Remove(choice);
    // }

    // Debug.LogFormat("Enemies {0} -> {1}, non-enemy bodies {2} -> {3}, grasses {4} -> {5}",
    // originalEnemyNum, newEnemyNum,
    // originalNonEnemyBodyNum, newNonEnemyBodyNum,
    // originalGrassesNum, newGrassNum
    // );

    #if UNITY_EDITOR
    var depth = floor.depth;
    floor.depth = 20;
    // Encounters.AddOctopus(floor, floor.root);
    // Encounters.AddCheshireWeeds(floor, floor.root);
    // Encounters.AddWebs2x(floor, floor.root);
    // Encounters.AddLlaora(floor, floor.root);
    // Encounters.AddBloodstone(floor, floor.root);
    // Encounters.AddStalk(floor, floor.root);
    // Encounters.AddClumpshroom(floor, floor.root);

    // Encounters.AddFruitingBodies(floor, floor.root);
    // Encounters.AddNecroroot(floor, floor.root);
    // Encounters.AddPoisonmoss(floor, floor.root);
    // Encounters.FillWithFerns(floor, floor.root);
    // Encounters.AddFakeWall(floor, floor.root);
    floor.depth = depth;
    #endif

    /// add a signpost onto the floor
    if (Tips.tipMap.ContainsKey(floor.depth)) {
      /// put it near the upstairs
      var signpostSearchStartPos = floor.upstairs?.landing ?? floor.root.center;
      var signpostPos = floor.BreadthFirstSearch(signpostSearchStartPos, (tile) => true).Skip(5).Where(t => t is Ground && t.CanBeOccupied() && t.grass == null).FirstOrDefault();
      if (signpostPos != null) {
        floor.Put(new Signpost(signpostPos.pos, Tips.tipMap[floor.depth]));
      }
    }
  }

  public Floor generateEndFloor(int depth) {
    // use bossfloor to get rid of the sound
    Floor floor = new BossFloor(depth, 14, 42);

    FloorUtils.SurroundWithWalls(floor);

    var room0 = new Room(floor);
    floor.rooms = new List<Room> { room0 };
    floor.root = room0;

    var treePos = room0.center + Vector2Int.up * 10;

    // carve out chasms/ground
    for (int y = 0; y < floor.height; y++) {
      var angle = Util.MapLinear(y, 0, floor.height / 2, 0, Mathf.PI / 2 * 1.7f);
      var width = Mathf.Max(0, Mathf.Cos(angle)) * (floor.width - 2f) + 1;
      var xMin = (floor.width / 2f - width / 2) - 1;
      var xMax = (xMin + width);
      for (int x = 0; x < floor.width; x++) {
        var pos = new Vector2Int(x, y);
        if (x < xMin || x > xMax || y > treePos.y) {
          floor.Put(new Chasm(pos));
        } else if (floor.tiles[pos] == null) {
          floor.Put(new Ground(pos));
        }
      }
    }

    floor.PlaceUpstairs(new Vector2Int(floor.width / 2 - 1, 1), false);

    Room roomBot = new Room(Vector2Int.one, new Vector2Int(floor.width - 2, 11));
    for (var i = 0; i < 1; i++) {
      Encounters.AddWater(floor, roomBot);
    }
    Encounters.TwelveRandomAstoria(floor, roomBot);
    for (var i = 0; i < 12; i++) {
      Encounters.AddGuardleaf(floor, roomBot);
    }

    foreach (var t in floor.EnumerateRoom(roomBot).Where(p => floor.tiles[p] is Ground && floor.grasses[p] == null)) {
      floor.Put(new SoftGrass(t));
    }

    foreach (var pos in floor.EnumerateCircle(treePos + Vector2Int.down, 2.5f).Where(p => p.y <= treePos.y)) {
      var grass = floor.grasses[pos];
      if (grass != null) {
        floor.Remove(grass);
      }
      floor.Put(new FancyGround(pos));
    }

    /// add Tree of Life right in the middle
    floor.Put(new TreeOfLife(treePos));
    floor.Put(new Ezra(treePos + Vector2Int.down * 2));

    return floor;
  }

  // A little dose of coziness, a half-reward, something nice, a breather
  // maybe a pool of water to wash off on, a place to rest and recover
  // maybe healing
  // 
  public Floor generateRestFloor(int depth) {
    Floor floor = new Floor(depth, 8 + 2, 6 + 2);
    FloorUtils.CarveGround(floor);
    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    var room0 = new Room(floor);

    floor.PlaceUpstairs(new Vector2Int(room0.min.x, room0.max.y));
    floor.PlaceDownstairs(new Vector2Int(room0.max.x, room0.min.y));

    Encounters.AddOneWater(floor, room0);
    shared.Rests.GetRandomAndDiscount(0.5f)(floor, room0);

    FloorUtils.TidyUpAroundStairs(floor);
    floor.root = room0;
    return floor;
  }

  public HomeFloor generateFloor0() {
    HomeFloor floor = new HomeFloor(15, 11);

    // fill with floor tiles by default
    FloorUtils.CarveGround(floor);

    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    // floor.PlaceUpstairs(new Vector2Int(1, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 2, floor.height / 2));

    var soils = new List<Soil>();
    for (int x = 3; x < floor.width - 2; x += 2) {
      int y = floor.height / 2 - 1;
      if (floor.tiles[x, y] is Ground) {
        soils.Add(new Soil(new Vector2Int(x, y)));
      }
      y = floor.height / 2 + 1;
      if (floor.tiles[x, y] is Ground) {
        soils.Add(new Soil(new Vector2Int(x, y)));
      }
    }
    floor.PutAll(soils);

    var room0 = new Room(floor);
    floor.rooms = new List<Room> { room0 };
    floor.root = room0;

    EncounterGroup.Plants.GetRandomAndDiscount(1f)(floor, room0);

    Encounters.AddWater(floor, room0);
    Encounters.ThreeAstoriasInCorner(floor, room0);

    #if UNITY_EDITOR
    // Encounters.MatureStoutShrub(floor, room0);
    floor.depth = 20;
    // Encounters.AddOctopus(floor, room0);
    // Encounters.AddSkulls(floor, room0);
    // Encounters.AddIronJelly(floor, room0);
    // Encounters.AddHoppers(floor, room0);
    // Encounters.AddDeathbloom(floor, room0);
    // Encounters.AddMushroom(floor, room0);
    // Encounters.AddGrasper(floor, room0);
    // Encounters.AddHydra(floor, room0);
    // Encounters.AddViolets(floor, room0);
    // Encounters.AddTunnelroot(floor, room0);
    // Encounters.AddWildekins(floor, room0);
    // Encounters.AddCrabs(floor, room0);
    // Encounters.AddParasite(floor, room0);
    // Encounters.AddCoralmoss(floor, room0);
    // Encounters.AddHangingVines(floor, room0);
    // Encounters.AddEveningBells(floor, room0);
    // Encounters.OneButterfly(floor, room0);
    // Encounters.AddSpiders(floor, room0);
    // Encounters.AddSpore(floor, room0);
    // Encounters.AddBladegrass(floor, room0);
    // Encounters.ScatteredBoombugs(floor, room0);
    // Encounters.AddBats(floor, room0);
    // Encounters.AddPumpkin(floor, room0);
    // Encounters.ScatteredBoombugs.Apply(floor, room0);
    // Encounters.AFewSnails(floor, room0);
    // Encounters.AFewBlobs(floor, room0);
    floor.depth = 0;
    #endif

    floor.Put(new Altar(new Vector2Int(floor.width/2, floor.height - 2)));

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  public HomeFloor generateGardeningActionPointsFloor0() {
    HomeFloor floor = new HomeFloor(31, 25);
    FloorUtils.CarveGround(floor);
    // FloorUtils.SurroundWithWalls(floor);
    foreach (var p in floor.EnumeratePerimeter()) {
      if (p.x == floor.width - 1) {
        floor.Put(new Wall(p));
      } else {
        floor.Put(new Chasm(p));
      }
    }
    FloorUtils.NaturalizeEdges(floor);

    var root = new Room(
      new Vector2Int(floor.width - 7, floor.height / 2 - 3),
      new Vector2Int(floor.width - 1, floor.height / 2 + 3)
    );
    floor.rooms = new List<Room> { root };
    floor.root = root;
    floor.AddInitialThickBrush();

    floor.PlaceDownstairs(new Vector2Int(root.max.x, root.center.y));
    Encounters.AddWater(floor, root);
    Encounters.OneAstoria(floor, root);
    FloorUtils.TidyUpAroundStairs(floor);

    // var tiles = FloorUtils.EmptyTilesInRoom(floor, room0).ToList();
    // tiles.Shuffle();
    // floor.PutAll(tiles.Take(10).Select(t => new HardGround(t.pos)).ToList());

    EncounterGroup.Plants.GetRandomAndDiscount(1f)(floor, root);

    return floor;
  }

  public Floor generateGardeningV1Floor0() {
    HomeFloor floor = new HomeFloor(13, 8);
    FloorUtils.CarveGround(floor);
    FloorUtils.SurroundWithWalls(floor);
    floor.PlaceDownstairs(new Vector2Int(floor.width - 2, floor.height / 2));

    var room0 = new Room(floor);
    floor.rooms = new List<Room> { room0 };
    floor.root = room0;
    Encounters.AddWater(floor, room0);
    Encounters.ThreeAstoriasInCorner(floor, room0);
    floor.Put(new Altar(new Vector2Int(floor.width/2, floor.height - 2)));
    FloorUtils.TidyUpAroundStairs(floor);

    var tiles = FloorUtils.EmptyTilesInRoom(floor, room0).ToList();
    tiles.Shuffle();
    floor.PutAll(tiles.Take(10).Select(t => new HardGround(t.pos)).ToList());

    EncounterGroup.Plants.GetRandomAndDiscount(1f)(floor, room0);
    return floor;
  }

  public Floor generateRewardFloor(int depth, params Encounter[] extraEncounters) {
    Floor floor = new Floor(depth, 12, 8);
    FloorUtils.CarveGround(floor);
    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    var room0 = new Room(floor);

    floor.PlaceUpstairs(new Vector2Int(room0.min.x, room0.max.y));
    floor.PlaceDownstairs(new Vector2Int(room0.max.x, room0.min.y));

    // Encounters.PlaceFancyGround(floor, room0);
    // Encounters.CavesRewards.GetRandomAndDiscount()(floor, room0);
    // EncounterGroup.Plants.GetRandomAndDiscount(0.9f)(floor, room0);
    // Encounters.AddTeleportStone(floor, room0);
    Encounters.AddOneWater(floor, room0);
    foreach (var encounter in extraEncounters) {
      encounter(floor, room0);
    }

    FloorUtils.TidyUpAroundStairs(floor);
    floor.root = room0;

    return floor;
  }

  public Floor generateChainFloor(int depth, int numRooms, int width, int height, int numMobs, int numGrasses, bool reward = false, Encounter[] preMobEncounters = null, params Encounter[] extraEncounters) {
    Floor floor = tryGenerateChainRoomFloor(depth, width, height, numRooms, preMobEncounters == null);
    // ensureConnectedness(floor);

    List<Encounter> encounters = new List<Encounter>();

    // X mobs
    for (var i = 0; i < numMobs; i++) {
      encounters.Add(EncounterGroup.Mobs.GetRandomAndDiscount());
    }

    // Y grasses
    for (var i = 0; i < numGrasses; i++) {
      encounters.Add(EncounterGroup.Grasses.GetRandomAndDiscount());
    }

    encounters.Add(EncounterGroup.Spice.GetRandomAndDiscount());

    encounters.AddRange(extraEncounters);

    int roomIntensity = 1;
    foreach (var room in floor.rooms) {
      for (var i = 0; i < roomIntensity; i++) {
        foreach(var encounter in encounters) {
          encounter(floor, room);
        }
      }
      roomIntensity++;

      // add slimes
      var entrancesAndExits = floor.EnumerateRoomPerimeter(room, -1).Where(pos => floor.tiles[pos].CanBeOccupied());
      foreach (var pos in entrancesAndExits) {
        floor.Put(new Slime(pos));
      }
    }

    // specifically used for e.g. moving downstairs to a center.
    if (preMobEncounters != null) {
      foreach (var encounter in preMobEncounters) {
        encounter(floor, floor.downstairsRoom);
      }
    }

    if (reward) {
      EncounterGroup.Rewards.GetRandomAndDiscount()(floor, floor.downstairsRoom);
    }

    FloorUtils.TidyUpAroundStairs(floor);
    foreach(var room in floor.rooms) {
      // make the room own the top and bottom edges so visibility will properly show the wall edges
      room.max += Vector2Int.one;
      room.min -= Vector2Int.one;
    }
    return floor;
  }

  private Floor tryGenerateChainRoomFloor(int depth, int width, int height, int numChains = 3, bool defaultEncounters = true) {
    int maxHeight = height + numChains;

    var rooms = new List<Room>();

    var x = 1;
    for(int i = 0; i < numChains; i++) {
      var thisWidth = width + i;
      var thisHeight = height + i;

      var min = new Vector2Int(x, (maxHeight + 2 - thisHeight) / 2);
      var max = min + new Vector2Int(thisWidth - 1, thisHeight - 1);
      var room = new Room(min, max);
      rooms.Add(room);

      // leave one line of wall space
      // HACK leave two so we can make each room have its own back wall
      x = max.x + 3;
    }

    Floor floor = new Floor(depth, x + 1, maxHeight + 2);
    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    floor.rooms = rooms;
    for(int i = 0; i < numChains; i++) {
      var room = rooms[i];

      FloorUtils.CarveGround(floor, floor.EnumerateRoom(room));
      if (i > 0) {
        //where the slime will go
        floor.Put(new Ground(new Vector2Int(room.min.x - 1, room.center.y)));
        floor.Put(new Ground(new Vector2Int(room.min.x - 2, room.center.y)));
      }

      if (defaultEncounters) {
        // one wall variation
        EncounterGroup.Walls.GetRandomAndDiscount()(floor, room);
        
        // chasms (bridge levels) should be relatively rare so only discount by 10% each time (this is still exponential decrease for the Empty case)
        // EncounterGroup.Chasms.GetRandomAndDiscount(0.04f)(floor, room);
      }
      floor.PlaceDownstairs(room.max - Vector2Int.one);
      if (i == 0) {
        floor.upstairsRoom = room;
      } else if (i == numChains - 1) {
        floor.downstairsRoom = room;
      }
    }

    FloorUtils.NaturalizeEdges(floor);

    floor.root = floor.rooms[0];
    return floor;
  }

  /// <summary>
  /// Generates one single room with one wall variation, X mob encounters, Y grass encounters, an optional reward.
  /// Good for a contained experience.
  /// </summary>
  public Floor generateSingleRoomFloor(int depth, int width, int height, int numMobs, int numGrasses, bool reward = false, Encounter[] preMobEncounters = null, params Encounter[] extraEncounters) {
    Floor floor = tryGenerateSingleRoomFloor(depth, width, height, preMobEncounters == null);
    ensureConnectedness(floor);
    floor.PutAll(
      floor.EnumeratePerimeter().Where(pos => floor.tiles[pos] is Ground).Select(pos => new Wall(pos))
    );
    var room0 = floor.root;
    if (preMobEncounters != null) {
      foreach (var encounter in preMobEncounters) {
        encounter(floor, room0);
      }
    }
    // X mobs
    for (var i = 0; i < numMobs; i++) {
      EncounterGroup.Mobs.GetRandomAndDiscount()(floor, room0);
    }

    // Y grasses
    for (var i = 0; i < numGrasses; i++) {
      EncounterGroup.Grasses.GetRandomAndDiscount()(floor, room0);
    }

    foreach (var encounter in extraEncounters) {
      encounter(floor, room0);
    }

    // a reward (optional)
    if (reward) {
      Encounters.AddWater(floor, room0);
      EncounterGroup.Rewards.GetRandomAndDiscount()(floor, room0);
    }

    EncounterGroup.Spice.GetRandom()(floor, room0);
    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  private Floor tryGenerateSingleRoomFloor(int depth, int width, int height, bool defaultEncounters = true) {
    // We want to inset the downstairs and upstairs into the left and right walls. To do this,
    // we add one more column on each side
    Floor floor = new Floor(depth, width + 2, height);
    Room room0 = new Room(
      new Vector2Int(2, 1),
      new Vector2Int(floor.width - 3, floor.height - 2)
    );

    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    FloorUtils.CarveGround(floor, floor.EnumerateRoom(room0));

    if (defaultEncounters) {
      // one wall variation
      EncounterGroup.Walls.GetRandomAndDiscount()(floor, room0);
      FloorUtils.NaturalizeEdges(floor);
      
      // chasms (bridge levels) should be relatively rare so only discount by 10% each time (this is still exponential decrease for the Empty case)
      EncounterGroup.Chasms.GetRandomAndDiscount(0.04f)(floor, room0);
    }

    floor.PlaceUpstairs(new Vector2Int(room0.min.x - 1, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(room0.max.x + 1, floor.height / 2));

    floor.root = room0;
    floor.rooms = new List<Room>();
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    return floor;
  }

  public Floor generateBlobBossFloor(int depth) {
    Floor floor = new BossFloor(depth, 12, 9);
    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Ground(p));
    }
    foreach(var p in floor.EnumeratePerimeter()) {
      floor.Put(new Wall(p));
    }
    floor.Put(new Wall(new Vector2Int(1, 1)));
    floor.Put(new Wall(new Vector2Int(floor.width - 1, 1)));
    floor.Put(new Wall(new Vector2Int(floor.width - 1, floor.height - 1)));
    floor.Put(new Wall(new Vector2Int(1, floor.height - 1)));

    Room room0 = new Room(floor);
    floor.Put(new Wall(room0.center + new Vector2Int(2, 2)));
    floor.Put(new Wall(room0.center + new Vector2Int(2, -2)));
    floor.Put(new Wall(room0.center + new Vector2Int(-2, -2)));
    floor.Put(new Wall(room0.center + new Vector2Int(-2, 2)));

    floor.PlaceUpstairs(new Vector2Int(1, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 2, floor.height / 2));

    floor.root = room0;
    floor.rooms = new List<Room>();
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    // add boss
    floor.Put(new Blobmother(floor.downstairs.pos));

#if experimental_grassesonbossfloor
    EncounterGroup.Grasses.GetRandomAndDiscount()(floor, room0);
#endif

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  public Floor generateFungalColonyBossFloor(int depth) {
    Floor floor = new BossFloor(depth, 13, 9);
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    Room room0 = new Room(floor);

    void CutOutCircle(Vector2Int center, float radius) {
      FloorUtils.CarveGround(floor, floor.EnumerateCircle(center, radius));
    }

    // start and end paths
    CutOutCircle(new Vector2Int(3, floor.height / 2), 2f);
    CutOutCircle(new Vector2Int(floor.width - 4, floor.height / 2), 2f);
    CutOutCircle(room0.center, 4.5f);
    floor.Put(new FungalColony(room0.center));

    // turn some of the walls and empty space into fungal walls
    foreach (var pos in floor.EnumerateFloor()) {
      var t = floor.tiles[pos];
      if (t is Wall && floor.GetAdjacentTiles(t.pos).Any(t2 => t2.CanBeOccupied())) {
        floor.Put(new FungalWall(pos));
      } else if (t is Ground && t.CanBeOccupied()) {
        if (MyRandom.value < 0.25f) {
          floor.Put(new FungalWall(pos));
        }
      }
    }

    // re-apply normal Wall to outer perimeter
    floor.PutAll(floor.EnumeratePerimeter().Select(pos => new Wall(pos)));

    // block entrance
    // floor.PutAll(new FungalWall(new Vector2Int(7, 5)), new FungalWall(new Vector2Int(7, 6)), new FungalWall(new Vector2Int(7, 7)));
    floor.PlaceUpstairs(new Vector2Int(0, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 1, floor.height / 2));

    floor.root = room0;
    floor.rooms = new List<Room>();
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

#if experimental_grassesonbossfloor
    EncounterGroup.Grasses.GetRandomAndDiscount()(floor, room0);
#endif

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  public Floor generateEndBossFloor(int depth) {
    Floor floor = new BossFloor(depth, 15, 11);
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Ground(p));
    }
    foreach(var p in floor.EnumeratePerimeter()) {
      floor.Put(new Wall(p));
    }
    FloorUtils.NaturalizeEdges(floor);
    foreach(var p in floor.EnumerateFloor()) {
      if (floor.tiles[p] is Wall) {
        floor.Put(new Chasm(p));
      }
    }

    Room room0 = new Room(floor);

    floor.Put(new CorruptedEzra(room0.center));
    floor.PlaceUpstairs(new Vector2Int(0, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 1, floor.height / 2));

    floor.root = room0;
    floor.rooms = new List<Room>();
    floor.upstairsRoom = room0;
    floor.downstairsRoom = room0;

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  /// <summary>
  /// Generate a floor broken up into X smaller rooms, based on a number of "splits". Each room contains:
  /// one mob, one grass, one random encounter.
  /// </summary>
  public Floor generateMultiRoomFloor(int depth, int width = 60, int height = 20, int numSplits = 20, bool hasReward = false, params Encounter[] specialDownstairsEncounters) {
    Floor floor = tryGenerateMultiRoomFloor(depth, width, height, numSplits);
    ensureConnectedness(floor);

    var intermediateRooms = floor.rooms
      .Where((room) => room != floor.upstairsRoom && room != floor.downstairsRoom);

    Room rewardRoom = null;
    if (hasReward) {
      // the non-downstairs terminal room farthest away from the upstairs according to pathfinding
      rewardRoom = intermediateRooms
        .OrderByDescending((room) => floor.FindPath(floor.upstairs.pos, room.center).Count)
        .First();

      Encounters.PlaceFancyGround(floor, rewardRoom);
      Encounters.SurroundWithRubble(floor, rewardRoom);
      var rewardEncounter = EncounterGroup.Rewards.GetRandomAndDiscount();
      rewardEncounter(floor, rewardRoom);
    }

    var deadEndRooms = intermediateRooms.Where((room) => room != rewardRoom && room.connections.Count < 2);
    foreach (var room in deadEndRooms) {
      if (MyRandom.value < 0.05f) {
        Encounters.SurroundWithRubble(floor, room);
      }
      var encounter = EncounterGroup.Spice.GetRandomAndDiscount();
      encounter(floor, room);
    }

    foreach (var room in floor.rooms) {
      if (room != floor.upstairsRoom && room != rewardRoom) {
        // spawn a random encounter
        var encounter = EncounterGroup.Mobs.GetRandomAndDiscount();
        encounter(floor, room);
      }
#if experimental_chainfloors
      Encounters.SurroundWithRubble(floor, room);
#endif
    }

    // this includes abstract rooms!
    foreach(var room in floor.root.Traverse().Where((room) => room.depth >= 2)) {
      var encounter = EncounterGroup.Grasses.GetRandomAndDiscount();
      encounter(floor, room);
    }

    foreach (var encounter in specialDownstairsEncounters) {
      encounter(floor, floor.downstairsRoom);
    }

    FloorUtils.TidyUpAroundStairs(floor);

    return floor;
  }

  private static void ensureConnectedness(Floor floor) {
    // here's how we ensure connectedness:
    // 1. find all walkable tiles on a floor
    // 2. group them by connectedness using bfs, starting from the upstairs as the "mainland"
    // 3. *connect* every group past mainland, updating mainland as you go
    var walkableTiles = new HashSet<Tile>(floor
      .EnumerateFloor()
      .Select(p => floor.tiles[p])
      .Where(t => t.BasePathfindingWeight() != 0)
    );
    
    var mainland = makeGroup(floor.upstairs, ref walkableTiles);
    int guard = 0;
    while (walkableTiles.Any() && (guard++ < 99)) {
      var island = makeGroup(walkableTiles.First(), ref walkableTiles);
      // connect island to mainland
      var entitiesToAdd = connectGroups(mainland, island);
      floor.PutAll(entitiesToAdd);
      mainland.UnionWith(entitiesToAdd);
      mainland.UnionWith(island);
    }

    // mutates walkableTiles
    HashSet<Tile> makeGroup(Tile start, ref HashSet<Tile> allTiles) {
      var set = new HashSet<Tile>(floor.BreadthFirstSearch(start.pos, allTiles.Contains));
      allTiles.ExceptWith(set);
      return set;
    }

    // How to "connect" two groups? Two groups will be separated by at least one unwalkable
    // tile. There's many ways to connect two groups. We want to choose the least invasive.
    // 
    // Bfs with distance outwards from group2 until you hit a group1 tile. Backtrace the bfs - 
    // this will give you a line of at least 3 tiles: [group2 tile, <intermediate unwalkable tiles>, group1 tile]
    // Now we transform these. Easiest is to just turn unwalkables into ground.
    // We can also get fancy by changing wall -> ground+rock and chasm -> bridge
    // We can put an "activatable" on either side that spawns a bridge.
    // another option: just *randomly* select two nodes and draw a line between them
    List<Ground> connectGroups(HashSet<Tile> mainland, HashSet<Tile> island) {
      var mainlandNode = Util.RandomPick(mainland);
      var islandNode = Util.RandomPick(island);
      // return floor.EnumerateLine(mainlandNode.pos, islandNode.pos)
      return FloorUtils.Line3x3(floor, mainlandNode.pos, islandNode.pos)
        .Where(p => floor.tiles[p].BasePathfindingWeight() == 0)
        .Select(p => new Ground(p))
        .ToList();
    }
  }

  /// connectivity is not guaranteed
  private static Floor tryGenerateMultiRoomFloor(int depth, int width, int height, int numSplits) {
    Floor floor = new Floor(depth, width, height);

    // fill with wall
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Wall(p));
    }

    // randomly partition space into rooms
    Room root = new Room(floor);
    for (int i = 0; i < numSplits; i++) {
      bool success = root.randomlySplit();
      if (!success) {
        Debug.LogWarning("couldn't split at iteration " + i);
        break;
      }
    }

    // collect all rooms
    List<Room> rooms = root.Traverse().Where(n => n.isTerminal).ToList();

    // shrink it into a subset of the space available; adds more 'emptiness' to allow
    // for less rectangular shapes
    rooms.ForEach(room => room.randomlyShrink());

    // sort rooms by distance to top-left, where the upstairs will be.
    Vector2Int topLeft = new Vector2Int(0, floor.height);
    rooms.OrderBy(room => Util.manhattanDistance(room.getTopLeft() - topLeft));

    Room upstairsRoom = rooms.First();
    // 1-px padding from the top-left of the room
    Vector2Int upstairsPos = new Vector2Int(upstairsRoom.min.x + 1, upstairsRoom.max.y - 1);
    floor.PlaceUpstairs(upstairsPos);

    Room downstairsRoom = rooms.Last();
    // 1-px padding from the bottom-right of the room
    Vector2Int downstairsPos = new Vector2Int(downstairsRoom.max.x - 1, downstairsRoom.min.y + 1);
    floor.PlaceDownstairs(downstairsPos);

    floor.root = root;
    floor.rooms = rooms;
    floor.upstairsRoom = upstairsRoom;
    floor.downstairsRoom = downstairsRoom;

    // fill each room with floor
    rooms.ForEach(room => {
      FloorUtils.CarveGround(floor, floor.EnumerateRoom(room));
    });

    // in rare cases, connect rooms using the connectedness algorithm. creates really dense and tight rooms.
#if experimental_chainfloors
    var useConnectednessAlgorithm = false;
#else
    var useConnectednessAlgorithm = MyRandom.value < 0.2f;
#endif
    if (useConnectednessAlgorithm) {
      ensureConnectedness(floor);
    } else {
      // add connections between bsp siblings
      foreach (var (a, b) in ComputeRoomConnections(rooms, root)) {
        FloorUtils.CarveGround(floor, FloorUtils.Line3x3(floor, a, b));
      }
    }

    FloorUtils.NaturalizeEdges(floor);

#if !experimental_chainfloors
    // occasionally create a chasm in the multi room. Feels tight and cool, and makes the level harder.
    if (MyRandom.value < 0.1) {
      var depth2Room = Util.RandomPick(root.Traverse().Where(r => r.depth == 2));
      Encounters.ChasmsAwayFromWalls1(floor, depth2Room);
    }
#endif

    return floor;
  }

  /// Connect all the rooms together with at least one through-path
  private static List<(Vector2Int, Vector2Int)> ComputeRoomConnections(List<Room> rooms, Room root) {
    return BSPSiblingRoomConnections(rooms, root);
  }

  /// draw a path connecting siblings together, including intermediary nodes (guarantees connectedness)
  /// this tends to draw long lines that cut right through single thickness walls
  private static List<(Vector2Int, Vector2Int)> BSPSiblingRoomConnections(List<Room> rooms, Room root) {
    List<(Vector2Int, Vector2Int)> paths = new List<(Vector2Int, Vector2Int)>();
    foreach (var node in root.Traverse().Where(n => !n.isTerminal)) {
      Vector2Int nodeCenter = node.getCenter();
      RoomSplit split = node.split.Value;
      split.a.connections.Add(split.b);
      split.b.connections.Add(split.a);
      Vector2Int aCenter = split.a.getCenter();
      Vector2Int bCenter = split.b.getCenter();
      paths.Add((nodeCenter, aCenter));
      paths.Add((nodeCenter, bCenter));
    }
    return paths;
  }

  public static bool AreStairsConnected(Floor floor) {
    var path = floor.FindPath(floor.downstairs.pos, floor.upstairs.pos);
    return path.Any();
  }
}
