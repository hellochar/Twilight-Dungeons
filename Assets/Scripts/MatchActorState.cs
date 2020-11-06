using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchActorState : MonoBehaviour {
  public Actor actor;
  public new SpriteRenderer renderer;

  // Start is called before the first frame update
  public virtual void Start() {
    if (actor == null) {
      actor = GameModel.main.player;
    }
    this.renderer = GetComponent<SpriteRenderer>();
    this.transform.position = Util.withZ(this.actor.pos);
  }

  // Update is called once per frame
  public virtual void Update() {
    // sync positions
    if (Vector2.Distance(Util.getXY(this.transform.position), this.actor.pos) > 3) {
      this.transform.position = Util.withZ(this.actor.pos, this.transform.position.z);
    } else {
      this.transform.position = Util.withZ(Vector2.Lerp(Util.getXY(this.transform.position), actor.pos, 20f * Time.deltaTime), this.transform.position.z);
    }
    // don't need this because the renderer is sprite-masked
    // if (renderer != null) {
    //   renderer.enabled = actor.visible;
    // }
  }
}
