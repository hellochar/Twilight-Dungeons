using UnityEngine;

public abstract class Tile {
  public readonly Vector2Int pos;
  public TileVisiblity visiblity = TileVisiblity.Unexplored;
  public Tile(Vector2Int pos) {
    this.pos = pos;
  }

  /// 0.0 means unwalkable.
  /// weight 1 is "normal" weight.
  public virtual float GetPathfindingWeight() {
    return 1;
  }

  public virtual bool ObstructsVision() {
    return GetPathfindingWeight() == 0;
  }
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
}

public class Downstairs : Tile {
  public Downstairs(Vector2Int pos) : base(pos) { }
}

public class Dirt : Tile {
  public Dirt(Vector2Int pos) : base(pos) { }
}