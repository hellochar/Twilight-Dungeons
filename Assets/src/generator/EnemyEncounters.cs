using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = MyRandom;

public partial class Encounters {
  public static void JackalPile(Floor floor, Room room) {
    var tiles = FloorUtils.TilesFromCenter(floor, room);
    var num = RandomRangeBasedOnIndex(floor.depth / 4,
      (1, 1),
      (2, 2),
      (3, 3)
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Jackal(tile.pos));
    }
  }

  public static Encounter AddWallflowers = new Encounter((Floor floor, Room room) => {
    var tiles = FloorUtils.TilesFromCenter(floor, room).Where(Wallflower.CanOccupy);
    var num = 2;
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Wallflower(tile.pos));
    }
  });

  public static Encounter AddBird = new Encounter((Floor floor, Room room) => {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    var num = 2;
    for (int i = 0; i < num; i++) {
      floor.Put(new Bird(tile.pos));
    }
  });

  public static Encounter AddSnake = new Encounter((Floor floor, Room room) => {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    floor.Put(new Snake(tile.pos));
  });

  public static void AddSkullys(Floor floor, Room room) {
    var tiles = FloorUtils.TilesFromCenter(floor, room);
    // var num = RandomRangeBasedOnIndex(floor.depth / 2,
    //   (1, 2),
    //   (2, 2),
    //   (2, 3),
    //   (2, 4)
    // );
    var num = 1;
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Skully(tile.pos));
    }
  }

  public static void AddOctopus(Floor floor, Room room) {
    var tiles = FloorUtils.TilesFromCenter(floor, room);
    var num = RandomRangeBasedOnIndex(floor.depth / 6,
      (1, 1),
      (2, 2)
    );
    tiles.Shuffle();
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Octopus(tile.pos));
    }
  }

  public static void AddClumpshroom(Floor floor, Room room) {
    var tiles = FloorUtils.TilesAwayFromCenter(floor, room).Where(t => t.pos.x >= room.center.x);
    var startTile = tiles.Skip(MyRandom.Range(0, 4)).FirstOrDefault();
    if (startTile != null) {
      floor.Put(new Clumpshroom(startTile.pos));
    }
  }

  public static void AFewBlobs(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var budget = 1 + Mathf.Pow(floor.depth, 0.9f) / 3.6f;
    var numMini = 0;
    var numNormal = 0;
    while (budget > 0) {
      bool isMini = Random.value < 0.5f;
      var cost = isMini ? 0.75f : 1;
      if (cost > budget) {
        // so if our budget is 0.4 but the cost is 1,
        // we have a 40% chance of still creating the creature,
        // aka a 60% chance of exiting now
        bool exitNow = Random.value * cost > budget;
        if (exitNow) {
          break;
        }
      }

      if (isMini) {
        numMini++;
      } else {
        numNormal++;
      }
      budget -= cost;
    }
    foreach (var tile in tiles.Take(numMini)) {
      floor.Put(new MiniBlob(tile.pos));
    }
    foreach (var tile in tiles.Skip(numMini).Take(numNormal)) {
      floor.Put(new Blob(tile.pos));
    }
  }

  public static void AFewSnails(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = RandomRangeBasedOnIndex(floor.depth / 4,
      (1, 1),
      (2, 2),
      (2, 3)
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Snail(tile.pos));
    }
  }

  public static void AddBats(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = floor.depth < 13 ? 1 : 2;
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Bat(tile.pos));
    }
  }
  
  public static void AddFungalSentinel(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = 3;
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new FungalSentinel(tile.pos));
    }
  }

  public static void AddFungalBreeder(Floor floor, Room room) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new FungalBreeder(tile.pos));
    }
  }

  public static void AddSpiders(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = RandomRangeBasedOnIndex(floor.depth / 4,
      (1, 1), // 0 - 3
      (2, 2), // 4 - 7
      (3, 3), // 8 - 11
      (3, 3), // 12 - 15
      (4, 4), // 16 - 19
      (4, 4)  // 20 - 23
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Spider(tile.pos));
    }
  }

  public static void AddScorpions(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = RandomRangeBasedOnIndex((floor.depth - 10) / 4,
      (1, 1), // 10 - 13
      (1, 2), // 14 - 18
      (2, 2) // 19+
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Scorpion(tile.pos));
    }
  }

  public static void AddGolems(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = floor.depth < 16 ? 1 : 2;
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Golem(tile.pos));
    }
  }

  public static void AddParasite(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    foreach (var tile in tiles.Take(3)) {
      floor.Put(new Parasite(tile.pos));
    }
  }
  public static void AddParasite8x(Floor floor, Room room) => Twice(Twice(Twice(AddParasite)))(floor, room);

  public static void AddHydra(Floor floor, Room room) {
    var tile = FloorUtils.TilesFromCenter(floor, room).Where((t) => t.CanBeOccupied()).FirstOrDefault();
    if (tile != null) {
      floor.Put(new HydraHeart(tile.pos));
    }
  }

  public static void AddIronJelly(Floor floor, Room room) {
    var tile = FloorUtils.TilesFromCenter(floor, room).Where((t) => t.CanBeOccupied() && t.pos.x == 3).FirstOrDefault();
    if (tile != null) {
      floor.Put(new IronJelly(tile.pos));
    }
  }

  public static void AddGrasper(Floor floor, Room room) {
    // put it on a wall that's next to a Ground
    // var tile = Util.RandomPick(floor.EnumerateRoomTiles(room, 1).Where(t => t is Wall && t.body == null && floor.GetCardinalNeighbors(t.pos).Any(t2 => t2 is Ground)));
    var tile = Util.RandomPick(
      floor.EnumerateRoomTiles(room, 0).Where(t => t.CanBeOccupied())
    );
    if (tile != null) {
      floor.Put(new Grasper(tile.pos));
    }
  }

  public static void AddWildekins(Floor floor, Room room) {
    var tiles = FloorUtils.TilesFromCenter(floor, room);
    var num = RandomRangeBasedOnIndex((floor.depth - 24) / 4,
      (1, 1), // 24-27
      (1, 1), // 28-31
      (1, 1) // 32-35
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Wildekin(tile.pos));
    }
  }

  public static void AddDizapper(Floor floor, Room room) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new Dizapper(tile.pos));
    }
  }

  public static void AddGoo(Floor floor, Room room) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new Goo(tile.pos));
    }
  }

  public static void AddHardShell(Floor floor, Room room) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new HardShell(tile.pos));
    }
  }

  public static void AddHoppers(Floor floor, Room room) {
    var startTile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (startTile != null) {
      var num = RandomRangeBasedOnIndex((floor.depth - 24) / 4,
        (2, 2), // 24-27
        (2, 2), // 28-31
        (2, 3)  // 32-35
      );
      foreach (var tile in floor.BreadthFirstSearch(startTile.pos, t => t.CanBeOccupied()).Take(num)) {
        floor.Put(new Hopper(tile.pos));
      }
    }
  }

  public static void AddThistlebog(Floor floor, Room room) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new Thistlebog(tile.pos));
    }
  }

  public static void AddHealer(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    floor.Put(new Healer(Util.RandomPick(tiles).pos));
  }

  public static void AddPoisoner(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    floor.Put(new Poisoner(Util.RandomPick(tiles).pos));
  }

  public static void AddVulnera(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    floor.Put(new Vulnera(Util.RandomPick(tiles).pos));
  }

  public static void AddMuckola(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    floor.Put(new Muckola(Util.RandomPick(tiles).pos));
  }

  public static void AddPistrala(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    floor.Put(new Pistrala(Util.RandomPick(tiles).pos));
  }
}
