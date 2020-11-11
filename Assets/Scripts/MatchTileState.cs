using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MatchTileState : MonoBehaviour, IPointerClickHandler {
  public Tile owner;
  private SpriteRenderer[] renderers;
  private SpriteMask mask;
  private int sortingLayerEntity; 
  private int sortingLayerDefault;

  // Start is called before the first frame update
  void Start() {
    sortingLayerEntity = SortingLayer.NameToID("Entity");
    sortingLayerDefault = SortingLayer.NameToID("Default");
    this.renderers = GetComponentsInChildren<SpriteRenderer>();
    this.mask = GetComponent<SpriteMask>();
  }

  // Update is called once per frame
  void Update() {
    switch (owner.visiblity) {
      case TileVisiblity.Unexplored:
        foreach (var renderer in renderers) {
          renderer.enabled = false;
          renderer.color = Color.black;
        }
        if (mask != null) {
          mask.enabled = false;
        }
        break;
      case TileVisiblity.Visible:
        if (mask != null) {
          mask.enabled = true;
          // show all
          mask.backSortingLayerID = sortingLayerDefault;
        }
        foreach (var renderer in renderers) {
          renderer.enabled = true;
          renderer.color = Color.Lerp(renderer.color, Color.white, 0.2f);
        }
        break;
      case TileVisiblity.Explored:
        if (mask != null) {
          mask.enabled = true;
          // don't show entities
          mask.backSortingLayerID = sortingLayerEntity;
        }
        foreach (var renderer in renderers) {
          renderer.enabled = true;
          renderer.color = Color.Lerp(renderer.color, exploredMask, 0.2f);
        }
        break;
    }
  }

  public void OnPointerClick(PointerEventData pointerEventData) {
    GameModel.main.player.action = new MoveToTargetAction(GameModel.main.player, owner.pos);
  }

  private static Color exploredMask = new Color(1, 1, 1, 0.3f);
}
