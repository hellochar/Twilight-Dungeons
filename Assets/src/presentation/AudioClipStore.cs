using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioClipStore : MonoBehaviour {
  public static AudioClipStore main;

  public AudioClip move;
  public AudioClip attack;
  public AudioClip attackMiss;
  public AudioClip bossStart;
  public AudioClip bossDeath;
  public AudioClip death;
  public AudioClip plantHarvest;
  public AudioClip playerChangeWater;
  public AudioClip playerEquip;
  public AudioClip playerEquipmentBreak;
  public AudioClip playerHeal;
  public AudioClip playerGeneric;
  public AudioClip playerGetDebuff;
  public AudioClip playerPickupItem;
  public AudioClip playerHurt1, playerHurt2, playerHurt3;
  public AudioClip playerTakeStairs;
  public AudioClip playerWait;
  public AudioClip popupOpen;
  public AudioClip popupClose;
  public AudioClip summon;
  public AudioClip uiError;

  void Awake() {
    main = this;
  }
}

public static class AudioClipExtensions {
  public static void Play(this AudioClip clip, float volume = 1) {
    PlayerController.current.PlaySFX(clip, volume);
  }
}