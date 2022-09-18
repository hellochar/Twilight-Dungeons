using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
public class HomeFloor : Floor {
  public HomeFloor(int width, int height) : base(0, width, height) {
  }
}