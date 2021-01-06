using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public delegate void Encounter(Floor floor, Room room);

/// Specific Encounters are static, but bags of encounters are not; picking out of a bag will discount it.
public class Encounters {
  private static int RandomRangeBasedOnIndex(int index, params (int, int)[] values) {
    var (min, max) = Util.ClampGet(index, values);
    return Random.Range(min, max + 1);
  }

  // no op
  public static Encounter Empty = new Encounter((Floor, Room) => { });

  public static Encounter AFewBlobs = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var numBlobs = RandomRangeBasedOnIndex(floor.depth,
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
    var num = RandomRangeBasedOnIndex(floor.depth,
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
    var tiles = FloorUtils.TilesFromCenter(floor, room);
    var num = RandomRangeBasedOnIndex(floor.depth,
      (1, 2),
      (2, 2),
      (2, 3),
      (2, 4)
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Jackal(tile.pos));
    }
  });

  public static Encounter AddBats = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = RandomRangeBasedOnIndex(floor.depth / 5,
      (1, 1),
      (1, 2),
      (1, 4),
      (2, 3),
      (2, 4),
      (3, 4)
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Bat(tile.pos));
    }
  });

  public static Encounter AddSpiders = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = RandomRangeBasedOnIndex(floor.depth / 5,
      (1, 1),
      (1, 2),
      (1, 4),
      (2, 3),
      (2, 4),
      (3, 4)
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Spider(tile.pos));
    }
  });

  public static Encounter AddScorpions = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = RandomRangeBasedOnIndex(floor.depth / 10,
      (0, 1),
      (1, 2),
      (2, 3),
      (2, 4)
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Scorpion(tile.pos));
    }
  });

  public static Encounter AddGolems = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = RandomRangeBasedOnIndex(floor.depth / 10,
      (0, 1),
      (1, 2),
      (2, 3),
      (2, 4)
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Golem(tile.pos));
    }
  });

  public static Encounter AddParasite = new Encounter((floor, room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    foreach (var tile in tiles.Take(1)) {
      floor.Put(new Parasite(tile.pos));
    }
  });

  public static Encounter AddHydra = new Encounter((floor, room) => {
    var tile = FloorUtils.TilesFromCenter(floor, room).Where((t) => t.CanBeOccupied()).First();
    if (tile != null) {
      floor.Put(new HydraHeart(tile.pos));
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

  public static Encounter MatureWeirdwood = new Encounter((floor, room) => {
    AddPlantInCenter(floor, room, typeof(Weirdwood));
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
      var numSoftGrass = Random.Range(numTiles / 2, numTiles + 1);
      foreach (var tile in bfs.Take(numSoftGrass)) {
        var grass = new SoftGrass(tile.pos);
        floor.Put(grass);
      }
    }
  });

  public static Encounter AddBladegrass = new Encounter((floor, room) => {
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where(Bladegrass.CanOccupy));
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var start = Util.RandomPick(occupiableTiles);
      var bfs = floor.BreadthFirstSearch(start.pos, (tile) => occupiableTiles.Contains(tile));
      var numSoftGrass = Random.Range(numTiles / 4, numTiles / 2);
      foreach (var tile in bfs.Take(numSoftGrass)) {
        var grass = new Bladegrass(tile.pos);
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

  public static Encounter AddPoisonmoss = new Encounter((floor, room) => {
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where(Poisonmoss.CanOccupy));
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var start = Util.RandomPick(occupiableTiles);
      var bfs = floor.BreadthFirstSearch(start.pos, (tile) => occupiableTiles.Contains(tile));
      var num = Random.Range(2, 6);
      foreach (var tile in bfs.Take(num)) {
        floor.Put(new Poisonmoss(tile.pos));
      }
    }
  });

  public static Encounter AddWebs = new Encounter((floor, room) => {
    var tiles = FloorUtils.TilesSortedByCorners(floor, room).Where((tile) => tile.grass == null && tile is Ground).ToList();
    var num = Random.Range(tiles.Count / 8, tiles.Count / 2);
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Web(tile.pos));
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

  public static Encounter AddEveningBells = new Encounter((floor, room) => {
    var locations = new HashSet<Tile>(FloorUtils.EmptyTilesInRoom(floor, room).Where((tile) => !Mushroom.CanOccupy(tile)));
    var num = 1;
    for (var i = 0; i < num; i++) {
      var tile = Util.RandomPick(locations);
      if (tile != null) {
        var center = tile.pos;
        floor.Put(new Stump(center));
        floor.PutAll(floor
          .GetCardinalNeighbors(center)
          .Where(EveningBells.CanOccupy)
          .Select((t) => {
            var angle = Vector2.SignedAngle(new Vector2(0, -1), t.pos - center);
            return new EveningBells(t.pos, angle);
          })
        );
        locations.ExceptWith(floor.GetAdjacentTiles(tile.pos));
      }
    }
  });

  public static Encounter AddHangingVines = new Encounter((floor, room) => {
    var wallsWithGroundBelow = new HashSet<Vector2Int>(floor
      .EnumerateRoomTiles(room, 1)
      .Where((tile) =>
        tile is Wall &&

        tile.pos.y > 0 &&
        tile.pos.x > 0 &&
        tile.pos.x < floor.width - 1 &&

        floor.tiles[tile.pos + new Vector2Int(0, -1)] is Ground &&

        tile.grass == null &&
        floor.grasses[tile.pos + new Vector2Int(-1, 0)] == null &&
        floor.grasses[tile.pos + new Vector2Int(1, 0)] == null
      )
      .Select((tile) => tile.pos));
    var num = Random.Range(3, 8);
    for (int i = 0; i < num && wallsWithGroundBelow.Any(); i++) {
      var pos = Util.RandomPick(wallsWithGroundBelow);
      floor.Put(new HangingVines(pos));
      // disallow two consecutive vines
      wallsWithGroundBelow.Remove(pos + new Vector2Int(-1, 0));
      wallsWithGroundBelow.Remove(pos);
      wallsWithGroundBelow.Remove(pos + new Vector2Int(1, 0));
    }
  });

  public static Encounter AddAgave = new Encounter((floor, room) => {
    var livableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where(Agave.CanOccupy));
    var start = Util.RandomPick(livableTiles);
    var num = Random.Range(3, 9);
    if (start != null) {
      foreach (var tile in floor.BreadthFirstSearch(start.pos, livableTiles.Contains).Take(num)) {
        floor.Put(new Agave(tile.pos));
      }
    } else {
      Debug.Log("Couldn't find room to place Agave");
    }
  });

  public static Encounter AddMushroom = new Encounter((floor, room) => {
    var livableTiles = floor.EnumerateRoomTiles(room).Where(Mushroom.CanOccupy);
    if (!livableTiles.Any()) {
      Debug.LogError("Couldn't find a location for mushrooms!");
    }
    foreach (var tile in livableTiles) {
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

  public static Encounter OneButterfly = new Encounter((floor, room) => {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new Butterfly(tile.pos));
    }
  });

  public static Encounter AddWater = new Encounter((floor, room) => {
    var numWaters = Random.Range(3, 7);
    var startPos = room.center;
    foreach (var tile in FloorUtils.TilesAwayFromCenter(floor, room).Where((tile) => tile is Ground && tile.grass == null).Take(numWaters)) {
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

  public static Encounter WallPillars = new Encounter((floor, room) => {
    var positions = floor.EnumerateRoom(room).ToList();
    positions.Shuffle();
    var num = Random.Range(3, (positions.Count + 1) / 2);
    foreach (var pos in positions.Take(num)) {
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

  public WeightedRandomBag<Encounter> CavesMobs = new WeightedRandomBag<Encounter> {
    // { 2.5f, Empty },
    { 1, AFewBlobs },
    { 1, JackalPile },
    { 1, AFewSnails },
    { 0.4f, AddBats },
    { 0.35f, AddSpiders },
    { 0.2f, AddScorpions },
    { 0.2f, AddParasite },
    { 0.2f, AddGolems },
    { 0.1f, AddHydra },
  };

  public WeightedRandomBag<Encounter> CavesWalls = new WeightedRandomBag<Encounter> {
    { 3f, Empty },
    { 0.5f, WallPillars },
    { 0.5f, ChunkInMiddle },
    { 0.5f, LineWithOpening },
  };

  public WeightedRandomBag<Encounter> CavesGrasses = new WeightedRandomBag<Encounter> {
    { 1f, AddSoftGrass },
    { 0.75f, AddBladegrass },
    { 0.5f, AddAgave },
    { 0.5f, AddHangingVines },
    { 0.4f, AddEveningBells },
    { 0.4f, AddGuardleaf },
    { 0.4f, AddSpore },
    { 0.4f, AddWebs },
    { 0.4f, ScatteredBoombugs },
    { 0.2f, AddPoisonmoss },
    { 0.2f, AddDeathbloom },
  };

  public WeightedRandomBag<Encounter> CavesDeadEnds = new WeightedRandomBag<Encounter> {
    /// just to make it interesting, always give dead ends *something*
    { 5f, Empty },

    { 0.5f, AFewBlobs },
    { 0.5f, JackalPile },
    { 0.5f, AFewSnails },

    { 0.25f, AddSoftGrass },
    { 0.25f, AddBladegrass },

    { 0.2f, AddWater },
    { 0.2f, ScatteredBoombugs },

    { 0.1f, AddDeathbloom },
    { 0.1f, AddSpiders },
    { 0.1f, AddGuardleaf },
    { 0.1f, AddSpore },

    { 0.05f, AddEveningBells },
    { 0.05f, AddPoisonmoss },

    { 0.02f, AddScorpions },
    { 0.02f, AddParasite },
    { 0.02f, AddGolems },

    { 0.01f, AddHydra },
  };

  public WeightedRandomBag<Encounter> CavesRewards = new WeightedRandomBag<Encounter> {
    // { 2f, AddWater },
    { 1f, AddMushroom },
    { 1f, AddPumpkin },
    { 1f, OnePlumpAstoria },
    { 0.5f, AddJackalHide },
    { 0.5f, AddGloopShoes },
    { 0.5f, OneButterfly },
    { 0.5f, ThreePlumpAstoriasInCorner },
  };

  public WeightedRandomBag<Encounter> CavesPlantRewards = new WeightedRandomBag<Encounter> {
    { 1f, MatureBerryBush },
    { 1f, MatureThornleaf },
    { 1f, MatureWildWood },
    { 1f, MatureWeirdwood }
  };
}