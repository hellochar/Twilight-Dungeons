using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class WaterController : TileController, IOnTopActionHandler {
  Water water => (Water) tile;

  public override void Start() {
    base.Start();
    var animator = GetComponent<Animator>();
    float time = water.pos.x / 5f + water.pos.y / 4.6f;
    animator.Play("Idle", -1, time % 1f);
  }

  public void HandleOnTopAction() {
    Player player = GameModel.main.player;
    player.task = new GenericPlayerTask(player, () => water.Collect(player));
  }

  public string OnTopActionName => "Collect";
}

public interface IOnTopActionHandler {
  string OnTopActionName { get; }
  void HandleOnTopAction();
}