using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DepthTextController : MonoBehaviour {
  private TMPro.TMP_Text text;
  void Start() {
    text = transform.Find("Text").GetComponent<TMPro.TMP_Text>();
    text.text = "";
  }

  // Update is called once per frame
  void Update() {
    text.text = "Depth " + (GameModel.main.currentFloor.depth) + "\nTurn " + GameModel.main.currentFloor.age;
  }

#if experimental_retryondemand
  void RetryLevel() {
    GameModel.main = Serializer.LoadLevelStart();
    StartCoroutine(Transitions.GoToNewScene(this, null, "Scenes/Game"));
  }
#endif

  public void ShowPopup() {
    var playTime = TimeSpan.FromSeconds(Time.timeSinceLevelLoad).ToString(@"hh\:mm\:ss");
    var info = $"Playtime {playTime}\nSeed " + GameModel.main.seed.ToString("x");
    List<(string, Action)> buttons = new List<(string, Action)>();
    #if experimental_retryondemand
    buttons.Add(("Retry Level", RetryLevel));
    #endif
    Popups.Create(
      title: null,
      category: "",
      info: info,
      flavor: "",
      errorText: GameModel.main.turnManager.latestException?.ToString(),
      buttons: buttons
    );
  }
}
