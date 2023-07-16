using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "Place at home. Occasionally drops a Floof.")]
public class Ninetails : Destructible, IDeathHandler {
  public Ninetails(Vector2Int pos) : base(pos) {
  }

  public void HandleDeath(Entity source) {
    this.BecomeItemInInventory(new ItemPlantableNinetails());
  }
}

[Serializable]
internal class ItemPlantableNinetails : Item, ITargetedAction<Tile> {
  public string TargettedActionName => "Place";

  public string TargettedActionDescription => "Choose where to put the Ninetails.";

  public void PerformTargettedAction(Player player, Entity target) {
    player.SetTasks(
      new MoveNextToTargetTask(player, target.pos),
      new GenericOneArgTask<Vector2Int>(player, Place, target.pos)
    );
  }

  void Place(Vector2Int pos) {
    var floor = GameModel.main.player.floor;
    floor.Put(new HomeNinetails(pos));
    Destroy();
  }

  public IEnumerable<Tile> Targets(Player player) =>
    player.floor.depth == 0 ?
      Enumerable.Empty<Tile>() :
      player.floor.tiles.Where(t => t.CanBeOccupied());
}

[Serializable]
public class HomeNinetails : Body, ISteppable {
  public HomeNinetails(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 50;
  }

  public float timeNextAction { get; set; }

  public float turnPriority => 50;

  public float Step() {
    floor.Put(new ItemOnGround(pos, new ItemFloof(), pos));
    return 50;
  }
}

[Serializable]
[ObjectInfo(description: ".")]
public class ItemFloof : Item {
}