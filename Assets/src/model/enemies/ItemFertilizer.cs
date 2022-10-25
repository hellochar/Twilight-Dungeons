using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
[ObjectInfo("fertilizer")]
public class ItemFertilizer : Item, ITargetedAction<Plant> {
  public override string displayName => $"{Util.WithSpaces(aiActorType.Name)} Fertilizer";
  public override int stacksMax => 9;
  protected override bool StackingPredicate(Item other) {
    return (other as ItemFertilizer).aiActorType == aiActorType;
  }

  public readonly Type aiActorType;

  public ItemFertilizer(Type type) {
    this.aiActorType = type;
  }

  public void Fertilize(Plant p) {
    if (stacks < 9) {
      throw new CannotPerformActionException("Need 9 stacks!");
    }
    if (p.isMatured) {
      throw new CannotPerformActionException("Cannot fertilize grown plants!");
    }
    p.fertilizer = this;
    Destroy();
  }

  public void Imbue(IWeapon weapon) {
    if (weapon is Item item) {
      item.mods.Add(new ImproveDamageMod());
    }
  }

  internal override string GetStats() => "Get 9 stacks, then Fertilize a planted Seed.\n\nOnce grown, the plant's <#FFFF00>Weapons</color> will be imbued with:\n\t<#FFFF00>+1 damage.</color>";

  string ITargetedAction<Plant>.TargettedActionName => "Fertilize";
  IEnumerable<Plant> ITargetedAction<Plant>.Targets(Player player) {
    return player.floor.bodies.Where((b) => b is Plant p && !p.isMatured).Cast<Plant>();
  }

  void ITargetedAction<Plant>.PerformTargettedAction(Player player, Entity target) {
    player.SetTasks(
      new MoveNextToTargetTask(player, target.pos),
      new GenericPlayerTask(player, () => {
        Fertilize(target as Plant);
      })
    );
  }
}

[Serializable]
public class ImproveDamageMod : IItemMod, IAttackDamageModifier {
  public string displayName => "Blob Fertilizer - +1 damage";

  public int Modify(int input) {
    return input + 1;
  }
}