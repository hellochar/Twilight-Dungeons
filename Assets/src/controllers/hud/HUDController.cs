using System;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour {
  public static HUDController main;
  public GameObject
    hpBar,
    waterIndicator,
    inventoryToggle,
    inventoryContainer,
    statuses,
    depth,
    enemiesLeft,
    waitButton,
    settings,
    damageFlash
  ;
  public InventoryController playerInventory;
  public Image blackOverlay;

  public void Awake() {
    main = this;
  }

  public GameObject GetHUDGameObject(string name) {
    // use reflection to get gameObject matching name
    return (GameObject)GetType().GetField(name).GetValue(this);
  }
}