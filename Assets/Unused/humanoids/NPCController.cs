using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NPCController : ActorController {
  public NPC npc => (NPC) actor;

  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    if (actor.faction == Faction.Ally) {
      Player player = GameModel.main.player;
      if (player.IsNextTo(actor)) {
        return new ArbitraryPlayerInteraction(() => {
          EntityPopup.Show(npc);
        });
      } else {
        return new SetTasksPlayerInteraction(
          new MoveNextToTargetTask(player, actor.pos),
          new GenericPlayerTask(player, () => EntityPopup.Show(npc)).Free()
        );
      }
    } else {
      return base.GetPlayerInteraction(pointerEventData);
    }
  }
}