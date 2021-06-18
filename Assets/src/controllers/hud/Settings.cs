using UnityEngine;
using System;
using UnityEngine.Audio;

public delegate void SettingsModifier(ref Settings s);

[Serializable]
public struct Settings {
  public bool showSidePanel;
  public bool music;
  public bool sfx;
  public bool rightHanded;

  public static event Action OnChanged;

  private static Settings m_main = LoadOrGetDefaultSettings();
  public static Settings main => m_main;

  public static Settings LoadOrGetDefaultSettings() {
    try {
      if (PlayerPrefs.HasKey("settings")) {
        var savedJson = PlayerPrefs.GetString("settings");
        var settings = JsonUtility.FromJson<Settings>(savedJson);
        return settings;
      }
    } catch(Exception e) {
      Debug.LogError(e);
    }
    return Default();
  }

  public static Settings Default() {
    return new Settings {
      showSidePanel = true,
      music = true,
      sfx = true,
      rightHanded = true
    };
  }

  public static void Update(SettingsModifier modify) {
    var newSettings = main;
    modify(ref newSettings);
    Settings.Set(newSettings);
  }

  public static void Set(Settings newSettings, bool save = true) {
    m_main = newSettings;
    if (save) {
      var json = JsonUtility.ToJson(m_main);
      PlayerPrefs.SetString("settings", json);
      PlayerPrefs.Save();
    }
    OnChanged?.Invoke();
  }
}
