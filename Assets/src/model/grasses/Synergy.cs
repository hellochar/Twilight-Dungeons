using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Synergy {
  public static Dictionary<Type, Synergy> SynergyMapping = new Dictionary<Type, Synergy>() {
    [typeof(Guardleaf)] = new Synergy(
      Vector2Int.right,
      Vector2Int.left
    ),

    [typeof(Spores)] = new Synergy(
      Vector2Int.left,
      Vector2Int.left * 2
    ),

    [typeof(EveningBells)] = new Synergy(
      Vector2Int.up
    ),

    [typeof(SoftGrass)] = new Synergy(
      Vector2Int.down
    ),

    [typeof(Bloodwort)] = new Synergy(
      Vector2Int.right
    ),

    [typeof(Bladegrass)] = new Synergy(
      Vector2Int.left
    ),

    [typeof(Nubs)] = new Synergy(
      Vector2Int.left,
      Vector2Int.up,
      Vector2Int.right
    ),

    [typeof(Llaora)] = new Synergy(
      Vector2Int.up,
      Vector2Int.right
    ),

    [typeof(Violets)] = new Synergy(
      Vector2Int.down,
      Vector2Int.right
    ),

    [typeof(Fern)] = new Synergy(
      Vector2Int.right,
      Vector2Int.down,
      Vector2Int.left
    ),

    [typeof(Chiller)] = new Synergy(
      Vector2Int.left,
      Vector2Int.right
    ),
  };
  public Vector2Int[] offsets;

  public Synergy(params Vector2Int[] offsets) {
    this.offsets = offsets;
  }

  public static readonly Synergy Never = new Synergy(new Vector2Int[0]);

  public bool IsSatisfied(Entity entity) {
    if (offsets == null || offsets.Length == 0) {
      return false;
    }

    var floor = entity.floor as HomeFloor;
    foreach (var offset in offsets) {
      var targetPos = entity.pos + offset;
      if (!floor.InBounds(targetPos)) {
        return false;
      }
      var targetEntity = floor.grasses[targetPos] as Entity ?? floor.bodies[targetPos] as AIActor ?? floor.pieces[targetPos] as Entity;
      if (targetEntity == null || targetEntity.GetType() == entity.GetType()) {
        return false;
      }
    }
    return true;
  }
}
