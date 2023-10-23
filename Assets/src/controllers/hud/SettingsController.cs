using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsController : MonoBehaviour {
  // public Toggle righthandedToggle;
  public Toggle sidePanelToggle;
  public Toggle musicToggle;
  public Toggle soundEffectsToggle;
  public Toggle dPadToggle;
  public Toggle swipeToggle;

  void Start() {
    // righthandedToggle.SetIsOnWithoutNotify(Settings.main.rightHanded);
    // sidePanelToggle.SetIsOnWithoutNotify(Settings.main.showSidePanel);
    musicToggle.SetIsOnWithoutNotify(Settings.main.music);
    soundEffectsToggle.SetIsOnWithoutNotify(Settings.main.sfx);
    // dPadToggle.SetIsOnWithoutNotify(Settings.main.useDPad);
    // swipeToggle.SetIsOnWithoutNotify(Settings.main.swipeToMove);
  }

  public void BackToTitle() {
    Serializer.SaveMainToFile();
    SceneManager.LoadSceneAsync("Scenes/Intro");
  }

  public void Close() {
    gameObject.SetActive(false);
  }

  // public void SetRighthanded(bool on) => Settings.Update((ref Settings s) => s.rightHanded = on);
  // public void SetSidePanel(bool on) => Settings.Update((ref Settings s) => s.showSidePanel = on);
  public void SetMusic(bool on) => Settings.Update((ref Settings s) => s.music = on);
  public void SetSoundEffects(bool on) => Settings.Update((ref Settings s) => s.sfx = on);
  // public void SetUseDPad(bool on) => Settings.Update((ref Settings s) => s.useDPad = on);
  // public void SetSwipeToMove(bool on) => Settings.Update((ref Settings s) => s.swipeToMove = on);
}