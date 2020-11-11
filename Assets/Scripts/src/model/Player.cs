using System;
using UnityEngine;
using UnityEngine.Events;


public class Player : Actor {
  public int hp = 9;
  public int hpMax = 12;
  /// called when the player's action is set to something not null
  public event Action<ActorAction> OnSetPlayerAction;
  internal override float queueOrderOffset => 0f;

  public Player(Vector2Int pos) : base(pos) {
  }


  public override ActorAction action {
    get => base.action;
    set {
      base.action = value;
      OnSetPlayerAction?.Invoke(value);
    }
  }

  public override Vector2Int pos {
    get {
      return base.pos;
    }

    set {
      GameModel model = GameModel.main;
      if (floor != null) {
        floor.RemoveVisibility(this);
      }
      base.pos = value;
      if (floor != null) {
        floor.AddVisibility(this);
        Tile t = floor.tiles[value.x, value.y];
        model.EnqueueEvent(() => t.OnPlayerEnter());
      }
    }
  }

  // internal async Task WaitUntilActionIsDecided() {
  //   while(action == null) {
  //     await Task.Delay(16);
  //   }
  //   return;
  // }
}
