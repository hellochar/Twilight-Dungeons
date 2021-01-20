﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioClipStore : MonoBehaviour {
  public static AudioClipStore main;

  public AudioClip move;
  public AudioClip attack;
  public AudioClip attackMiss;
  public AudioClip death;
  public AudioClip gameStart;
  public AudioClip plantHarvest;
  public AudioClip playerEquip;
  public AudioClip playerHeal;
  public AudioClip playerGeneric;
  public AudioClip playerPickupItem;
  public AudioClip playerTakeStairs;
  public AudioClip playerWait;
  public AudioClip popupOpen;
  public AudioClip popupClose;
  public AudioClip uiError;

  void Awake() {
    main = this;
  }
}

public static class AudioClipExtensions {
  public static void PlayAtPoint(this AudioClip clip, Vector3 position, float volume = 1) {
    AudioSource.PlayClipAtPoint(clip, position, volume);
  }

  public static void Play(this AudioClip clip, float volume = 1) {
    AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
  }
}