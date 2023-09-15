public static class TileSectionConcavity {
  public static TileSection[] Sections;

  static TileSectionConcavity() {
    Sections = TileSection.WithRotations(TileSection.FromMultiString(SECTIONS3));
  }

  private static string SECTIONS3 =
@"
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

";

  private static string SECTIONS4 = @"
xxxx
x___
x___
xxxx

xxxx
x___
x___
x___

xxxx
_x__
_x__
xxxx

x_x_
x_xx
x___
xxxx

xxx_
x___
x__x
x_xx

xxxx
x__x
____
xxxx

___x
x__x
x__x
xxxx

xxxx
_xx_
__x_
xxx_
";
}