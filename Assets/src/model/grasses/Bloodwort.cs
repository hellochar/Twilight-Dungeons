using System;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[ObjectInfo("bloodwort", description: "Eat to gain 1 strength. If any Creature dies, a Bloodwort will spawn on its corpse.")]
public class Bloodwort : Grass {
  public static Item HomeItem => new ItemBloodwortTunic();
  public Bloodwort(Vector2Int pos) : base(pos) {
    BodyModifier = this;
  }

  [OnDeserialized]
  protected override void HandleEnterFloor() {
    floor.OnEntityRemoved += HandleEntityRemoved;
  }

  protected override void HandleLeaveFloor() {
    floor.OnEntityRemoved -= HandleEntityRemoved;
  }

  private void HandleEntityRemoved(Entity entity) {
    if (entity is AIActor) {
      floor.Put(new Bloodwort(entity.pos));
    }
  }

  public void Eat() {
    GameModel.main.player.SetTasks(
      new GenericPlayerTask(GameModel.main.player, EatImpl)
    );
  }

  public void EatImpl() {
    GameModel.main.player.statuses.Add(new StrengthStatus(1));
    Kill(GameModel.main.player);
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
  public override int stacksMax => int.MaxValue;

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    player.statuses.Add(new StrengthStatus(4));
    stacks--;
  }
}