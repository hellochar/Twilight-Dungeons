using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public enum FloorType {
  Slime,
  Processor,
  CraftingStation,
  Mystery,
  Plant,
  Healing,
  Empty,
  // Trade,
  Combat,
  Composter,
}

[System.Serializable]
[ObjectInfo(description: "Who knows what lies in the mists?")]
public class Mist : Chasm {
  public FloorType type;
  public readonly int depth;

  public Mist(Vector2Int pos, FloorType type, int depth) : base(pos) {
    this.type = type;
    this.depth = depth;
  }

  public override string description => $"Depth {depth}. Type {type.ToString()}.";

  [PlayerAction]
  public void Explore() {
    Serializer.SaveMainToCheckpoint();
    GameModel.main.player.UseResourcesOrThrow(actionPoints: 1);
    if (type == FloorType.Mystery) {
      // unbox mystery right now
      // var encounterTypes = Enum.GetValues(typeof(FloorType)).Cast<FloorType>().ToList();
      // encounterTypes.Remove(FloorType.Mystery);
      // encounterTypes.Remove(FloorType.Empty);
      var newEncounterType = MistsHomeFloor.bag.GetRandomWithout(new FloorType[] { FloorType.Mystery, FloorType.Empty });
      type = newEncounterType;
    }

    GameModel.main.activeMist = this;
    if (type == FloorType.Empty) {
      // immediately clear it
      floor.PlayerGoHome();
    } else {
      GameModel.main.PutPlayerAt(Generate.FloorOfType(GameModel.main.generator.EncounterGroup, GameModel.main.generator.shared, depth, type));
    }
  }

  public void ReplaceWith(Entity e) {
    var floor = this.floor;
    floor.Put(new HomeGround(pos));
    e.pos = this.pos;
    floor.Put(e);
  }

  public async Task ClearAndGiveRewardAsync() {
    var cavesFloor = GameModel.main.cave;

    GameModel.main.PutPlayerAt(0, GameModel.main.home.center);

    var choice = await cavesFloor.CreateRewards().ShowRewardUIAndWaitForChoice();
    var item = choice[0];
    switch(item) {
      case ItemGrass g:
        g.PlaceGrass(this);
        break;
      case ItemPlaceableEntity e:
        e.PlaceEntity(pos);
        break;
    }
    Clear();
  }

  public void Clear() {
    floor.Put(new HomeGround(pos));
  }
}
