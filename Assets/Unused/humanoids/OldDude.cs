using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
public abstract class NPC : AIActor {
  public NPC(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    hp = baseMaxHp = 3;
    ClearTasks();
  }
}

[Serializable]
public class OldDude : NPC {
  public override string displayName => "Florist";

  public bool questCompleted = false;

  public OldDude(Vector2Int pos) : base(pos) {}

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    GameModel.main.EnqueueEvent(() => {
      var hasDeathbloom = floor.grasses.Any(g => g is Deathbloom);
      if (!hasDeathbloom) {
        Encounters.AddDeathbloom(floor, floor.root);
      }
    });
  }

  protected override ActorTask GetNextTask() {
    var adjacentEnemy = floor.AdjacentActors(pos).FirstOrDefault(a => a.faction == Faction.Enemy);
    if (adjacentEnemy != null) {
      return new RunAwayTask(this, adjacentEnemy.pos, 1, true);
    }
    return new WaitTask(this, 1);
  }

  ItemDeathbloomFlower playersDeathbloom => GameModel.main.player.inventory.OfType<ItemDeathbloomFlower>().FirstOrDefault();

  public void Trade() {
    var deathbloom = playersDeathbloom;
    if (deathbloom == null) {
      throw new CannotPerformActionException("You have no Deathbloom Flower to show.");
    }

    GameModel.main.player.inventory.RemoveItem(deathbloom);
    inventory.AddItem(deathbloom);
    // playersDeathbloom.Destroy();
    GameModel.main.player.water += 100;
    // Popups.CreateStandard("Old Dude", "", "Thanks!", "", null);
    questCompleted = true;
  }

  public override List<MethodInfo> GetPlayerActions() {
    var actions = base.GetPlayerActions();
    if (questCompleted) {
      return actions;
    }

    if (playersDeathbloom != null) {
      actions.Add(GetType().GetMethod("Trade"));
    }

    return actions;
  }

  public override string description {
    get {
      if (questCompleted) {
        return "The Florist is peacefully humming.";
      } else if (playersDeathbloom != null) {
        return $"You have a Deathbloom Flower! It's so beautiful...\n\nTrade it to me for 100 water?\"";
      } else {
        return "\"I'm in search of a Deathbloom Flower. If you find one, let me know.\"";
      }
    }
  }
}

// this one's not that fun because you have to swap positions with him and nudge him right next to the Spores
[Serializable]
public class MossMan : NPC, IStatusAddedHandler {
  bool questCompleted = false;

  public MossMan(Vector2Int pos) : base(pos) {}

  int desiredNumSpores;

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    GameModel.main.EnqueueEvent(() => {
      desiredNumSpores = Mathf.Min(floor.EnemiesLeft() / 3, 2);
      var hasSpores = floor.grasses.Any(g => g is Spores);
      if (!hasSpores) {
        Encounters.AddSpore(floor, floor.root);
      }
    });
  }

  protected override ActorTask GetNextTask() {
    var adjacentEnemy = floor.AdjacentActors(pos).FirstOrDefault(a => a.faction == Faction.Enemy);
    if (adjacentEnemy != null) {
      return new RunAwayTask(this, adjacentEnemy.pos, 1, true);
    }
    return new WaitTask(this, 1);
  }

  public override string description =>
    questCompleted ?
      "The Mossman is in psychedelic euphoria." :
      $"\"I love Spores! Create {desiredNumSpores} on this level and I'll give you my hat!\n";

  public void HandleStatusAdded(Status status) {
    if (status is SporedStatus && !questCompleted) {
      questCompleted = true;
      inventory.AddItem(new ItemMushroomCap());
      inventory.TryDropAllItems(floor, pos);
    }
  }
}

// this guy's too strong, he's basically a floor clear since enemies never target him. it is VERY fun though.
// it also makes the moves too fast and can be hard to follow.
[Serializable]
[ObjectInfo(description: "A mercenary for hire.")]
public class Mercenary : AIActor, IBodyTakeAttackDamageHandler {
  public bool isHired => faction == Faction.Ally;

  public Mercenary(Vector2Int pos) : base(pos) {
    faction = Faction.Neutral;
    hp = baseMaxHp = 8;
    SetTasks(new WaitTask(this, 1));
  }

  internal override (int, int) BaseAttackDamage() => (2, 3);

