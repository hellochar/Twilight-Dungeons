using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class MistsHomeFloor : HomeFloor {
  public static MistsHomeFloor generate(int numFloors) {
    var floor = new MistsHomeFloor(20, 20);
    var floorTypes = Enum.GetValues(typeof(FloorType)).Cast<FloorType>();
    foreach (var pos in floor.EnumerateFloor()) {
      FloorType type;
      // if (MyRandom.value < 0.5f) {
      //   type = FloorType.Empty;
      // } else {
      type = Util.RandomPick(floorTypes);
      // }
      if (type == FloorType.Empty) {
        floor.Put(new HomeGround(pos));
      } else {
        floor.Put(new Mist(pos, type));
      }
    }

    var center = floor.center;
    Room room0 = new Room(floor.center - Vector2Int.one, floor.center + Vector2Int.one);
    floor.rooms = new List<Room>() { room0 };
    floor.root = room0;
    foreach (var p in floor.EnumerateRoom(room0)) {
      floor.Put(new HomeGround(p));
    }
    // floor.Put(new Bedroll(center));
    floor.startPos = center;
    // Encounters.AddWater(floor, room0);
    // show chasm edges
    room0.max += Vector2Int.one;
    room0.min -= Vector2Int.one;


    return floor;
  }

  public MistsHomeFloor(int width, int height) : base(width, height) {}

  public override void Put(Entity entity) {
    base.Put(entity);
    if (entity is HomeGround && root != null) {
      root.ExtendToEncompass(new Room(entity.pos - Vector2Int.one, entity.pos + Vector2Int.one));
      RecomputeVisibility();
      // f.RecomputeVisibility();
    }
  }
}

[Serializable]
[ObjectInfo("house", description: "Sleep to regain your Action Points.")]
public class Bedroll : Piece {
  public Bedroll(Vector2Int pos) : base(pos) { }

  public override string displayName => "Home";

  [PlayerAction]
  public void Sleep() {
    // GameModel.main.player.Heal(0);
    GameModel.main.GoNextDay();
  }
}