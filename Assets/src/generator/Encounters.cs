using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = MyRandom;

public delegate void Encounter(Floor floor, Room room);

/// Specific Encounters are static, but bags of encounters are not; picking out of a bag will discount it.
public class Encounters {
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

  public static void JackalPile(Floor floor, Room room) {
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
  }

  public static void AFewBlobs(Floor floor, Room room) {
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
  }

  public static void AFewSnails(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = RandomRangeBasedOnIndex(floor.depth,
      (1, 2),
      (1, 3),
      (2, 4),
      (2, 4)
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Snail(tile.pos));
    }
  }

  public static void AddBats(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = RandomRangeBasedOnIndex(floor.depth / 4,
      (1, 1),
      (1, 1),
      (2, 2),
      (2, 3),
      (2, 3),
      (2, 4)
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Bat(tile.pos));
    }
  }

  public static void AddSpiders(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = RandomRangeBasedOnIndex(floor.depth / 4,
      (1, 1), // 0 - 3
      (1, 1), // 4 - 7
      (2, 2), // 8 - 11
      (2, 2), // 12 - 15
      (3, 3), // 16 - 19
      (3, 3)  // 20 - 23
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Spider(tile.pos));
    }
  }

  public static void AddScorpions(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = RandomRangeBasedOnIndex((floor.depth - 12) / 4,
      (1, 1),
      (1, 2),
      (2, 2),
      (2, 3),
      (2, 4),
      (3, 4)
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Scorpion(tile.pos));
    }
  }

  public static void AddGolems(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    var num = RandomRangeBasedOnIndex((floor.depth - 12) / 4,
      (1, 1),
      (1, 1),
      (1, 2),
      (1, 2),
      (2, 2),
      (2, 2),
      (2, 3),
      (2, 3),
      (2, 4),
      (2, 4)
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Golem(tile.pos));
    }
  }

  // public static void AddTeleportStone(Floor floor, Room room0) {
  //   var tiles = FloorUtils.TilesFromCenter(floor, room0).Where((t) => t.CanBeOccupied());
  //   var tile = tiles.First();
  //   // var tile = floor.upstairs;
  //   floor.Put(new TeleportStone(tile.pos));
  // }

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
    var tile = FloorUtils.TilesFromCenter(floor, room).Where((t) => t.CanBeOccupied()).FirstOrDefault();
    if (tile != null) {
      floor.Put(new IronJelly(tile.pos));
    }
  }

  public static void AddGrasper(Floor floor, Room room) {
    // put it on a wall that's next to a Ground
    var tile = Util.RandomPick(floor.EnumerateRoomTiles(room, 1).Where(t => t is Wall && t.body == null && floor.GetCardinalNeighbors(t.pos).Any(t2 => t2 is Ground)));
    if (tile != null) {
      floor.Put(new Grasper(tile.pos));
    }
  }

  public static void AddWildekins(Floor floor, Room room) {
    var tiles = FloorUtils.TilesFromCenter(floor, room);
    var num = RandomRangeBasedOnIndex((floor.depth - 24) / 4,
      (2, 3), // 24-27
      (2, 4), // 28-31
      (3, 4) // 32-35
    );
    foreach (var tile in tiles.Take(num)) {
      floor.Put(new Wildekin(tile.pos));
    }
  }

  public static void AddHoppers(Floor floor, Room room) {
    var startTile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (startTile != null) {
      var num = RandomRangeBasedOnIndex((floor.depth - 24) / 4,
        (2, 2), // 24-27
        (2, 3), // 28-31
        (3, 3)  // 32-35
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

  public static void AddCrabs(Floor floor, Room room) {
    var tiles = FloorUtils.EmptyTilesInRoom(floor, room);
    tiles.Shuffle();
    foreach (var tile in tiles.Take(3)) {
      floor.Put(new Crab(tile.pos));
    }
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
      var numSoftGrass = Random.Range(numTiles / 2, numTiles + 1) * mult;
      foreach (var tile in bfs.Take(numSoftGrass)) {
        var grass = new SoftGrass(tile.pos);
        floor.Put(grass);
      }
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
    var occupiableTiles = floor.EnumerateRoomTiles(room).Where((tile) => Fern.CanOccupy(tile) && tile.grass == null);
    foreach (var tile in occupiableTiles) {
      var grass = new Fern(tile.pos);
      floor.Put(grass);
    }
  }

  public static void AddBladegrass(Floor floor, Room room) => AddBladegrassImpl(floor, room, 1);
  public static void AddBladegrass4x(Floor floor, Room room) => AddBladegrassImpl(floor, room, 4);
  public static void AddBladegrassImpl(Floor floor, Room room, int mult) {
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where(tile => Bladegrass.CanOccupy(tile) && tile.grass == null));
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var start = Util.RandomPick(occupiableTiles);
      var bfs = floor.BreadthFirstSearch(start.pos, (tile) => occupiableTiles.Contains(tile));
      var num = Random.Range(Mathf.CeilToInt(numTiles * 0.25f), Mathf.FloorToInt(numTiles * 0.44f)) * mult;
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
      var num = Random.Range(numTiles / 3, (int)(numTiles * 2f / 3));
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

  public static void AddGuardleaf(Floor floor, Room room) => AddGuardleafImpl(floor, room, 1);
  public static void AddGuardleaf2x(Floor floor, Room room) => AddGuardleafImpl(floor, room, 2);
  public static void AddGuardleaf4x(Floor floor, Room room) => AddGuardleafImpl(floor, room, 4);
  public static void AddGuardleafImpl(Floor floor, Room room, int mult) {
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where((tile) => Guardleaf.CanOccupy(tile) && tile.grass == null));
    var numTiles = occupiableTiles.Count;
    if (numTiles > 0) {
      var start = Util.RandomPick(occupiableTiles);
      var bfs = floor.BreadthFirstSearch(start.pos, (tile) => occupiableTiles.Contains(tile));
      var num = Random.Range(3, 7) * mult;
      foreach (var tile in bfs.Take(num)) {
        floor.Put(new Guardleaf(tile.pos));
      }
    }
  }

  public static void AddTunnelroot(Floor floor, Room room) {
    var start = Util.RandomPick(floor.EnumerateRoomTiles(room).Where((tile) => Tunnelroot.CanOccupy(tile) && tile.grass == null));
    if (start == null) {
      return;
    }
    /// special - put partner far away on this floor
    var partner = Util.RandomPick(
      floor.EnumerateRoomTiles(floor.root)
        .Where((tile) => tile != start && Tunnelroot.CanOccupy(tile) && tile.grass == null)
        .OrderByDescending(start.DistanceTo)
        .Take(40)
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
    var occupiableTiles = new HashSet<Tile>(floor.EnumerateRoomTiles(room).Where(t => Poisonmoss.CanOccupy(t) && t.grass == null));
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
    var num = Random.Range(tiles.Count / 8, tiles.Count / 2);
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
    var tiles = FloorUtils.TilesSortedByCorners(floor, room).Where((tile) => Brambles.CanOccupy(tile) && tile.grass == null).ToList();
    var num = Random.Range(tiles.Count / 6, tiles.Count / 2);
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
    var num = Random.Range(3, 8);
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
    var num = Random.Range(3, 9);
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
    foreach (var tile in livableTiles) {
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

  public static void FiftyRandomAstoria(Floor floor, Room room) {
    var positions = FloorUtils.EmptyTilesInRoom(floor, room);
    positions.Shuffle();
    foreach (var tile in positions.Take(50)) {
      floor.Put(new Astoria(tile.pos));
    }
  }

  public static void OneAstoria(Floor floor, Room room) {
    var positions = FloorUtils.TilesSortedByCorners(floor, room).Where(t => t.CanBeOccupied() && t is Ground && t.grass == null);
    foreach (var tile in positions.Take(1)) {
      floor.Put(new Astoria(tile.pos));
    }
  }

  public static void AddOldDude(Floor floor, Room room){
    Vector2Int stairPos= floor.upstairs.pos;
    var livableTiles = floor.EnumerateCircle(stairPos, 4).Select(position => floor.tiles[position]).Where((t) => t is Ground ? true : false);
    floor.Put(new OldDude(Util.RandomPick(livableTiles).pos));
  }


  public static void ScatteredBoombugs(Floor floor, Room room) => ScatteredBoombugsImpl(floor, room, 1);
  public static void ScatteredBoombugs4x(Floor floor, Room room) => ScatteredBoombugsImpl(floor, room, 4);
  public static void ScatteredBoombugsImpl(Floor floor, Room room, int mult) {
    var emptyTilesInRoom = FloorUtils.EmptyTilesInRoom(floor, room);
    emptyTilesInRoom.Shuffle();
    var num = Random.Range(1, 3);
    foreach (var tile in emptyTilesInRoom.Take(num)) {
      foreach (var nearbyTile in floor.BreadthFirstSearch(tile.pos, emptyTilesInRoom.Contains).Take(mult)) {
        floor.Put(new Boombug(nearbyTile.pos));
      }
    }
  }

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
    foreach (var tile in FloorUtils.TilesAwayFromCenter(floor, room).Where((tile) => tile is Ground && tile.grass == null).Take(numWaters)) {
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

  public static void AddPumpkin(Floor floor, Room room) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new ItemOnGround(tile.pos, new ItemPumpkin()));
    }
  }

  public static void AddThickBranch(Floor floor, Room room) {
    var tile = Util.RandomPick(FloorUtils.EmptyTilesInRoom(floor, room));
    if (tile != null) {
      floor.Put(new ItemOnGround(tile.pos, new ItemThickBranch()));
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
    floor.Put(new Ground(floor.downstairs.pos));

    var center = new Vector2Int(room.max.x - 2, room.center.y);
    // clear radius two
    foreach (var pos in floor.EnumerateRectangle(center - new Vector2Int(2, 2), center + new Vector2Int(3, 3))) {
      floor.Put(new HardGround(pos));
      if (floor.grasses[pos] != null) {
        floor.Remove(floor.grasses[pos]);
      }
    }
    floor.PlaceDownstairs(center);
  }

  public static void FungalColonyAnticipation(Floor floor, Room room) {
    var downstairs = floor.downstairs;
    // remove all enemies
    foreach (var body in floor.EnumerateRoom(room, 1).Select(p => floor.bodies[p]).Where(b => b != null)) {
      floor.Remove(body);
    }


    // surround with fungal walls
    var distance2Walls = floor
      .EnumerateRectangle(downstairs.pos - new Vector2Int(2, 2), downstairs.pos + new Vector2Int(3, 3))
      .Where(p => Util.DiamondMagnitude(p - downstairs.pos) > 1).ToList();
    floor.PutAll(distance2Walls.Select(p => new FungalWall(p)));

    // put a fungal breeder
    var breederTile = Util.RandomPick(floor.GetAdjacentTiles(downstairs.pos).Where(t => t.CanBeOccupied() && t != downstairs));
    if (breederTile != null) {
      floor.Put(new FungalBreeder(breederTile.pos));
    }

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
    switch(MyRandom.Range(0, 3)) {
      case 0:
        // left side
        ChasmBridgeImpl(floor, room, 1, 1);
        break;
      case 1:
        // right side
        ChasmBridgeImpl(floor, room, 1, -1);
        break;
      case 2:
      default:
        // both sides are cut off
        ChasmBridgeImpl(floor, room, 3, 0);
        break;
    }
  }
  private static void ChasmBridgeImpl(Floor floor, Room room, int thickness, int crossScalar) {
    // ignore room. Connect the stairs
    var origin = floor.upstairs?.pos ?? new Vector2Int(1, floor.boundsMax.y - 1);
    var end = floor.downstairs?.pos ?? new Vector2Int(floor.boundsMax.x - 1, 1);
    var offset = new Vector2(end.x - origin.x, end.y - origin.y);
    var direction = offset.normalized;

    foreach (var tile in floor.EnumerateFloor().Select(p => floor.tiles[p]).ToList()) {
      var tileOffset = tile.pos - origin;
      var dot = Vector2.Dot(tileOffset, direction);
      var cross = (tileOffset.x * direction.y - direction.x * tileOffset.y) * crossScalar;
      var dotNorm = dot / offset.magnitude;
      var projPos = origin + direction * dot;
      var dist = Vector2.Distance(projPos, tile.pos);
      if (dist > thickness && cross <= 0) {
        floor.Put(new Chasm(tile.pos));
      }
    }
  }

  /// experimental; unused
  public static void ChasmGrowths(Floor floor, Room room) {
    // var numGrowths = 3;
    // for(var i = 0; i < numGrowths; i++) {
      // var pos = MyRandom.Range(floor.boundsMin, floor.boundsMax);
      // var pos = floor.boundsMax - Vector2Int.one;
      var pos = floor.boundsMin + Vector2Int.one;
      var numTiles = (float) floor.width * floor.height;
      // var num = MyRandom.Range(Mathf.RoundToInt(numTiles / 8), Mathf.RoundToInt(numTiles / 4));
      var num = (int)(numTiles / 3);
      floor.PutAll(floor.BreadthFirstSearch(pos).Take(num).Select(t => new Chasm(t.pos)).ToList());
    // }
  }
}