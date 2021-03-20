using System;
using UnityEngine;

public class NecrorootController : GrassController {
  public Necroroot necroroot => (Necroroot) grass;
  public ParticleSystem ps;
  public SpriteRenderer corpse;

  public override void Start() {
    base.Start();
    Update();
  }

  void Update() {
    if (!necroroot.IsDead) {
      var isParticleSystemActive = necroroot.actor != null || necroroot.corpse != null;
      ps.gameObject.SetActive(isParticleSystemActive);
    }
    if (corpse.sprite == null && necroroot.corpse != null) {
      var corpsePrefab = FloorController.GetEntityPrefab(necroroot.corpse);
      // set corpse sprite
      corpse.sprite = corpsePrefab.GetComponentInChildren<SpriteRenderer>().sprite;
      var particleSystemMainModule = ps.main;
      particleSystemMainModule.simulationSpeed = 1f;
    }
  }
}
