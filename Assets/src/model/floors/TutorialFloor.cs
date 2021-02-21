using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class TutorialFloor : Floor {
  public BerryBush berryBush;
  public Blob blob;
  public List<Actor> jackals;
  public event Action OnTutorialEnded;

  public TutorialFloor() : base(-1, 53, 9) {
    /// things to explicitly teach:
    /// tap to move (or use D-Pad)
    /// move into enemy to attack
    /// pick items up by moving over them
    /// collect water
    /// tap hold to learn about an item
    /// game is turn based
    /// game is all about tactical movement
    Room startRoom = new Room(new Vector2Int(1, 1), new Vector2Int(5, 7));
    Room oneBlob = new Room(new Vector2Int(10, 1), new Vector2Int(17, 7));
    Room jackalsAndGuardleaf = new Room(new Vector2Int(21, 1), new Vector2Int(27, 7));
    Room berryBushAndWater = new Room(new Vector2Int(31, 1), new Vector2Int(37, 7));
    Room blobsAndBat = new Room(new Vector2Int(41, 1), new Vector2Int(51, 7));

    // fill with walls
    foreach (var p in EnumerateFloor()) {
      Put(new Wall(p));
    }

    var cY = startRoom.center.y;

    // first room:
    FloorUtils.PutGround(this, EnumerateRoom(startRoom));
    FloorUtils.PutGround(this, EnumerateLine(startRoom.center, oneBlob.center));

    FloorUtils.PutGround(this, EnumerateRoom(oneBlob));
    FloorUtils.PutGround(this, FloorUtils.Line3x3(this, oneBlob.center, jackalsAndGuardleaf.center));
    Put(new Wall(new Vector2Int(19, cY - 1)));
    Put(new Wall(new Vector2Int(19, cY + 1)));

    FloorUtils.PutGround(this, EnumerateRoom(jackalsAndGuardleaf));
    FloorUtils.PutGround(this, FloorUtils.Line3x3(this, jackalsAndGuardleaf.center, berryBushAndWater.center));

    FloorUtils.PutGround(this, EnumerateRoom(berryBushAndWater));
    FloorUtils.PutGround(this, FloorUtils.Line3x3(this, berryBushAndWater.center, blobsAndBat.center));

    FloorUtils.PutGround(this, EnumerateRoom(blobsAndBat));
    FloorUtils.NaturalizeEdges(this);

    // add rubble in first room
    Put(new Rubble(new Vector2Int(7, cY)));
    Put(new Rubble(new Vector2Int(8, cY)));
    Put(new Rubble(new Vector2Int(9, cY - 1)));
    Put(new Rubble(new Vector2Int(9, cY)));
    Put(new Rubble(new Vector2Int(9, cY + 1)));

    // second room - one blob
    this.blob = new Blob(new Vector2Int(15, 4));
    Put(blob);

    // third room - three jackals and guardleaf
    this.jackals = new List<Actor>();
    var guardleafCenter = new Vector2Int(jackalsAndGuardleaf.min.x - 1, cY);
    var jackalCenter = guardleafCenter + new Vector2Int(6, 0);
    jackals.Add(new Jackal(jackalCenter));
    jackals.Add(new Jackal(jackalCenter + Vector2Int.up));
    jackals.Add(new Jackal(jackalCenter + Vector2Int.down));
    PutAll(jackals);
    Put(new Guardleaf(guardleafCenter));
    Put(new Guardleaf(guardleafCenter + Vector2Int.up));
    Put(new Guardleaf(guardleafCenter + Vector2Int.down));
    Put(new Guardleaf(guardleafCenter + Vector2Int.left));

    // fourth room - a berry bush
    Put(new Soil(berryBushAndWater.center));
    Put(new Soil(berryBushAndWater.center + new Vector2Int(3, 0)));
    this.berryBush = new BerryBush(berryBushAndWater.center);
    berryBush.GoNextStage();
    Put(berryBush);
    berryBush.stage.harvestOptions.RemoveAt(2);
    berryBush.stage.harvestOptions.RemoveAt(1);
    Encounters.AddWater(this, berryBushAndWater);

    // last room - two blobs and a bat, filled with soft grass
    Encounters.FillWithSoftGrass(this, blobsAndBat);
    Put(new Blob(blobsAndBat.center));
    Put(new Blob(blobsAndBat.center + Vector2Int.up));
    Put(new Bat(blobsAndBat.center + new Vector2Int(-1, -2)));
    PlaceDownstairs(new Vector2Int(blobsAndBat.max.x, cY));
  }

  internal override void PlayerGoDownstairs() {
    OnTutorialEnded?.Invoke();
  }
}
