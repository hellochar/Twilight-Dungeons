using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BramblesController : GrassController/*, IEntityClickedHandler*/ {

  public Brambles brambles => (Brambles) grass;

  public override void Start() {
    base.Start();
    brambles.OnSharpened += HandleSharpened;
  }

  private void HandleSharpened() {
    GetComponent<Animator>().SetTrigger("Sharpened");
  }

  // public void PointerClick(PointerEventData eventData) {
  //   Player player = GameModel.main.player;
  //   player.SetTasks(
  //     new MoveNextToTargetTask(player, brambles.pos),
  //     new AttackGroundTask(player, brambles.pos)
  //   );
  // }
}