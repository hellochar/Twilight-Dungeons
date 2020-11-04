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
    }
  }

  public override Vector2Int pos {
    get {
      return base.pos;
    }

    set {
      GameModel model = GameModel.main;
      if (model != null) {
        model.currentFloor.RemoveVisibility(this);
      }
      base.pos = value;
      if (model != null) {
        model.currentFloor.AddVisibility(this);
        Tile t = model.currentFloor.tiles[value.x, value.y];
        model.EnqueueEvent(() => t.OnPlayerEnter());
      }
    }
  }
}
