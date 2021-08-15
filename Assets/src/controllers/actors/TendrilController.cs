using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TendrilController : ActorController {
  public Tendril tendril => (Tendril) actor;
  public Grasper grasper => tendril.owner;
  public Sprite straight, angled, end;
  SpriteRenderer sr;

  private class GrasperHandler : IActionPerformedHandler {
    TendrilController controller;

    public GrasperHandler(TendrilController controller) {
      this.controller = controller;
    }

    public void HandleActionPerformed(BaseAction final, BaseAction initial) {
      controller.UpdateSprite();
    }
  }

  public override void Start() {
    base.Start();
    sr = sprite.GetComponent<SpriteRenderer>();
    grasper.nonserializedModifiers.Add(new GrasperHandler(this));
    tendril.OnPulse += HandlePulse;
    UpdateSprite();
  }

  private void HandlePulse() {
    gameObject.AddComponent<PulseAnimation>()?.Larger();
  }

  void UpdateSprite() {
    if (tendril.IsDead) {
      return;
    }
    // pick which of the three types of connectors we are: straight, turn, or end
    var index = grasper.tendrils.IndexOf(tendril);
    var prevPos = index == 0 ? grasper.pos : grasper.tendrils[index - 1].pos;
    var nextPos = index == grasper.tendrils.Count - 1 ? null : grasper.tendrils[index + 1]?.pos;

    var prevToMe = tendril.pos - prevPos;
    /// All three sprites "come in" from the bottom. Angle base will be the final angle for a straight or end section.
    /// for a turn, we need to add angleBase and the turn angle.
    var angleBase = Vector2.SignedAngle(Vector2.up, prevToMe);

    bool isEnd = nextPos == null;
    if (isEnd) {
      sr.transform.rotation = Quaternion.Euler(0, 0, angleBase);
      sr.sprite = end;
      return;
    }

    var meToNext = nextPos.Value - tendril.pos;

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
