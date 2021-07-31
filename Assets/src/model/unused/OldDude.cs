using System;
using System.Linq;
using UnityEngine;

[Serializable]
[ObjectInfo(description: "An old dude. Looks like he's seen his way around the caves a few times.")]
public class OldDude : AIActor {

  public OldDude(Vector2Int pos) : base(pos) {
    faction = Faction.Neutral;
    hp = baseMaxHp = 8;
    SetTasks(new MoveRandomlyTask(this));
  }

  protected override ActorTask GetNextTask() {
        return new MoveRandomlyTask(this);
  }

  public void RevealFloor(){
    var f = this.floor;
    var allPositions = f.EnumerateFloor();
    foreach(Vector2Int p in allPositions){
      Tile t = floor.tiles[p];
      if(t.visibility == TileVisiblity.Unexplored){
        t.visibility = TileVisiblity.Explored;
      }
    }
  }
}