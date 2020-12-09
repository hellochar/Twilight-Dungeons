using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MatchGrassState : MonoBehaviour {
  public Grass grass;

  // Start is called before the first frame update
  public virtual void Start() {
    this.transform.position = Util.withZ(this.grass.pos, this.transform.position.z);
    grass.OnNoteworthyAction += HandleNoteworthyAction;
  }

  private void HandleNoteworthyAction() {
    gameObject.AddComponent<PulseAnimation>();
  }
}
