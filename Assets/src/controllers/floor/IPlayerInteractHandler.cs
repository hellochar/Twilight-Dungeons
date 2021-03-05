using UnityEngine.EventSystems;

public interface IPlayerInteractHandler {
  void HandleInteracted(PointerEventData pointerEventData);
}
