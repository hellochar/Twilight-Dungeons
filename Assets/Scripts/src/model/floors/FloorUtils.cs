using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class FloorUtils {
  public static List<Tile> TilesSortedByCorners(Floor floor, Room room) {
    var tiles = floor.EnumerateRoomTiles(room).ToList();
    tiles.Sort((x, y) => (int)Mathf.Sign(Vector2.Distance(y.pos, room.centerFloat) - Vector2.Distance(x.pos, room.centerFloat)));
    return tiles;
  }
}