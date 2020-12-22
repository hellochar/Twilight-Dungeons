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

  // Start is called before the first frame update
  void Start() {
    // first, highlight all visible, unoccupied soils

    // for each soil, we want to add a highlight onto it.
    foreach (var e in entities) {
      var highlight = Instantiate(selectHighlightPrefab, Util.withZ(e.pos, -1), Quaternion.identity, transform);
      highlight.SetActive(true);
      highlight.GetComponent<Button>().onClick.AddListener(() => Selected(e));
    }
  }

  public void Selected(Entity e) {
    OnSelected?.Invoke(e);
    Destroy(gameObject);
  }

  public void Cancel() {
    OnCancelled?.Invoke();
    Destroy(gameObject);
  }
}
