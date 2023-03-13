using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class Generators {
  public static HomeFloor generateFloor0(EncounterGroup EncounterGroup) {
    HomeFloor floor = new HomeFloor(15, 11);

    // fill with floor tiles by default
    FloorUtils.CarveGround(floor);

    FloorUtils.SurroundWithWalls(floor);
    FloorUtils.NaturalizeEdges(floor);

    // floor.PlaceUpstairs(new Vector2Int(1, floor.height / 2));
    floor.PlaceDownstairs(new Vector2Int(floor.width - 2, floor.height / 2));

    var soils = new List<Soil>();
    for (int x = 3; x < floor.width - 2; x += 2) {
      int y = floor.height / 2 - 1;
      if (floor.tiles[x, y] is Ground) {
        soils.Add(new Soil(new Vector2Int(x, y)));
      }
      y = floor.height / 2 + 1;
      if (floor.tiles[x, y] is Ground) {
        soils.Add(new Soil(new Vector2Int(x, y)));
      }
    }
    floor.PutAll(soils);

    var room0 = new Room(floor);
    floor.rooms = new List<Room> { room0 };
    floor.root = room0;

    EncounterGroup.Plants.GetRandomAndDiscount(1f)(floor, room0);

    Encounters.AddWater(floor, room0);
    Encounters.ThreeAstoriasInCorner(floor, room0);

    floor.Put(new Altar(new Vector2Int(floor.width/2, floor.height - 2)));

    FloorUtils.TidyUpAroundStairs(floor);
    return floor;
  }
}