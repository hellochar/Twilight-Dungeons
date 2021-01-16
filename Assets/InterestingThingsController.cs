using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class InterestingThingsController : MonoBehaviour {
  public Player player => GameModel.main.player;
  public Dictionary<string, GameObject> thingMap = new Dictionary<string, GameObject>();
  public GameObject content;
  public GameObject buttonPrefab;

  void Start() {
    player.OnMove += HandleMove;
    buttonPrefab.SetActive(false);
    UpdateItems();
  }

  bool needsUpdate = false;
  private void HandleMove(Vector2Int newPos, Vector2Int oldPos) {
    needsUpdate = true;
  }

  void Update() {
    // if (needsUpdate) {
      UpdateItems();
      // needsUpdate = false;
    // }
  }

  void UpdateItems() {
    var newInterestingThings = GetInterestingThings().GroupBy(e => e.displayName).ToDictionary((grouping) => grouping.Key, (grouping) => grouping.First());
    var toRemove = thingMap.Keys.Except(newInterestingThings.Keys).ToList();

    // remove old items
    foreach (var name in toRemove) {
      Destroy(thingMap[name]);
      thingMap.Remove(name);
    }

    // add new items
    foreach (var name in newInterestingThings.Keys) {
      if (!thingMap.ContainsKey(name)) {
        var buttonObject = Instantiate(buttonPrefab, content.transform.position, Quaternion.identity, content.transform);
        buttonObject.SetActive(true);
        thingMap[name] = buttonObject;

        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => {
          GameModelController.main.CurrentFloorController.ShowPopupFor(newInterestingThings[name]);
          // TODO create popup for entity
        });
        buttonObject.GetComponentInChildren<TMPro.TMP_Text>().text = name;
      }
    }
  }

  /// interesting things: bodies and grasses
  IEnumerable<Entity> GetInterestingThings() {
    foreach (var pos in player.floor.EnumerateCircle(player.pos, player.visibilityRange)) {
      var body = player.floor.bodies[pos];
      var grass = player.floor.grasses[pos];
      if (body != null && body != player && body.isVisible) {
        yield return body;
      }
      if (grass != null && grass.isVisible) {
        yield return grass;
      }
    }
  }
}
