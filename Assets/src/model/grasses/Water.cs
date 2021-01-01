using UnityEngine;

public class Water : Grass {
  public Water(Vector2Int pos) : base(pos) {
  }

  public void Collect(Player player) {
    player.water++;
    Kill();
  }
}