using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
[ObjectInfo(spriteName: "fruitingbody", description: "Infects one of your equipment slots if you're hit by its spray.\nIf you have all 5 infections, instead heal 1 HP.", flavorText: "Did you know? Sporocarp of a basidiomycete is known as a basidiocarp or basidiome, while the fruitbody of an ascomycete is known as an ascocarp.")]
public class FruitingBody : AIActor, INoTurnDelay {
  [field: NonSerialized] /// controller only
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
      KillSelf();
    }
  }

  private void Infect(IEnumerable<EquipmentSlot> uninfectedSlots, Player player) {
    var infectionType = InfectionTypes[Util.RandomPick(uninfectedSlots)];
    var constructor = infectionType.GetConstructor(new Type[0]);
    EquippableItem infection = (EquippableItem)constructor.Invoke(new object[0]);

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
class ItemTanglefoot : EquippableItem, IBodyMoveHandler, ISticky {
  // 3.5% chance per turn
  private static float prdC = (float) PseudoRandomDistribution.CfromP(0.04m);
  internal override string GetStats() => "You're infected with Tanglefoot!\nMoving over a Tile without grass will occasionally Constrict you and grow a Guardleaf at your location.\nDoes not trigger at home.";
  public override EquipmentSlot slot => EquipmentSlot.Footwear;

  public override int stacksMax => 5;
  public override bool disjoint => true;

  private PseudoRandomDistribution prd;

  public ItemTanglefoot() {
    prd = new PseudoRandomDistribution(prdC);
  }

  public void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    var canTrigger = newPos != oldPos && player.grass == null && player.floor.depth != 0 && Guardleaf.CanOccupy(player.tile);
    if (!canTrigger) {
      return;
    }

    var shouldTrigger = prd.Test();
    if (shouldTrigger) {
      player.statuses.Add(new ConstrictedStatus(null));
      player.floor.Put(new Guardleaf(player.pos));
      stacks--;
    }
  }
}

[Serializable]
[ObjectInfo("stiffarm")]
class ItemStiffarm : EquippableItem, IWeapon, IAttackDamageTakenModifier, ISticky {
  internal override string GetStats() => "You're infected with Stiffarm!\nYou take +1 damage from attacks.";
  public override EquipmentSlot slot => EquipmentSlot.Weapon;
  public override int stacksMax => 15;
  public override bool disjoint => true;
  public (int, int) AttackSpread => (2, 3);

  public ItemStiffarm() {
  }

  public int Modify(int input) {
    return input + 1;
  }
}

[Serializable]
[ObjectInfo("bulbous-skin")]
class ItemBulbousSkin : EquippableItem, ISticky {
  public override EquipmentSlot slot => EquipmentSlot.Armor;

  public override int stacksMax => 1;
  public override bool disjoint => true;

  internal override string GetStats() => "You're infected with Bulbous Skin!\nPress Germinate to take 1 damage and create 4 Mushrooms around you.";
  public ItemBulbousSkin() {
  }

  public void Germinate(Player player) {
    player.SetTasks(new GenericPlayerTask(player, GerminateBaseAction));
    stacks--;
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
class ItemThirdEye : EquippableItem, ISticky, IActionPerformedHandler, IAttackDamageTakenModifier {
  internal override string GetStats() => "You're infected with a Third Eye!\nYou can see creatures' exact HP.\nTake 1 more attack damage.";
  public override EquipmentSlot slot => EquipmentSlot.Headwear;
  public override int stacksMax => 40;
  public override bool disjoint => true;
  private float reduction;
  public ItemThirdEye() {
  }

  public override void OnEquipped() {
    player.statuses.Add(new ThirdEyeStatus());
  }

  public override void OnUnequipped() {
    player.statuses.RemoveOfType<ThirdEyeStatus>();
  }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    stacks--;
  }

  public int Modify(int input) {
    return input + 1;
  }
}

[Serializable]
[ObjectInfo("scaly-skin")]
class ItemScalySkin : EquippableItem, ISticky, IAttackDamageTakenModifier, IActionPerformedHandler {
  internal override string GetStats() => "You're infected with Scaly Skin!\nBlock 1 damage.\nLose 8 water every 25 turns.";

  public override EquipmentSlot slot => EquipmentSlot.Offhand;
  public override int stacksMax => 8;
  public override bool disjoint => true;
  private float timeLostWater;

  public ItemScalySkin() {
    timeLostWater = GameModel.main.time;
  }

  public int Modify(int input) {
    if (input > 0) {
      stacks--;
    }
    return input - 1;
  }

  public void HandleActionPerformed(BaseAction final, BaseAction initial) {
    if (GameModel.main.time - timeLostWater >= 25) {
      timeLostWater = GameModel.main.time;
      player.water = Math.Max(player.water - 8, 0);
    }
  }
}

[Serializable]
[ObjectInfo("third-eye")]
class ThirdEyeStatus : Status {
  public override bool Consume(Status other) => true;
  public override string Info() => "You can see creatures' exact HP!";
}
