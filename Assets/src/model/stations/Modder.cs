using System;
using UnityEngine;

[Serializable]
[ObjectInfo("gnarled-tree", description: "Modifies the Equipment to have the properties of a random Creature or Grass.")]
class Modder : Station, IDaySteppable {
  public Modder(Vector2Int pos) : base(pos) {
    inventory.allowDragAndDrop = true;
  }

  public override int maxDurability => 5;

  public override bool isActive => equipment != null;
  public EquippableItem equipment => inventory[0] as EquippableItem;

  public void StepDay() {
    if (equipment != null) {
      Entity entityToBaseModOff;
      if (MyRandom.value < 0.5) {
        var randomGrassConstructor = Util.RandomPick(Corruption.allGrassTypeConstructors);
        var grass = randomGrassConstructor.Invoke(new object[] { tile.pos }) as Grass;
        entityToBaseModOff = grass;
      } else {
        var randomCreatureConstructor = Util.RandomPick(Corruption.allCreatureTypeConstructors);
        var aiActor = randomCreatureConstructor.Invoke(new object[] { tile.pos }) as AIActor;
        entityToBaseModOff = aiActor;
      }
      var e = equipment;
      e.mods.Add(new ModEntityProxy(entityToBaseModOff));
      inventory.RemoveItem(e);
      floor.Put(new ItemOnGround(pos, e));
      this.ReduceDurability();
    }
  }
}

[Serializable]
public class ModEntityProxy : IItemMod, IModifierProvider {
  public string displayName => $"Mod: {entity.displayName}";
  public Entity entity;

  private Entity[] arr;
  public ModEntityProxy(Entity entity) {
    this.entity = entity;
    arr = new Entity[] { entity };
  }

  public IEnumerable<object> MyModifiers => arr;
}
