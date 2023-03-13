using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class Generators {

  public static Floor generateChainFloor(EncounterGroup EncounterGroup, int depth, int numRooms, int width, int height, int numMobs, int numGrasses, bool reward = false, Encounter[] preMobEncounters = null, params Encounter[] extraEncounters) {
    Floor floor = tryGenerateChainRoomFloor(EncounterGroup, depth, width, height, numRooms, preMobEncounters == null);
    // ensureConnectedness(floor);

    List<Encounter> mobEncounters = new List<Encounter>();
    List<Encounter> grassEncounters = new List<Encounter>();

    // this is essentially a poor man's "don't show the same content until all of it has been cycled through"
    var reduceChance = 0.999f;

    // X mobs
    for (var i = 0; i < numMobs; i++) {
      mobEncounters.Add(EncounterGroup.Mobs.GetRandomWithoutAndDiscount(mobEncounters, reduceChance));
      // mobEncounters.Add(EncounterGroup.Mobs.GetRandomAndDiscount());
    }

    // Y grasses
    for (var i = 0; i < numGrasses; i++) {
      grassEncounters.Add(EncounterGroup.Grasses.GetRandomWithoutAndDiscount(grassEncounters, reduceChance));
    }

    grassEncounters.Add(EncounterGroup.Spice.GetRandom());

    // Encounter rewardEncounter = null;
    // if (reward) {
    //   rewardEncounter = EncounterGroup.Rewards.GetRandomWithoutAndDiscount(encounters, reduceChance);
    // }

    // encounters.AddRange(extraEncounters);
    // if (reward) {
    //   encounters.Add(EncounterGroup.Rewards.GetRandomAndDiscount());
    // }

    int roomIntensity = 1;
    foreach (var room in floor.rooms) {
      for (var i = 0; i < roomIntensity; i++) {
        foreach(var encounter in mobEncounters) {
          encounter(floor, room);
        }
      }
      foreach(var encounter in grassEncounters) {
        encounter(floor, room);
      }
      // if (roomIntensity >= 3) {
        // if (rewardEncounter != null) {
        //   rewardEncounter(floor, room);
        // }
        // if (depth % 3 == 0) {
        //   Encounters.OneAstoria(floor, room);
        // }
      // }
      roomIntensity++;
      // mobEncounters.Add(EncounterGroup.Mobs.GetRandomAndDiscount());

      // add slimes
      // var entrancesAndExits = floor.EnumerateRoomPerimeter(room, -1).Where(pos => floor.tiles[pos].CanBeOccupied());
      // foreach (var pos in entrancesAndExits) {
      //   floor.Put(new Slime(pos));
      // }
    }

    // specifically used for e.g. moving downstairs to a center.
    if (preMobEncounters != null) {
      foreach (var encounter in preMobEncounters) {
        encounter(floor, floor.downstairsRoom);
      }
    }

    FloorUtils.TidyUpAroundStairs(floor);
    foreach(var room in floor.rooms) {
      // make the room own the top and bottom edges so visibility will properly show the wall edges
      room.max += Vector2Int.one;
      room.min -= Vector2Int.one;
    }
    return floor;
  }

  private static Floor tryGenerateChainRoomFloor(EncounterGroup EncounterGroup, int depth, int width, int height, int numChains = 3, bool defaultEncounters = true) {
    int heightFor(int i) {
      return height + (i >= 2 ? 1 : 0);
    }
    int maxHeight = heightFor(numChains - 1);
    // int maxHeight = height;

    var rooms = new List<Room>();

    var x = 1;
    for(int i = 0; i < numChains; i++) {
      var thisWidth = width + i;
      var thisHeight = heightFor(i);

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
    List<Vector2Int> slimeAdjacent = new List<Vector2Int>();

    floor.rooms = rooms;
    for(int i = 0; i < numChains; i++) {
      var room = rooms[i];

      FloorUtils.CarveGround(floor, floor.EnumerateRoom(room));
      if (i > 0) {
        //where the slime will go
        var p1 = new Vector2Int(room.min.x - 1, room.center.y);
        slimeAdjacent.Add(p1 + Vector2Int.right);
        floor.Put(new Ground(p1));
        floor.Put(new Slime(p1));

        var p2 = new Vector2Int(room.min.x - 2, room.center.y);
        slimeAdjacent.Add(p2 + Vector2Int.left);
        floor.Put(new Ground(p2));
        floor.Put(new Slime(p2));

      }

      if (defaultEncounters) {
        // one wall variation
        EncounterGroup.Walls.GetRandomAndDiscount()(floor, room);
        
        // chasms (bridge levels) should be relatively rare so only discount by 10% each time (this is still exponential decrease for the Empty case)
        // EncounterGroup.Chasms.GetRandomAndDiscount(0.04f)(floor, room);
      }
      // floor.PlaceDownstairs(room.max - Vector2Int.one);
      if (i == 0) {
        // floor.startRoom = room;
      } else if (i == numChains - 1) {
        floor.downstairsRoom = room;
      }
    }

    FloorUtils.NaturalizeEdges(floor);

    floor.SetStartPos(new Vector2Int(rooms[0].min.x, rooms[0].center.y));

    // ensure walkability
    // foreach (var pos in floor.EnumerateLine(floor.startPos, new Vector2Int(floor.downstairsRoom.max.x, floor.downstairsRoom.center.y))) {
    foreach (var pos in slimeAdjacent) {
      if (!(floor.tiles[pos] is Ground)) {
        floor.Put(new Ground(pos));
      }
    }

    floor.root = floor.rooms[0];
    return floor;
  }

}