using System;

/// <summary>
/// Flags representing movement capabilities and tile blocking layers.
/// A body can traverse a tile if it has at least one movement layer
/// that is NOT blocked by the tile.
/// </summary>
[Flags]
[Serializable]
public enum CollisionLayer {
  None = 0,
  Walking = 1 << 0,
  Flying = 1 << 1,
  All = Walking | Flying,
}

/// <summary>
/// Implement on Grass (or other entities) to make them block movement
/// on the tile they occupy.
/// </summary>
public interface IBlocksMovement {
  CollisionLayer BlockedLayers { get; }
}
