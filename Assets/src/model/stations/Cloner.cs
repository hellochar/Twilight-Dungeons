using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("cloner", description: "Duplicates nearby Grass.")]
public class Cloner : Station, IDaySteppable {
  public override int maxDurability => 9;
  public Cloner(Vector2Int pos) : base(pos) { }

  public void StepDay() {
    var nearbyGroundToAddGrass = Util.RandomPick(
      floor.GetAdjacentTiles(pos).Where(t => t.grass == null)
    );
    // there's nothing left
    if (nearbyGroundToAddGrass == null) {
      KillSelf();
      return;
    }
    var nearbyGrass = Util.RandomPick(
      floor.GetAdjacentTiles(pos).Select(t => t.grass).Where(g => g != null)
    );
    if (nearbyGrass != null) {
      var constructor = nearbyGrass.GetType().GetConstructor(new Type[1] { typeof(Vector2Int) });
      var newGrass = (Grass)constructor.Invoke(new object[] { nearbyGroundToAddGrass.pos });
      grass.floor.Put(newGrass);
    }
  }
}