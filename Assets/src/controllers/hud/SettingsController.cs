using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour {
  private static List<MoveMode> ChoiceToMoveMode = new List<MoveMode> {
    MoveMode.DPad,
    MoveMode.TouchTile,
    MoveMode.DPad | MoveMode.TouchTile,
  };

  public TMPro.TMP_Dropdown movementDropdown;

  void Start() {
    movementDropdown.SetValueWithoutNotify(ChoiceToMoveMode.IndexOf(Settings.main.moveMode));
  }

  public void Close() {
    gameObject.SetActive(false);
  }
  public void SetMoveMode(int choice) {
    var newSettings = Settings.main;
    newSettings.moveMode = ChoiceToMoveMode[choice];
    Settings.Set(newSettings);
  }
}

[Serializable]
public struct Settings {
  public static event Action OnChanged;

  private static Settings m_main = GetOrCreateDefaultSettings();
  public static Settings main => m_main;

  public static Settings GetOrCreateDefaultSettings() {
    try {
      if (PlayerPrefs.HasKey("settings")) {
        var savedJson = PlayerPrefs.GetString("settings");
        Debug.Log("Retrieved " + savedJson);
        var settings = JsonUtility.FromJson<Settings>(savedJson);
        return settings;
      }
    } catch(Exception e) {
      Debug.LogError(e);
    }
    return new Settings {
      moveMode = MoveMode.DPad | MoveMode.TouchTile,
    };
  }

  public static void Set(Settings newSettings) {
    m_main = newSettings;
    var json = JsonUtility.ToJson(m_main);
    Debug.Log(json);
    PlayerPrefs.SetString("settings", json);
    OnChanged?.Invoke();
  }

  public MoveMode moveMode;
}

[Flags]
public enum MoveMode {
  DPad = 1,
  TouchTile = 2
}