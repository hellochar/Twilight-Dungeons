using System;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[ObjectInfo("bloodwort", description: "When you walk over the Bloodwort, destroy it and gain 2 strength.")]
public class Bloodwort : Grass, IActorEnterHandler {
  public static bool CanOccupy(Tile tile) => tile is Ground;

  public void HandleActorEnter(Actor who) {
    if (who is Player p) {
      p.statuses.Add(new StrengthStatus(2));
      Kill(p);
    }
  }

  public Bloodwort(Vector2Int pos) : base(pos) {
  }

  // [OnDeserialized]
  // protected override void HandleEnterFloor(StreamingContext context) {
  //   floor.OnEntityRemoved += HandleEntityRemoved;
  // }

  // protected override void HandleLeaveFloor() {
  //   floor.OnEntityRemoved -= HandleEntityRemoved;
  // }

  // private void HandleEntityRemoved(Entity entity) {
  //   if (entity is AIActor && entity.IsDead) {
  //     floor.Put(new Bloodwort(entity.pos));
  //   }
  // }
}

[Serializable]
[ObjectInfo("bloodwort", description: "Gain 4 stacks of Strength whenever you take attack damage.")]
internal class ItemBloodwortTunic : EquippableItem, IBodyTakeAttackDamageHandler, IDurable {
  public override EquipmentSlot slot => EquipmentSlot.Armor;

  public int durability { get; set; }

  public int maxDurability => 3;

  ItemBloodwortTunic() {
    durability = maxDurability;
  }

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    player.statuses.Add(new StrengthStatus(4));
    this.ReduceDurability();
  }
}