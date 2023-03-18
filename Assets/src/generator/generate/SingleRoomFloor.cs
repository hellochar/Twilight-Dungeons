using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class SingleRoomFloorParams : FloorGenerationParams {
  public readonly EncounterGroup EncounterGroup;
  public readonly int width;
  public readonly int height;
  public readonly int numMobs;
  public readonly int numGrasses;
  public readonly bool reward;
  public readonly Encounter[] preMobEncounters;
  public readonly Encounter[] extraEncounters;
  public SingleRoomFloorParams(EncounterGroup encounterGroup, int depth, int width, int height, int numMobs, int numGrasses, bool reward = false, Encounter[] preMobEncounters = null, params Encounter[] extraEncounters)
    : base(depth) {
    EncounterGroup = encounterGroup;
    this.width = width;
    this.height = height;
    this.numMobs = numMobs;
    this.numGrasses = numGrasses;
    this.reward = reward;
    this.preMobEncounters = preMobEncounters;
    this.extraEncounters = extraEncounters;
  }

  public override Floor generate() {
    // todo move implementation here
    return Generate.SingleRoomFloor(this);
  }
}

public static partial class Generate {
  /// <summary>
  /// Generates one single room with one wall variation, X mob encounters, Y grass encounters, an optional reward.
  /// Good for a contained experience.
  /// </summary>
  public static Floor SingleRoomFloor(SingleRoomFloorParams args) {
    Floor floor = tryGenerateSingleRoomFloor(args);
    ensureConnectedness(floor);
    floor.PutAll(
      floor.EnumeratePerimeter().Where(pos => floor.tiles[pos] is Ground).Select(pos => new Wall(pos))
    );
    var room0 = floor.root;
    if (args.preMobEncounters != null) {
      foreach (var encounter in args.preMobEncounters) {
        encounter.Apply(floor, room0);
      }
    }
    // X mobs
    for (var i = 0; i < args.numMobs; i++) {
      args.EncounterGroup.Mobs.GetRandomAndDiscount().Apply(floor, room0);
    }

    // Y grasses
    for (var i = 0; i < args.numGrasses; i++) {
      args.EncounterGroup.Grasses.GetRandomAndDiscount().Apply(floor, room0);
    }

    foreach (var encounter in args.extraEncounters) {
      encounter.Apply(floor, room0);
    }

    // a reward (optional)
    if (args.reward) {
      Encounters.AddWater.Apply(floor, room0);
      args.EncounterGroup.Rewards.GetRandomAndDiscount().Apply(floor, room0);
    }

    args.EncounterGroup.Spice.GetRandom().Apply(floor, room0);
    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }

  static Floor tryGenerateSingleRoomFloor(SingleRoomFloorParams args) {
    bool defaultEncounters = args.preMobEncounters == null;
    // We want to inset the downstairs and upstairs into the left and right walls. To do this,
    // we add one more column on each side
    Floor floor = new Floor(args.depth, args.width + 2, args.height);
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
      args.EncounterGroup.Walls.GetRandomAndDiscount().Apply(floor, room0);
      FloorUtils.NaturalizeEdges(floor);
      
      // chasms (bridge levels) should be relatively rare so only discount by 10% each time (this is still exponential decrease for the Empty case)
      args.EncounterGroup.Chasms.GetRandomAndDiscount(0.04f).Apply(floor, room0);
    }

    floor.SetStartPos(new Vector2Int(room0.min.x - 1, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(room0.max.x + 1, floor.height / 2));

    floor.root = room0;
    floor.rooms = new List<Room>();
    // floor.startRoom = room0;
    floor.downstairsRoom = room0;

    return floor;
  }

  public static void ensureConnectedness(Floor floor) {
    // here's how we ensure connectedness:
    // 1. find all walkable tiles on a floor
    // 2. group them by connectedness using bfs, starting from the upstairs as the "mainland"
    // 3. *connect* every group past mainland, updating mainland as you go
    var walkableTiles = new HashSet<Tile>(floor
      .EnumerateFloor()
      .Select(p => floor.tiles[p])
      .Where(t => t.BasePathfindingWeight() != 0)
    );
    
    var mainland = makeGroup(floor.startTile, ref walkableTiles);
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

}