using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MatchAudioMixerSettings : MonoBehaviour {
  public AudioMixer mixer;
  public static MatchAudioMixerSettings instance;

  void Start() {
    /// hacky way to ensure there's only one of these. Each scene should have one MixerMatchSettings object.
    if (instance != null) {
      Destroy(this.gameObject);
      return;
    }
    DontDestroyOnLoad(this.gameObject);
    instance = this;
    Settings.OnChanged += MatchSettings;
    MatchSettings();
  }

  private void MatchSettings() {
    mixer.SetFloat("musicVolume", Settings.main.music ? 0 : -80);
    mixer.SetFloat("sfxVolume", Settings.main.sfx ? 0 : -80);
  }
}
