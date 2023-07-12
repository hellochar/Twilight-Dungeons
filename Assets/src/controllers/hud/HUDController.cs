using System;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour {
  public static HUDController main;
  public GameObject hpBar, waterIndicator, inventoryToggle, inventoryContainer, statuses, depth, enemiesLeft, waitButton, settings;
  public Image blackOverlay;

  public void Awake() {
    main = this;
  }
}