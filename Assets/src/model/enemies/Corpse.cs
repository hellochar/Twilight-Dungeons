using System;
using UnityEngine;

[Serializable]
public class Corpse : ItemOnGround {
  public Corpse(Vector2Int pos, Actor original) : base(pos, new ItemCorpse(original)) {}

  public override void StepDay() {
    // deteriorate into organic matter
    var floor = this.floor;
    KillSelf();
    GameModel.main.EnqueueEvent(() => {
      floor.Put(new OrganicMatterOnGround(pos));
    });
  }
}

[Serializable]
[ObjectInfo("CircleFilled16", description: "The remains of a dead creature.")]
public class ItemCorpse : Item {

  public readonly Actor original;

  public ItemCorpse(Actor original) {
    this.original = original;
  }

  public override int stacksMax => 1;
}
