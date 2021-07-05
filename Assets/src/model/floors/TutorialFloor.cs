using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class TutorialFloor : Floor {
  public BerryBush berryBush;
  public Blob blob;
  public Guardleaf guardleaf;
  public List<Actor> jackals;
  public Bat bat;
  public Astoria astoria;
  public Room portRoom, blobRoom, dogsRoom, bushRoom, endRoom;

  public event Action OnTutorialEnded;

  public TutorialFloor() : base(-1, 73, 9) {
    this.portRoom = new Room(new Vector2Int(0, 0), new Vector2Int(5, 8));
    this.blobRoom = new Room(new Vector2Int(portRoom.max.x + 5, 1), new Vector2Int(portRoom.max.x + 5 + 11, 7));
    this.dogsRoom = new Room(new Vector2Int(blobRoom.max.x + 4, 1), new Vector2Int(blobRoom.max.x + 4 + 14, 7));
    this.bushRoom = new Room(new Vector2Int(dogsRoom.max.x + 4, 1), new Vector2Int(dogsRoom.max.x + 4 + 7, 7));
    this.endRoom =  new Room(new Vector2Int(bushRoom.max.x + 4, 1), new Vector2Int(bushRoom.max.x + 4 + 10, 7));

    // fill with walls
    foreach (var p in this.EnumerateFloor()) {
      Put(new Wall(p));
    }

    var cY = portRoom.center.y;

    // first room:
    FloorUtils.PutGround(this, this.EnumerateRoom(portRoom));
    FloorUtils.PutGround(this, this.EnumerateLine(portRoom.center, blobRoom.center));

    FloorUtils.PutGround(this, this.EnumerateRoom(blobRoom));
    FloorUtils.PutGround(this, FloorUtils.Line3x3(this, blobRoom.center, dogsRoom.center));
    Put(new Wall(new Vector2Int(blobRoom.max.x + 1, cY - 1)));
    Put(new Rubble(new Vector2Int(blobRoom.max.x + 1, cY)));
    Put(new Wall(new Vector2Int(blobRoom.max.x + 1, cY + 1)));

    FloorUtils.PutGround(this, this.EnumerateRoom(dogsRoom));
    FloorUtils.PutGround(this, FloorUtils.Line3x3(this, dogsRoom.center, bushRoom.center));

    FloorUtils.PutGround(this, this.EnumerateRoom(bushRoom));
    FloorUtils.PutGround(this, FloorUtils.Line3x3(this, bushRoom.center, endRoom.center));

    FloorUtils.PutGround(this, this.EnumerateRoom(endRoom));
    FloorUtils.NaturalizeEdges(this);

    // naturalize edges may sometimes create walls in this location
    // which can confuse players because they think it's a dead end. make sure
    // this never happens.
    Put(new Ground(new Vector2Int(blobRoom.max.x, cY)));

    // add rubble in first room
    Put(new Rubble(new Vector2Int(7, cY)));
    Put(new Rubble(new Vector2Int(8, cY)));
    Put(new Rubble(new Vector2Int(9, cY - 1)));
    Put(new Rubble(new Vector2Int(9, cY)));
    Put(new Rubble(new Vector2Int(9, cY + 1)));

    // second room - one blob
    this.blob = new Blob(new Vector2Int(15, 4));
    Put(this.blob);

    // third room - three jackals and guardleaf
    this.jackals = new List<Actor>();
    var guardleafCenter = new Vector2Int(dogsRoom.min.x - 1, cY);
    var jackalCenter = guardleafCenter + new Vector2Int(7, 0);
    this.jackals.Add(new Jackal(jackalCenter));
    this.jackals.Add(new Jackal(jackalCenter + Vector2Int.up));
    this.jackals.Add(new Jackal(jackalCenter + Vector2Int.down));
    PutAll(this.jackals);
    Put(new Guardleaf(guardleafCenter));
    Put(new Guardleaf(guardleafCenter + Vector2Int.up));
    Put(new Guardleaf(guardleafCenter + Vector2Int.down));
    this.guardleaf = new Guardleaf(guardleafCenter + Vector2Int.left);
    Put(this.guardleaf);
    Put(new Guardleaf(guardleafCenter + Vector2Int.right));
    Put(this.astoria = new Astoria(new Vector2Int(dogsRoom.max.x, cY + 1)));

    // fourth room - a berry bush
    Put(new Soil(bushRoom.center));
    Put(new Soil(bushRoom.center + new Vector2Int(3, 0)));
    this.berryBush = new BerryBush(bushRoom.center);
    berryBush.GoNextStage();
    Put(berryBush);
    Encounters.AddWater(this, bushRoom);

    // last room - two blobs and a bat, filled with soft grass
    Encounters.FillWithSoftGrass(this, endRoom);
    Put(new Blob(endRoom.center));
    Put(new Blob(endRoom.center + Vector2Int.up));
    this.bat = new Bat(endRoom.center + new Vector2Int(-1, -2));
    Put(this.bat);
    PlaceDownstairs(new Vector2Int(endRoom.max.x, cY));

    FloorUtils.TidyUpAroundStairs(this);
  }

  internal override void PlayerGoDownstairs() {
    PlayerPrefs.SetInt("hasSeenPrologue", 1);
    GameModel.main.turnManager.OnPlayersChoice += () => OnTutorialEnded?.Invoke();
  }
}
