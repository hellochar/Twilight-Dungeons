using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Leaves a trail of Corruption that spreads across the map.")]
public class CorruptedEzra : Boss, IBodyMoveHandler {
  public CorruptedEzra(Vector2Int pos) : base(pos) {
    hp = baseMaxHp = 100;
  }

  public void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    floor.Put(new Corruption(oldPos));
  }

  protected override ActorTask GetNextTask() {
    var player = GameModel.main.player;
    if (player.grass is Corruption || IsNextTo(player)) {
      return new AttackTask(this, player);
    } else {
      return new ChaseTargetTask(this, player);
    }
  }

  internal override (int, int) BaseAttackDamage() => (3, 4);
}

[System.Serializable]
[ObjectInfo(description: "After three turns, spreads to nearby Tiles and morphs into a random Grass.\nStanding over this will allow Ezra to attack you at range.")]
public class Corruption : Grass, ISteppable, IDeathHandler, IActorEnterHandler {
  public float timeNextAction { get; set; }
  public float turnPriority => 9;
  public Corruption(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 5;
    // allGrassTypeConstructors;
    // allCreatureTypeConstructors;
  }

  private static List<System.Reflection.ConstructorInfo> _allGrassTypeConstructors;
  public static List<System.Reflection.ConstructorInfo> allGrassTypeConstructors {
    get {
      if (_allGrassTypeConstructors == null) {
        _allGrassTypeConstructors = AppDomain.CurrentDomain.GetAssemblies()
          .SelectMany(a => a.GetTypes())
          .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Grass)) && !t.IsSubclassOf(typeof(Corruption)))
          .Select(grassType => grassType.GetConstructor(new Type[1] { typeof(Vector2Int) }))
          .Where(c => c != null)
          .ToList();
      }
      return _allGrassTypeConstructors;
    }
  }

  private static List<System.Reflection.ConstructorInfo> _allCreatureTypeConstructors;
  public static List<System.Reflection.ConstructorInfo> allCreatureTypeConstructors {
    get {
      if (_allCreatureTypeConstructors == null) {
      _allCreatureTypeConstructors = AppDomain.CurrentDomain.GetAssemblies()
          .SelectMany(a => a.GetTypes())
          .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(AIActor)) && !t.IsSubclassOf(typeof(Boss)))
          .Select(creatureType => creatureType.GetConstructor(new Type[1] { typeof(Vector2Int) }))
          .Where(c => c != null)
          .ToList();
      }
      return _allCreatureTypeConstructors;
    }
  }

  public void HandleDeath(Entity source) {
    if (source != this) {
      return;
    }

    if (MyRandom.value < 1) {
      var randomGrassConstructor = Util.RandomPick(allGrassTypeConstructors);
      var grass = randomGrassConstructor.Invoke(new object[] { tile.pos }) as Grass;
      if (grass != null) {
        floor.Put(grass);
      }
    } else {
      var randomCreatureConstructor = Util.RandomPick(allCreatureTypeConstructors);
      var aiActor = randomCreatureConstructor.Invoke(new object[] { tile.pos }) as AIActor;
      if (aiActor != null) {
        floor.Put(aiActor);
      }
    }
  }

  public float Step() {
    foreach (var tile in floor.GetCardinalNeighbors(pos).Where(t => t is Ground).ToList()) {
      floor.Put(new Corruption(tile.pos));
    }
    KillSelf();
    return 1;
  }

  public void HandleActorEnter(Actor who) {
    if (who is Player) {
      Kill(who);
    }
  }
}