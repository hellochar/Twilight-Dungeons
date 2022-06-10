using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class SignpostController : TileController, IOnTopActionHandler, ITapHandler {
  public Signpost signpost => (Signpost) tile;
  public string OnTopActionName => "Read";

  public override void Start() {
    base.Start();
    if (!signpost.hasRead) {
      // PrefabCache.Effects.Instantiate("Highlight", transform);
    }
  }

  public void HandleOnTopAction() {
    signpost.ShowSignpost();
    /// hack - destroy highlight
    var highlight = transform.Find("Highlight(Clone)");
    if (highlight != null) {
      highlight.gameObject.AddComponent<FadeThenDestroy>();
    }
  }

  public void Tapped() {
    signpost.ShowSignpost();
  }
}
