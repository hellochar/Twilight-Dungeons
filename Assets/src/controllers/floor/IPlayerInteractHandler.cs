using UnityEngine.EventSystems;

public interface IPlayerInteractHandler {
  void HandleInteracted(PointerEventData pointerEventData);
}

public interface ILongTapHandler {
  void HandleLongTap();
}
