using System.Collections;
using System.Collections.Generic;
using Tayx.Graphy;
using UnityEngine;

public class EnableGraphyInDevelopmentBuild : MonoBehaviour {
  public GraphyManager graphy;
  void Start() {
    if (Application.isEditor) {
      graphy.m_enableOnStartup = false;
    }
    if (Debug.isDebugBuild || Application.isEditor) {
      // no-op
    } else {
      Destroy(graphy.gameObject);
    }
  }
}
