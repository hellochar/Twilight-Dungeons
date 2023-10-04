using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = MyRandom;

public delegate void Encounter(Floor floor, Room room);

/// Specific Encounters are static, but bags of encounters are not; picking out of a bag will discount it.
public partial class Encounters {
  private static Encounter Twice(Encounter input) {
    Encounter result = (floor, room) => {
      input(floor, room);
      input(floor, room);
    };
    return result;
  }

  private static int RandomRangeBasedOnIndex(int index, params (int, int)[] values) {
    var (min, max) = Util.ClampGet(index, values);
    return Random.Range(min, max + 1);
  }

  // no op
  public static void Empty(Floor Floor, Room Room) { }

  public static Encounter AddScuttlers4x = Twice(Twice(AddScuttlers));

  public static Encounter AddScuttlers = new Encounter((Floor floor, Room room) => {
    var tiles = new HashSet<Tile>(FloorUtils.EmptyTilesInRoom(floor, room).Where(t => t.grass == null && t.CanBeOccupied()));
    var startTile = Util.RandomPick(tiles);
    var num = 3;
    foreach (var tile in floor.BreadthFirstSearch(startTile.pos, t => tiles.Contains(t)).Take(num)) {
      floor.Put(new ScuttlerUnderground(tile.pos));
    }
  });

  public static Encounter AddShielders = new Encounter((Floor floor, Room room) => {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    floor.Put(new Shielder(Util.RandomPick(tiles).pos));
  });

