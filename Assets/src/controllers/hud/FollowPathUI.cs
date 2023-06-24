using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FollowPathUI : MonoBehaviour {
  private GameObject reticle;
  private GameObject pathDotPrefab;
  [NonSerialized]
  private Player player;
  public List<GameObject> pathDots = new List<GameObject>();
  private FollowPathTask _task;
  public FollowPathTask task {
    get { return _task; }
    set {
      _task = value;
      ResetPathDots();
    }
  }

  // Start is called before the first frame update
  void Start() {
    pathDotPrefab = PrefabCache.UI.GetPrefabFor("PathDot");
    reticle = Instantiate(PrefabCache.UI.GetPrefabFor("Reticle"), new Vector3(), Quaternion.identity, transform);
    reticle.SetActive(false);
    player = GameModel.main.player;
    player.OnSetTask += HandleSetPlayerTask;
    InteractionController.OnProposedTasksChanged += HandleProposedTaskChanged;
  }

  void OnDestroy() {
    player.OnSetTask -= HandleSetPlayerTask;
    InteractionController.OnProposedTasksChanged -= HandleProposedTaskChanged;
  }

  private void HandleProposedTaskChanged(SetTasksPlayerInteraction interaction) {
    if (interaction != null) {
      var firstTask = interaction.tasks.FirstOrDefault();
      task = firstTask as FollowPathTask;
    }
  }

  void HandleSetPlayerTask(ActorTask action) {
    task = action as FollowPathTask;
  }

  // Update is called once per frame
  void Update() {
    if (task == null && pathDots != null) {
      ResetPathDots();
    }
    if (task != null) {
      UpdatePathSprites();
      UpdateReticle();
    }
  }

  void UpdatePathSprites() {
    // remove paths as they're tackled
    while(pathDots.Count > task.path.Count) {
      Destroy(pathDots[0]);
      pathDots.RemoveAt(0);
    }
    while (pathDots.Count < task.path.Count) {
      pathDots.Add(Instantiate(pathDotPrefab, transform));
    }
    for(int i = 0; i < task.path.Count; i++) {
      var pos = task.path[i];
      var dot = pathDots[i];
      dot.transform.position = Util.withZ(pos, 0);
    }
  }

  private void ResetPathDots() {
    pathDots.ForEach(sprite => Destroy(sprite));
    pathDots.Clear();
    reticle.SetActive(false);
  }

  void UpdateReticle() {
    reticle.SetActive(true);
    reticle.transform.position = Util.withZ(task.target, reticle.transform.position.z);
  }
}
