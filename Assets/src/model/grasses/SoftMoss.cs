// using System;
// using UnityEngine;

// [Serializable]
// [ObjectInfo("soft-moss", description: "Plant at home to turn into Soil.\nIn the caves, it is destroyed when any creature walks off it.")]
// public class SoftMoss : Grass, IDaySteppable, IActorEnterHandler, IActorLeaveHandler {
//   public SoftMoss(Vector2Int pos) : base(pos) {
//   }

//   public void HandleActorEnter(Actor who) {
//     if (who is Player p) {
//       BecomeItemInInventory(new ItemGrass(GetType(), 1), p);
//     }
//   }

//   public void HandleActorLeave(Actor who) {
//     if (floor.depth != 0) {
//       Kill(who);
//     }
//   }

//   public void StepDay() {
//     if (!(tile is Soil)) {
//       floor.Put(new Soil(pos));
//       KillSelf();
//     }
//   }
// }
