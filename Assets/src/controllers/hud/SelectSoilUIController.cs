using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectSoilUIController : MonoBehaviour {
  public GameObject selectHighlightPrefab;
  public event Action<Soil> OnSoilSelected;
  public event Action OnCancelled;

  // Start is called before the first frame update
  void Start() {
    // first, highlight all visible, unoccupied soils
    var allSoils = GameModel.main.currentFloor.tiles.Where(tile => tile is Soil && tile.isVisible && tile.CanBeOccupied()).Cast<Soil>();

    // for each soil, we want to add a highlight onto it.
    foreach (var soil in allSoils) {
      var highlight = Instantiate(selectHighlightPrefab, Util.withZ(soil.pos, -1), Quaternion.identity, transform);
      highlight.SetActive(true);
      highlight.GetComponent<Button>().onClick.AddListener(() => SoilSelected(soil));
    }
  }

  public void SoilSelected(Soil soil) {
    OnSoilSelected?.Invoke(soil);
    Destroy(gameObject);
  }

  public void Cancel() {
    OnCancelled?.Invoke();
    Destroy(gameObject);
  }
}
