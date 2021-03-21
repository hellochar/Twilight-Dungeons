using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
[ObjectInfo(spriteName: "fruitingbody", description: "Infects you with random equipment on your body.\nIf you have all 5 infections, heal 1 HP.", flavorText: "Did you know? Sporocarp of a basidiomycete is known as a basidiocarp or basidiome, while the fruitbody of an ascomycete is known as an ascocarp.")]
public class FruitingBody : AIActor, INoTurnDelay {
  [field:NonSerialized] /// controller only
  public event Action OnSprayed;
  float cooldown;
  public FruitingBody(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 1;
    faction = Faction.Neutral;
    cooldown = MyRandom.Range(0, 10);
    ClearTasks();
  }

  protected override ActorTask GetNextTask() {
    if (cooldown > 0) {
      cooldown--;
      return new WaitTask(this, 1);
    } else {
      return new TelegraphedTask(this, 1, new GenericBaseAction(this, Spray));
    }
  }

  static Dictionary<EquipmentSlot, Type> InfectionTypes = new Dictionary<EquipmentSlot, Type>() {
    [EquipmentSlot.Footwear] = typeof(ItemTanglefoot),
    [EquipmentSlot.Weapon] = typeof(ItemStiffarm),
    [EquipmentSlot.Armor] = typeof(ItemBulbousSkin),
    [EquipmentSlot.Headwear] = typeof(ItemThirdEye),
    [EquipmentSlot.Offhand] = typeof(ItemScalySkin)
  };

  private void Spray() {
    cooldown = 10;
    OnSprayed?.Invoke();
    /// apply a random infection to all nearby creatures
    var player = GameModel.main.player;
    if (player.IsNextTo(this)) {
      var uninfectedSlots = InfectionTypes.Keys.Where((slot) => player.equipment[slot]?.GetType() != InfectionTypes[slot]);
      if (uninfectedSlots.Count() == 0) {
        player.Heal(1);
      } else {
        Infect(uninfectedSlots, player);
      }
    }
  }

  private void Infect(IEnumerable<EquipmentSlot> uninfectedSlots, Player player) {
    var infectionType = InfectionTypes[Util.RandomPick(uninfectedSlots)];
    var constructor = infectionType.GetConstructor(new Type[0]);
    EquippableItem infection = (EquippableItem) constructor.Invoke(new object[0]);

    var existingEquipment = player.equipment[infection.slot];
    if (existingEquipment != null && !(existingEquipment is ItemHands)) {
      player.equipment.RemoveItem(existingEquipment);
      if (!(existingEquipment is ISticky)) {
        /// drop it onto the ground
        floor.Put(new ItemOnGround(player.pos, existingEquipment));
      }
    }
    player.equipment.AddItem(infection);
  }
}

[Serializable]
[ObjectInfo("tanglefoot")]
class ItemTanglefoot : EquippableItem, IDurable, IBodyMoveHandler, ISticky {
  internal override string GetStats() => "You're infected with Tanglefoot!\nMoving over a Tile without grass will occasionally Constrict you and grow a Guardleaf at your location.\nDoes not trigger at home.";
  public override EquipmentSlot slot => EquipmentSlot.Footwear;

  public int durability { get; set; }

  public int maxDurability => 10;

  public ItemTanglefoot() {
    durability = maxDurability;
  }

  public void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    if (player.floor.depth == 0) {
      return;
    }
    var canTrigger = newPos != oldPos && player.grass == null;
    var shouldTrigger = MyRandom.value < 0.02f;
    if (canTrigger && shouldTrigger) {
      player.statuses.Add(new ConstrictedStatus(null));
      player.floor.Put(new Guardleaf(player.pos));
      this.ReduceDurability();
    }
  }
}

[Serializable]
[ObjectInfo("stiffarm")]
class ItemStiffarm : EquippableItem, IDurable, IWeapon, IAttackDamageTakenModifier, ISticky {
  internal override string GetStats() => "You're infected with from Stiffarm!\nYou take +1 damage from attacks.";
  public override EquipmentSlot slot => EquipmentSlot.Weapon;
  public int durability { get; set; }
  public int maxDurability => 30;
  public (int, int) AttackSpread => (2, 3);

  public ItemStiffarm() {
    durability = maxDurability;
  }

  public int Modify(int input) {
    return input + 1;
  }
}

[Serializable]
[ObjectInfo("bulbous-skin")]
class ItemBulbousSkin : EquippableItem, IDurable, ISticky {
  public override EquipmentSlot slot => EquipmentSlot.Armor;

  public int durability { get; set; }

  public int maxDurability => 4;

  internal override string GetStats() => "You're infected with Bulbous Skin!\nPress Germinate to take 1 damage and create 4 Mushrooms around you.";
  public ItemBulbousSkin() {
    durability = maxDurability;
  }

  public void Germinate(Player player) {
    player.SetTasks(new GenericPlayerTask(player, GerminateBaseAction));
    this.ReduceDurability();
  }

  void GerminateBaseAction() {
    player.TakeDamage(1, player);
    foreach (var tile in player.floor.GetCardinalNeighbors(player.pos)) {
      if (tile is Ground) {
        player.floor.Put(new Mushroom(tile.pos));
      }
    }
  }

  public override List<MethodInfo> GetAvailableMethods(Player actor) {
    var methods = base.GetAvailableMethods(actor);
    methods.Add(GetType().GetMethod("Germinate"));
    return methods;
  }
}

[Serializable]
[ObjectInfo("third-eye")]
class ItemThirdEye : EquippableItem, IDurable, ISticky, IActionPerformedHandler {
  internal override string GetStats() => "You're infected with a Third Eye!\nLose half your vision range.\nYou can see creatures' exact HP.";
  public override EquipmentSlot slot => EquipmentSlot.Headwear;
  public int durability { get; set; }
  public int maxDurability => 200;
  private int reduction;
  public ItemThirdEye() {
    durability = maxDurability;
  }

  public override void OnEquipped() {
    reduction = Mathf.CeilToInt(player.visibilityRange / 2);
    player.visibilityRange -= reduction;
    player.statuses.Add(new ThirdEyeStatus());
  }

  public override void OnUnequipped() {
    player.visibilityRange += reduction;
    player.statuses.RemoveOfType<ThirdEyeStatus>();
  }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    this.ReduceDurability();
  }
}

[Serializable]
[ObjectInfo("scaly-skin")]
class ItemScalySkin : EquippableItem, ISticky, IDurable, IAttackDamageTakenModifier, IActionPerformedHandler {
  internal override string GetStats() => "You're infected with Scaly Skin!\nBlock 1 damage.\nLose 10 water every 25 turns.";

  public override EquipmentSlot slot => EquipmentSlot.Offhand;
  public int durability { get; set; }
  public int maxDurability => 20;
  private float timeLostWater;

  public ItemScalySkin() {
    durability = maxDurability;
    timeLostWater = GameModel.main.time;
  }

  public int Modify(int input) {
    if (input > 0) {
      this.ReduceDurability();
    }
    return input - 1;
  }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (GameModel.main.time - timeLostWater >= 25) {
      timeLostWater = GameModel.main.time;
      player.water = Math.Max(player.water - 10, 0);
    }
  }
}

[Serializable]
[ObjectInfo("third-eye")]
class ThirdEyeStatus : Status {
  public override bool Consume(Status other) => true;
  public override string Info() => "You can see creatures' exact HP!";
}
