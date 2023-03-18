using UnityEngine;
using UnityEngine.EventSystems;

public class DownstairsController : TileController, IOnTopActionHandler {
  public Downstairs downstairs => (Downstairs) tile;

  public string OnTopActionName => ((IOnTopActionHandler)downstairs).OnTopActionName;

  public void HandleOnTopAction() {
    ((IOnTopActionHandler)downstairs).HandleOnTopAction();
  }

  // public override void Start() {
  //   base.Start();
  //   if (downstairs.floor.depth == 11) {
  //     PrefabCache.Effects.Instantiate("Stairs Before Blobmother", transform);
  //   } else if (downstairs.floor.depth == 22) {
  //     PrefabCache.Effects.Instantiate("Stairs Before Fungal Colony", transform);
  //   }
  // }

  // public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
  //   Player player = GameModel.main.player;
  //   return new SetTasksPlayerInteraction(
  //     new MoveNextToTargetTask(player, tile.pos),
  //     new GenericPlayerTask(player, () => EntityPopup.Show(tile))
  //   );
  // }
}
