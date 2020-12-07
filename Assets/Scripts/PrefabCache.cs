using System;
using System.Collections.Generic;
using UnityEngine;

class PrefabCache : PrefabCache<object> {
  public static PrefabCache<ActorTask> Tasks = new PrefabCache<ActorTask>("Tasks");
  public static PrefabCache<Status> Statuses = new PrefabCache<Status>("Statuses");
  public static PrefabCache<BaseAction> BaseActions = new PrefabCache<BaseAction>("BaseActions");

  public PrefabCache(string path) : base(path) {}
}

class PrefabCache<T> {
  private Dictionary<Type, GameObject> cache = new Dictionary<Type, GameObject>();
  public string Path { get; set; }

  public PrefabCache(string path) {
    Path = path;
  }

  public GameObject GetPrefabFor(T obj) {
    var type = obj.GetType();
    if (!cache.ContainsKey(type)) {
      // attempt to load it
      var name = type.Name;
      var prefabOrNull = Resources.Load<GameObject>($"{Path}/{name}");
      cache.Add(type, prefabOrNull);
    }
    return cache[type];
  }

  public GameObject MaybeInstantiateFor(T obj, Transform transform) {
    if (obj != null) {
      GameObject prefab = GetPrefabFor(obj);
      if (prefab != null) {
        return UnityEngine.Object.Instantiate(prefab, transform.position, Quaternion.identity, transform);
      }
    }
    return null;
  }
}