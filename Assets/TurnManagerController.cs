using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TurnManagerController : MonoBehaviour {
  public GameModel model => GameModel.main;
  public TurnManager turnManager => model.turnManager;
  // Start is called before the first frame update
  public Dictionary<SteppableEntity, GameObject> markers = new Dictionary<SteppableEntity, GameObject>();
  public GameObject markerPrefab;
  private RectTransform rectTransform;

  void Start() {
    rectTransform = GetComponent<RectTransform>();
    markerPrefab.SetActive(false);
    turnManager.OnStep += HandleStep;
    // RemoveAddAndUpdateMarkers();
  }

  public bool needsUpdate = false;
  private void HandleStep(SteppableEntity e) {
    needsUpdate = true;
  }

  void LateUpdate() {
    // if (needsUpdate) {
    RemoveAddAndUpdateMarkers();
    UpdatePlayerActionMarkers();
    // }
  }

  private void UpdatePlayerActionMarkers() {
    void UpdateMarker(string name, ActionType type) {
      var newTime = model.player.GetActionCost(type);
      var gameObject = transform.Find(name).gameObject;
      RepositionX(gameObject, newTime);
    }
    UpdateMarker("Attack", ActionType.ATTACK);
    UpdateMarker("Move", ActionType.MOVE);
    UpdateMarker("Wait", ActionType.WAIT);
  }

  public void RemoveAddAndUpdateMarkers() {
    needsUpdate = false;
    var entities = model.GetAllEntitiesInPlay().Where((a) => a is Actor && a.timeUntilTurn <= 2.5 && a.isVisible).ToList();
    entities = entities.OrderBy((a) => a.timeUntilTurn + a.turnPriority * .001f).ToList();
    // remove old markers
    var toRemove = markers.Keys.Except(entities).ToList();
    foreach (var e in toRemove) {
      markers[e].AddComponent<FadeThenDestroy>();
      markers.Remove(e);
    }

    // add new markers
    foreach (var e in entities) {
      if (!markers.ContainsKey(e)) {
        var marker = Instantiate(markerPrefab, transform.position, Quaternion.identity, transform);
        marker.SetActive(true);

        // match sprite
        var image = marker.transform.Find("Creature").GetComponent<Image>();

        GameObject gameObjectForEntity;
        if (e == model.player) {
          gameObjectForEntity = GameObject.Find("Player");
        } else {
          // var currentFloorController = GameModelController.main.CurrentFloorController;
          // gameObjectForEntity = currentFloorController.GameObjectFor(e);
          gameObjectForEntity = FloorController.GetEntityPrefab(e);
        }
        var entitySpriteRenderer = gameObjectForEntity.GetComponentInChildren<SpriteRenderer>();

        image.sprite = entitySpriteRenderer.sprite;
        image.color = entitySpriteRenderer.color;

        markers.Add(e, marker);
      }
    }

    var entityGroupedByTime = entities
      .GroupBy((e) => e.timeUntilTurn)
      .ToDictionary(grouping => grouping.Key, grouping => grouping.OrderBy(e => e.turnPriority).ToList());

    // update all markers
    for (int i = 0; i < entities.Count; i++) {
      var entity = entities[i];
      var marker = markers[entity];

      // put it in the right place
      var priorityList = entityGroupedByTime[entity.timeUntilTurn];
      var order = priorityList.IndexOf(entity);
      var groupPixelY = -order * 16;
      marker.transform.Find("Creature").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, groupPixelY);

      RepositionX(marker, entity.timeUntilTurn);
    }
  }

  void RepositionX(GameObject marker, float timeUntilTurn) {
    RectTransform markerTransform = marker.GetComponent<RectTransform>();
    var pos = markerTransform.anchoredPosition;
    var pixelX = timeUntilTurn * 100;
    if (pos.x != pixelX) {
      StartCoroutine(AnimateMarker(markerTransform, pixelX));
    }
  }

  IEnumerator AnimateMarker(RectTransform marker, float pixelX) {
    var pos = marker.anchoredPosition;
    for (int i = 0; i < 10; i++) {
      pos.x = Mathf.Lerp(pos.x, pixelX, 0.33f);
      marker.anchoredPosition = pos;
      yield return new WaitForEndOfFrame();
    }
  }
}
