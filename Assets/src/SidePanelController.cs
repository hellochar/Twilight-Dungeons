using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SidePanelController : MonoBehaviour {
  public Player player => GameModel.main.player;
  public Dictionary<string, GameObject> entries = new Dictionary<string, GameObject>();
  public Dictionary<string, Entity> entities;
  public GameObject creaturesContainer;
  public GameObject grassesContainer;
  public GameObject othersContainer;
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

  private void HandlePlayersChoice() {
    UpdateItems();
  }


  void Update() {
    // 1 is the title, 2 is the inactive Button prefab
    var hasCreatures = creaturesContainer.transform.childCount > 2;
    // 1 is the title
    var hasGrasses = grassesContainer.transform.childCount > 1;
    var hasOthers = othersContainer.transform.childCount > 1;

    creaturesContainer.SetActive(hasCreatures);
    grassesContainer.SetActive(hasGrasses);
    othersContainer.SetActive(hasOthers);
  }

  void UpdateItems() {
    entities = GetInterestingThings().GroupBy(e => e.displayName).ToDictionary((grouping) => grouping.Key, (grouping) => grouping.First());
    var toRemove = entries.Keys.Except(entities.Keys).ToList();

    // remove old items
    foreach (var name in toRemove) {
      Destroy(entries[name]);
      entries.Remove(name);
    }

    // add new items
    foreach (var name in entities.Keys) {
      var entity = entities[name];
      if (!entries.ContainsKey(name)) {
        var contentBox = entity is Grass ? grassesContainer : entity is Actor ? creaturesContainer : othersContainer;
        var buttonObject = Instantiate(buttonPrefab, contentBox.transform.position, Quaternion.identity, contentBox.transform);
        buttonObject.SetActive(true);
        entries[name] = buttonObject;

        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => {
          var firstEntityOfType = entities[name];
          GameModelController.main.CurrentFloorController.ShowPopupFor(firstEntityOfType);
          // TODO create popup for entity
        });
        buttonObject.GetComponentInChildren<TMPro.TMP_Text>().text = name;
      }
    }
  }

  /// interesting things: bodies and grasses
  IEnumerable<Entity> GetInterestingThings() {
    var floor = GameModel.main.currentFloor;
    foreach (var pos in floor.EnumerateCircle(player.pos, player.visibilityRange)) {
      var body = floor.tiles[pos].body;
      var grass = floor.grasses[pos];
      if (body != null && body != player && body.isVisible) {
        yield return body;
      }
      if (grass != null && grass.isVisible) {
        yield return grass;
      }
    }
  }
}
