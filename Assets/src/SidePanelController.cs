using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SidePanelController : MonoBehaviour {
  public Player player => GameModel.main.player;
  public Dictionary<string, GameObject> entries = new Dictionary<string, GameObject>();
  public Dictionary<string, Entity> interestingEntities;
  public GameObject creaturesContainer;
  public GameObject grassesContainer;
  public GameObject buttonPrefab;

  void Start() {
    GameModel.main.turnManager.OnPlayersChoice += HandlePlayersChoice;
    Settings.OnChanged += MatchSettings;
    buttonPrefab.SetActive(false);
    UpdateItems();
  }

  private void MatchSettings() {
    gameObject.SetActive(Settings.main.showSidePanel);
  }

  bool needsUpdate = false;
  private void HandlePlayersChoice() {
    GameModel.main.EnqueueEvent(() => needsUpdate = true);
  }


  void Update() {
    if (needsUpdate) {
      UpdateItems();
      needsUpdate = false;
    }
  }

  void UpdateItems() {
    interestingEntities = GetInterestingThings().GroupBy(e => e.displayName).ToDictionary((grouping) => grouping.Key, (grouping) => grouping.First());
    var toRemove = entries.Keys.Except(interestingEntities.Keys).ToList();

    // remove old items
    foreach (var name in toRemove) {
      Destroy(entries[name]);
      entries.Remove(name);
    }

    // add new items
    foreach (var name in interestingEntities.Keys) {
      var entity = interestingEntities[name];
      if (!entries.ContainsKey(name)) {
        var contentBox = entity is Grass ? grassesContainer : creaturesContainer;
        var buttonObject = Instantiate(buttonPrefab, contentBox.transform.position, Quaternion.identity, contentBox.transform);
        buttonObject.SetActive(true);
        entries[name] = buttonObject;

        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => {
          var firstEntityOfType = interestingEntities[name];
          GameModelController.main.CurrentFloorController.ShowPopupFor(firstEntityOfType);
          // TODO create popup for entity
        });
        buttonObject.GetComponentInChildren<TMPro.TMP_Text>().text = name;
      }
    }

    // 1 is the title, 2 is the inactive Button prefab
    var hasCreatures = creaturesContainer.transform.childCount > 2;
    // 1 is the title
    var hasGrasses = grassesContainer.transform.childCount > 1;

    creaturesContainer.SetActive(hasCreatures);
    grassesContainer.SetActive(hasGrasses);
  }

  /// interesting things: bodies and grasses
  IEnumerable<Entity> GetInterestingThings() {
    var floor = GameModel.main.currentFloor;
    foreach (var pos in floor.EnumerateCircle(player.pos, player.visibilityRange)) {
      var actor = floor.tiles[pos].actor;
      var grass = floor.grasses[pos];
      if (actor != null && actor != player && actor.isVisible) {
        yield return actor;
      }
      if (grass != null && grass.isVisible) {
        yield return grass;
      }
    }
  }
}