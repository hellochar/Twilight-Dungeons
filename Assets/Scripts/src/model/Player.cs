using UnityEngine;

public class Player : Actor {
  public Player(Vector2Int pos) : base(pos) {
  }

  public override Vector2Int pos {
    get {
      return base.pos;
    }

    set {
      if (GameModel.main != null) {
        GameModel.main.currentFloor.RemoveVisibility(this);
      }
      base.pos = value;
      if (GameModel.main != null) {
        GameModel.main.currentFloor.AddVisibility(this);
        Tile t = GameModel.main.currentFloor.tiles[value.x, value.y];
        t.OnPlayerEnter();
      }
    }
  }
}
