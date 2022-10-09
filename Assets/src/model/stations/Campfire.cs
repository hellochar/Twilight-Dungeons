using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("campfire", description: "Heals you for 1 HP and removes one negative status.")]
public class Campfire : Station, IDaySteppable {
  public bool usedForTheDay { get; private set; }
  public override bool isActive => !usedForTheDay;

  public override string description => $"{(usedForTheDay ? "Already used today.\n\n" : "")}{base.description}";

  public override int maxDurability => 6;

  [field:NonSerialized] /// controller-only
  public event Action OnHealed;
  public Campfire(Vector2Int pos) : base(pos) {
    usedForTheDay = false;
  }

  [PlayerAction]
  public void Heal() {
    if (usedForTheDay) {
      throw new CannotPerformActionException("Already used this Campfire today!");
    }
    Player p = GameModel.main.player;
    // p.UseActionPointOrThrow();
    usedForTheDay = true;
    p.Heal(1);
    var debuffs = p.statuses.list.Where((s) => s.isDebuff);
    foreach (var debuff in debuffs) {
      if (debuff is StackingStatus s) {
        s.stacks--;
        break;
      } else {
        p.statuses.Remove(debuff);
        break;
      }
    }
    OnHealed?.Invoke();
    this.ReduceDurability();
  }

  public void StepDay() {
    usedForTheDay = false;
  }
}
