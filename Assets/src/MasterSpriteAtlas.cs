using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public static class MasterSpriteAtlas {
  private static SpriteAtlas _spriteAtlas;
  public static SpriteAtlas atlas {
    get {
      if (_spriteAtlas == null) {
        _spriteAtlas = Resources.Load<SpriteAtlas>("Master Sprite Atlas");
      }
      return _spriteAtlas;
    }
  }
}