using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Adds and removes Tile prefabs to match the state of a Floor variable.
public class FloorController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler {

  [NonSerialized]
  public Floor floor;
  public static Dictionary<System.Type, GameObject> EntityPrefabs = new Dictionary<System.Type, GameObject>();
  protected Dictionary<Entity, GameObject> gameObjectMap = new Dictionary<Entity, GameObject>();

  public static GameObject GetEntityPrefab(Entity e) {
    var type = e.GetType();
    if (!EntityPrefabs.ContainsKey(type)) {
      string category = "";
      switch (e) {
        case Tile t:
          category = "Tiles/";
          break;
        case Grass g:
          category = "Grasses/";
          break;
        case Plant p:
          category = "Plants/";
          break;
        case Body b:
          category = "Actors/";
          break;
      }
      string resourcePath = $"Entities/{category}{type.Name}";
      EntityPrefabs.Add(type, Resources.Load<GameObject>(resourcePath));
    }
    return EntityPrefabs[type];
  }

  public GameObject GameObjectFor(Entity entity) {
    return gameObjectMap[entity];
  }

  // Start is called before the first frame update
  public virtual void Start() {
    this.InstantiateGameObjectsToMatchFloor();
    floor.OnEntityAdded += HandleEntityAdded;
    floor.OnEntityRemoved += HandleEntityRemoved;
    #if UNITY_EDITOR
    // LogEnemyHP();
    #endif
  }

  void OnDestroyed() {
    floor.OnEntityAdded -= HandleEntityAdded;
    floor.OnEntityRemoved -= HandleEntityRemoved;
  }

  void LogEnemyHP() {
    var enemies = floor.bodies.Where((b) => b is Actor x && x.faction == Faction.Enemy);
    var allHP = enemies.Select((a) => a.maxHp).Sum();
    Debug.Log("Depth " + floor.depth +", HP " + allHP, this);
  }

  void HandleEntityRemoved(Entity e) {
    if (e == GameModel.main.player) {
      gameObjectMap.Remove(e);
      return;
    }
    if (gameObjectMap.TryGetValue(e, out var currentObject)) {
      currentObject.AddComponent<FadeThenDestroy>();
      gameObjectMap.Remove(e);
    } else {
      Debug.LogWarning("" + e + " was removed from floor " + floor + " but didn't have a GameObject.");
    }
  }

  void HandleEntityAdded(Entity e) {
    InstantiateGameObjectForEntity(e);
  }

  void InstantiateGameObjectsToMatchFloor() {
    foreach (var e in floor.entities) {
      InstantiateGameObjectForEntity(e);
    }
  }

  private void InstantiateGameObjectForEntity(Entity entity) {
    /// Player is Instantiated separately, but we still associate it to the gameobjectmap
    /// so it gets caught by the OnPointerClick code
    if (entity is Player) {
      var gameObject = GameObject.Find("Player");
      gameObjectMap[entity] = gameObject;
      return;
    }
    if (gameObjectMap.ContainsKey(entity)) {
      throw new System.Exception("Creating gameObject for entity that already has one" + entity);
    }
    GameObject prefab = GetEntityPrefab(entity);
    if (prefab != null) {
      Vector3 pos = new Vector3(entity.pos.x, entity.pos.y, prefab.transform.position.z);
      GameObject gameObject = Instantiate(prefab, pos, Quaternion.identity, this.transform);
      if (entity is Tile tile) {
        gameObject.GetComponent<TileController>().tile = tile;
      } else if (entity is Body body) {
        gameObject.GetComponent<BodyController>().body = body;
      } else if (entity is Grass grass) {
        gameObject.GetComponent<GrassController>().grass = grass;
      } else if (entity is ItemOnGround itemOnGround) {
        gameObject.GetComponent<ItemOnGroundController>().itemOnGround = itemOnGround;
      }
      gameObjectMap[entity] = gameObject;
    } else {
      Debug.LogWarning($"Couldn't find prefab for {entity.GetType()}");
    }
  }

  public Entity[] GetVisibleEntitiesInLayerOrder(Vector2Int pos) {
    if (!floor.InBounds(pos)) {
      return new Entity[0];
    }

    var tile = floor.tiles[pos];
    var body = tile.body;
    var itemOnGround = tile.item;
    var grass = tile.grass;

    return new Entity[] { body, itemOnGround, grass, tile }
      .Where(e => e != null && e.isVisible)
      .ToArray();
  }

  public static bool isInputAllowed = true;

  public void OnPointerDown(PointerEventData eventData) {
    if (!CameraZoom.IsZoomGuardActive && isInputAllowed) {
      hold = new InputHold(Time.time, eventData);
      HoldProgressBar.main.HoldStart();
    }
  }

