using System;
using UnityEngine;

[Serializable]
[ObjectInfo("bloodwort", description: "If the Player is attacked while standing over the Bloodwort, they get 4 stacks of strength and the Bloodwort dies.")]
public class Bloodwort : Grass, IBodyTakeAttackDamageHandler {
  public static Item HomeItem => new ItemBloodwortTunic();
  public Bloodwort(Vector2Int pos) : base(pos) {
    BodyModifier = this;
  }

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    if (body is Player p) {
      p.statuses.Add(new StrengthStatus(4));
      KillSelf();
    }
  }
}

[Serializable]
[ObjectInfo("bloodwort", description: "Gain 4 stacks of Strength whenever you take attack damage.")]
internal class ItemBloodwortTunic : EquippableItem, IBodyTakeAttackDamageHandler {
  public override EquipmentSlot slot => EquipmentSlot.Armor;

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    player.statuses.Add(new StrengthStatus(4));
    stacks--;
  }
}