using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizePitch : MonoBehaviour {
  public AudioSource source;
  public float low = 0.75f;
  public float high = 1.25f;
  void Start() {
    if (source == null) {
      source = GetComponent<AudioSource>();
    }
    source.pitch = Random.Range(low, high);
  }
}
