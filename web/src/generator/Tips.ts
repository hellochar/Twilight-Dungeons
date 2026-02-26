/**
 * Depth → signpost tip text mapping.
 * Port of C# Tips.cs.
 */
export const tipMap: Map<number, string> = new Map([
  [1, 'Tap or click a tile to move there.\nUse WASD or arrow keys too.'],
  [2, 'Tap an enemy next to you to attack.\nOr walk into them!'],
  [3, 'You can move diagonally.\nTry Q, E, Z, C keys.'],
  [4, 'Water heals you 1 HP.\nStep on it when hurt.'],
  [5, 'Some enemies have special attacks.\nWatch for telegraphed moves!'],
  [10, 'Deeper floors have tougher enemies.\nBut you can handle it.'],
  [19, 'The final stretch.\nStay focused.'],
]);
