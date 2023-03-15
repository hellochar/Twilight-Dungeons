using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "An old dude. Looks like he's seen his way around the caves a few times.")]
public class OldDude : AIActor {
  int numWantedDeathblooms;
  bool questCompleted = false;

  public OldDude(Vector2Int pos) : base(pos) {
    faction = Faction.Neutral;
    hp = baseMaxHp = 8;
    SetTasks(new WaitTask(this, 1));
  }

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    GameModel.main.EnqueueEvent(() => {
      numWantedDeathblooms = Mathf.Min(GameModel.main.player.inventory.capacity, floor.EnemiesLeft());
      var hasDeathbloom = floor.grasses.Any(g => g is Deathbloom);
      if (!hasDeathbloom) {
        Encounters.AddDeathbloom.Apply(floor, floor.root);
      }
    });
  }

  protected override ActorTask GetNextTask() {
    var adjacentEnemy = floor.AdjacentActors(pos).FirstOrDefault(a => a.faction == Faction.Enemy);
    if (adjacentEnemy != null) {
      return new RunAwayTask(this, adjacentEnemy.pos, 1, false);
    }
    return new WaitTask(this, 1);
  }

  public string TestQuestStatus() {
    if (questCompleted) {
      return "The old dude is peacefully humming.";
    } else {
      if (GameModel.main.player.inventory
          .Where(item => item is ItemDeathbloomFlower)
          .Count() >= numWantedDeathblooms) {
        questCompleted = true;
        GameModel.main.player.water += 200;
        return $"Wow, that's a lot of Deathbloom Flowers! Here's 200 water for your trouble.";
      } else {
        return $"Show me {numWantedDeathblooms} Deathbloom Flowers to get a reward!";
      }
    }
  }
}

[Serializable]
[ObjectInfo(description: "A mossy man. Loves inhaling mushrooms.")]
public class MossMan : AIActor {
  bool questCompleted = false;

  public MossMan(Vector2Int pos) : base(pos) {
    faction = Faction.Neutral;
    hp = baseMaxHp = 8;
    SetTasks(new WaitTask(this, 1));
  }

  protected override void HandleEnterFloor() {
    base.HandleEnterFloor();
    GameModel.main.EnqueueEvent(() => {
      var hasSpores = floor.grasses.Any(g => g is Spores);
      if (!hasSpores) {
        Encounters.AddSpore.Apply(floor, floor.root);
      }
    });
  }

  protected override ActorTask GetNextTask() {
    var adjacentEnemy = floor.AdjacentActors(pos).FirstOrDefault(a => a.faction == Faction.Enemy);
    if (adjacentEnemy != null) {
      return new RunAwayTask(this, adjacentEnemy.pos, 1, false);
    }
    return new WaitTask(this, 1);
  }

  public string TestQuestStatus() {
    if (questCompleted) {
      return "The old dude is peacefully humming.";
    } else {
      if (statuses.Has<SporedStatus>()) {
        questCompleted = true;
        inventory.AddItem(new ItemMushroomCap());
        inventory.TryDropAllItems(floor, pos);
        return $"Ahh, finally! Here's a reward for your trouble.";
      } else {
        return $"Give me the Spored Status to get a reward!";
      }
    }
  }
}

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
[ObjectInfo(description: "A mercenary for hire.")]
public class Gambler : AIActor {
  public Gambler(Vector2Int pos) : base(pos) {
    faction = Faction.Neutral;
    hp = baseMaxHp = 8;
    SetTasks(new WaitTask(this, 1));
  }
  private static List<System.Reflection.ConstructorInfo> allItemTypeConstructors;

  protected override ActorTask GetNextTask() {
    // var adjacentEnemy = floor.AdjacentActors(pos).FirstOrDefault(a => a.faction == Faction.Enemy);
    // if (adjacentEnemy != null) {
    //   return new RunAwayTask(this, adjacentEnemy.pos, 1, false);
    // }
    // return new MoveRandomlyTask(this);
    return new WaitTask(this, 1);
  }

  public void Gamble() {
    var player = GameModel.main.player;
    if (player.water >= 50) {
      player.water -= 50;
      if (allItemTypeConstructors == null) {
        allItemTypeConstructors = AppDomain.CurrentDomain.GetAssemblies()
          .SelectMany(a => a.GetTypes())
          .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(EquippableItem)))
          .Select(itemType => itemType.GetConstructor(new Type[0]) ?? itemType.GetConstructor(new Type[1] { typeof(int) }))
          .Where(c => c != null)
          .ToList();
      }

      var constructor = Util.RandomPick(allItemTypeConstructors);
      Item item = null;
      if (constructor.GetParameters().Length == 0) {
        item = constructor.Invoke(new object[0]) as Item;
      } else {
        item = constructor.Invoke(new object[1] { 1 }) as Item;
      }
      if (item != null) {
        inventory.AddItem(item);
        inventory.TryDropAllItems(floor, pos);
      }
    } else {
      throw new CannotPerformActionException("Need 50 water!");
    }
  }
}

static class HumanoidEncounters {
  public static void AddOldDude(Floor floor, Room room){
    Vector2Int stairPos = floor.startTile.pos;
    var livableTiles = floor.EnumerateCircle(stairPos, 3).Select(position => floor.tiles[position]).Where((t) => t.CanBeOccupied());
    floor.Put(new OldDude(Util.RandomPick(livableTiles).pos));
  }

  public static void AddMossMan(Floor floor, Room room){
    Vector2Int stairPos = floor.startTile.pos;
    var livableTiles = floor.EnumerateCircle(stairPos, 3).Select(position => floor.tiles[position]).Where((t) => t.CanBeOccupied());
    floor.Put(new MossMan(Util.RandomPick(livableTiles).pos));
  }

  public static void AddMercenary(Floor floor, Room room){
    Vector2Int stairPos = floor.startTile.pos;
    var livableTiles = floor.EnumerateCircle(stairPos, 3).Select(position => floor.tiles[position]).Where((t) => t.CanBeOccupied());
    floor.Put(new Mercenary(Util.RandomPick(livableTiles).pos));
  }

  public static void AddGambler(Floor floor, Room room){
    Vector2Int stairPos = floor.startTile.pos;
    var livableTiles = floor.EnumerateCircle(stairPos, 3).Select(position => floor.tiles[position]).Where((t) => t.CanBeOccupied());
    floor.Put(new Gambler(Util.RandomPick(livableTiles).pos));
  }
}