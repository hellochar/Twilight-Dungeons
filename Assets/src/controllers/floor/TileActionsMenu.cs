using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// In-world popover tooltip that shows contextual actions when the player taps a tile.
/// Appears as a small floating menu above the tapped tile with action buttons.
/// </summary>
public class TileActionsMenu : MonoBehaviour {
  private static TileActionsMenu _instance;
  private static TileActionsMenu instance {
    get {
      if (_instance == null) {
        var go = new GameObject("TileActionsMenu");
        _instance = go.AddComponent<TileActionsMenu>();
      }
      return _instance;
    }
  }

  private GameObject panel;
  private Vector2Int targetPos;
  private CanvasGroup canvasGroup;

  public static bool IsShowing => _instance != null && _instance.panel != null;

  public static bool IsShowingForPos(Vector2Int pos) {
    return IsShowing && _instance.targetPos == pos;
  }

  public static void Show(Vector2Int pos, List<(string name, Action action)> actions) {
    instance.ShowMenu(pos, actions);
  }

  public static void Hide() {
    if (_instance != null) {
      _instance.HideMenu();
    }
  }

  private void ShowMenu(Vector2Int pos, List<(string name, Action action)> actions) {
    HideMenu();
    if (actions.Count == 0) return;

    targetPos = pos;

    var canvasObj = GameObject.Find("Canvas");
    if (canvasObj == null) return;

    // Create the panel container
    panel = new GameObject("TileActionsPanel");
    panel.transform.SetParent(canvasObj.transform, false);
    var panelRect = panel.AddComponent<RectTransform>();

    // Background with rounded feel (dark semi-transparent)
    var bg = panel.AddComponent<Image>();
    bg.color = new Color(0.08f, 0.06f, 0.12f, 0.93f);

    // Vertical layout for stacked buttons
    var layout = panel.AddComponent<VerticalLayoutGroup>();
    layout.spacing = 2;
    layout.padding = new RectOffset(3, 3, 3, 3);
    layout.childAlignment = TextAnchor.MiddleCenter;
    layout.childForceExpandWidth = true;
    layout.childForceExpandHeight = false;

    var fitter = panel.AddComponent<ContentSizeFitter>();
    fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

    // CanvasGroup for fade animation
    canvasGroup = panel.AddComponent<CanvasGroup>();

    // Add action buttons
    foreach (var (name, action) in actions) {
      CreateActionButton(name, action, panel.transform);
    }

    // Position at tile
    UpdatePosition();

    // Animate in - scale up and fade in
    canvasGroup.alpha = 0;
    panel.transform.localScale = Vector3.one * 0.5f;
    StartCoroutine(AnimateIn());
  }

