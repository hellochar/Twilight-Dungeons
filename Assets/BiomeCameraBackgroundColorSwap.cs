using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiomeCameraBackgroundColorSwap : MonoBehaviour {
  public Camera camera;
  public Color early, mid, late;
  // Start is called before the first frame update
  void Start() {
    camera = GetComponent<Camera>();
  }

  // Update is called once per frame
  void Update() {
    var generator = GameModel.main.generator;
    if (generator.EncounterGroup == generator.earlyGame) {
      camera.backgroundColor = early;
    } else if (generator.EncounterGroup == generator.midGame) {
      camera.backgroundColor = mid;
    } else if (generator.EncounterGroup == generator.everything) {
      camera.backgroundColor = late;
    }
  }
}
