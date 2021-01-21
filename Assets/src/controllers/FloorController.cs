using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Adds and removes Tile prefabs to match the state of a Floor variable.
public class FloorController : MonoBehaviour, IPointerClickHandler {

  public Floor floor;
  public static Dictionary<System.Type, GameObject> EntityPrefabs = new Dictionary<System.Type, GameObject>();
  private Dictionary<Entity, GameObject> gameObjectMap = new Dictionary<Entity, GameObject>();

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
  void Start() {
    this.InstantiateGameObjectsToMatchFloor();
    floor.OnEntityAdded += HandleEntityAdded;
    floor.OnEntityRemoved += HandleEntityRemoved;
    #if UNITY_EDITOR
      LogEnemyHP();
    #endif
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
    GameObject currentObject = gameObjectMap[e];
    if (currentObject == null) {
      Debug.LogWarning("" + e + " was removed from floor " + floor + " but didn't have a GameObject.");
    }
    currentObject.AddComponent<FadeThenDestroy>();
    gameObjectMap.Remove(e);
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
      Debug.LogError($"Couldn't find prefab for {entity.GetType()}");
    }
  }

  private InputHold hold;
  void Update() {
    if (Input.GetMouseButtonDown(0)) {
      hold = new InputHold(Time.time);
    } else if (Input.GetMouseButton(0) && (hold?.ShouldTrigger(Time.time) ?? false)) {
      hold.triggered = true;
      Handheld.Vibrate();
      Debug.Log("Triggered!");
      ShowObjectInfoPopupOverMousePosition();
    } else if (Input.GetMouseButtonUp(0)) {
      hold = null;
    }
  }

  void ShowObjectInfoPopupOverMousePosition() {
    var mousePosition = Input.mousePosition;
    var worldPos = Camera.main.ScreenToWorldPoint(mousePosition);
    var pos = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));

    if (!floor.InBounds(pos)) {
      return;
    }

    var tile = floor.tiles[pos];
    var body = tile.body;
    var itemOnGround = tile.item;
    var grass = tile.grass;

    var entityList = new Entity[] { body, itemOnGround, grass, tile };
    
    var entity = entityList.Where((e) => e != null && e.isVisible).FirstOrDefault();

    if (entity != null) {
      ShowPopupFor(entity);
    }
  }

  public void ShowPopupFor(Entity entity) {
    string description = entity.description + "\n\n";
    if (entity is Body b) {
      if (b is Actor a) {
        description += $"{Util.DescribeDamageSpread(a.BaseAttackDamage())}\n";
      }
      description += $"Max HP: {b.maxHp}\n";
    }
    var entityGameObject = gameObjectMap[entity];

    var spritePrefab = PrefabCache.UI.GetPrefabFor("Entity Image");
    var sprite = Instantiate(spritePrefab);
    var image = sprite.GetComponentInChildren<Image>();
    image.sprite = entityGameObject.GetComponentInChildren<SpriteRenderer>().sprite;
    image.color = entityGameObject.GetComponentInChildren<SpriteRenderer>().color;

    Popups.Create(
      title: entity.displayName,
      info: description.Trim(),
      flavor: ObjectInfo.GetFlavorTextFor(entity),
      sprite: sprite
    );
    Destroy(sprite);
  }

  public void OnPointerClick(PointerEventData eventData) {
    if (!CameraZoom.IsZoomGuardActive && Settings.main.moveMode.HasFlag(MoveMode.TouchTile)) {
      var worldPos = eventData.pointerCurrentRaycast.worldPosition;
      var pos = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
      UserInteractAt(pos, eventData);
    }
  }

  public void UserInteractAt(Vector2Int pos, PointerEventData eventData) {
    var tile = floor.tiles[pos];
    var body = tile.body;
    var itemOnGround = tile.item;
    var grass = tile.grass;

    if (TryGetFirstEntityClickHandler(out var handler, body, grass, itemOnGround, tile)) {
      handler.PointerClick(eventData);
    }
  }

  bool TryGetEntityClickHandler(Entity e, out IEntityClickedHandler handler) {
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
      if (TryGetEntityClickHandler(entity, out handler)) {
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
  public readonly float threshold;
  public bool triggered = false;

  public InputHold(float time, float threshold = THRESHOLD) {
    this.time = time;
    this.threshold = threshold;
  }

  public bool ShouldTrigger(float t) => !triggered && (t - time > threshold);
}