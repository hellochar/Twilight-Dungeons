using System;
using System.Collections.Generic;
using System.Linq;
/*
Cave network user flow:
1. Player starts at home base. Sees 2 downstairs.
2. The first time a player walks into the downstairs, that level
gets lazily generated. The stairs you took is saved.
3. The player is in Floor 1A (or 1B). There's combat. There is
no upstairs, but there's a Rope that takes you back.


So we have Real Floors but we also have "blueprint" floors.
We have a Blueprint for the entire cave system that already maps out:
What nodes there are
For each node, what the generation parameters are
What their connections are
*/

[Serializable]
public class CaveNode {
  // multiple higher levels could feed into the same CaveNode
  public HashSet<CaveNode> parents = new HashSet<CaveNode>();
  // don't mutate directly
  public HashSet<CaveNode> children = new HashSet<CaveNode>();
  public FloorGenerationParams parameters;
  public string name;

  public Floor Floor { get; private set; }
  // public Floor Floor {
  //   get {
  //     if (floor == null) {
  //       this.floor = GameModel.main.generator.generateFloor(parameters);
  //     }
  //     return floor;
  //   }
  // }

  public CaveNode(string name, FloorGenerationParams parameters) {
    this.name = name;
    this.parameters = parameters;
  }

  public Floor Realize() {
    UnityEngine.Assertions.Assert.IsNull(Floor);

    if (Floor == null) {
      Floor = GameModel.main.generator.generateFloor(parameters);
      Floor.name = name;
      foreach(var child in children) {
        AddCavePathToNode(this, child);
      }
    }
    return Floor;
  }

  public static void AddCavePathToNode(CaveNode source, CaveNode destination) {
    var floor = source.Floor;
    // find a free tile and put a downstairs there
    var tile = Util.RandomPick(
      floor.EnumerateFloor().OrderByDescending(pos => pos.x).Select(pos => floor.tiles[pos])
      .Where(t => t.CanBeOccupied() && t.grass == null && !(t is Downstairs)).Skip(2).Take(3)
    );
    var cavePath = new CavePath(tile.pos, source, destination);
    floor.Put(cavePath);
  }


  public void Connect(CaveNode other) {
    children.Add(other);
    other.parents.Add(this);
  }
}

// represents a DAG of floors.
// Each floor is a Node that has the following properties:
// 1. an ID
// 2. a Depth
// 3. A "blueprint" that the floor was generated by
// 4. possibly - an "index" for which "horizontal" location it is in the given depth
[Serializable]
public class CaveNetwork {
  public static CaveNetwork generateExample(FloorGenerator generator) {
    // basic example:
    /*       home
     *     A       B
     * C       D        E
     *         F
     */

    // EncounterBag stations = new EncounterBag {
    //   { 1, Encounters.AddProcessor },
    //   { 1, Encounters.AddComposter },
    //   { 1, Encounters.AddDesalinator },
    //   { 1, Encounters.AddSoilMixer },
    //   { 1, Encounters.AddCloner },
    // };

    var home = new CaveNode("home", new HomeFloorParams());

    var oneA = new CaveNode("1A", new SingleRoomFloorParams(generator.earlyGame, 1, 9, 7, 1, 1, extraEncounters: Encounters.AddSlimeSource));
    var oneB = new CaveNode("1B", new SingleRoomFloorParams(generator.earlyGame, 1, 9, 7, 1, 1, extraEncounters: Encounters.AddMatterSource));

    home.Connect(oneA);
    home.Connect(oneB);

    var twoA = new CaveNode("2A", new SingleRoomFloorParams(generator.earlyGame, 3, 10, 8, 2, 1, false, null,
      Encounters.AddSlimeSource
    // , stations.GetRandomAndRemove()
    ));
    var twoB = new CaveNode("2B", new SingleRoomFloorParams(generator.earlyGame, 3, 10, 8, 3, 1, false, null,
      Encounters.OneAstoria
    // , stations.GetRandomAndRemove()
    ));
    var twoC = new CaveNode("2C", new SingleRoomFloorParams(generator.earlyGame, 3, 10, 8, 2, 1, false, null,
      Encounters.AddMatterSource
    // , stations.GetRandomAndRemove()
    ));

    oneA.Connect(twoA);
    oneA.Connect(twoB);

    oneB.Connect(twoB);
    oneB.Connect(twoC);

    var threeA = new CaveNode("3A", new SingleRoomFloorParams(generator.earlyGame, 6, 11, 8, 4, 2, false, null, 
      Encounters.AddSlime
      //, stations.GetRandomAndRemove()
    ));
    twoA.Connect(threeA);

    // var threeB = new CaveNode("3A", new SingleRoomFloorParams(generator.earlyGame, 3, 9, 7, 3, 3));
    var threeB = new CaveNode("3B", new MultiRoomFloorParams(generator.earlyGame, 6, 12, 10, 6));
    twoB.Connect(threeB);

    var threeC = new CaveNode("3C", new SingleRoomFloorParams(generator.earlyGame, 6, 11, 8, 4, 2, false, null, 
      Encounters.AddOrganicMatters
      // , stations.GetRandomAndRemove()
    ));
    twoC.Connect(threeC);

    CaveNetwork network = new CaveNetwork(home);
    return network;
  }

  public HashSet<CaveNode> nodes;
  public CaveNode root;

  public CaveNetwork(CaveNode root) {
    this.root = root;
    nodes = new HashSet<CaveNode>(TraverseImpl(root));
  }

  private IEnumerable<CaveNode> TraverseImpl(CaveNode node) {
    yield return node;

    foreach (var child in node.children) {
      // careful - this will double-count
      foreach (var ancestor in TraverseImpl(child)) {
        yield return ancestor;
      }
    }
  }
}
