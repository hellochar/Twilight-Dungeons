using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TileController : MonoBehaviour, IPointerClickHandler {
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
    Update();
  }

  // Update is called once per frame
  void Update() {
    switch (owner.visibility) {
      case TileVisiblity.Unexplored:
        foreach (var renderer in renderers) {
          renderer.enabled = false;
          unexploredMask.a = renderer.color.a;
          renderer.color = unexploredMask;
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
          visibleMask.a = renderer.color.a;
          renderer.color = Color.Lerp(renderer.color, visibleMask, 0.2f);
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
          exploredMask.a = renderer.color.a;
          renderer.color = Color.Lerp(renderer.color, exploredMask, 0.2f);
        }
        break;
    }
  }

  public virtual void OnPointerClick(PointerEventData pointerEventData) {
    if (owner.visibility != TileVisiblity.Unexplored) {
      GameModel.main.player.task = new MoveToTargetTask(GameModel.main.player, owner.pos);
    }
  }

  private static Color unexploredMask = new Color(0, 0, 0);
  private static Color exploredMask = new Color(0.3f, 0.3f, 0.3f);
  private static Color visibleMask = new Color(1, 1, 1);
}
