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
  public Toggle musicToggle;
  public Toggle soundEffectsToggle;

  void Start() {
    movementDropdown.SetValueWithoutNotify(ChoiceToMoveMode.IndexOf(Settings.main.moveMode));
    sidePanelToggle.SetIsOnWithoutNotify(Settings.main.showSidePanel);
    musicToggle.SetIsOnWithoutNotify(Settings.main.music);
    soundEffectsToggle.SetIsOnWithoutNotify(Settings.main.sfx);
  }

  public void BackToTitle() {
    Serializer.SaveMainToFile();
    SceneManager.LoadSceneAsync("Scenes/Intro");
  }

  public void Close() {
    gameObject.SetActive(false);
  }

  public void SetMoveMode(int choice) => Settings.Update((ref Settings s) => s.moveMode = ChoiceToMoveMode[choice]);

  public void SetSidePanel(bool on) => Settings.Update((ref Settings s) => s.showSidePanel = on);

  public void SetMusic(bool on) => Settings.Update((ref Settings s) => s.music = on);

  public void SetSoundEffects(bool on) => Settings.Update((ref Settings s) => s.sfx = on);
}