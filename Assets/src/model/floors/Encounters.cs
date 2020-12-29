using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate void Encounter(Floor floor, Room room);

public static class Encounters {
  private static int RandomRangeBasedOnFloor(Floor floor, params (int, int)[] values) {
    if (floor.depth == 0) { // When I debug encounters by adding them to floor 0
      return 3;
    }
    var (min, max) = Util.ClampPick(floor.depth - 1, values);
    return Random.Range(min, max + 1);
  }

  // no op
  public static Encounter Empty = new Encounter((Floor, Room) => { });

  public static Encounter AFewBlobs = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var numBlobs = RandomRangeBasedOnFloor(floor,
      (1, 1),
      (1, 2),
      (1, 3),
      (2, 3)
    );
    foreach (var tile in tiles.Take(numBlobs)) {
      floor.Put(new Blob(tile.pos));
    }
  });

  public static Encounter AFewSnails = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = RandomRangeBasedOnFloor(floor,
      (2, 2),
      (2, 3),
      (2, 4),
      (3, 5)
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Snail(tile.pos));
    }
  });

  public static Encounter JackalPile = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.OrderBy(x => Vector2.Distance(x.pos, room.centerFloat));
    var num = RandomRangeBasedOnFloor(floor,
      (1, 2),
      (2, 2),
      (2, 3),
      (2, 4)
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Jackal(tile.pos));
    }
  });

  public static Encounter BatInCorner = new Encounter((floor, room) => {
    var tiles = FloorUtils.TilesSortedAwayFromFloorCenter(floor, room).Where(tile => tile.CanBeOccupied());
    // sort by farthest distance to center of map
    foreach (var tile in tiles.Take(1)) {
      floor.Put(new Bat(tile.pos));
    }
  });

  public static Encounter MatureBerryBush = new Encounter((floor, room) => {
    // add a soil at the center
    Tile tile = FloorUtils.EmptyTileNearestCenter(floor, room);

    if (tile != null && !(tile is Downstairs || tile is Upstairs)) {
      floor.Put(new Soil(tile.pos));
      var bush = new BerryBush(tile.pos);
      // jump to Mature
      bush.stage = bush.stage.NextStage.NextStage;
      floor.Put(bush);
    }
  });

  public static Encounter FreeSoil = new Encounter((floor, room) => {
    Tile tile = FloorUtils.EmptyTileNearestCenter(floor, room);
    floor.Put(new Soil(tile.pos));
  });

  public static Encounter AddSoftGrass = new Encounter((floor, room) => {
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where((tile) => tile is Ground && tile.grass == null));
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var start = Util.RandomPick(occupiableTiles);
      var bfs = floor.BreadthFirstSearch(start.pos, (tile) => occupiableTiles.Contains(tile));
      var numSoftGrass = Random.Range(numTiles / 4, numTiles + 1);
      foreach (var tile in bfs.Take(numSoftGrass)) {
        var grass = new SoftGrass(tile.pos);
        floor.Put(grass);
      }
    }
  });

  public static Encounter AddBrambles = new Encounter((floor, room) => {
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where((tile) => tile is Ground && tile.grass == null));
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var start = Util.RandomPick(occupiableTiles);
      var bfs = floor.BreadthFirstSearch(start.pos, (tile) => occupiableTiles.Contains(tile));
      var numSoftGrass = Random.Range(numTiles / 8, numTiles / 4);
      foreach (var tile in bfs.Take(numSoftGrass)) {
        var grass = new Brambles(tile.pos);
        floor.Put(grass);
      }
    }
  });

  public static Encounter AddSpore = new Encounter((floor, room) => {
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where((tile) => tile is Ground && tile.grass == null));
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var start = Util.RandomPick(occupiableTiles);
      var grass = new Spores(start.pos);
      floor.Put(grass);
    }
  });

  public static Encounter AddHangingVines = new Encounter((floor, room) => {
    var wallsWithGroundBelow = floor
      .EnumerateRoomTiles(room, 1)
      .Where((tile) =>
        tile.grass == null &&
        tile is Wall &&
        tile.pos.y > 0 &&
        floor.tiles[tile.pos + new Vector2Int(0, -1)] is Ground
      );
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
    if (tilesWhereMushroomsCanLive.Count == 0) {
      Debug.LogError("Couldn't find a location for mushrooms!");
    }
    foreach (var tile in tilesWhereMushroomsCanLive) {
      floor.Put(new Mushroom(tile.pos));
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
    var entrancesAndExits = perimeter.Where(tile => tile.CanBeOccupied());
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

  public static Encounter OnePlumpAstoria = new Encounter((floor, room) => {
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

  public static Encounter OneButterfly = new Encounter((floor, room) => {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new Butterfly(tile.pos));
    }
  });

  public static Encounter AddWater = new Encounter((floor, room) => {
    var numWaters = Random.Range(3, 7);
    var startPos = room.center;
    foreach (var tile in floor.BreadthFirstSearch(startPos, (tile) => tile is Ground).Take(numWaters)) {
      floor.Put(new Water(tile.pos));
    }
  });

  public static Encounter AddJackalHide = new Encounter((floor, room) => {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new ItemOnGround(tile.pos, new ItemJackalHide()));
    }
  });

  public static Encounter AddGloopShoes = new Encounter((floor, room) => {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new ItemOnGround(tile.pos, new ItemGloopShoes()));
    }
  });

  public static Encounter AddPumpkin = new Encounter((floor, room) => {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new ItemOnGround(tile.pos, new ItemPumpkin()));
    }
  });

  public static WeightedRandomBag<Encounter> CavesMobs = new WeightedRandomBag<Encounter> {
    { 2.5f, Empty },
    { 1, AFewBlobs },
    { 1, JackalPile },
    { 1, AFewSnails },
    { 0.4f, BatInCorner },
    { 0.35f, OneSpider }
  };

  public static WeightedRandomBag<Encounter> CavesGrasses = new WeightedRandomBag<Encounter> {
    { 3f, Empty },
    { 1f, AddSoftGrass },
    { 0.75f, AddHangingVines },
    { 0.5f, AddSpore },
    { 0.5f, AddBrambles },
    { 0.4f, ScatteredBoombugs },
    { 0.2f, AddDeathbloom },
  };

  public static WeightedRandomBag<Encounter> CavesDeadEnds = new WeightedRandomBag<Encounter> {
    /// just to make it interesting, always give dead ends *something*
    { 5f, Empty },
    { 0.5f, AddWater },
    { 0.2f, FreeSoil },
    { 0.2f, AddDeathbloom },
    { 0.5f, AFewBlobs },
    { 0.5f, JackalPile },
    { 0.5f, AFewSnails },
    { 0.1f, OneSpider },
  };

  public static WeightedRandomBag<Encounter> CavesRewards = new WeightedRandomBag<Encounter> {
    { 1f, AddMushroom },
    { 1f, AddPumpkin },
    { 1f, OnePlumpAstoria },
    { 0.5f, AddJackalHide },
    { 0.5f, AddGloopShoes },
    { 0.5f, OneButterfly },
    { 0.5f, ThreePlumpAstoriasInCorner },
    { 0.1f, MatureBerryBush }
  };
}