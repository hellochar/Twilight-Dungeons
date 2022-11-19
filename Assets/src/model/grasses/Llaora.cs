using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("llaora", flavorText: "", description: "You may Disperse the Llaora, confusing Enemies in radius 2 for 10 turns.")]
public class Llaora : Grass {
  public static Item HomeItem => new ItemFloralHeaddress();
  public static bool CanOccupy(Tile tile) => tile is Ground;

  public Llaora(Vector2Int pos) : base(pos) { }

  public static float radius => 2.5f;

  public void Disperse(Player who) {
    OnNoteworthyAction();
    DisperseEffect(this);
    Kill(who);
  }

  public static void DisperseEffect(Entity e) {
    foreach (var enemy in e.floor.BodiesInCircle(e.pos, radius).OfType<Actor>().Where(a => a.faction == Faction.Enemy)) {
      enemy.statuses.Add(new ConfusedStatus(10));
    }
    LlaoraController.PlayPoofVfx(e);
  }
}

[Serializable]
[ObjectInfo("floral-headdress")]
internal class ItemFloralHeaddress : EquippableItem, IUsable {
  public override EquipmentSlot slot => EquipmentSlot.Headwear;
  public override int stacksMax => int.MaxValue;

  public void Use(Actor a) {
    Llaora.DisperseEffect(a);
    stacks--;
  }
}