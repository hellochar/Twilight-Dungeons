using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using static YieldContribution;

public delegate void OnNoteworthyAction();

[Serializable]
public class Grass : Entity, IDaySteppable {
  private Vector2Int _pos;
  public override Vector2Int pos {
    get => _pos;
    /// do not allow moving grass
    set { }
  }
  public virtual object BodyModifier { get; protected set; }

  [NonSerialized] /// controller only
  public OnNoteworthyAction OnNoteworthyAction = delegate {};
  [OnDeserialized]
  void HandleDeserialized() {
    OnNoteworthyAction = delegate {};
  }

  public Grass(Vector2Int pos) : base() {
    this._pos = pos;
  }

  public void BecomeItemInInventory(Item innerItem, Player player) {
    var floor = this.floor;
    var item = new ItemVisibleBox(innerItem);
    Kill(actor);
    if (!player.inventory.AddItem(item, this)) {
      floor.Put(new ItemOnGround(pos, item, pos));
    }
  }

  public static YieldContributionRule[] BaseGrassContributionRules => new YieldContributionRule[] {
    // AgeYieldContribution,
    // NearGrassYieldContribution,
    SoilWateredYieldContribution,
    SoilNutrientYieldContribution
  };

  public virtual YieldContributionRule[] contributionRules => BaseGrassContributionRules;

  public List<YieldContribution> latestContributions = new List<YieldContribution>();

  public virtual void StepDay() {
    var item = this.GetHomeItem();
    if (item == null) {
      return;
    }

    // int yield = YieldContributionUtils.Recompute(this, contributionRules, this.latestContributions);

    // int itemCost = YieldContributionUtils.GetCost(item);
    // int stacks = yield / itemCost;
    
    // if (stacks > 0) {
    //   // our yield is high enough, drop an item
    //   item.stacks = stacks;
      floor.Put(new ItemOnGround(pos, item));
    // }
  }
}
