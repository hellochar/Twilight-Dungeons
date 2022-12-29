using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PieceController : MonoBehaviour, IEntityController, IPlayerInteractHandler {
  [NonSerialized]
  public Piece piece;
  protected GameObject sprite;

  public virtual void Start() {
    sprite = transform.Find("Sprite")?.gameObject;
  }

  public virtual PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    if (piece.IsDead) {
      return null; // don't do anything to dead actors
    }
    if (piece.tile.visibility == TileVisiblity.Unexplored) {
      return null;
    }

    Tile t = pointerEventData == null ? piece.tile : Util.GetVisibleTileAt(pointerEventData.position);
    return new SetTasksPlayerInteraction(
      new MoveNextToTargetTask(player, t.pos),
      new GenericPlayerTask(player, () => EntityPopup.Show(piece))
    );
  }
}
