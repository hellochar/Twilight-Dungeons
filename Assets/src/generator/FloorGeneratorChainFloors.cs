
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FloorGeneratorChainFloors : FloorGenerator {
  public FloorGeneratorChainFloors(List<int> floorSeeds) : base(floorSeeds) {
  }

  protected override EncounterGroup GetEncounterGroup(int depth) {
    /// configure the EncounterGroup
    if (depth <= 6) {
      return earlyGame;
    } else if (depth <= 12) {
      return everything;
    } else {
      return midGame;
    }
  }

  protected override void InitFloorGenerators() {
    floorGenerators = new List<Func<Floor>>() {
      // early game
      () => generateHomeFloor(),
      () => generateChainFloor(1 , 5, 6, 5, 1, 1, true),
      () => generateChainFloor(2 , 5, 6, 5, 1, 1, true),
      () => generateChainFloor(3 , 5, 6, 5, 2, 2, true),
      () => generateChainFloor(4 , 5, 7, 6, 2, 2, true),
      () => generateChainFloor(5 , 5, 7, 6, 3, 3, true),
      // () => generateRewardFloor(6, shared.Plants.GetRandomAndDiscount(1f)),
      () => generateRewardFloor(6, Encounters.AddWater, Encounters.OneAstoria),
      () => generateBlobBossFloor(7),

      // midgame
      () => generateChainFloor(8 , 5, 7, 6, 1, 1, true),
      () => generateChainFloor(9 , 5, 7, 6, 1, 1, true),
      () => generateChainFloor(10 , 5, 7, 6, 2, 2, true),
      () => generateChainFloor(11 , 5, 8, 7, 2, 2, true),
      () => generateChainFloor(12, 5, 8, 7, 3, 3, true),
      // () => generateRewardFloor(13, shared.Plants.GetRandomAndDiscount(1f)),
      () => generateRewardFloor(13, Encounters.AddWater, Encounters.OneAstoria),
      () => generateFungalColonyBossFloor(14),

      // endgame
      () => generateChainFloor(15, 5, 9, 7, 1, 1, true),
      () => generateChainFloor(16, 5, 9, 7, 1, 1, true),
      () => generateChainFloor(17, 5, 9, 7, 2, 2, true),
      () => generateChainFloor(18, 5, 10, 8, 2, 2, true),
      () => generateChainFloor(19, 5, 10, 8, 3, 3, true),
      // () => generateRewardFloor(20, shared.Plants.GetRandomAndDiscount(1f)),
      () => generateRewardFloor(20, Encounters.AddWater, Encounters.OneAstoria),
      () => generateEndBossFloor(21),
      () => generateEndFloor(22),
    };
  }

  public Floor generateChainFloor(int depth, int numRooms, int width, int height, int numMobs, int numGrasses, bool reward = false, Encounter[] preMobEncounters = null, params Encounter[] extraEncounters) {
    Floor floor = tryGenerateChainRoomFloor(depth, width, height, numRooms, preMobEncounters == null);
    // ensureConnectedness(floor);

    List<Encounter> encounters = new List<Encounter>();

    // this is essentially a poor man's "don't show the same content until all of it has been cycled through"
    var reduceChance = 0.999f;

    // X mobs
    for (var i = 0; i < numMobs; i++) {
      encounters.Add(EncounterGroup.Mobs.GetRandomWithoutAndDiscount(encounters, reduceChance));
    }

    // Y grasses
    for (var i = 0; i < numGrasses; i++) {
      encounters.Add(EncounterGroup.Grasses.GetRandomWithoutAndDiscount(encounters, reduceChance));
    }

    encounters.Add(EncounterGroup.Spice.GetRandom());

    Encounter rewardEncounter = null;
    if (reward) {
      rewardEncounter = EncounterGroup.Rewards.GetRandomWithoutAndDiscount(encounters, reduceChance);
    }
    // encounters.AddRange(extraEncounters);
    // if (reward) {
    //   encounters.Add(EncounterGroup.Rewards.GetRandomAndDiscount());
    // }

    int roomIntensity = 1;
    foreach (var room in floor.rooms) {
      for (var i = 0; i < roomIntensity; i++) {
        foreach(var encounter in encounters) {
          encounter(floor, room);
        }
      }
      // if (roomIntensity >= 3) {
        if (rewardEncounter != null) {
          rewardEncounter(floor, room);
        }
        // if (depth % 3 == 0) {
        //   Encounters.OneAstoria(floor, room);
        // }
      // }
      roomIntensity++;

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

  private Floor tryGenerateChainRoomFloor(int depth, int width, int height, int numChains = 3, bool defaultEncounters = true) {
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

    floor.rooms = rooms;
    for(int i = 0; i < numChains; i++) {
      var room = rooms[i];

      FloorUtils.CarveGround(floor, floor.EnumerateRoom(room));
      if (i > 0) {
        //where the slime will go
        var p1 = new Vector2Int(room.min.x - 1, room.center.y);
        var p2 = new Vector2Int(room.min.x - 2, room.center.y);
        floor.Put(new Ground(p1));
        floor.Put(new Slime(p1));

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

    floor.root = floor.rooms[0];
    return floor;
  }

}
