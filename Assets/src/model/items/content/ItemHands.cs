using System.Collections.Generic;
using System.Reflection;

public class ItemHands : EquippableItem, IWeapon {
  public override EquipmentSlot slot => EquipmentSlot.Weapon;
  private Player player;

  public ItemHands(Player player) {
    this.player = player;
  }

  public override Inventory inventory {
    get => player.equipment;
    // no op. 
    set {}
  }

  public (int, int) AttackSpread => (1, 2);

  public override List<MethodInfo> GetAvailableMethods(Player player) {
    return new List<MethodInfo>();
  }
}
