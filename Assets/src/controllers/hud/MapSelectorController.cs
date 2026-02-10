using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapSelectorController : MonoBehaviour {
  public GameObject selectHighlightPrefab;
  public event Action<Entity> OnSelected;
  public event Action OnCancelled;
  public List<Entity> entities;
  public string message;
  public Sprite previewSprite;
  private GameObject hud;
  private GameObject messageGameObject;

  // Preview state
  private Entity previewEntity;
  private GameObject previewSpriteObject;
  private GameObject confirmButtonObject;

  // Start is called before the first frame update
  void Start() {
    // for each entity, we want to add a highlight onto it.
    foreach (var e in entities) {
      var highlight = Instantiate(selectHighlightPrefab, Util.withZ(e.pos, -1), Quaternion.identity, transform);
      highlight.SetActive(true);
      highlight.GetComponent<Button>().onClick.AddListener(() => PreviewTarget(e));
    }

    // show message
    if (message != null && message.Length > 0) {
      messageGameObject = PrefabCache.UI.Instantiate("Map Selector Message");
      messageGameObject.transform.SetParent(GameObject.Find("Canvas").transform, false);
      var textComponent = messageGameObject.GetComponentInChildren<TMPro.TMP_Text>();
      textComponent.text = message;
    }

    // hide UI
    this.hud = GameObject.Find("HUD");
    hud.SetActive(false);
  }

  void OnDestroy() {
    hud.SetActive(true);
    Destroy(messageGameObject);
    // confirmButtonObject is on the Canvas, not a child of this object
    if (confirmButtonObject != null) {
      Destroy(confirmButtonObject);
    }
  }

  private void PreviewTarget(Entity e) {
    // Double-tap the same target confirms the selection
    if (previewEntity == e) {
      ConfirmSelection();
      return;
    }

    previewEntity = e;

    // Clean up old preview sprite
    if (previewSpriteObject != null) {
      Destroy(previewSpriteObject);
    }

    // Show preview sprite at target position
    if (previewSprite != null) {
      previewSpriteObject = new GameObject("TargetPreview");
      previewSpriteObject.transform.SetParent(transform);
      previewSpriteObject.transform.position = Util.withZ(e.pos, -0.5f);
      var sr = previewSpriteObject.AddComponent<SpriteRenderer>();
      sr.sprite = previewSprite;
      // Match the sorting layer of the highlight prefabs so the preview is visible
      var highlightRenderer = selectHighlightPrefab.GetComponent<SpriteRenderer>();
      sr.sortingLayerID = highlightRenderer.sortingLayerID;
      sr.sortingOrder = highlightRenderer.sortingOrder + 1;
      sr.color = new Color(1f, 1f, 1f, 0.75f);
    }

    // Show confirm button if not already shown
    if (confirmButtonObject == null) {
      ShowConfirmButton();
    }
  }

  private void ShowConfirmButton() {
    var canvas = GameObject.Find("Canvas").transform;
    confirmButtonObject = Instantiate(PrefabCache.UI.GetPrefabFor("Action Button"), canvas);
    confirmButtonObject.GetComponentInChildren<TMPro.TMP_Text>().text = "Confirm";
    confirmButtonObject.GetComponent<Button>().onClick.AddListener(ConfirmSelection);

    // Position at bottom center of screen
    var rt = confirmButtonObject.GetComponent<RectTransform>();
    rt.anchorMin = new Vector2(0.5f, 0);
    rt.anchorMax = new Vector2(0.5f, 0);
    rt.pivot = new Vector2(0.5f, 0);
    rt.anchoredPosition = new Vector2(0, 80);
    // Make it wider for easy tapping
    rt.sizeDelta = new Vector2(160, 48);
  }

  private void ConfirmSelection() {
    if (previewEntity != null) {
      Selected(previewEntity);
    }
  }

  public void Selected(Entity e) {
    hud.SetActive(true);
    OnSelected?.Invoke(e);
    Destroy(gameObject);
  }

  public void Cancel() {
    hud.SetActive(true);
    OnCancelled?.Invoke();
    Destroy(gameObject);
  }
}
