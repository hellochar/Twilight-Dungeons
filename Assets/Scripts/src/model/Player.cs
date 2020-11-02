using UnityEngine;
using UnityEngine.Events;

public class Player : Actor {
  public int hp = 9;
  public int hpMax = 12;
  /// called when the player's action is set to something not null
  public UnityEvent OnSetPlayerAction = new UnityEvent();
  internal override float queueOrderOffset => 0f;

  public Player(Vector2Int pos) : base(pos) {
  }


  public override ActorAction action {
    get => base.action;
    set {
      base.action = value;
      if (value != null) {
        OnSetPlayerAction.Invoke();
      }
      // GameModel.main.OnSetPlayerAction();
    }
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
