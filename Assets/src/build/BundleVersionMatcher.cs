using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BundleVersionMatcher : MonoBehaviour {
  // Start is called before the first frame update
  void Start() {
    GetComponent<TMPro.TMP_Text>().text = Application.version;
  }
}
