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
  public IEnumerable<Entity> entities;
  public string message;
  private GameObject hud;
  private GameObject messageGameObject;

  // Start is called before the first frame update
  void Start() {
    // for each entity, we want to add a highlight onto it.
    foreach (var e in entities) {
      var highlight = Instantiate(selectHighlightPrefab, Util.withZ(e.pos, -1), Quaternion.identity, transform);
      highlight.SetActive(true);
      highlight.GetComponent<Button>().onClick.AddListener(() => Selected(e));
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
