using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Pushes you towards the next NerveRoot in the line.")]
public class NerveRoot : Grass, ISteppable {
  public NerveRoot next;

  public float timeNextAction { get; set; }

  public float turnPriority => 9;

  public static bool CanOccupy(Tile tile) => tile is Ground;
  public NerveRoot(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 1;
  }

  public float Step() {
    if (actor != null && next != null && next.pos != null) {
      GameModel.main.EnqueueEvent(() => {
        actor.pos = next.pos;
      });
      OnNoteworthyAction();
    }
    return 1;
  }

  public static void AddNerveRoot(Floor floor, Room room) {
    // create a circuit around the floor

    var roomTiles = floor.EnumerateRoomTiles(room);
    // left
    var horizontal = floor
      .EnumerateLine(new Vector2Int(room.min.x, room.center.y), new Vector2Int(room.max.x, room.center.y))
      .Where(pos => floor.tiles[pos].CanBeOccupied());
    var left = horizontal.First();
    var right = horizontal.Last();

    var vertical = floor
      .EnumerateLine(new Vector2Int(room.center.x, room.min.y), new Vector2Int(room.center.x, room.max.y))
      .Where(pos => floor.tiles[pos].CanBeOccupied());
    var bottom = vertical.First();
    var top = vertical.Last();

    var pathWithDuplicates = floor.FindPath(left, top);
    pathWithDuplicates.AddRange(floor.FindPath(top, right));
    pathWithDuplicates.AddRange(floor.FindPath(right, bottom));
    pathWithDuplicates.AddRange(floor.FindPath(bottom, left));
    var path = pathWithDuplicates.Distinct();

    var nerveRoots = path.Select(pos => new NerveRoot(pos)).ToList();
    for (int i = 0; i < nerveRoots.Count - 1; i++) {
      nerveRoots[i].next = nerveRoots[i + 1];
    }
    nerveRoots.Last().next = nerveRoots[0];
    floor.PutAll(nerveRoots);
  }

}
