using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchTileState : MonoBehaviour {
  public Tile owner;
  private new SpriteRenderer renderer;
  private SpriteMask mask;

  // Start is called before the first frame update
  void Start() {
    this.renderer = GetComponent<SpriteRenderer>();
    this.mask = GetComponent<SpriteMask>();
  }

  // Update is called once per frame
  void Update() {
    switch (owner.visiblity) {
      case TileVisiblity.Unexplored:
        renderer.enabled = false;
        renderer.color = Color.black;
        if (mask != null) { mask.enabled = false; }
        break;
      case TileVisiblity.Visible:
        if (mask != null) { mask.enabled = true; }
        renderer.enabled = true;
        renderer.color = Color.Lerp(renderer.color, Color.white, 0.2f);
        break;
      case TileVisiblity.Explored:
        if (mask != null) { mask.enabled = true; }
        renderer.enabled = true;
        renderer.color = Color.Lerp(renderer.color, exploredMask, 0.2f);
        break;
    }
  }

  private static Color exploredMask = new Color(1, 1, 1, 0.3f);
}
