using System;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Spreads along the edges of Walls. Any creature that walks into it will ")]
public class Irridine : Grass {
  public readonly float angle;
  public static bool CanOccupy(Tile tile) => Mushroom.CanOccupy(tile);

  public Irridine(Vector2Int pos, float angle = 0) : base(pos) {
    this.angle = angle;
  }
}
