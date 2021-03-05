using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class WaterController : TileController {
  Water water => (Water) tile;

  public override void Start() {
    base.Start();
    var animator = GetComponent<Animator>();
    float time = water.pos.x / 5f + water.pos.y / 4.6f;
    animator.Play("Idle", -1, time % 1f);
  }

  public override void HandleInteracted(PointerEventData pointerEventData) {
    var player = GameModel.main.player;
    player.SetTasks(
      new MoveNextToTargetTask(player, water.pos),
      new GenericPlayerTask(player, () => water.Collect(player))
    );
  }
}
