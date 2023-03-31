using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Stream : Tile {
  private readonly List<Vector2Int> poses;
  private int index;

  public Vector2Int? prevPos => index == 0 ? (null as Vector2Int?) : poses[index - 1];
  public Vector2Int? nextPos => index == poses.Count() - 1 ? (null as Vector2Int?) : poses[index + 1];

  public Stream(List<Vector2Int> poses, int index) : base(poses[index]) {
    this.poses = poses;
    this.index = index;
  }
}