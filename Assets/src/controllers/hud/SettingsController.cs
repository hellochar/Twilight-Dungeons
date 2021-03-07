using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsController : MonoBehaviour {
  public Toggle sidePanelToggle;
  public Toggle musicToggle;
  public Toggle soundEffectsToggle;

  void Start() {
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

  public void SetSidePanel(bool on) => Settings.Update((ref Settings s) => s.showSidePanel = on);

  public void SetMusic(bool on) => Settings.Update((ref Settings s) => s.music = on);

  public void SetSoundEffects(bool on) => Settings.Update((ref Settings s) => s.sfx = on);
}