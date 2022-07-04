using System.Collections.Generic;
using System.Reflection;

[System.Serializable]
public class ItemHands : EquippableItem, IWeapon {
  public override EquipmentSlot slot => EquipmentSlot.Weapon;
  private new Player player;

  public ItemHands(Player player) {
    this.player = player;
  }

  public override Inventory inventory {
    get => player.equipment;
    // no op. 
    set {}
  }

  public (int, int) AttackSpread => (1, 1);

  public override List<MethodInfo> GetAvailableMethods(Player player) {
    return new List<MethodInfo>();
  }
}
