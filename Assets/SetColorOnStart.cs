using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetColorOnStart : MonoBehaviour {
  public Image image;
  public Color color;
  // Start is called before the first frame update
  void Start() {
    image.color = color;
  }
}
