using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Walk over to sharpen.\nOnce sharpened, the first creature walking into this Bladegrass takes 2 damage and removes the Bladegrass.\nSharpened Bladegrass dies on its own after 10 turns.")]
public class Bladegrass : Grass, IActorEnterHandler, IActorLeaveHandler {
  public static Item HomeItem => new ItemBladegrassBlade();
  public static bool CanOccupy(Tile tile) => tile is Ground;
  public bool isSharp = false;
  [field:NonSerialized] /// controller only
  public event Action OnSharpened;
  public Bladegrass(Vector2Int pos) : base(pos) {}

  public void Sharpen() {
    if (!isSharp) {
      isSharp = true;
      OnSharpened?.Invoke();
      AddTimedEvent(10, KillSelf);
    }
  }

  public void HandleActorLeave(Actor obj) {
    Sharpen();
  }

  public void HandleActorEnter(Actor actor) {
    if (isSharp) {
      Kill(actor);
      actor.TakeDamage(2, this);
    }
  }
}

[Serializable]
[ObjectInfo("bladegrass")]
internal class ItemBladegrassBlade : EquippableItem, IWeapon {
  public (int, int) AttackSpread => (2, 2);
  public override EquipmentSlot slot => EquipmentSlot.Weapon;
  public override int stacksMax => int.MaxValue;
}