using UnityEngine;

public abstract class Tile : Entity {
  public Vector2Int pos { get; }
  public TileVisiblity visiblity = TileVisiblity.Unexplored;
  public Tile(Vector2Int pos) => this.pos = pos;

  /// 0.0 means unwalkable.
  /// weight 1 is "normal" weight.
  public virtual float GetPathfindingWeight() {
    return 1;
  }

  public virtual bool ObstructsVision() {
    return GetPathfindingWeight() == 0;
  }

  public virtual void OnPlayerEnter() {}
}

public enum TileVisiblity {
  Unexplored, Visible, Explored
}

public class Ground : Tile {
  public Ground(Vector2Int pos) : base(pos) { }
}

public class Wall : Tile {
  public Wall(Vector2Int pos) : base(pos) { }
  public override float GetPathfindingWeight() {
    return 0;
  }
}

public class Upstairs : Tile {
  public Upstairs(Vector2Int pos) : base(pos) {
  }

  public override void OnPlayerEnter() {
    Floor prevFloor = GameModel.main.floors[GameModel.main.activeFloorIndex - 1];
    if (prevFloor != null) {
      GameModel.main.PutPlayerAt(prevFloor, true);
    }
  }
}

public class Downstairs : Tile {
  public Downstairs(Vector2Int pos) : base(pos) { }

  public override void OnPlayerEnter() {
    Floor nextFloor = GameModel.main.floors[GameModel.main.activeFloorIndex + 1];
    if (nextFloor != null) {
      GameModel.main.PutPlayerAt(nextFloor, false);
    }
  }
}

public class Dirt : Tile {
  public Dirt(Vector2Int pos) : base(pos) { }
}