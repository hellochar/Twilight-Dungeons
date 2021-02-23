using UnityEngine;
using System;
using UnityEngine.Audio;

public delegate void SettingsModifier(ref Settings s);

[Serializable]
public struct Settings {
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
      moveMode = MoveMode.DPad | MoveMode.TouchTile,
      showSidePanel = true,
      music = true,
      soundEffects = true
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

  public MoveMode moveMode;
  public bool showSidePanel;
  public bool music;
  public bool soundEffects;
}

[Flags]
public enum MoveMode {
  DPad = 1,
  TouchTile = 2
}