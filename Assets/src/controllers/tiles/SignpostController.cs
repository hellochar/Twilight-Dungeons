using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class SignpostController : TileController {
  Signpost signpost => (Signpost) tile;

  public override void PointerClick(PointerEventData pointerEventData) {
    if (tile.visibility != TileVisiblity.Unexplored) {
      GameModel.main.player.task = new MoveToTargetThenPerformTask(GameModel.main.player, tile.pos, ShowSignpost);
    }
  }

  void ShowSignpost() {
    Popups.Create(
      title: "Tip",
      category: "",
      info: signpost.text,
      flavor: "",
      sprite: null
    );
  }
}
