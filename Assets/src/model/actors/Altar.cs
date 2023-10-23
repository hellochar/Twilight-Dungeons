using System;
using UnityEngine;

[Serializable]
public class Altar : Body, IHideInSidebar {
  public Altar(Vector2Int pos) : base(pos) {
    this.hp = this.baseMaxHp = 100;
  }
}
