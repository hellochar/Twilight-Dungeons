using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("platelet", "Plant at home. Will add to nearby plants' harvests.")]
public class Platelet : Grass, IActorEnterHandler {
  public Platelet(Vector2Int pos) : base(pos) {
  }

  public void HandleActorEnter(Actor who) {
    if (who is Player p) {
      BecomeItemInInventory(new ItemGrass(GetType()), p);
    }
  }

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    var nearbyPlant = floor.bodies.Where(b => b is Plant && IsNextTo(b)).Cast<Plant>().FirstOrDefault();
    if (nearbyPlant != null) {
      var option0 = nearbyPlant.stage.harvestOptions[0];
      if (option0 != null) {
        option0.AddItem(new ItemGrass(typeof(Platelet), 3), this, true);
      }
    }
  }
}
