using UnityEngine;

[ObjectInfo(description: "Tap to collect. Planting a seed costs 1 water.", flavorText: "Water water everywhere...")]
public class Water : Grass {
  public Water(Vector2Int pos) : base(pos) {
  }

  public void Collect(Player player) {
    player.water++;
    Kill();
  }
}