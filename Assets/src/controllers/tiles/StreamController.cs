using System;
using UnityEngine;

public class StreamController : TileController {
  Stream stream => (Stream) tile;
  public Sprite straight, angled, end;

  public override void Start() {
    base.Start();
    var animator = GetComponent<Animator>();
    // float time = stream.pos.x / 5f + stream.pos.y / 4.6f;
    // animator.Play("Idle", -1, time % 1f);

    UpdateSprite();
  }

  private void UpdateSprite() {
    // pick which of the three types of connectors we are: straight, turn, or end
    var sr = renderers[0];

    bool isStart = stream.prevPos == null;
    if (isStart) {
      // the end sprite comes from the bottom but we want to align it with the next tile.
      var nextToMe = stream.pos - stream.nextPos.Value;
      var angle = Vector2.SignedAngle(Vector2.up, nextToMe);
      sr.transform.rotation = Quaternion.Euler(0, 0, angle);
      sr.sprite = end;
      return;
    }

    var prevToMe = stream.pos - stream.prevPos.Value;
    /// All three sprites "come in" from the bottom. Angle base will be the final angle for a straight or end section.
    /// for a turn, we need to add angleBase and the turn angle.
    var angleBase = Vector2.SignedAngle(Vector2.up, prevToMe);

    bool isEnd = stream.nextPos == null;
    if (isEnd) {
      sr.transform.rotation = Quaternion.Euler(0, 0, angleBase);
      sr.sprite = end;
      return;
    }

    var meToNext = stream.nextPos.Value - stream.pos;

    bool isStraight = prevToMe == meToNext;
    if (isStraight) {
      sr.transform.rotation = Quaternion.Euler(0, 0, angleBase);
      sr.sprite = straight;
    } else {
      // this is where next "should" be to match the turn in the sprite (which goes bottom -> right).
      // This unity method does a left turn, so we negate it to get a right turn.
      var spriteTurnAngle = -Vector2.Perpendicular(prevToMe);

      float finalAngle;
      if (meToNext == spriteTurnAngle) {
        // we're lined up already!
        finalAngle = angleBase;
      } else {
        // we know we're not straight so the only case left is that the next tendril is
        // to the LEFT.
        // To handle this case we take advantage of the fact that the Sprite's start and end boundaries
        // look the same. So we can just hard code in a -90 degree shift and it'll look fine.
        finalAngle = angleBase - 90;
      }
      // it's a turn. Turn sprite goes from below to right by default
      sr.transform.rotation = Quaternion.Euler(0, 0, finalAngle);
      sr.sprite = angled;
    }
  }
}
