using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo("campfire", description: "Heals you for 4 HP. Single Use.")]
public class Campfire : Station {
  public bool usedForTheDay { get; private set; }
  public override bool isActive => !usedForTheDay;

  public override string description => $"{(usedForTheDay ? "Already used today.\n\n" : "")}{base.description}";

  public override int maxDurability => 1;

  [field:NonSerialized] /// controller-only
  public event Action OnHealed;
  public Campfire(Vector2Int pos) : base(pos) {
    usedForTheDay = false;
  }

  [PlayerAction]
  public void Heal() {
    // if (usedForTheDay) {
    //   throw new CannotPerformActionException("Already used this Campfire today!");
    // }
    Player p = GameModel.main.player;
    // p.UseResourcesOrThrow(actionPoints: 1);
    // usedForTheDay = true;
    p.Heal(4);
    // var debuffs = p.statuses.list.Where((s) => s.isDebuff);
    // foreach (var debuff in debuffs) {
    //   if (debuff is StackingStatus s) {
    //     s.stacks--;
    //     break;
    //   } else {
    //     p.statuses.Remove(debuff);
    //     break;
    //   }
    // }
    OnHealed?.Invoke();
    this.ReduceDurability();
  }

  // [PlayerAction]
  // public void GetFood() {
  //   // if (usedForTheDay) {
  //   //   throw new CannotPerformActionException("Already used this Campfire today!");
  //   // }
  //   Player p = GameModel.main.player;
  //   // p.UseActionPointOrThrow();
  //   // usedForTheDay = true;

  //   p.UseResourcesOrThrow(water: 25);

  //   floor.Put(new ItemOnGround(pos, new ItemCreatureFood()));
  //   OnHealed?.Invoke();
  // }

  // public void StepDay() {
  //   usedForTheDay = false;
  // }
}