  public void OnPointerUp(PointerEventData eventData) {
    if (isInputAllowed && !CameraZoom.IsZoomGuardActive && !(hold?.triggered ?? false)) {
      var pos = RaycastToTilePos(eventData.pointerCurrentRaycast);
      var tappedEntity = GetVisibleEntitiesInLayerOrder(pos).FirstOrDefault();
      if (tappedEntity != null) {
        ShowPopupFor(tappedEntity);
      }
    }
    hold = null;
    HoldProgressBar.main.HoldEnd();
  }

  public void OnPointerClick(PointerEventData eventData) {
    // if (isInputAllowed && !CameraZoom.IsZoomGuardActive && Settings.main.moveMode.HasFlag(MoveMode.TouchTile)) {
    //   var pos = RaycastToTilePos(eventData.pointerCurrentRaycast);
    //   var pressPos = RaycastToTilePos(eventData.pointerPressRaycast);
    //   // don't trigger a click if you've touch-dragged over multiple tiles to prevent accidental touches
    //   if (pressPos == pos) {
    //     UserInteractAt(pos, eventData);
    //   }
    // }
  }

  private InputHold hold;
  void Update() {
    if (hold != null && isInputAllowed) {
      if (CameraZoom.IsZoomGuardActive) {
        hold = null;
      } else {
        var time = Time.time;
        HoldProgressBar.main.HoldUpdate(hold.PercentDone(time));
        if (hold.ShouldTrigger(time)) {
          hold.triggered = true;
          var mousePosition = Input.mousePosition;
          var worldPos = Camera.main.ScreenToWorldPoint(mousePosition);
          var pos = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
          UserInteractAt(pos, hold.pointerEventData);
          // ShowObjectInfoPopupOverTouch();
        }
      }
    }
  }

  void ShowObjectInfoPopupOverTouch() {
    var mousePosition = Input.mousePosition;
    var worldPos = Camera.main.ScreenToWorldPoint(mousePosition);
    var pos = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));

    var tappedEntity = GetVisibleEntitiesInLayerOrder(pos).FirstOrDefault();

    if (tappedEntity != null) {
      Handheld.Vibrate();
      ShowPopupFor(tappedEntity);
    }
  }

  public void ShowPopupFor(Entity entity) {
    string description = entity.description + "\n\n";
    if (entity is Body b) {
      if (b is Actor a) {
        description += Util.DescribeDamageSpread(a.BaseAttackDamage());
      }
      description += $"Max HP: {b.maxHp}\n";
    }
    var entityGameObject = gameObjectMap[entity];

    var spritePrefab = PrefabCache.UI.GetPrefabFor("Entity Image");
    var spriteGameObject = Instantiate(spritePrefab);
    var image = spriteGameObject.GetComponentInChildren<Image>();
    var sprite = ObjectInfo.GetSpriteFor(entity) ?? entityGameObject.GetComponentInChildren<SpriteRenderer>()?.sprite;
    image.sprite = sprite;
    image.color = entityGameObject.GetComponentInChildren<SpriteRenderer>().color;

    Popups.Create(
      title: entity.displayName,
      category: GetCategoryForEntity(entity),
      info: description.Trim(),
      flavor: ObjectInfo.GetFlavorTextFor(entity),
      sprite: spriteGameObject
    );
    Destroy(spriteGameObject);
  }

  private static string GetCategoryForEntity(Entity entity) {
    switch (entity) {
      case Tile t:
        return "Tile";
      case Actor a:
        return "Creature";
      case Grass g:
        return "Grass";
      default:
        return "Other";
    }
  }

  private static Vector2Int RaycastToTilePos(RaycastResult raycast) {
    var worldPos = raycast.worldPosition;
    var pos = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
    return pos;
  }

  public void UserInteractAt(Vector2Int pos, PointerEventData eventData) {
    var entities = GetVisibleEntitiesInLayerOrder(pos);
    if (TryGetFirstEntityClickHandler(out var handler, entities)) {
      handler.PointerClick(eventData);
    }
  }

  bool TryGetEntityClickedHandler(Entity e, out IEntityClickedHandler handler) {
    if (e != null && gameObjectMap.TryGetValue(e, out var gameObject)) {
      if (gameObject.TryGetComponent<IEntityController>(out var controller)) {
        if (controller is IEntityClickedHandler h) {
          handler = h;
          return true;
        }
      }
    }
    handler = null;
    return false;
  }

  bool TryGetFirstEntityClickHandler(out IEntityClickedHandler handler, params Entity[] entities) {
    foreach (var entity in entities) {
      if (TryGetEntityClickedHandler(entity, out handler)) {
        return true;
      }
    }
    handler = null;
    return false;
  }
}

class InputHold {
  public const float THRESHOLD = 0.5f;
  public readonly float time;
  public readonly PointerEventData pointerEventData;
  public readonly float threshold = THRESHOLD;
  public bool triggered = false;

  public InputHold(float time, PointerEventData pointerEventData) {
    this.time = time;
    this.pointerEventData = pointerEventData;
  }

  public float PercentDone(float t) => Mathf.Clamp((t - time) / threshold, 0, 1);

  public bool ShouldTrigger(float t) => !triggered && PercentDone(t) >= 1;
}