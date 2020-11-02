using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchActorPosition : MonoBehaviour {
  public Actor actor;

  // Start is called before the first frame update
  void Start() {
    if (actor == null) {
      actor = GameModel.main.player;
    }
    this.transform.position = Util.withZ(this.actor.pos);
  }

  // Update is called once per frame
  void Update() {
    // sync positions
    if (Vector2.Distance(Util.getXY(this.transform.position), this.actor.pos) > 3) {
      this.transform.position = Util.withZ(this.actor.pos, this.transform.position.z);
    } else {
      this.transform.position = Util.withZ(Vector2.Lerp(Util.getXY(this.transform.position), actor.pos, 20f * Time.deltaTime));
    }
  }
}
