using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class WaterController : GrassController, IEntityClickedHandler {

  public override void Start() {
    var animator = GetComponent<Animator>();
    float time = grass.pos.x / 5f + grass.pos.y / 4.6f;
    animator.Play("Idle", -1, time % 1f);
    // var spriteRenderer = GetComponent<SpriteRenderer>();
    // NeighborTileset.ApplyNeighborAwareTile((Water) grass, spriteRenderer);
  }

  public void PointerClick(PointerEventData pointerEventData) {
    var player = GameModel.main.player;
    var waterPail = (ItemWaterPail) player.inventory.First((item) => item is ItemWaterPail);
    if (waterPail != null) {
      player.SetTasks(
        new MoveNextToTargetTask(player, grass.pos),
        new GenericTask(player, (_) => {
          waterPail.AddStack();
          grass.Kill();
        })
      );
    }
  }
}
