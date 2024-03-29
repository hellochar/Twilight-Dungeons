﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class DestroyAtParticleSystemEnd : MonoBehaviour {
  public GameObject toDestroy;
  new ParticleSystem particleSystem;
  void Start() {
    particleSystem = GetComponent<ParticleSystem>();
  }

  // Update is called once per frame
  void Update() {
    if (!particleSystem.IsAlive()) {
      Destroy(toDestroy);
    }
  }
}
