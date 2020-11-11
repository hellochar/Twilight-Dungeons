using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Entity {
  Guid guid {get; }
  Vector2Int pos { get; }
}