  protected override ActorTask GetNextTask() {
    // var adjacentEnemy = floor.AdjacentActors(pos).FirstOrDefault(a => a.faction == Faction.Enemy);
    // if (adjacentEnemy != null) {
    //   return new RunAwayTask(this, adjacentEnemy.pos, 1, false);
    // }
    return new MoveRandomlyTask(this);
    // return new WaitTask(this, 1);
  }

  public void HandleTakeAttackDamage(int damage, int hp, Actor source) {
    if (source != this) {
      SetTasks(
        new ChaseTargetTask(this, source, 1),
        new AttackTask(this, source)
      );
    }
  }

  public void Hire() {
    var player = GameModel.main.player;
    if (player.water >= 100) {
      player.water -= 100;
      SetAI(new CharmAI(this));
      statuses.Add(new CharmedStatus());
      faction = Faction.Ally;
    } else {
      throw new CannotPerformActionException("Need 100 water!");
    }
  }
}

[Serializable]
[ObjectInfo(description: "")]
public class Gambler : NPC {
  private static readonly WeightedRandomBag<Item> STOCK = new WeightedRandomBag<Item>() {
    { 1f, new ItemWildwoodWreath() },
    { 0.1f, new ItemWildwoodRod() },

    { 1f, new ItemBarkShield() },
    { 0.1f, new ItemCharmBerry(1) },

    { 1f, new ItemBacillomyte() },
    { 0.1f, new ItemBroodleaf() },

    { 1f, new ItemCatkin() },
    { 1f, new ItemHardenedSap() },
    { 0.1f, new ItemCrescentVengeance() },

    { .5f, new ItemPlatedArmor() },
    { 0.01f, new ItemStompinBoots() },

    { 1f, new ItemMushroomCap() },
    { 0.1f, new ItemLivingArmor() },

    { 1f, new ItemThornShield() },
    { 1f, new ItemCrownOfThorns() },
    { 0.1f, new ItemBlademail() },

    { 1f, new ItemWitchsShiv() },
    { 1f, new ItemBackstepShoes() },

    // misc content

    { 1f, new ItemCoralChunk(1) },
    { 1f, new ItemGoldenFern() },
    { 1f, new ItemStoutShield() },
  };
  public Gambler(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 9;
    var myStock = STOCK.Clone();
    // go down to only 3 items
    for (int i = 0; i < 3; i++) {
      var item = myStock.GetRandomAndRemove();
      inventory.AddItem(item);
    }
  }

  public override string description {
    get {
      if (!inventory.ItemsNonNull().Any()) {
        return "\"Ah, I'm all out! Best of luck.\"";
      }
      return "\"I know just what you need! Won't you take the plunge? Just 100 water!\"";
    }
  }

  protected override ActorTask GetNextTask() {
    if (MyRandom.value < 0.5f) {
      var range3Tiles = floor.EnumerateCircle(floor.startPos, 3).Where((pos) => floor.tiles[pos].CanBeOccupied());
      var target = Util.RandomPick(range3Tiles);
      return new MoveToTargetTask(this, target);
    } else {
      return new WaitTask(this, 3);
    }
  }

  public void Gamble() {
    var player = GameModel.main.player;
    if (player.water >= 100) {
      player.water -= 100;

      Item item = inventory.ItemsNonNull().FirstOrDefault();
      if (item != null) {
        inventory.TryDropItem(floor, pos, item);
      }
    } else {
      throw new CannotPerformActionException("Need 100 water!");
    }
  }

  public override List<MethodInfo> GetPlayerActions() {
    var methods = base.GetPlayerActions();
    if (inventory.ItemsNonNull().Any()) {
      methods.Add(GetType().GetMethod("Gamble"));
    }
    return methods;
  }
}

static class HumanoidEncounters {
  public static void AddNPCNearStart(Floor floor, Room room, Func<Vector2Int, Entity> fn) {
    Vector2Int stairPos = floor.startPos;
    var livableTiles = floor.EnumerateCircle(stairPos, 3).Select(position => floor.tiles[position]).Where((t) => t.CanBeOccupied());
    floor.Put(fn(Util.RandomPick(livableTiles).pos));
  }

  public static void AddOldDude(Floor floor, Room room) => AddNPCNearStart(floor, room, v => new OldDude(v));

  public static void AddMossMan(Floor floor, Room room) => AddNPCNearStart(floor, room, v => new MossMan(v));

  public static void AddMercenary(Floor floor, Room room) => AddNPCNearStart(floor, room, v => new Mercenary(v));

  public static void AddGambler(Floor floor, Room room) => AddNPCNearStart(floor, room, v => new Gambler(v));
}