using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HideAtHomeOption { Hide, Show }

public class HideAtHome : MonoBehaviour {
  public HideAtHomeOption option = HideAtHomeOption.Hide;
  void Start() {
    GameModel.main.home.OnEntityAdded += UpdateVisibility;
    GameModel.main.home.OnEntityRemoved += UpdateVisibility;
    UpdateVisibility(GameModel.main.player);
  }

  void OnDestroy() {
    GameModel.main.home.OnEntityAdded -= UpdateVisibility;
    GameModel.main.home.OnEntityRemoved -= UpdateVisibility;
  }

  void UpdateVisibility(Entity e) {
    if (e == GameModel.main.player) {
      var isAtHome = e.floor == GameModel.main.home;
      var isActive = option == HideAtHomeOption.Show ? isAtHome : !isAtHome;
      gameObject.SetActive(isActive);
    }
  }
}
