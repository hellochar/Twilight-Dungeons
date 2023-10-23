using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;

public class TopBannerController : MonoBehaviour {
  private TMPro.TMP_Text text;
  private Image image;
  void Start() {
    text = transform.Find("Text").GetComponent<TMPro.TMP_Text>();
    image = GetComponent<Image>();
    text.text = "";
  }

  // Update is called once per frame
  void Update() {
    // if (GameModel.main.depth == 0) {
    //   image.SetEnabled(false);
    // } else {
    //   image.SetEnabled(true);
    // }
    text.text = GetBannerText();
    // GetComponent<HorizontalLayoutGroup>().CalculateLayoutInputHorizontal();
  }

  string GetBannerText() {
    bool isTutorial = GameModel.main.currentFloor is TutorialFloor;
    if (GameModel.main.depth == 0 && !isTutorial) {
      return "Home";
    }

    List<string> textSections = new List<string>();
    if (!isTutorial) {
      textSections.Add($"Depth {GameModel.main.cave.depth}");
    }

    if (GameModel.main.currentFloor.isCleared) {
      textSections.Add("Cleared!");
    } else {
      if (!GameModel.main.permadeath && GameModel.main.attempt > 1) {
        textSections.Add($"Try {GameModel.main.attempt}/{GameModel.MAX_ATTEMPTS}");
      }

      if (GameModel.main.currentFloor.age > 0) {
        textSections.Add($"Turn {Mathf.CeilToInt(GameModel.main.currentFloor.age)}");
      }
    }
    
    return String.Join("   ", textSections);
  }

  public void ShowPopup() {
    var playTime = TimeSpan.FromSeconds(Time.timeSinceLevelLoad).ToString(@"hh\:mm\:ss");
    var info = $"Playtime {playTime}\nSeed " + GameModel.main.seed.ToString("x");
    var buttons = new List<(string, Action)>();
    if (GameModel.main.canRetry) {
      buttons.Add(("Retry", () => GameOverSceneController.Retry(this)));
    };
    Popups.CreateStandard(
      title: null,
      category: "",
      info: info,
      flavor: "",
      buttons: buttons,
      errorText: GameModel.main.turnManager.latestException?.ToString()
    );
  }
}
