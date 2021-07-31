using System.Linq;
using UnityEngine;

// public class TeleportStone : Actor {
//   public TeleportStone(Vector2Int pos) : base(pos) {
//     hp = baseMaxHp = 999;
//     faction = Faction.Neutral;
//     this.timeNextAction += 99999;
//   }

//   protected override float Step() {
//     return 99999;
//   }

//   public void TeleportPlayer() {
//     var model = GameModel.main;
//     var player = model.player;
//     if (model.activeFloorIndex == 0) {
//       var targetFloor = model.floors[Mathf.FloorToInt(player.deepestDepthVisited / 8) * 8];
//       if (targetFloor != model.currentFloor) {
//         model.PutPlayerAt(targetFloor, false);
//         var stone = targetFloor.actors.First((a) => a is TeleportStone);
//         player.pos = stone.pos;
//       }
//     } else {
//       var targetFloor = model.floors[0];
//       model.PutPlayerAt(targetFloor, false);
//       var stone = targetFloor.actors.First((a) => a is TeleportStone);
//       player.pos = stone.pos;
//     }
//   }
// }
