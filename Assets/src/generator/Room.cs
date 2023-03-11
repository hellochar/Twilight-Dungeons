
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = MyRandom;

/// Represents a room in a floor; internally implements a BSP tree
[System.Serializable]
public class Room {
  /// rooms are at least 3x3
  public readonly static int MIN_ROOM_SIZE = 3;

  /// max is inclusive
  public Vector2Int min, max;

  public RoomSplit? split;
  public Room parent;
  internal bool isRoot => this.parent == null;

  public bool isTerminal => this.split == null;
  /// Only used in terminal rooms; get other rooms that this one is connected to.
  public List<Room> connections = new List<Room>();

  public Vector2Int center => (min + max) / 2;
  public Vector2 centerFloat => new Vector2((min.x + max.x) / 2.0f, (min.y + max.y) / 2.0f);
  public Rect rect => new Rect(min, new Vector2(width, height));
  public string name;

  public override string ToString() {
    return name ?? $"{min} to {max}";
  }

  public Room(Room parent, Vector2Int min, Vector2Int max) {
    this.parent = parent;
    this.min = min;
    this.max = max;
  }

  public Room(Vector2Int min, Vector2Int max) : this(null, min, max) { }

  public Room(Floor floor) : this(
    new Vector2Int(1, 1),
    new Vector2Int(floor.width - 2, floor.height - 2)) { }

  public int width {
    get {
      // add one because max is inclusive
      return max.x - min.x + 1;
    }
  }

  public int height {
    get {
      // add one because max is inclusive
      return max.y - min.y + 1;
    }
  }

  public void randomlyShrink() {
    if (!this.isTerminal) {
      throw new System.Exception("Tried shinking a non-terminal BSPNode.");
    }
    // randomly decide a new width and height that's within the alloted space
    // 5
    int roomWidth = Random.Range(Room.MIN_ROOM_SIZE, width + 1);
    int roomHeight = Random.Range(Room.MIN_ROOM_SIZE, height + 1);
    // // shrink at least 1 in each dimension
    // int roomWidth = Random.Range(Room.MIN_ROOM_SIZE, width - 1);
    // int roomHeight = Random.Range(Room.MIN_ROOM_SIZE, height - 1);

    // min.x = 1, max.x = 5, 5 - 5 + 1 = 1
    int startX = Random.Range(min.x, max.x - roomWidth + 1);
    int startY = Random.Range(min.y, max.y - roomHeight + 1);

    this.min = new Vector2Int(startX, startY);

    // subtract 1 from width/height since max is inclusive
    this.max = new Vector2Int(startX + roomWidth - 1, startY + roomHeight - 1);
  }

  internal void Shrink(int amount) {
    this.min += Vector2Int.one * amount;
    this.max -= Vector2Int.one * amount;
  }

  public bool randomlySplit() {
    if (this.isTerminal) {
      return this.doSplit();
    } else {
      // randomly pick a child and split it. If not successful, try the other one.
      Room a = this.split.Value.a;
      Room b = this.split.Value.b;

      var (firstChoice, secondChoice) = Random.value < 0.5 ? (a, b) : (b, a);

      if (firstChoice.randomlySplit()) {
        return true;
      } else {
        return secondChoice.randomlySplit();
      }
    }
  }

  private bool canSplitVertical {
    get {
      // to split a room, we'd need at minimum for each room to be the split size, plus 1 unit space between them
      return height >= (MIN_ROOM_SIZE * 2 + 1);
    }
  }

  private bool canSplitHorizontal {
    get {
      return width >= (MIN_ROOM_SIZE * 2 + 1);
    }
  }

  private bool canSplit {
    get {
      if (isTerminal) {
        return canSplitVertical || canSplitHorizontal;
      }
      return isTerminal && (canSplitVertical || canSplitHorizontal);
    }
  }

  public int depth => parent == null ? 0 : parent.depth + 1;

  private bool doSplit() {
    if (!this.isTerminal) {
      throw new System.Exception("Attempted to call doSplit() on a BSPNode that is already split!");
    }
    // we are too small of a room; exit
    if (!canSplitVertical && !canSplitHorizontal) {
      return false;
    } else if (canSplitVertical && !canSplitHorizontal) {
      doSplitVertical();
      return true;
    } else if (!canSplitVertical && canSplitHorizontal) {
      doSplitHorizontal();
      return true;
    } else {
      // last case - both are possible
      // split the larger dimension
      var chanceToBeHorizontal = (float) width / (width + height);
      if (Random.value < chanceToBeHorizontal) {
      // if (Random.value < 0.5) {
        doSplitHorizontal();
      } else {
        doSplitVertical();
      }
      return true;
    }
  }

  private void doSplitHorizontal() {
    // e.g. range is [0, 11]. We can split anywhere from 3-8
    int splitMax = max.x - MIN_ROOM_SIZE;
    int splitMin = min.x + MIN_ROOM_SIZE;
    int splitPoint = Random.Range(splitMin, splitMax);
    // int splitPoint = (splitMax + splitMin) / 2;
    Room a = new Room(this, this.min, new Vector2Int(splitPoint - 1, this.max.y));
    // add one tile of space between rooms
    Room b = new Room(this, new Vector2Int(splitPoint + 1, this.min.y), this.max);
    this.split = new RoomSplit(a, b, SplitDirection.Horizontal, splitPoint);
  }

  private void doSplitVertical() {
    int splitMax = max.y - MIN_ROOM_SIZE;
    int splitMin = min.y + MIN_ROOM_SIZE;
    int splitPoint = Random.Range(splitMin, splitMax);
    // int splitPoint = (splitMax + splitMin) / 2;
    Room a = new Room(this, this.min, new Vector2Int(this.max.x, splitPoint - 1));
    Room b = new Room(this, new Vector2Int(this.min.x, splitPoint + 1), this.max);
    this.split = new RoomSplit(a, b, SplitDirection.Vertical, splitPoint);
  }

  public IEnumerable<Room> Traverse() {
    yield return this;
    if (!isTerminal) {
      foreach (var r in split.Value.a.Traverse()) {
        yield return r;
      }
      foreach (var r in split.Value.b.Traverse()) {
        yield return r;
      }
    }
  }

  internal Vector2Int getCenter() {
    return (this.max + this.min) / 2;
  }

  internal Vector2Int getTopLeft() {
    return new Vector2Int(this.min.x, this.max.y);
  }

  public bool Contains(Vector2Int pos) {
    return pos.x >= min.x && pos.x <= max.x &&
           pos.y >= min.y && pos.y <= max.y;
  }

  internal void ExtendToEncompass(Room room) {
    min = Vector2Int.Min(min, room.min);
    max = Vector2Int.Max(max, room.max);
  }

  // how far away is the point to the closest point in this room?
  public int DistanceTo(Vector2Int pos) {
    if (Contains(pos)) {
      return 0;
    }
    // one will be negative, one will be positive - take the positive one
    int distanceX = Math.Max(-(pos.x - min.x), pos.x - max.x);
    int distanceY = Math.Max(-(pos.y - min.y), pos.y - max.y);
    return Math.Max(distanceX, distanceY);
  }
}

[System.Serializable]
public struct RoomSplit {

  public RoomSplit(Room a, Room b, SplitDirection direction, int coordinate) {
    this.a = a;
    this.b = b;
    this.direction = direction;
    this.coordinate = coordinate;
  }
  public Room a { get; }
  public Room b { get; }
  public SplitDirection direction { get; }
  public int coordinate { get; }
}

[System.Serializable]
public enum SplitDirection { Vertical, Horizontal }

