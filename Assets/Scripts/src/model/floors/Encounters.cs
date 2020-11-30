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
  static Encounter Empty = new Encounter((Floor, Room) => {});

  static Encounter AFewBlobs = new Encounter((floor, room) => {
    var emptyTilesInRoom = floor.EnumerateRoomTiles(room).Where(t => t.CanBeOccupied()).ToList();
    emptyTilesInRoom.Sort((x, y) => Random.value < 0.5 ? -1 : 1);
    var numBlobs = Random.Range(2, 4);
    foreach (var tile in emptyTilesInRoom.Take(numBlobs)) {
      floor.Add(new Blob(tile.pos));
    }
  });

  static Encounter JackalPile = new Encounter((floor, room) => {
    var emptyTilesInRoom = floor.EnumerateRoomTiles(room).Where(t => t.CanBeOccupied()).ToList();
    emptyTilesInRoom.Sort((x, y) => Random.value < 0.5 ? -1 : 1);
    emptyTilesInRoom.Sort((x, y) => Vector2Int.Distance(x.pos, room.center) < Vector2Int.Distance(y.pos, room.center) ? -1 : 1);
    var numJackals = Random.Range(3, 7);
    foreach (var tile in emptyTilesInRoom.Take(numJackals)) {
      floor.Add(new Jackal(tile.pos));
    }
  });

  static Encounter BatInCorner = new Encounter((floor, room) => {
    var emptyTilesInRoom = floor.EnumerateRoomTiles(room).Where(t => t.CanBeOccupied()).ToList();
    // sort by farthest distance to center
    emptyTilesInRoom.Sort((x, y) => (int) Mathf.Sign(Vector2.Distance(y.pos, room.centerFloat) - Vector2.Distance(x.pos, room.centerFloat)));
    foreach (var tile in emptyTilesInRoom.Take(1)) {
      floor.Add(new Bat(tile.pos));
    }
  });

  static Encounter MatureBush = new Encounter((floor, room) => {
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

  static Encounter CoverWithGrass = new Encounter((floor, room) => {
    foreach (var tile in floor.EnumerateRoomTiles(room).Where((tile) => tile is Ground)) {
      var grass = new Grass(tile.pos);
      floor.Add(grass);
    }
  });

  public static WeightedRandomBag<Encounter> CavesStandard = new WeightedRandomBag<Encounter> {
    { 1.75f, Empty },
    { 1, AFewBlobs },
    { 1, JackalPile },
    { 0.8f, BatInCorner },
    { 0.4f, CoverWithGrass },
    { 0.1f, MatureBush },
  };
}