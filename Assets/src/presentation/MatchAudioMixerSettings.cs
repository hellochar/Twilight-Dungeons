using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MatchAudioMixerSettings : MonoBehaviour {
  public AudioMixer mixer;

  /// Each scene should have one MixerMatchSettings object.
  void Start() {
    Settings.OnChanged += MatchSettings;
    MatchSettings();
  }

  void OnDestroy() {
    Settings.OnChanged -= MatchSettings;
  }

  private void MatchSettings() {
    mixer.SetFloat("musicVolume", Settings.main.music ? 0 : -80);
    mixer.SetFloat("sfxVolume", Settings.main.sfx ? 0 : -80);
  }
}
