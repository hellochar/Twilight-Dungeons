using UnityEngine;
using System;

[Serializable]
public struct Settings {
  public static event Action OnChanged;

  private static Settings m_main = GetOrCreateDefaultSettings();
  public static Settings main => m_main;

  public static Settings GetOrCreateDefaultSettings() {
    try {
      if (PlayerPrefs.HasKey("settings")) {
        var savedJson = PlayerPrefs.GetString("settings");
        var settings = JsonUtility.FromJson<Settings>(savedJson);
        return settings;
      }
    } catch(Exception e) {
      Debug.LogError(e);
    }
    return new Settings {
      moveMode = MoveMode.DPad | MoveMode.TouchTile,
      showSidePanel = true,
    };
  }

  public static void Set(Settings newSettings) {
    m_main = newSettings;
    var json = JsonUtility.ToJson(m_main);
    PlayerPrefs.SetString("settings", json);
    PlayerPrefs.Save();
    OnChanged?.Invoke();
  }

  public MoveMode moveMode;
  public bool showSidePanel;
}

[Flags]
public enum MoveMode {
  DPad = 1,
  TouchTile = 2
}