  private IEnumerator AnimateIn() {
    float duration = 0.12f;
    float start = Time.time;
    while (Time.time - start < duration) {
      float t = (Time.time - start) / duration;
      // ease out
      t = 1f - (1f - t) * (1f - t);
      if (canvasGroup != null) {
        canvasGroup.alpha = t;
      }
      if (panel != null) {
        panel.transform.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one, t);
      }
      yield return null;
    }
    if (canvasGroup != null) canvasGroup.alpha = 1;
    if (panel != null) panel.transform.localScale = Vector3.one;
  }

  private void CreateActionButton(string name, Action action, Transform parent) {
    var btnObj = new GameObject(name);
    btnObj.transform.SetParent(parent, false);

    // Background
    var btnBg = btnObj.AddComponent<Image>();
    btnBg.color = new Color(0.2f, 0.17f, 0.28f, 1f);

    // Layout element for sizing
    var layoutElem = btnObj.AddComponent<LayoutElement>();
    layoutElem.minWidth = 70;
    layoutElem.minHeight = 24;
    layoutElem.preferredHeight = 24;

    // Button component
    var btnComponent = btnObj.AddComponent<Button>();
    var colors = btnComponent.colors;
    colors.normalColor = new Color(0.2f, 0.17f, 0.28f, 1f);
    colors.highlightedColor = new Color(0.35f, 0.3f, 0.48f, 1f);
    colors.pressedColor = new Color(0.45f, 0.4f, 0.6f, 1f);
    btnComponent.colors = colors;

    // Text
    var textObj = new GameObject("Text");
    textObj.transform.SetParent(btnObj.transform, false);
    var textRect = textObj.AddComponent<RectTransform>();
    textRect.anchorMin = Vector2.zero;
    textRect.anchorMax = Vector2.one;
    textRect.offsetMin = new Vector2(6, 0);
    textRect.offsetMax = new Vector2(-6, 0);

    var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
    text.text = name;
    text.fontSize = 13;
    text.alignment = TMPro.TextAlignmentOptions.Center;
    text.color = new Color(0.9f, 0.87f, 0.95f, 1f);

    // On click - perform action and close menu
    Action capturedAction = action;
    btnComponent.onClick.AddListener(() => {
      HideMenu();
      capturedAction?.Invoke();
    });
  }

  private void UpdatePosition() {
    if (panel == null || Camera.main == null) return;

    // Position above the tile
    Vector3 worldPos = new Vector3(targetPos.x, targetPos.y + 0.65f, 0);
    Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

    var panelRect = panel.GetComponent<RectTransform>();
    panelRect.position = screenPos;

    // Pivot from bottom center so it grows upward from the tile
    panelRect.pivot = new Vector2(0.5f, 0f);

    // Clamp to screen bounds
    ClampToScreen(panelRect);
  }

  private void ClampToScreen(RectTransform rect) {
    Vector3 pos = rect.position;
    // Force layout rebuild to get accurate size
    Canvas.ForceUpdateCanvases();

    float halfWidth = rect.rect.width * 0.5f;
    float height = rect.rect.height;
    float padding = 4f;

    pos.x = Mathf.Clamp(pos.x, halfWidth + padding, Screen.width - halfWidth - padding);
    pos.y = Mathf.Clamp(pos.y, padding, Screen.height - height - padding);

    rect.position = pos;
  }

  void Update() {
    if (panel != null) {
      UpdatePosition();
    }
  }

  private void HideMenu() {
    if (panel != null) {
      Destroy(panel);
      panel = null;
    }
    canvasGroup = null;
  }

  void OnDestroy() {
    HideMenu();
    if (_instance == this) _instance = null;
  }

  // --- Static helpers to build contextual actions for a tile position ---

  /// <summary>
  /// Build the list of actions for a given tile position based on what entities are there.
  /// </summary>
  public static List<(string name, Action action)> BuildActionsForTile(
    Vector2Int pos,
    FloorController floorController,
    PointerEventData pointerEventData
  ) {
    var actions = new List<(string name, Action action)>();
    var player = GameModel.main.player;
    var floor = floorController.floor;

    // Can't interact with out-of-bounds or unexplored tiles
    if (!floor.InBounds(pos)) return actions;
    var tile = floor.tiles[pos];
    if (tile == null || tile.visibility == TileVisiblity.Unexplored) return actions;

    var entities = floorController.GetVisibleEntitiesInLayerOrder(pos);

    // Get the top interactable entity and its handler
    floorController.TryGetFirstControllerComponent<IPlayerInteractHandler>(
      entities, out var handler, out var topEntity
    );

    // If tapping on the player themselves
    if (topEntity is Player) {
      // Check for IOnTopActionHandler (e.g. standing on item, stairs)
      if (floorController.TryGetFirstControllerComponent<IOnTopActionHandler>(
        entities, out var onTopHandler, out var onTopEntity)) {
        actions.Add((onTopHandler.OnTopActionName, () => {
          onTopHandler.HandleOnTopAction();
        }));
      }
      actions.Add(("Wait", () => {
        player.SetTasks(new WaitTask(player, 1));
      }));
      AddInspectAction(actions, topEntity);
      return actions;
    }

    // Determine default action label and add it
    if (handler != null && topEntity != null) {
      var interaction = handler.GetPlayerInteraction(pointerEventData);
      if (interaction != null && !(interaction is ArbitraryPlayerInteraction)) {
        string label = GetActionLabel(topEntity, player);
        actions.Add((label, () => interaction.Perform()));
      }
    }

    // Add inspect action for the top-most entity (but not for plain tiles)
    var inspectEntity = entities.FirstOrDefault();
    if (inspectEntity != null && !(inspectEntity is Tile)) {
      AddInspectAction(actions, inspectEntity);
    }

    return actions;
  }

  private static void AddInspectAction(List<(string name, Action action)> actions, Entity entity) {
    actions.Add(("Inspect", () => {
      EntityPopup.Show(entity);
    }));
  }

  private static string GetActionLabel(Entity entity, Player player) {
    if (entity is Actor actor) {
      if (actor.faction == Faction.Enemy || actor.faction == Faction.Neutral) {
        return "Attack";
      }
      if (actor.faction == Faction.Ally) {
        return player.IsNextTo(actor) ? "Swap" : "Move To";
      }
    }
    if (entity is Body) {
      return "Attack";
    }
    if (entity is ItemOnGround) {
      return "Pick Up";
    }
    if (entity is Grass) {
      return "Cut";
    }
    // Default for tiles
    return "Move";
  }
}