  public static void AddCheshireWeeds(Floor floor, Room room) {
    var tiles = FloorUtils.TilesFromCenter(floor, room).Where(CheshireWeedSprout.CanOccupy);
    // tiles.Shuffle();
    var num = floor.depth <= 12 ? 1 : floor.depth <= 24 ? 2 : 3;
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new CheshireWeedSprout(tile.pos));
    }
  }

  // public static void AddTeleportStone(Floor floor, Room room0) {
  //   var tiles = FloorUtils.TilesFromCenter(floor, room0).Where((t) => t.CanBeOccupied());
  //   var tile = tiles.First();
  //   // var tile = floor.upstairs;
  //   floor.Put(new TeleportStone(tile.pos));
  // }

  public static void AddCrabs(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    foreach (var tile in tiles.Take(1)) {
      floor.Put(new Crab(tile.pos));
    }
  }

  public static void AddStalk(Floor floor, Room room) {
    // var x = MyRandom.Range(room.min.x + 1, room.max.x);
    var x = room.center.x;
    var line = floor
      .EnumerateLine(new Vector2Int(x, 0), new Vector2Int(x, floor.height - 1))
      .Where(pos => floor.tiles[pos].CanBeOccupied());
    floor.PutAll(line.Select(pos => new Stalk(pos)));
  }

  public static void AddPumpkin(Floor floor, Room room) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    floor.Put(new Pumpkin(tile.pos));
  }

  public static void MatureBerryBush(Floor floor, Room room) => AddPlantToRoom(floor, room, typeof(BerryBush));
  public static void MatureWildWood(Floor floor, Room room) => AddPlantToRoom(floor, room, typeof(Wildwood));
  public static void MatureThornleaf(Floor floor, Room room) => AddPlantToRoom(floor, room, typeof(Thornleaf));
  public static void MatureWeirdwood(Floor floor, Room room) => AddPlantToRoom(floor, room, typeof(Weirdwood));
  public static void MatureKingshroom(Floor floor, Room room) => AddPlantToRoom(floor, room, typeof(Kingshroom));
  public static void MatureFrizzlefen(Floor floor, Room room) => AddPlantToRoom(floor, room, typeof(Frizzlefen));
  public static void MatureChangErsWillow(Floor floor, Room room) => AddPlantToRoom(floor, room, typeof(ChangErsWillow));
  public static void MatureStoutShrub(Floor floor, Room room) => AddPlantToRoom(floor, room, typeof(StoutShrub));
  public static void MatureBroodpuff(Floor floor, Room room) => AddPlantToRoom(floor, room, typeof(Broodpuff));
  public static void MatureFaeleaf(Floor floor, Room room) => AddPlantToRoom(floor, room, typeof(Faeleaf));

  private static void AddPlantToRoom(Floor floor, Room room, System.Type type) {
    // Add to random soil, or center of room
    Tile tile = Util.RandomPick(floor.EnumerateRoomTiles(room).Where(t => t is Soil && t.CanBeOccupied()));
    if (tile == null) {
      tile = FloorUtils.TilesFromCenter(floor, room).Where(t => t.CanBeOccupied()).FirstOrDefault();
    }
    if (tile != null) {
      var constructor = type.GetConstructor(new Type[] { typeof(Vector2Int) });
      var plant = (Plant)constructor.Invoke(new object[1] { tile.pos });
      plant.GoNextStage();
      plant.GoNextStage();
      floor.Put(plant);
    }
  }

  public static void AddSoftGrass(Floor floor, Room room) => AddSoftGrassImpl(floor, room, 1);
  public static void AddSoftGrass4x(Floor floor, Room room) => AddSoftGrassImpl(floor, room, 1);
  public static void AddSoftGrassImpl(Floor floor, Room room, int mult) {
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where((tile) => tile is Ground && tile.grass == null));
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var start = Util.RandomPick(occupiableTiles);
      var bfs = floor.BreadthFirstSearch(start.pos, (tile) => occupiableTiles.Contains(tile));
      var numSoftGrass = Mathf.RoundToInt(Random.Range(numTiles / 4, numTiles / 2 + 1) * mult);
      foreach (var tile in bfs.Take(numSoftGrass)) {
        var grass = new SoftGrass(tile.pos);
        floor.Put(grass);
      }
    }
  }

  public static void AddLlaora(Floor floor, Room room) {
    var tile = Util.RandomPick(
      FloorUtils.TilesFromCenter(floor, room)
        .Where(tile => Llaora.CanOccupy(tile) && tile.grass == null && tile.pos.x <= 5)
    );
    if (tile != null) {
      floor.Put(new Llaora(tile.pos));
    }
  }

  public static Encounter AddBloodwort = new Encounter((Floor floor, Room room) => {
    var tile = Util.RandomPick(
      FloorUtils.TilesFromCenter(floor, room)
        .Where(tile => Bloodwort.CanOccupy(tile) && tile.grass == null && tile.pos.x < room.center.x)
    );
    if (tile != null) {
      floor.Put(new Bloodwort(tile.pos));
    }
  });

  public static void AddBloodstone(Floor floor, Room room) {
    var tile = Util.RandomPick(
      FloorUtils.EmptyTilesInRoom(floor, room)
        .Where(tile => tile.pos.x < 3)
    );
    if (tile != null) {
      floor.Put(new Bloodstone(tile.pos));
    }
  }

  public static void AddGoldGrass(Floor floor, Room room) {
    var roomTiles = floor.EnumerateRoomTiles(room);

    var perimeter = roomTiles
      .Except(floor.EnumerateRoomTiles(room, -1))
      .Where(tile => tile.CanBeOccupied() && tile is Ground && tile.grass == null);
    
    var start = Util.RandomPick(perimeter);
    if (start == null) {
      return;
    }
    var end = roomTiles
      .Where(tile => tile.CanBeOccupied() && tile is Ground && tile.grass == null)
      .OrderByDescending(t => t.DistanceTo(start))
      .FirstOrDefault();
    if (end == null) {
      return;
    }

    var path = floor.FindPath(start.pos, end.pos, true);
    foreach (var pos in path.Where(p => floor.tiles[p] is Ground)) {
      floor.Put(new GoldGrass(pos));
    }
  }

  public static void FillWithSoftGrass(Floor floor, Room room) {
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where((tile) => tile is Ground && tile.grass == null));
    foreach (var tile in occupiableTiles) {
      var grass = new SoftGrass(tile.pos);
      floor.Put(grass);
    }
  }

  public static void FillWithFerns(Floor floor, Room room) {
    var occupiableTiles = FloorUtils.TilesFromCenter(floor, room).Where((tile) => Fern.CanOccupy(tile) && tile.grass == null);
    occupiableTiles = occupiableTiles.Take(MyRandom.Range(5, 10));
    var ferns = occupiableTiles.Select(tile => new Fern(tile.pos)).ToArray();
    // var hasGoldenFern = Random.Range(0, 100) < ferns.Length;
    // if (hasGoldenFern) {
    //   // replace a fern with a GoldenFern
    //   var indexToReplace = Random.Range(0, ferns.Length);
    //   ferns[indexToReplace] = new GoldenFern(ferns[indexToReplace].pos);
    // }
    floor.PutAll(ferns);
  }

  public static void AddBladegrass(Floor floor, Room room) => AddBladegrassImpl(floor, room, 1);
  public static void AddBladegrass4x(Floor floor, Room room) => AddBladegrassImpl(floor, room, 4);
  public static void AddBladegrassImpl(Floor floor, Room room, int mult) {
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where(tile => Bladegrass.CanOccupy(tile) && tile.grass == null));
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var start = Util.RandomPick(occupiableTiles);
      var bfs = floor.BreadthFirstSearch(start.pos, (tile) => occupiableTiles.Contains(tile));
      var num = MyRandom.Range(numTiles / 10, numTiles / 5) * mult;
      foreach (var tile in bfs.Take(num)) {
        var grass = new Bladegrass(tile.pos);
        floor.Put(grass);
      }
    }
  }

  public static void AddViolets(Floor floor, Room room) {
    var occupiableTiles = FloorUtils.TilesFromCenter(floor, room).Where(tile => Violets.CanOccupy(tile) && tile.grass == null).ToList();
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var num = Random.Range(numTiles / 9, numTiles / 5);
      if (Random.value < 0.2f) {
        /// fill up every square
        num = numTiles;
      }
      foreach (var tile in occupiableTiles.Take(num)) {
        var grass = new Violets(tile.pos);
        floor.Put(grass);
      }
    }
  }

  public static void AddFaegrass(Floor floor, Room room) => AddFaegrassImpl(floor, Random.Range(5, 8));
  public static void AddFaegrassImpl(Floor floor, int num) {
    if (num == 0) {
      return;
    }

    var tiles = floor.tiles.Where(Faegrass.CanOccupy).ToList();
    tiles.Shuffle();
    int numPlaced = 0;
    foreach (var tile in tiles) {
      if (!floor.GetAdjacentTiles(tile.pos).Any((t) => t.grass is Faegrass)) {
        floor.Put(new Faegrass(tile.pos));
        numPlaced++;
        if (numPlaced >= num) {
          break;
        }
      }
    }
  }

  public static void AddGuardleaf(Floor floor, Room room) => AddGuardleafImpl(floor, room, 1);
  public static void AddGuardleaf2x(Floor floor, Room room) => AddGuardleafImpl(floor, room, 2);
  public static void AddGuardleaf4x(Floor floor, Room room) => AddGuardleafImpl(floor, room, 4);
  public static void AddGuardleafImpl(Floor floor, Room room, int mult) {
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where((tile) => Guardleaf.CanOccupy(tile) && tile.grass == null));
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var start = Util.RandomPick(occupiableTiles);
      var bfs = floor.BreadthFirstSearch(start.pos, (tile) => occupiableTiles.Contains(tile));
      var num = Random.Range(1, 4) * mult;
      foreach (var tile in bfs.Take(num)) {
        floor.Put(new Guardleaf(tile.pos));
      }
    }
  }

  public static void AddTunnelroot(Floor floor, Room room) {
    var start = FloorUtils.TilesAwayFromCenter(floor, room)
      .Where((tile) => Tunnelroot.CanOccupy(tile) && tile.grass == null)
      .Skip(MyRandom.Range(0, 4)).FirstOrDefault();
    if (start == null) {
      return;
    }
    /// special - put partner far away on this floor
    var partner = Util.RandomPick(
      floor.EnumerateRoomTiles(floor.root)
        .Where((tile) =>
          tile != start &&
          Tunnelroot.CanOccupy(tile) &&
          tile.grass == null &&
          !floor.GetAdjacentTiles(tile.pos).Any(t2 => t2.grass is Tunnelroot || t2 is Downstairs))
        .OrderByDescending(start.DistanceTo)
        .Take(20)
    );
    if (partner == null) {
      return;
    }

    var root1 = new Tunnelroot(start.pos);
    var root2 = new Tunnelroot(partner.pos);
    floor.Put(root1);
    floor.Put(root2);
    root1.PartnerWith(root2);
  }
  public static void AddTunnelroot4x(Floor floor, Room room) => Twice(Twice(AddTunnelroot))(floor, room);

  public static void AddPoisonmoss(Floor floor, Room room) {
    var occupiableTiles = new HashSet<Tile>(
      floor
        .EnumerateRoomTiles(room)
        // don't spawn under creatures since it will cause room collapse
        .Where(t => Poisonmoss.CanOccupy(t) && t.grass == null && t.CanBeOccupied())
      );
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var start = Util.RandomPick(occupiableTiles);
      var bfs = floor.BreadthFirstSearch(start.pos, (tile) => occupiableTiles.Contains(tile));
      var num = Random.Range(2, 6);
      foreach (var tile in bfs.Take(num)) {
        floor.Put(new Poisonmoss(tile.pos));
      }
    }
  }

  public static void AddCoralmoss(Floor floor, Room room) {
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where(Coralmoss.CanOccupy));
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var start = Util.RandomPick(occupiableTiles);
      var dfs = floor.BreadthFirstSearch(start.pos, (tile) => occupiableTiles.Contains(tile), true);
      var num = Random.Range(3, 12);
      foreach (var tile in dfs.Take(num)) {
        floor.Put(new Coralmoss(tile.pos));
      }
    }
  }

  public static void AddWebs2x(Floor floor, Room room) => Twice(AddWebs)(floor, room);
  public static void AddWebs(Floor floor, Room room) {
    var tiles = FloorUtils.TilesSortedByCorners(floor, room).Where((tile) => tile.grass == null && tile is Ground).ToList();
    tiles.Reverse();
    var num = Random.Range(tiles.Count / 12, tiles.Count / 8);
    foreach (var tile in tiles.Take(num)) {
      // about 50% chance that out of 5 open squares, one will have a space.
      // this optimizes the ability for the player to squeeze into "holes" in
      // the web and run enemies into them
      if (Random.value < 0.87f) {
        floor.Put(new Web(tile.pos));
      }
    }
  }

  public static void AddBrambles(Floor floor, Room room) {
    var tiles = FloorUtils
      .TilesSortedByCorners(floor, room)
      // don't spawn under creatures since it will cause room collapse
      .Where(t => Brambles.CanOccupy(t) && t.grass == null && t.CanBeOccupied())
      .ToList();
    var num = Random.Range(tiles.Count / 12, tiles.Count / 6);
    while (num >= 2) {
      var tile = tiles[tiles.Count - 1];
      floor.Put(new Brambles(tile.pos));
      tiles.RemoveRange(tiles.Count - 2, 2);
      num -= 2;
    }
  }

  public static void AddSpore(Floor floor, Room room) {
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where((tile) => tile is Ground && tile.grass == null));
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var start = Util.RandomPick(occupiableTiles);
      var grass = new Spores(start.pos);
      floor.Put(grass);
    }
  }

  public static void AddNecroroot(Floor floor, Room room) {
    var tiles = floor.EnumerateRoomTiles(room).Where((tile) => tile is Ground && tile.grass == null).OrderBy(t => Vector2.Distance(t.pos, room.centerFloat));
    foreach (var tile in tiles.Take(tiles.Count() / 2)) {
      floor.Put(new Necroroot(tile.pos));
    }
  }

  public static void AddFruitingBodies(Floor floor, Room room) {
    var positions = FloorUtils.EmptyTilesInRoom(floor, room);
    positions.Shuffle();
    var num = Random.Range(3, (positions.Count + 1) / 4);
    foreach (var tile in positions.Take(num)) {
      if (!floor.GetAdjacentTiles(tile.pos).Any((t) => t.actor is FruitingBody)) {
        floor.Put(new FruitingBody(tile.pos));
      }
    }
  }

  public static void AddEveningBells(Floor floor, Room room) {
    int Score(Tile t) {
      return floor
        .GetAdjacentTiles(t.pos)
        .Where(t2 => EveningBells.CanOccupy(t2) && t2.grass == null)
        .Count();
    }
    var tilesOrderedByScore =
      FloorUtils.EmptyTilesInRoom(floor, room)
      .OrderByDescending(Score);
    var highestScore = Score(tilesOrderedByScore.First());

    var tilesWithBestScore = tilesOrderedByScore.TakeWhile(t => Score(t) == highestScore);

    var tile = Util.RandomPick(tilesWithBestScore);
    if (tile != null) {
      var center = tile.pos;
      floor.Put(new Stump(center));
      floor.PutAll(floor
        .GetAdjacentTiles(center)
        .Where(EveningBells.CanOccupy)
        .Select((t) => {
          var angle = Vector2.SignedAngle(new Vector2(0, -1), t.pos - center);
          return new EveningBells(t.pos, angle);
        })
      );
    }
  }

  public static void AddVibrantIvy(Floor floor, Room room) {
    var startTile = Util.RandomPick(floor.EnumerateRoomTiles(room).Where(VibrantIvy.CanOccupy));
    if (startTile == null) {
      Debug.LogWarning("Couldn't find a location for Vibrant Ivy!");
    } else {
      var num = MyRandom.Range(5, 10);
      if (MyRandom.value < 0.2f) {
        num += 10;
      }
      floor.PutAll(
        floor
          .BreadthFirstSearch(startTile.pos, VibrantIvy.CanOccupy, mooreNeighborhood: true)
          .Take(num)
          .Select(t => new VibrantIvy(t.pos))
      );
    }
  }

  public static void AddHangingVines(Floor floor, Room room) {
    var wallsWithGroundBelow = new HashSet<Vector2Int>(floor
      .EnumerateRoomTiles(room, 1)
      .Where((tile) =>
        tile is Wall &&

        tile.pos.y > 0 &&
        tile.pos.x > 0 &&
        tile.pos.x < floor.width - 1 &&

        floor.tiles[tile.pos + Vector2Int.down] is Ground &&

        tile.grass == null &&
        floor.grasses[tile.pos + Vector2Int.left] == null &&
        floor.grasses[tile.pos + Vector2Int.right] == null
      )
      .Select((tile) => tile.pos));
    var num = Random.Range(2, 5);
    for (int i = 0; i < num && wallsWithGroundBelow.Any(); i++) {
      var pos = Util.RandomPick(wallsWithGroundBelow);
      floor.Put(new HangingVines(pos));
      // disallow two consecutive vines
      wallsWithGroundBelow.Remove(pos + Vector2Int.left);
      wallsWithGroundBelow.Remove(pos);
      wallsWithGroundBelow.Remove(pos + Vector2Int.right);
    }
  }

  public static void AddHangingVines2x(Floor floor, Room room) => Twice(AddHangingVines)(floor, room);

  public static void AddAgave(Floor floor, Room room) {
    var livableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where(Agave.CanOccupy));
    var start = Util.RandomPick(livableTiles);
    var num = Random.Range(3, 7);
    if (start != null) {
      foreach (var tile in floor.BreadthFirstSearch(start.pos, livableTiles.Contains).Take(num)) {
        floor.Put(new Agave(tile.pos));
      }
    } else {
      Debug.LogWarning("Couldn't find room to place Agave");
    }
  }

  public static void AddRedcaps(Floor floor, Room room) {
    var start = Util.RandomPick(
      FloorUtils
        .TilesSortedByCorners(floor, room)
        .Where(t => t is Ground)
        .Take(9)
    );
    var num = MyRandom.Range(2, 6);
    if (start == null) {
      Debug.Log("No place to spawn Redcaps");
      return;
    }
    foreach (var tile in floor.BreadthFirstSearch(start.pos, t => t is Ground).Take(num)) {
      floor.Put(new Redcap(tile.pos));
    }
  }


  public static void AddMushroom(Floor floor, Room room) {
    var livableTiles = floor.EnumerateRoomTiles(room).Where(Mushroom.CanOccupy);
    if (!livableTiles.Any()) {
      Debug.LogError("Couldn't find a location for mushrooms!");
    }
    foreach (var tile in Util.RandomRange(livableTiles, MyRandom.Range(5, 12))) {
      floor.Put(new Mushroom(tile.pos));
    }
  }

  public static void PlaceFancyGround(Floor floor, Room room) {
    var groundTiles = floor.EnumerateRoomTiles(room).Where((tile) => tile is Ground);
    foreach (var tile in groundTiles) {
      // this doesn't replace the grass, item, or actor
      floor.Put(new FancyGround(tile.pos));
    }
  }

  public static void SurroundWithRubble(Floor floor, Room room) {
    var perimeter = floor.EnumerateRoomTiles(room, 1).Except(floor.EnumerateRoomTiles(room, 0));
    var entrancesAndExits = perimeter.Where(tile => tile.CanBeOccupied());
    foreach (var tile in entrancesAndExits) {
      floor.Put(new Rubble(tile.pos));
    }
  }

  public static void ThreeAstoriasInCorner(Floor floor, Room room) {
    var positions = FloorUtils.TilesSortedByCorners(floor, room).Where(t => t.CanBeOccupied() && t is Ground && t.grass == null);
    foreach (var tile in positions.Take(3)) {
      floor.Put(new Astoria(tile.pos));
    }
  }

  public static void TwelveRandomAstoria(Floor floor, Room room) {
    var positions = FloorUtils.EmptyTilesInRoom(floor, room);
    positions.Shuffle();
    foreach (var tile in positions.Take(12)) {
      floor.Put(new Astoria(tile.pos));
    }
  }

  public static void OneAstoria(Floor floor, Room room) {
    var positions = FloorUtils.TilesSortedByCorners(floor, room).Where(t => t.CanBeOccupied() && t is Ground && t.grass == null);
    foreach (var tile in positions.Take(1)) {
      floor.Put(new Astoria(tile.pos));
    }
  }


  public static void ScatteredBoombugs(Floor floor, Room room) {
    var emptyTilesInRoom = FloorUtils.EmptyTilesInRoom(floor, room);
    emptyTilesInRoom.Shuffle();
    var num = 1;
    foreach (var tile in emptyTilesInRoom.Take(num)) {
      var boombug = new Boombug(tile.pos);
      floor.Put(boombug);
    }
  }
  public static void ScatteredBoombugs4x(Floor floor, Room room) {
    var startTile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (startTile != null) {
      var num = MyRandom.Range(4, 11);
      foreach (var tile in floor.BreadthFirstSearch(startTile.pos, t => t.CanBeOccupied()).Take(num)) {
        var boombug = new Boombug(tile.pos);
        boombug.statuses.Add(new ConstrictedStatus(null, 999));
        floor.Put(boombug);
      }
    }
  }

  // public static void AddDandypuffs(Floor floor, Room room) {
  //   var start = Util.RandomPick(floor.EnumerateRoomTiles(room).Where(Dandypuff.CanOccupy));
  //   var num = MyRandom.Range(1, 2);
  //   if (start == null) {
  //     Debug.Log("No place to spawn Dandypuffs");
  //     return;
  //   }
  //   foreach (var tile in floor.BreadthFirstSearch(start.pos, Dandypuff.CanOccupy).Take(num)) {
  //     floor.Put(new Dandypuff(tile.pos));
  //   }
  // }

  public static void AddDeathbloom(Floor floor, Room room) {
    // Tile tile = FloorUtils.EmptyTileNearestCenter(floor, room);
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room).Where((t) => t is Ground && t.grass == null).ToList();
    tiles.Sort((x, y) => Vector2Int.Distance(x.pos, room.center) < Vector2Int.Distance(y.pos, room.center) ? -1 : 1);

    var tile = tiles.FirstOrDefault();
    if (tile != null) {
      floor.Put(new Deathbloom(tile.pos));
    }
  }

  public static void OneButterfly(Floor floor, Room room) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new ItemOnGround(tile.pos, new ItemButterfly()));
    }
  }

  public static void AddWater(Floor floor, Room room) {
    var numWaters = Random.Range(3, 6);
    var startPos = room.center;
    foreach (var tile in FloorUtils.TilesAwayFromCenter(floor, room).Where((tile) => tile is Ground && tile.grass == null).Take(numWaters)) {
      floor.Put(new Water(tile.pos));
    }
  }

  public static void AddOneWater(Floor floor, Room room) {
    var numWaters = 1;
    var startPos = room.center;
    foreach (var tile in FloorUtils.TilesAwayFrom(floor, room, floor.downstairsPos).Where((tile) => tile is Ground && tile.grass == null).Take(numWaters)) {
      floor.Put(new Water(tile.pos));
    }
  }

  public static void AddJackalHide(Floor floor, Room room) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new ItemOnGround(tile.pos, new ItemJackalHide()));
    }
  }

  public static void AddGloopShoes(Floor floor, Room room) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new ItemOnGround(tile.pos, new ItemGloopShoes()));
    }
  }

  public static void AddThickBranch(Floor floor, Room room) => RewardItemImpl(floor, room, new ItemThickBranch());
  public static void AddBatTooth(Floor floor, Room room) => RewardItemImpl(floor, room, new ItemBatTooth());
  public static void AddSnailShell(Floor floor, Room room) => RewardItemImpl(floor, room, new ItemSnailShell(1));
  public static void AddSpiderSandals(Floor floor, Room room) => RewardItemImpl(floor, room, new ItemSpiderSandals(15));

  private static void RewardItemImpl(Floor floor, Room room, Item item) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new ItemOnGround(tile.pos, item));
    }
  }

  public static void AddFakeWall(Floor floor, Room room) {
    var tiles = new List<Tile>();
    // only spawn along the top edge
    for (int x = 0; x < floor.width - 1; x++) {
      var tile = floor.tiles[x, floor.height - 1];
      if (tile is Wall && tile.grass == null && floor.tiles[x, floor.height - 2].CanBeOccupied()) {
        tiles.Add(tile);
      }
    }
    var t = Util.RandomPick(tiles);
    if (t != null) {
      floor.Put(new FakeWall(t.pos));
    }
  }

  public static void AddDownstairsInRoomCenter(Floor floor, Room room) {
    // remove current downstairs
    var downstairs = floor.downstairs;
    if (downstairs != null) {
      floor.Put(new Wall(downstairs.pos));
    }

    var posX = floor.depth < 9 ? room.max.x - 1 : room.max.x - 2;
    var center = new Vector2Int(posX, room.center.y);
    // clear radius one
    foreach (var pos in floor.GetAdjacentTiles(center).Select(t => t.pos).ToList()) {
      floor.Put(new HardGround(pos));
      if (floor.grasses[pos] != null) {
        floor.Remove(floor.grasses[pos]);
      }
    }
    floor.downstairsPos = center;
    // floor.PlaceDownstairs(center);
  }

  public static void FungalColonyAnticipation(Floor floor, Room room) {
    var downstairsPos = floor.downstairsPos;
    // // remove all enemies
    // foreach (var body in floor.EnumerateRoom(room, 1).Select(p => floor.bodies[p]).Where(b => b != null)) {
    //   floor.Remove(body);
    // }

    // surround with fungal walls
    var posesWithinDistance2 = floor
      .EnumerateRectangle(downstairsPos - new Vector2Int(2, 2), downstairsPos + new Vector2Int(3, 3))
      .Where(p => p != downstairsPos).ToList();
    foreach (var pos in posesWithinDistance2) {
      if (MyRandom.value < 0.75f) {
        floor.Put(new FungalWall(pos));
      }
    }

    // // put a fungal breeder
    // var breederTile = Util.RandomPick(floor.GetAdjacentTiles(downstairsPos).Where(t => t.CanBeOccupied() && t.pos != downstairsPos));
    // if (breederTile != null) {
    //   floor.Put(new FungalBreeder(breederTile.pos));
    // }
  }

  public static void WallPillars(Floor floor, Room room) {
    var positions = floor.EnumerateRoom(room).Where(p => floor.tiles[p] is Ground).ToList();
    positions.Shuffle();
    var num = Random.Range(3, (positions.Count + 1) / 2);
    var isWall = Random.value < 0.8f;
    foreach (var pos in positions.Take(num)) {
      if (!floor.GetAdjacentTiles(pos).Any((t) => t is Wall)) {
        if (isWall) {
          floor.Put(new Wall(pos));
        } else {
          floor.Put(new Rubble(pos));
        }
      }
    }
  }

  public static void PerlinCutoffs(Floor floor, Room room) {
    var offsetX = Random.value;
    var offsetY = Random.value;
    foreach (var pos in floor.EnumerateRoom(room, 1)) {
      var noise = Mathf.PerlinNoise(pos.x / 2.3f + offsetX, pos.y / 2.4f + offsetY);
      if (noise < 0.5f) {
        floor.Put(new Chasm(pos));
      }
    }
  }
  public static void Concavity(Floor floor, Room room) {
    var pos = floor.center;

    var section = Util.RandomPick(TileSectionConcavity.Sections);
    // TODO support 4x4's being aligned left or right
    var topLeft = pos + new Vector2Int(-section.width / 2, section.height / 2);
    section.Blit(floor, topLeft);
  }

  public static void ChunkInMiddle(Floor floor, Room room) {
    var chunkSize = Random.Range(1, 6);
    var positions = floor.BreadthFirstSearch(room.center).Where(t => t is Ground).Take(chunkSize).Select(t => t.pos);
    var isWall = Random.value < 0.8f;
    foreach (var pos in positions) {
      if (isWall) {
        floor.Put(new Wall(pos));
      } else {
        floor.Put(new Rubble(pos));
      }
    }
  }

  public static void LineWithOpening(Floor floor, Room room) {
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
  }

  public static void InsetLayerWithOpening(Floor floor, Room room) {
    var insetLength = Random.Range(3, 6);
    var inset = floor.EnumeratePerimeter(insetLength).ToList();
    var start = Random.Range(0, inset.Count);
    // rotate inset to the right some random amount
    inset = inset.Skip(start).Concat(inset.Take(start)).ToList();
    // remove a 3 long hole to walk through
    var openingLength = 2;
    while (Random.value < 0.66f && openingLength < inset.Count / 2) {
      openingLength++;
    }
    foreach (var pos in inset.Skip(3)) {
      floor.Put(new Wall(pos));
    }
  }

  public static void ChasmsAwayFromWalls2(Floor floor, Room room) => ChasmsAwayFromWallsImpl(floor, room, 2);
  public static void ChasmsAwayFromWalls1(Floor floor, Room room) => ChasmsAwayFromWallsImpl(floor, room, 1, 2);

  /// only replaces Grounds (not HardGround or stairs)
  private static void ChasmsAwayFromWallsImpl(Floor floor, Room room, int cliffEdgeSize, int extrude = 1) {
    var roomTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room, extrude));
    var walls = roomTiles.Where(t => t is Wall);
    var floorsOnCliffEdge = new List<Tile>();

    Queue<Tile> frontier = new Queue<Tile>(walls);
    HashSet<Tile> seen = new HashSet<Tile>(walls);
    var distanceToWall = new Dictionary<Tile, int>();
    foreach (var w in walls) {
      distanceToWall[w] = 0;
    }

    while (frontier.Any()) {
      Tile tile = frontier.Dequeue();
      var distance = distanceToWall[tile];

      // act on the tile
      if (tile is Ground) {
        floorsOnCliffEdge.Add(tile);
      }

      var adjacent = floor.GetAdjacentTiles(tile.pos).Except(seen).ToList();
      foreach (var next in adjacent) {
        var existingDistance = distanceToWall.ContainsKey(next) ? distanceToWall[next] : 9999;
        var nextDistance = Mathf.Min(existingDistance, distance + 1);
        distanceToWall[next] = nextDistance;
        if (nextDistance <= cliffEdgeSize) {
          frontier.Enqueue(next);
          seen.Add(next);
        }
      }
    }

    var grounds = roomTiles.Where(t => t is Ground);
    var groundsTooFarAway = grounds.Except(floorsOnCliffEdge).Select(t => t.pos).ToList();
    foreach (var pos in groundsTooFarAway) {
      floor.Put(new Chasm(pos));
    }
  }

  public static void ChasmBridge(Floor floor, Room room) {
    switch(MyRandom.Range(0, 4)) {
      case 0:
        // top-right cutoff
        ChasmBridgeImpl(floor, room, 1, 1);
        break;
      case 1:
        // bottom-left cutoff
        ChasmBridgeImpl(floor, room, 1, -1);
        break;
      case 2:
        // both sides are cut off, and it's thin
        ChasmBridgeImpl(floor, room, 2, 0);
        break;
      case 3:
      default:
        // both sides are cut off
        ChasmBridgeImpl(floor, room, 3, 0);
        break;
    }
  }
  private static void ChasmBridgeImpl(Floor floor, Room room, float thickness, int crossScalar) {
    // ignore room. Connect the stairs
    var origin = /*floor.upstairs?.pos ??*/ new Vector2Int(1, floor.boundsMax.y - 2);
    var end = /*floor.downstairs?.pos ??*/ new Vector2Int(floor.boundsMax.x - 1, 1);
    var offset = new Vector2(end.x - origin.x, end.y - origin.y);
    var direction = offset.normalized;

    foreach (var pos in floor.EnumerateFloor()) {
      var tileOffset = pos - origin;
      var dot = Vector2.Dot(tileOffset, direction);
      var cross = (tileOffset.x * direction.y - direction.x * tileOffset.y) * crossScalar;
      var dotNorm = dot / offset.magnitude;
      var projPos = origin + direction * dot;
      var dist = Vector2.Distance(projPos, pos);
      if (dist > thickness && cross <= 0) {
        floor.Put(new Chasm(pos));
      }
    }
    // adjust start and end positions
    floor.startPos = origin;
    floor.downstairsPos = end;
  }

  public static void RubbleCluster(Floor floor, Room room) => ObjectClusterImpl(floor, t => new Rubble(t.pos));
  public static void StalkCluster(Floor floor, Room room) => ObjectClusterImpl(floor, t => new Stalk(t.pos));
  public static void StumpCluster(Floor floor, Room room) => ObjectClusterImpl(floor, t => new Stump(t.pos));

  private static void ObjectClusterImpl(Floor floor, Func<Tile, Entity> factory, int? num = null) {
    if (num == null) {
      num = MyRandom.Range(3, 6);
    }
    floor.PutAll(
      FloorUtils.Clusters(floor, new Vector2Int(2, 2), num.Value).Select(t => factory(t))
    );
  }

  /// experimental; unused
  public static void ChasmGrowths(Floor floor, Room room) {
    floor.PutAll(
      FloorUtils.Clusters(floor, new Vector2Int(0, 0), 7).Select(t => new Chasm(t.pos))
    );
  }
}