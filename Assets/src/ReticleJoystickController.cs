using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ReticleJoystickController : MonoBehaviour {
  public GameObject stick;
  public LineRenderer lineRenderer;
  public GameObject[] hitTargets;
  public GameObject reticlePrefab;
  GameObject reticle;
  public float worldScalar = 0.01f;
  public float maxDistance = 1.4f;
  Vector2 pointerOffset => Util.getXY(Input.mousePosition) - Util.getXY(transform.position);
  private float timePressed;

  // misclick inset
  private static float d = 0.17f;
  private static float innerEdge = 0.5f + d;
  private static float outerEdge = innerEdge + 1;
  public (Rect rect, Vector2Int move)[] TracerButtons = new[] {
    (new Rect(innerEdge,  innerEdge,  1,   1  ), new Vector2Int(1, 1)),
    (new Rect(innerEdge,  -d,         1,   2*d), new Vector2Int(1, 0)),
    (new Rect(innerEdge,  -outerEdge, 1,   1  ), new Vector2Int(1, -1)),
    (new Rect(-d,         -outerEdge, 2*d, 1  ), new Vector2Int(0, -1)),
    (new Rect(-outerEdge, -outerEdge, 1,   1  ), new Vector2Int(-1, -1)),
    (new Rect(-outerEdge, -d,         1,   2*d), new Vector2Int(-1, 0)),
    (new Rect(-outerEdge, innerEdge,  1,   1  ), new Vector2Int(-1, 1)),
    (new Rect(-d,         innerEdge,  2*d, 1  ), new Vector2Int(0, 1)),
  };

  public void BeginPress(Vector2 mousePosition) {
    gameObject.SetActive(true);
    lineRenderer.enabled = true;
    transform.position = Util.withZ(mousePosition);
    timePressed = Time.time;
    foreach (var hitTarget in hitTargets) {
      hitTarget.SetActive(true);
    }
    Update();
  }

  void Update() {
    if (Input.touchCount >= 2) {
      EndInteraction();
    }
    stick.transform.position = transform.position + Util.withZ(pointerOffset);
    var playerPos = Util.withZ(GameModel.main.player.pos);
    var worldOffset = new Vector3(pointerOffset.x, pointerOffset.y, 0) * worldScalar;
    var distanceRaw = worldOffset.magnitude;
    var finalDistance = (float) System.Math.Tanh(distanceRaw / maxDistance) * maxDistance;
    worldOffset.Normalize();
    worldOffset *= finalDistance;
    
    var tracerPos = playerPos + worldOffset;
    lineRenderer.SetPositions(new Vector3[] {
      playerPos,
      tracerPos
    });
    Vector2Int tracerGridTile = new Vector2Int(Mathf.RoundToInt(tracerPos.x), Mathf.RoundToInt(tracerPos.y));
    UpdateShowHitTargetAndMaybeReticle(tracerPos, tracerGridTile, Util.getXY(worldOffset));
  }

  private void UpdateShowHitTargetAndMaybeReticle(Vector3 tracerPos, Vector2Int tracerGridTile, Vector2 offset) {
    // sort tracer buttons by the value's offset distance
    System.Array.Sort(TracerButtons, (pairA, pairB) => {
      var pairADist = Vector2.Distance(pairA.move, offset);
      var pairBDist = Vector2.Distance(pairB.move, offset);
      return pairADist.CompareTo(pairBDist);
    });

    // show nearest three hit targets
    for(var i = 0; i < hitTargets.Length; i++) {
      hitTargets[i].transform.position = Util.withZ(GameModel.main.player.pos + TracerButtons[i].move);
      if (TracerButtons[i].rect.Contains(offset)) {
        hitTargets[i].GetComponent<SpriteRenderer>().color = Color.white;
      } else {
        // at dist 0.2 or lower, pure white.
        // at dist 0.4, heavy falloff
        var dist = Vector2.Distance(TracerButtons[i].move, offset);
        var alpha = Util.MapLinear(dist, 0.2f, 0.75f, 1f, 0f);
        hitTargets[i].GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, alpha);
      }
    }

    // show reticle if we're successfully in one
    if (TracerButtons[0].rect.Contains(offset)) {
      UpdateReticle(GameModel.main.player.pos + TracerButtons[0].move);
      LetGo();
    } else {
      EnsureReticleDestroyed();
    }
  }

  void EnsureReticleDestroyed() {
    if (reticle != null) {
        Destroy(reticle);
        reticle = null;
    }
  }

  void UpdateReticle(Vector2Int tracerGridTile) {
    if (reticle == null) {
      reticle = Instantiate(reticlePrefab, Util.withZ(tracerGridTile), Quaternion.identity);
    } else {
      reticle.transform.position = Util.withZ(tracerGridTile);
    }
  }

  public void LetGo() {
    TryPress();
    EndInteraction();
  }

  void EndInteraction() {
    gameObject.SetActive(false);
    lineRenderer.enabled = false;
    foreach (var hitTarget in hitTargets) {
      hitTarget.SetActive(false);
    }
    EnsureReticleDestroyed();
  }

  void TryPress() {
    if (reticle != null) {
      var offset = Util.getXY(reticle.transform.position) - GameModel.main.player.pos;
      MovePlayer((int)offset.x, (int)offset.y);
    }
  }

  public void Wait() {
    MovePlayer(0, 0);
  }

  public void MovePlayer(int dx, int dy) {
    var interactionController = GameModelController.main.CurrentFloorController.GetComponent<InteractionController>();
    var pos = GameModel.main.player.pos + new Vector2Int(dx, dy);
    /// this potentially does *anything* - set player action, open a popup, or be a no-op.
    interactionController.Interact(pos, null);
  }
}
