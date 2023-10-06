using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TileGroup {
  public static HashSet<Tile> makeGroupAndRemove(Tile start, ref HashSet<Tile> allTiles) {
    var set = new HashSet<Tile>(start.floor.BreadthFirstSearch(start.pos, allTiles.Contains));
    allTiles.ExceptWith(set);
    return set;
  }

  public static List<HashSet<Tile>> partitionIntoDisjointGroups(HashSet<Tile> allTiles) {
    List<HashSet<Tile>> allGroups = new List<HashSet<Tile>>();
    int guard = 0;
    while (allTiles.Any() && (guard++ < 99)) {
      var group = makeGroupAndRemove(allTiles.First(), ref allTiles);
      allGroups.Add(group);
    }
    return allGroups;
  }

  public static Tile getCenterTile(IEnumerable<Tile> tiles) {
    var centroid = new Vector2(
      tiles.Sum(t => t.pos.x) / (float) tiles.Count(),
      tiles.Sum(t => t.pos.y) / (float) tiles.Count()
    );

    var tileClosestToCentroid = tiles.OrderBy(t => (t.pos - centroid).sqrMagnitude).FirstOrDefault();

    return tileClosestToCentroid;
  }
}
