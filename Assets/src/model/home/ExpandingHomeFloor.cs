using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class ExpandingHomeFloor : HomeFloor {
  public static ExpandingHomeFloor generate(int numFloors) {
    var startSize = new Vector2Int(6, 6);
    var finalSize = startSize + 2 * Vector2Int.one * numFloors;
    ExpandingHomeFloor floor = new ExpandingHomeFloor(finalSize.x, finalSize.y);
    foreach (var p in floor.EnumerateFloor()) {
      floor.Put(new Chasm(p));
    }

    var center = floor.center;
    var min = floor.center - startSize / 2;
    Room room0 = new Room(
      min,
      min + startSize - Vector2Int.one
    );
    floor.rooms = new List<Room>() { room0 };
    floor.root = room0;
    foreach (var p in floor.EnumerateRoom(room0)) {
      floor.Put(new HomeGround(p));
    }

    floor.startPos = new Vector2Int(room0.min.x, room0.center.y);
    // put a pit at the center
    floor.Put(new Pit(room0.center));
    Encounters.AddWater(floor, room0);

    // show chasm edges
    room0.max += Vector2Int.one;
    room0.min -= Vector2Int.one;

    return floor;
  }

  public ExpandingHomeFloor(int width, int height) : base(width, height) { }

  public void Expand() {
    foreach(var pos in this.EnumerateRoomPerimeter(root)) {
      Put(new HomeGround(pos));
    }
    root.max += Vector2Int.one;
    root.min -= Vector2Int.one;
  }

  public override void Put(Entity entity) {
    base.Put(entity);
    if (entity is HomeGround) {
      root.ExtendToEncompass(new Room(entity.pos - Vector2Int.one, entity.pos + Vector2Int.one));
      RecomputeVisibility();
      // f.RecomputeVisibility();
    }
  }
}

[Serializable]
public class HomeSection {
  public Type[,] Types { get; }
  public readonly string source;
  public HomeSection(Type[,] types, string source) {
    Types = types;
    this.source = source;
  }

  public int width => Types.GetLength(0);
  public int height => Types.GetLength(1);

  public void Blit(Floor floor, Vector2Int topLeft) {
    var bottomLeft = topLeft - new Vector2Int(0, height - 1);
    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        var pos = bottomLeft + new Vector2Int(x, y);
        if (!floor.InBounds(pos)) {
          continue;
        }

        var tileType = Types[x, y];
        var constructor = tileType.GetConstructor(new Type[] { typeof(Vector2Int) });
        var tile = (Tile)constructor.Invoke(new object[] { pos });
        floor.Put(tile);
      }
    }
  }

  public static HomeSection FromString(string source) {
    /* s will look like: @"
xx_
xxx
 x
"
    */
    source = source.Trim();
    if (source.Length == 0) {
      throw new Exception("Empty string!");
    }
    var lines = source.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

    // Text's coordinate system is +y down. Our game's coordinate system is +y up.
    // To account for this we flip the order of lines here.
    lines = lines.Reverse().ToArray();

    var width = lines.Select(line => line.Length).Max();
    var height = lines.Length;
    Type[,] Types = new Type[width, height];
    for(int y = 0; y < height; y++) {
      for (int x = 0; x < width; x++) {
        var line = lines[y];
        var curChar = line[x];
        Type tileType = null;
        switch (curChar) {
          case 'x':
            tileType = typeof(HomeGround);
            break;
          case '0':
            tileType = typeof(Wall);
            break;
          case '_':
          case ' ':
          default:
            tileType = typeof(Chasm);
            break;
        }
        Types[x, y] = tileType;
      }
    }
    return new HomeSection(Types, source);
  }

  public static HomeSection[] FromMultiString(string multiSections) {
    Regex r = new Regex("^$", RegexOptions.Multiline);
    var sections2 = r.Split(multiSections);
    var sections = multiSections.Split("====".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
    return sections.Select(section => FromString(section)).ToArray();
  }

  public static HomeSection[] StandardSections = FromMultiString(@"
xx_
xxx
_xx

====

xx
_x

====

_x_
xxx
_x_

====

_x_
xxx
__x

====

__x_
xxxx

====

xxxx
_x__

====

xxx
xx_

====

xx_
xxx

====

x__
xxx
x__
");
}