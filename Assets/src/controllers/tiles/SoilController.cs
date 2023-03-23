using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class SoilController : MonoBehaviour, IEntityController, IPlayerInteractHandler {
  [NonSerialized]
  public Soil soil;

  public virtual PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    if (soil.isVisible) {
      return new SetTasksPlayerInteraction(
        new MoveNextToTargetTask(player, soil.pos),
        new ShowInteractPopupTask(player, soil)
      );
    }
    return null;
  }

}
