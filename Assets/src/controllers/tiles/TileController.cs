using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TileController : MonoBehaviour, IEntityController, IPlayerInteractHandler {
  [NonSerialized]
  public Tile tile;
  public SpriteRenderer[] renderers;
  public SpriteMask maskVisible;
  public SpriteMask maskExplored;

  TileVisiblity lastTileVisibility;

  // Start is called before the first frame update
  public virtual void Start() {
    SetVisibilityAndMasks();
  }

  public void SetVisibilityAndMasks() {
    switch (tile.visibility) {
      case TileVisiblity.Unexplored:
        foreach (var renderer in renderers) {
          renderer.enabled = false;
          // unexploredMask.a = renderer.color.a;
          // renderer.color = unexploredMask;
        }
        maskVisible.enabled = false;
        maskExplored.enabled = false;
        break;
      case TileVisiblity.Visible:
        foreach (var renderer in renderers) {
          renderer.enabled = true;
          // visibleMask.a = renderer.color.a;
          // renderer.color = visibleMask;
          // renderer.color = Color.Lerp(renderer.color, visibleMask, 0.2f);
        }
        maskVisible.enabled = true;
        maskExplored.enabled = false;
        break;
      case TileVisiblity.Explored:
        foreach (var renderer in renderers) {
          renderer.enabled = true;
          // exploredMask.a = renderer.color.a;
          // renderer.color = exploredMask;
          // renderer.color = Color.Lerp(renderer.color, exploredMask, 0.2f);
        }
        maskVisible.enabled = false;
        maskExplored.enabled = true;
        break;
    }
    lastTileVisibility = tile.visibility;
  }

  // Update is called once per frame
  public virtual void Update() {
    if (tile.visibility != lastTileVisibility) {
      SetVisibilityAndMasks();
    }
  }

  public virtual PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    if (tile.visibility != TileVisiblity.Unexplored) {
      return new SetTasksPlayerInteraction(
        new MoveToTargetTask(GameModel.main.player, tile.pos)
      );
    }
    return null;
  }

  private static Color unexploredMask = new Color(0, 0, 0);
  // private static Color exploredMask = new Color(0.75f, 0.75f, 0.85f);
  // private static Color visibleMask = new Color(1, 1, 1);
}
