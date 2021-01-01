using System.Collections.Generic;
using UnityEngine;

// public class Kingshroom : Plant {
//   public override int maxWater => 3;
//   class Mature : PlantStage {
//     public override float StepTime => 99999;
//     public override void Step() { }
//     public override string getUIText() => $"Ready to harvest.";
//   }

//   public Kingshroom(Vector2Int pos) : base(pos, new Seed()) {
//     stage.NextStage = new Mature();
//   }

//   public override Inventory CullRewards() {
//     return new Inventory(new ItemKingshroomHat(), new ItemKingshroomPowder());
//   }

//   public override Inventory HarvestRewards() {
//     throw new System.NotImplementedException();
//   }
// }

// internal class ItemKingshroomHat : EquippableItem, IModifierProvider {
//   public ItemKingshroomHat() { }

//   public override EquipmentSlot slot => EquipmentSlot.Head;

//   public IEnumerable<object> MyModifiers => IsEquipped ? this : null;

//   internal override string GetStats() => "When you kill an enemy, a mushroom grows in its place.";
// }

// internal class ItemKingshroomPowder : Item, IUsable {
//   public ItemKingshroomPowder() {}

//   public void Use(Actor a) {
//     // create a ring of mushrooms around you.
//   }
// }
