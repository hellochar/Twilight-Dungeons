using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[ObjectInfo("vibrant-ivy")]
public class VibrantIvy : Grass, IActorEnterHandler, IAttackHandler, IActorLeaveHandler {
  public override string description =>
    $"Camouflages the player.\nCreatures that would chase you will move randomly instead, and you will not wake sleeping creatures.\nMoving or attacking from within the Vibrant Ivy destroys it.";

  public static bool CanOccupy(Tile tile) {
    var floor = tile.floor;
    // hugging at least one 4-neighbor wall
    var cardinalNeighbors = floor.GetCardinalNeighbors(tile.pos);
    var isHuggingWall = cardinalNeighbors.Any((pos) => pos is Wall);
    var isGround = tile is Ground || tile is Water;
    var isNotIvy = !(tile.grass is VibrantIvy);
    
    return isHuggingWall && isGround && isNotIvy;
  }

  [OptionalField] // added 1.11.0
  private int stacks;
  public int Stacks => stacks;

  public VibrantIvy(Vector2Int pos) : base(pos) {
    BodyModifier = this;
  }

  protected override void HandleEnterFloor() {
    ComputeStacks();
    if (actor is Player player) {
      player.statuses.Add(new CamouflagedStatus());
    }
  }

  protected override void HandleLeaveFloor() {
    if (actor is Player player) {
      player.statuses.RemoveOfType<CamouflagedStatus>();
    }
  }

  public void HandleActorEnter(Actor who) {
    if (who is Player player) {
      player.statuses.Add(new CamouflagedStatus());
      OnNoteworthyAction();
    }
  }

  public void OnAttack(int damage, Body target) {
    if (actor is Player player) {
      LoseStack(player);
    }
  }

  public void HandleActorLeave(Actor who) {
    if (who is Player player) {
      LoseStack(player);
    }
  }

  private void LoseStack(Player player) {
    stacks--;
    if (stacks == 0) {
      Kill(player);
    }
  }

  public void ComputeStacks() {
    stacks = floor.GetCardinalNeighbors(pos).Where(t => t is Wall).Count();
  }
}

[Serializable]
[ObjectInfo("vibrant-ivy")]
internal class CamouflagedStatus : Status, IPlayerCamouflage {
  public CamouflagedStatus() {
  }

  public override void Start() {
    actor.floor.RecomputeVisibility();
  }

  public override void End() {
    actor.floor.RecomputeVisibility();
  }

  public override bool Consume(Status other) => true;

  public override string Info() =>
    "Creatures that would chase you will move randomly instead, and you will not wake sleeping creatures.\nMoving or attacking from within the Vibrant Ivy destroys it.";

  public override void Step() {
    if (!(actor?.grass is VibrantIvy)) {
      Remove();
    }
  }
}

public interface IPlayerCamouflage {}
