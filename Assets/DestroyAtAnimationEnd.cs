using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAtAnimationEnd : MonoBehaviour {
  public void HandleAnimationEnd() {
    Destroy(gameObject.transform.parent.gameObject);
  }
}
