using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchTileState : MonoBehaviour {
  public Tile owner;
  private new SpriteRenderer renderer;

  // Start is called before the first frame update
  void Start() {
    this.renderer = GetComponent<SpriteRenderer>();
  }

  // Update is called once per frame
  void Update() {
    switch (owner.visiblity) {
      case TileVisiblity.Unexplored:
        renderer.enabled = false;
        break;
      case TileVisiblity.Visible:
        renderer.enabled = true;
        renderer.color = new Color32(255, 255, 255, 255);
        break;
      case TileVisiblity.Explored:
        renderer.enabled = true;
        renderer.color = new Color32(128, 128, 128, 255);
        break;
    }
  }
}
