using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class MultiRoomFloorParams : FloorGenerationParams {
  private readonly EncounterGroup EncounterGroup;
  private readonly int width;
  private readonly int height;
  private readonly int numSplits;
  private readonly bool hasReward;
  private readonly Encounter[] specialDownstairsEncounters;

  public MultiRoomFloorParams(EncounterGroup EncounterGroup, int depth, int width = 60, int height = 20, int numSplits = 20, bool hasReward = false, params Encounter[] specialDownstairsEncounters)
    : base(depth)
  {
    this.EncounterGroup = EncounterGroup;
    this.width = width;
    this.height = height;
    this.numSplits = numSplits;
    this.hasReward = hasReward;
    this.specialDownstairsEncounters = specialDownstairsEncounters;
  }

  public override Floor generate() {
    return Generate.MultiRoomFloor(EncounterGroup, depth, width, height, numSplits, hasReward, specialDownstairsEncounters);
  }
}

public static partial class Generate {
  /// <summary>
  /// Generate a floor broken up into X smaller rooms, based on a number of "splits". Each room contains:
  /// one mob, one grass, one random encounter.
  /// </summary>
  public static Floor MultiRoomFloor(EncounterGroup EncounterGroup, int depth, int width = 60, int height = 20, int numSplits = 20, bool hasReward = false, params Encounter[] specialDownstairsEncounters) {
    Floor floor = tryGenerateMultiRoomFloor(depth, width, height, numSplits);
    ensureConnectedness(floor);

    var intermediateRooms = floor.rooms
      .Where((room) => room != floor.startRoom && room != floor.downstairsRoom);

    Room rewardRoom = null;
    if (hasReward) {
      // the non-downstairs terminal room farthest away from the upstairs according to pathfinding
      rewardRoom = intermediateRooms
        .OrderByDescending((room) => floor.FindPath(floor.startTile.pos, room.center).Count)
        .First();

      Encounters.PlaceFancyGround.Apply(floor, rewardRoom);
      Encounters.SurroundWithRubble.Apply(floor, rewardRoom);
      var rewardEncounter = EncounterGroup.Rewards.GetRandomAndDiscount();
      rewardEncounter.Apply(floor, rewardRoom);
    }

    var deadEndRooms = intermediateRooms.Where((room) => room != rewardRoom && room.connections.Count < 2);
    foreach (var room in deadEndRooms) {
      if (MyRandom.value < 0.05f) {
        Encounters.SurroundWithRubble.Apply(floor, room);
      }
      var encounter = EncounterGroup.Spice.GetRandomAndDiscount();
      encounter.Apply(floor, room);
    }

    foreach (var room in floor.rooms) {
      if (room != floor.startRoom && room != rewardRoom) {
        // spawn a random encounter
        var encounter = EncounterGroup.Mobs.GetRandomAndDiscount();
        encounter.Apply(floor, room);
      }
#if experimental_chainfloors
      Encounters.SurroundWithRubble(floor, room);
#endif
    }

    // this includes abstract rooms!
    foreach(var room in floor.root.Traverse().Where((room) => room.depth >= 2)) {
      var encounter = EncounterGroup.Grasses.GetRandomAndDiscount();
      encounter.Apply(floor, room);
    }

    foreach (var encounter in specialDownstairsEncounters) {
      encounter.Apply(floor, floor.downstairsRoom);
    }

    FloorUtils.TidyUpAroundStairs(floor);

    return floor;
  }

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
    floor.SetStartPos(upstairsPos);

    Room downstairsRoom = rooms.Last();
    // 1-px padding from the bottom-right of the room
    Vector2Int downstairsPos = new Vector2Int(downstairsRoom.max.x - 1, downstairsRoom.min.y + 1);
    floor.PlaceDownstairs(downstairsPos);

    floor.root = root;
    floor.rooms = rooms;
    // floor.startRoom = upstairsRoom;
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
      Encounters.ChasmsAwayFromWalls1.Apply(floor, depth2Room);
    }
#endif

    return floor;
  }

  /// connectivity is not guaranteed
  /// Connect all the rooms together with at least one through-path
  public static List<(Vector2Int, Vector2Int)> ComputeRoomConnections(List<Room> rooms, Room root) {
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
    var path = floor.FindPath(floor.downstairs.pos, floor.startTile.pos);
    return path.Any();
  }
}