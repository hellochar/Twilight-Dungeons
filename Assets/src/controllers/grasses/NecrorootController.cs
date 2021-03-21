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
      var isParticleSystemActive = necroroot.corpse != null;
      ps.gameObject.SetActive(isParticleSystemActive);
    }
    if (corpse.sprite == null && necroroot.corpse != null) {
      var corpsePrefab = FloorController.GetEntityPrefab(necroroot.corpse);
      // set corpse sprite
      corpse.sprite = corpsePrefab.GetComponentInChildren<SpriteRenderer>().sprite;
    }
    if (necroroot.corpse != null) {
      var particleSystemMainModule = ps.main;
      var timeElapsed = necroroot.age - necroroot.ageCorpseCaptured;
      var simulationSpeed = timeElapsed > 3.01 ? 0 : 0.25f * Mathf.Pow(4, timeElapsed);
      particleSystemMainModule.simulationSpeed = Mathf.Lerp(particleSystemMainModule.simulationSpeed, simulationSpeed, 0.2f);
    }
  }
}
