import { TileSection } from './TileSection';

/**
 * 15 concavity patterns × 4 rotations for wall variation.
 * Port of C# TileSectionConcavity.cs — SECTIONS3 data.
 */
const SECTIONS3 = `
xxx
__x
xxx

xxx
_x_
xxx

xxx
_x_
_x_

xxx
x__
x__

xxx
___
xx_

xxx
x__
_x_

xxx
x__
__x

xx_
_x_
_xx

xx_
x__
xx_

xx_
__x
__x

xx_
__x
_xx

xx_
__x
xx_

xx_
_x_
_x_

x__
xx_
_x_

xx_
_x_
__x
`;

export const concavitySections: TileSection[] =
  TileSection.withRotations(TileSection.fromMultiString(SECTIONS3));
