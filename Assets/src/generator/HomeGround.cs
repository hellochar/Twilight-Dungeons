using UnityEngine;

[System.Serializable]
public class HomeGround : Ground {
  public HomeGround(Vector2Int pos) : base(pos) {
  }

  public override float GetPathfindingWeight() {
    var originalPathfindingWeight = (body != null && body != GameModel.main.player) ? 0 : BasePathfindingWeight();
    // at home, consider Pieces to also block pathfinding
    if (floor is HomeFloor f && f.pieces[pos] != null) {
      return 0;
    }
    return originalPathfindingWeight;
  }
}

[System.Serializable]
internal class BigTree : Body {
  public BigTree(Vector2Int pos) : base(pos) {
  }

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    PutIfInBoundsAndEmpty(new ProxyBody(pos + new Vector2Int(0, 1)));
    // PutIfInBoundsAndEmpty(new ProxyBody(pos + new Vector2Int(-1, 1)));
    // PutIfInBoundsAndEmpty(new ProxyBody(pos + new Vector2Int(+1, 1)));

    PutIfInBoundsAndEmpty(new ProxyBody(pos + new Vector2Int(0, 2)));
    PutIfInBoundsAndEmpty(new ProxyBody(pos + new Vector2Int(-1, 2)));
    PutIfInBoundsAndEmpty(new ProxyBody(pos + new Vector2Int(+1, 2)));

    // PutIfInBoundsAndEmpty(new ProxyBody(pos + new Vector2Int(0, 3)));
  }

  void PutIfInBoundsAndEmpty(Entity e) {
    if (floor.InBounds(e.pos) && floor.tiles[e.pos].CanBeOccupied()) {
      floor.Put(e);
    }
  }
}

[System.Serializable]
internal class ProxyBody : Body {
  public readonly Body target;

  public ProxyBody(Vector2Int pos, Body target = null) : base(pos) {
  }
}