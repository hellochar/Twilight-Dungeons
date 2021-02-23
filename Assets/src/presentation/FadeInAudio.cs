using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeInAudio : MonoBehaviour {
  private AudioSource source;
  // Start is called before the first frame update
  void Start() {
    source = GetComponent<AudioSource>();
    var finalVolume = source.volume;
    source.volume = 0;
    StartCoroutine(Transitions.FadeAudio(source, 2, finalVolume));
  }
}
