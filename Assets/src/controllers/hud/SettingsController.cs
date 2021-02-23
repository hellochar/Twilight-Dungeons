using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsController : MonoBehaviour {
  private static List<MoveMode> ChoiceToMoveMode = new List<MoveMode> {
    MoveMode.DPad,
    MoveMode.TouchTile,
    MoveMode.DPad | MoveMode.TouchTile,
  };

  public TMPro.TMP_Dropdown movementDropdown;
  public Toggle sidePanelToggle;

  void Start() {
    movementDropdown.SetValueWithoutNotify(ChoiceToMoveMode.IndexOf(Settings.main.moveMode));
    sidePanelToggle.SetIsOnWithoutNotify(Settings.main.showSidePanel);
  }

  public void Restart() {
    GameModel.GenerateNewGameAndSetMain();
    SceneManager.LoadSceneAsync("Scenes/Game");
  }

  public void SaveGame() {
    Serializer.SaveMainToFile();
  }

  public void LoadGame() {
    GameModel.main = Serializer.LoadFromFile();
    Restart();
  }

  public void Close() {
    gameObject.SetActive(false);
  }

  public void SetMoveMode(int choice) {
    var newSettings = Settings.main;
    newSettings.moveMode = ChoiceToMoveMode[choice];
    Settings.Set(newSettings);
  }

  public void SetSidePanel(bool on) {
    var newSettings = Settings.main;
    newSettings.showSidePanel = on;
    Settings.Set(newSettings);
  }
}