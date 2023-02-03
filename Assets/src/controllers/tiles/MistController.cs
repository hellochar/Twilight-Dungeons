using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class MistController : MonoBehaviour {
  public SpriteRenderer typeRenderer;
  [Serializable]
  public struct EncounterTypeInfo {
    public FloorType type;
    public Sprite sprite;
  }

  public EncounterTypeInfo[] EncounterTypeMapping;

  public Mist mist => GetComponent<ChasmController>().tile as Mist;

  public void Start() {
    typeRenderer.sprite = EncounterTypeMapping.First(entry => entry.type == mist.type).sprite;
  }

  public void Update() {
    typeRenderer.gameObject.SetActive(mist.isExplored);
  }
}
