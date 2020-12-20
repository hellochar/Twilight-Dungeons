using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate void Encounter(Floor floor, Room room);

public static class Encounters {
  // no op
  public static Encounter Empty = new Encounter((Floor, Room) => { });

  public static Encounter AFewBlobs = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var numBlobs = Random.Range(2, 4);
    foreach (var tile in tiles.Take(numBlobs)) {
      floor.Put(new Blob(tile.pos));
    }
  });

  public static Encounter AFewSnails = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    // sort by farthest distance to center
    foreach (var tile in tiles.Take(Random.Range(2, 4))) {
      floor.Put(new Snail(tile.pos));
    }
  });

  public static Encounter JackalPile = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    // TODO replace with bfs floodfill with random direction
    // emptyTilesInRoom.Sort((x, y) => Random.value < 0.5 ? -1 : 1);
    tiles.Sort((x, y) => Vector2Int.Distance(x.pos, room.center) < Vector2Int.Distance(y.pos, room.center) ? -1 : 1);
    var numJackals = Random.Range(2, 5);
    foreach (var tile in tiles.Take(numJackals)) {
      floor.Put(new Jackal(tile.pos));
    }
  });

  public static Encounter BatInCorner = new Encounter((floor, room) => {
    var tiles = FloorUtils.TilesSortedByCorners(floor, room).Where(tile => tile.CanBeOccupied());
    // sort by farthest distance to center
    foreach (var tile in tiles.Take(1)) {
      floor.Put(new Bat(tile.pos));
    }
  });

  public static Encounter MatureWildwood = new Encounter((floor, room) => {
    // add a soil at the center
    Tile tile = FloorUtils.EmptyTileNearestCenter(floor, room);

    if (tile != null && !(tile is Downstairs || tile is Upstairs)) {
      floor.Put(new Soil(tile.pos));
      var bush = new Wildwood(tile.pos);
      // jump to Mature
      bush.stage = bush.stage.NextStage.NextStage;
      floor.Put(bush);
    }
  });

  public static Encounter FreeSoil = new Encounter((floor, room) => {
    Tile tile = FloorUtils.EmptyTileNearestCenter(floor, room);
    floor.Put(new Soil(tile.pos));
  });

  public static Encounter CoverWithSoftGrass = new Encounter((floor, room) => {
    foreach (var tile in floor.EnumerateRoomTiles(room).Where((tile) => tile is Ground && tile.grass == null)) {
      var grass = new SoftGrass(tile.pos);
      floor.Put(grass);
    }
  });

  public static Encounter AddHangingVines = new Encounter((floor, room) => {
    var wallsWithGroundBelow = floor.EnumerateRoomTiles(room, 1).Where((tile) => tile is Wall && tile.pos.y > 0 && floor.tiles[tile.pos + new Vector2Int(0, -1)] is Ground);
    while (wallsWithGroundBelow.Any()) {
      var skipLength = Random.Range(3, 7);
      foreach (var tile in wallsWithGroundBelow.Take(1)) {
        floor.Put(new HangingVines(tile.pos));
      }
      wallsWithGroundBelow = wallsWithGroundBelow.Skip(1 + skipLength);
    }
  });

  public static Encounter AddMushroom = new Encounter((floor, room) => {
    IEnumerable<Tile> tiles = floor.EnumerateRoomTiles(room).ToList();
    var tilesWhereMushroomsCanLive = tiles.Where((tile) => Mushroom.CanLiveIn(tile)).ToList();
    if (tilesWhereMushroomsCanLive.Any()) {
      var chosenTile = Util.RandomPick(tilesWhereMushroomsCanLive);
      floor.Put(new Mushroom(chosenTile.pos));
    } else {
      Debug.LogError("Couldn't find a location for mushrooms!");
    }
  });

  public static Encounter PlaceFancyGround = new Encounter((floor, room) => {
    var groundTiles = floor.EnumerateRoomTiles(room).Where((tile) => tile is Ground);
    foreach (var tile in groundTiles) {
      // this doesn't replace the grass, item, or actor
      floor.Put(new FancyGround(tile.pos));
    }
  });

  public static Encounter SurroundWithRubble = new Encounter((floor, room) => {
    var perimeter = floor.EnumerateRoomTiles(room, 1).Except(floor.EnumerateRoomTiles(room, 0));
    var entrancesAndExits = perimeter.Where(tile => tile.BasePathfindingWeight() != 0);
    foreach (var tile in entrancesAndExits) {
      floor.Put(new Rubble(tile.pos));
    }
  });

  public static Encounter ThreePlumpAstoriasInCorner = new Encounter((floor, room) => {
    var positions = FloorUtils.TilesSortedByCorners(floor, room).Where(t => t.CanBeOccupied() && t is Ground && t.grass == null);
    foreach (var tile in positions.Take(3)) {
      floor.Put(new PlumpAstoria(tile.pos));
    }
  });

  public static Encounter OneAstoria = new Encounter((floor, room) => {
    var positions = FloorUtils.EmptyTilesInRoom(floor, room);
    positions.Shuffle();
    foreach (var tile in positions.Take(1)) {
      floor.Put(new PlumpAstoria(tile.pos));
    }
  });


  public static Encounter ScatteredBoombugs = new Encounter((floor, room) => {
    var emptyTilesInRoom = FloorUtils.EmptyTilesInRoom(floor, room);
    emptyTilesInRoom.Shuffle();
    var num = Random.Range(1, 3);
    foreach (var tile in emptyTilesInRoom.Take(num)) {
      floor.Put(new Boombug(tile.pos));
    }
  });

  public static Encounter AddDeathbloom = new Encounter((floor, room) => {
    // Tile tile = FloorUtils.EmptyTileNearestCenter(floor, room);
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room).Where((t) => t is Ground && t.grass == null).ToList();
    tiles.Sort((x, y) => Vector2Int.Distance(x.pos, room.center) < Vector2Int.Distance(y.pos, room.center) ? -1 : 1);

    var tile = tiles.FirstOrDefault();
    if (tile != null) {
      floor.Put(new Deathbloom(tile.pos));
    }
  });

  public static Encounter OneSpider = new Encounter((floor, room) => {
    var floorCenter = floor.boundsMax / 2;
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    var farthestFromCenter = tiles.OrderByDescending(t => t.DistanceTo(floorCenter)).FirstOrDefault();
    if (farthestFromCenter != null) {
      floor.Put(new Spider(farthestFromCenter.pos));
    }
  });

  public static Encounter OneLocust = new Encounter((floor, room) => {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new Locust(tile.pos));
    }
  });

  public static WeightedRandomBag<Encounter> CavesMobs = new WeightedRandomBag<Encounter> {
    { 2.5f, Empty },
    { 1, AFewBlobs },
    { 1, JackalPile },
    { 1, AFewSnails },
    { 1f, BatInCorner }
  };

  public static WeightedRandomBag<Encounter> CavesGrasses = new WeightedRandomBag<Encounter> {
    { 6f, Empty },
    { 1f, CoverWithSoftGrass },
    { 0.9f, AddHangingVines },
    { 1f, ScatteredBoombugs },
    { 0.2f, AddDeathbloom },
  };

  public static WeightedRandomBag<Encounter> CavesDeadEnds = new WeightedRandomBag<Encounter> {
    /// just to make it interesting, always give dead ends *something*
    { 2f, Empty },
    { 0.2f, FreeSoil },
    { 1f, OneAstoria },
    { 0.2f, AddDeathbloom },
    { 0.5f, AFewBlobs },
    { 0.5f, JackalPile },
    { 0.5f, AFewSnails }
  };

  public static WeightedRandomBag<Encounter> CavesRewards = new WeightedRandomBag<Encounter> {
    { 1f, AddMushroom },
    { 1f, FreeSoil },
    { 1f, OneAstoria },
    { 0.5f, OneSpider },
    { 0.5f, OneLocust },
    { 0.5f, ThreePlumpAstoriasInCorner },
    { 0.1f, MatureWildwood }
  };
}