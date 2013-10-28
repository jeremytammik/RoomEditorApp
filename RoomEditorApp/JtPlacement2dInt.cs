#region Namespaces
using System;
using Autodesk.Revit.DB;
using System.Diagnostics;
#endregion

namespace RoomEditorApp
{
  /// <summary>
  /// A 2D integer-based transformation, 
  /// i.e. translation and rotation.
  /// </summary>
  class JtPlacement2dInt
  {
    /// <summary>
    /// Translation.
    /// </summary>
    public Point2dInt Translation { get; set; }

    /// <summary>
    /// Rotation in degrees.
    /// </summary>
    public int Rotation { get; set; }

    /// <summary>
    /// The family symbol UniqueId.
    /// </summary>
    public string SymbolId { get; set; }

    public JtPlacement2dInt( FamilyInstance fi )
    {
      LocationPoint lp = fi.Location as LocationPoint;

      Debug.Assert( null != lp,
        "expected valid family instanace location point" );

      Translation = new Point2dInt( lp.Point );

      Rotation = Util.ConvertRadiansToDegrees( lp.Rotation );

      SymbolId = fi.Symbol.UniqueId;
    }

    /// <summary>
    /// Return an SVG transform,
    /// either for native SVG or Raphael.
    /// </summary>
    public string SvgTransform
    {
      get
      {
        return string.Format(
          "R{2}T{0},{1}",
          //"translate({0},{1}) rotate({2})",
          Translation.X, 
          Util.SvgFlipY( Translation.Y ), 
          Util.SvgFlipY( Rotation ) );
      }
    }
  }
}
