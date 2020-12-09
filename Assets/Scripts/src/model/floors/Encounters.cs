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
  public static Encounter Empty = new Encounter((Floor, Room) => {});

  public static Encounter AFewBlobs = new Encounter((floor, room) => {
    var emptyTilesInRoom = floor.EnumerateRoomTiles(room).Where(t => t.CanBeOccupied()).ToList();
    emptyTilesInRoom.Shuffle();
    var numBlobs = Random.Range(2, 4);
    foreach (var tile in emptyTilesInRoom.Take(numBlobs)) {
      floor.Add(new Blob(tile.pos));
    }
  });

  public static Encounter JackalPile = new Encounter((floor, room) => {
    var emptyTilesInRoom = floor.EnumerateRoomTiles(room).Where(t => t.CanBeOccupied()).ToList();
    // TODO replace with bfs floodfill with random direction
    // emptyTilesInRoom.Sort((x, y) => Random.value < 0.5 ? -1 : 1);
    emptyTilesInRoom.Sort((x, y) => Vector2Int.Distance(x.pos, room.center) < Vector2Int.Distance(y.pos, room.center) ? -1 : 1);
    var numJackals = Random.Range(3, 7);
    foreach (var tile in emptyTilesInRoom.Take(numJackals)) {
      floor.Add(new Jackal(tile.pos));
    }
  });

  public static Encounter BatsInCorner = new Encounter((floor, room) => {
    var emptyTilesInRoom = FloorUtils.TilesSortedByCorners(floor, room).Where(tile => tile.CanBeOccupied());
    // sort by farthest distance to center
    foreach (var tile in emptyTilesInRoom.Take(2)) {
      floor.Add(new Bat(tile.pos));
    }
  });

  public static Encounter MatureBush = new Encounter((floor, room) => {
    // add a soil at the center
    var emptyTilesInRoom = floor.EnumerateRoomTiles(room).Where(t => t.CanBeOccupied()).ToList();
    emptyTilesInRoom.Sort((x, y) => Vector2Int.Distance(x.pos, room.center) < Vector2Int.Distance(y.pos, room.center) ? -1 : 1);
    var emptyTileNearestCenter = emptyTilesInRoom.FirstOrDefault();

    if (emptyTileNearestCenter != null && !(emptyTileNearestCenter is Downstairs || emptyTileNearestCenter is Upstairs)) {
      floor.tiles.Put(new Soil(emptyTileNearestCenter.pos));
      var bush = new BerryBush(emptyTileNearestCenter.pos);
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
    var tilesNextToWalls = floor.EnumerateRoomTiles(room).Where((tile) => tile is Ground && floor.GetAdjacentTiles(tile.pos).Any(x => x is Wall) && tile.grass == null);
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

  public static WeightedRandomBag<Encounter> CavesMobs = new WeightedRandomBag<Encounter> {
    { 1.5f, Empty },
    { 1, AFewBlobs },
    { 1, JackalPile },
    { 0.8f, BatsInCorner },
    { 0.1f, MatureBush },
  };

  public static WeightedRandomBag<Encounter> CavesGrasses = new WeightedRandomBag<Encounter> {
    { 5f, Empty },
    { 1, CoverWithSoftGrass },
    { 1f, AddHangingVines },
    { 1f, AddMushroom },
    { 0.2f, ThreePlumpAstoriasInCorner },
  };
}