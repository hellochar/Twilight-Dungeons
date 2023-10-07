using System.Collections;
using System.Collections.Generic;
using Tayx.Graphy;
using UnityEngine;

public class AddIngameDebugConsoleInDevelopment : MonoBehaviour {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
  public GameObject IngameConsoleDebuggerPrefab;
  void Start() {
    if (Debug.isDebugBuild || Application.isEditor) {
      UnityEngine.Object.Instantiate(IngameConsoleDebuggerPrefab);
    }
  }
#endif
}