using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

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
    AddPlantInCenter(floor, room, typeof(BerryBush));
  });
  
  public static Encounter MatureWildWood = new Encounter((floor, room) => {
    AddPlantInCenter(floor, room, typeof(Wildwood));
  });

  public static Encounter MatureThornleaf = new Encounter((floor, room) => {
    AddPlantInCenter(floor, room, typeof(Thornleaf));
  });

  private static void AddPlantInCenter(Floor floor, Room room, System.Type type) {
    Tile tile = FloorUtils.TilesFromCenter(floor, room).FirstOrDefault();
    if (tile != null) {
      floor.Put(new Soil(tile.pos));
      var constructor = type.GetConstructor(new Type[] { typeof(Vector2Int) });
      var plant = (Plant) constructor.Invoke(new object[1] { tile.pos });
      plant.GoNextStage();
      plant.GoNextStage();
      floor.Put(plant);
    }
  }

  public static Encounter FreeSoil = new Encounter((floor, room) => {
    Tile tile = FloorUtils.TilesFromCenter(floor, room).First();
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

  public static Encounter AddGuardleaf = new Encounter((floor, room) => {
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where((tile) => tile is Ground && tile.grass == null));
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var start = Util.RandomPick(occupiableTiles);
      var bfs = floor.BreadthFirstSearch(start.pos, (tile) => occupiableTiles.Contains(tile));
      var num = Random.Range(3, 7);
      foreach (var tile in bfs.Take(num)) {
        floor.Put(new Guardleaf(tile.pos));
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
      ).ToList();
    for (int i = 1; i < wallsWithGroundBelow.Count; i += Random.Range(2, 4)) {
      floor.Put(new HangingVines(wallsWithGroundBelow[i].pos));
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
    foreach (var tile in FloorUtils.TilesSortedAwayFromFloorCenter(floor, room).Where((tile) => tile is Ground && tile.grass == null).Take(numWaters)) {
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
    var tile = FloorUtils.TilesFromCenter(floor, room).FirstOrDefault();
    if (tile != null) {
      floor.Put(new ItemOnGround(tile.pos, new ItemPumpkin()));
    }
  });

  public static Encounter WallPillars = new Encounter((floor, room) => {
    var positions = floor.EnumerateRoom(room).ToList();
    positions.Shuffle();
    foreach (var pos in positions) {
      if (!floor.GetAdjacentTiles(pos).Any((t) => t is Wall)) {
        floor.Put(new Wall(pos));
      }
    }
  });

  public static Encounter ChunkInMiddle = new Encounter((floor, room) => {
    var chunkSize = Random.Range(1, 6);
    var positions = floor.BreadthFirstSearch(room.center, (tile) => true).Take(chunkSize).Select(t => t.pos);
    foreach(var pos in positions) {
      floor.Put(new Wall(pos));
    }
  });

  public static Encounter LineWithOpening = new Encounter((floor, room) => {
    var start = Util.RandomPick(floor.EnumeratePerimeter());
    var end = new Vector2Int(floor.width - 1 - start.x, floor.height - 1 - start.y);
    var line = floor
      .EnumerateLine(start, end)
      .Select(p => floor.tiles[p])
      .Where(t => t.CanBeOccupied() && !(t is Downstairs || t is Upstairs)).ToList();
    // remove the middle
    var openingSize = Random.Range(2, line.Count / 2 + 1);
    var openingStart = Random.Range(0, line.Count - 1 - openingSize + 1);
    line.RemoveRange(openingStart, openingSize);
    foreach (var oldTile in line) {
      floor.Put(new Wall(oldTile.pos));
    }
  });

  public static WeightedRandomBag<Encounter> CavesMobs = new WeightedRandomBag<Encounter> {
    // { 2.5f, Empty },
    { 1, AFewBlobs },
    { 1, JackalPile },
    { 1, AFewSnails },
    { 0.4f, BatInCorner },
    { 0.35f, OneSpider }
  };

  public static WeightedRandomBag<Encounter> CavesWalls = new WeightedRandomBag<Encounter> {
    { 3f, Empty },
    { 0.5f, WallPillars },
    { 0.5f, ChunkInMiddle },
    { 0.5f, LineWithOpening },
  };

  public static WeightedRandomBag<Encounter> CavesGrasses = new WeightedRandomBag<Encounter> {
    // { 1f, Empty },
    { 1f, AddSoftGrass },
    { 0.75f, AddBrambles },
    { 0.75f, AddHangingVines },
    { 0.5f, AddGuardleaf },
    { 0.5f, AddSpore },
    { 0.4f, ScatteredBoombugs },
    { 0.2f, AddDeathbloom },
  };

  public static WeightedRandomBag<Encounter> CavesDeadEnds = new WeightedRandomBag<Encounter> {
    /// just to make it interesting, always give dead ends *something*
    { 5f, Empty },
    { 0.5f, AddWater },
    { 0.1f, AddDeathbloom },
    { 0.2f, ScatteredBoombugs },
    { 0.5f, AFewBlobs },
    { 0.5f, JackalPile },
    { 0.5f, AFewSnails },
    { 0.1f, OneSpider },
    { 0.25f, AddSoftGrass },
    { 0.25f, AddBrambles },
    { 0.1f, AddGuardleaf },
    { 0.1f, AddSpore },
  };

  public static WeightedRandomBag<Encounter> CavesRewards = new WeightedRandomBag<Encounter> {
    { 1f, AddMushroom },
    { 1f, AddPumpkin },
    { 1f, OnePlumpAstoria },
    { 0.5f, AddJackalHide },
    { 0.5f, AddGloopShoes },
    { 0.5f, OneButterfly },
    { 0.5f, ThreePlumpAstoriasInCorner },
  };

  public static WeightedRandomBag<Encounter> CavesPlantRewards = new WeightedRandomBag<Encounter> {
    { 1f, MatureBerryBush },
    { 1f, MatureThornleaf },
    { 1f, MatureWildWood },
  };
}