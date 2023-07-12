using System;
using UnityEngine;

public class HUDController : MonoBehaviour {
  public static HUDController main;
  public GameObject hpBar, waterIndicator, inventoryToggle, inventoryContainer, statuses, depth, enemiesLeft, waitButton, settings;

  public void Awake() {
    main = this;
  }
}