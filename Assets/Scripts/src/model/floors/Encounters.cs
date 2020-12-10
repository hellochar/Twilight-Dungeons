using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Encounter {
  public Encounter(System.Action<Floor, Room> apply) {
    this.Apply = apply;
  }

  public System.Action<Floor, Room> Apply { get; }
}

public static class Encounters {
  // no op
  public static Encounter Empty = new Encounter((Floor, Room) => { });

  public static Encounter AFewBlobs = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var numBlobs = Random.Range(2, 4);
    foreach (var tile in tiles.Take(numBlobs)) {
      floor.Add(new Blob(tile.pos));
    }
  });

  public static Encounter JackalPile = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    // TODO replace with bfs floodfill with random direction
    // emptyTilesInRoom.Sort((x, y) => Random.value < 0.5 ? -1 : 1);
    tiles.Sort((x, y) => Vector2Int.Distance(x.pos, room.center) < Vector2Int.Distance(y.pos, room.center) ? -1 : 1);
    var numJackals = Random.Range(2, 5);
    foreach (var tile in tiles.Take(numJackals)) {
      floor.Add(new Jackal(tile.pos));
    }
  });

  public static Encounter BatsInCorner = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    // sort by farthest distance to center
    foreach (var tile in tiles.Take(2)) {
      floor.Add(new Bat(tile.pos));
    }
  });

  public static Encounter MatureWildwood = new Encounter((floor, room) => {
    // add a soil at the center
    Tile tile = FloorUtils.EmptyTileNearestCenter(floor, room);

    if (tile != null && !(tile is Downstairs || tile is Upstairs)) {
      floor.tiles.Put(new Soil(tile.pos));
      var bush = new Wildwood(tile.pos);
      // jump to Mature
      bush.stage = bush.stage.NextStage.NextStage;
      floor.Add(bush);
    }
  });

  public static Encounter CoverWithSoftGrass = new Encounter((floor, room) => {
    foreach (var tile in floor.EnumerateRoomTiles(room).Where((tile) => tile is Ground)) {
      var grass = new SoftGrass(tile.pos);
      floor.Add(grass);
    }
  });

  public static Encounter AddHangingVines = new Encounter((floor, room) => {
    var wallsWithGroundBelow = floor.EnumerateRoomTiles(room, 1).Where((tile) => tile is Wall && tile.pos.y > 0 && floor.tiles[tile.pos + new Vector2Int(0, -1)] is Ground);
    while (wallsWithGroundBelow.Any()) {
      var skipLength = Random.Range(2, 5);
      foreach (var tile in wallsWithGroundBelow.Take(1)) {
        floor.Add(new HangingVines(tile.pos));
      }
      wallsWithGroundBelow = wallsWithGroundBelow.Skip(1 + skipLength);
    }
  });

  public static Encounter AddMushroom = new Encounter((floor, room) => {
    var tilesNextToWalls = FloorUtils.TilesNextToWalls(floor, room);
    var spacing = 5;
    while (tilesNextToWalls.Any()) {
      var chosenTile = Util.RandomPick(tilesNextToWalls.Take(spacing));
      floor.Add(new Mushroom(chosenTile.pos));
      tilesNextToWalls = tilesNextToWalls.Skip(spacing);
    }
    // // also pick another Encounter
    // var otherEncounter = CavesStandard.GetRandomWithout(CoverWithGrass, AddRedvines);
    // otherEncounter.Apply(floor, room);
  });

  public static Encounter ThreePlumpAstoriasInCorner = new Encounter((floor, room) => {
    var positions = FloorUtils.TilesSortedByCorners(floor, room).Where(t => t.CanBeOccupied() && t is Ground && t.grass == null);
    foreach (var tile in positions.Take(3)) {
      floor.Add(new PlumpAstoria(tile.pos));
    }
  });

  public static Encounter ScatteredBoombugs = new Encounter((floor, room) => {
    var emptyTilesInRoom = FloorUtils.EmptyTilesInRoom(floor, room);
    emptyTilesInRoom.Shuffle();
    var num = Random.Range(2, 4);
    foreach (var tile in emptyTilesInRoom.Take(num)) {
      floor.Add(new Boombug(tile.pos));
    }
  });

  public static Encounter AddDeathbloom = new Encounter((floor, room) => {
    // Tile tile = FloorUtils.EmptyTileNearestCenter(floor, room);
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room).Where((t) => t is Ground && t.grass == null).ToList();
    tiles.Sort((x, y) => Vector2Int.Distance(x.pos, room.center) < Vector2Int.Distance(y.pos, room.center) ? -1 : 1);

    var tile = tiles.FirstOrDefault();
    if (tile != null) {
      floor.Add(new Deathbloom(tile.pos));
    }
  });

  public static WeightedRandomBag<Encounter> CavesMobs = new WeightedRandomBag<Encounter> {
    { 1.5f, Empty },
    { 1, AFewBlobs },
    { 1, JackalPile },
    { 1, ScatteredBoombugs },
    // { 0.8f, BatsInCorner },
    { 0.1f, MatureWildwood },
  };

  public static WeightedRandomBag<Encounter> CavesGrasses = new WeightedRandomBag<Encounter> {
    { 5f, Empty },
    { 1f, CoverWithSoftGrass },
    { 1f, AddHangingVines },
    { 1f, AddMushroom },
    { 1f, AddDeathbloom },
    { 0.5f, ThreePlumpAstoriasInCorner },
  };
}