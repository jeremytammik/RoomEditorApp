#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
#endregion

namespace RoomEditorApp
{
#if NEED_BOUNDING_BOX_XYZ
  /// <summary>
  /// A bounding box for a collection of XYZ instances.
  /// The components of a tuple are read-only and cannot 
  /// be changed after instantiation, so I cannot use 
  /// that easily.
  /// The components of an XYZ are read-only and cannot 
  /// be changed except by re-instantiation, so I cannot 
  /// use that easily either.
  /// </summary>
  class JtBoundingBoxXyz // : Tuple<XYZ, XYZ>
  {
    /// <summary>
    /// Minimum and maximum X, Y and Z values.
    /// </summary>
    double xmin, ymin, zmin, xmax, ymax, zmax;

    /// <summary>
    /// Initialise to infinite values.
    /// </summary>
    public JtBoundingBoxXyz()
    //: base(
    //  new XYZ( double.MaxValue, double.MaxValue, double.MaxValue ),
    //  new XYZ( double.MinValue, double.MinValue, double.MinValue ) )
    {
      //Min = new XYZ( double.MaxValue, double.MaxValue, double.MaxValue );
      //Max = new XYZ( double.MinValue, double.MinValue, double.MinValue );
      xmin = ymin = zmin = double.MaxValue;
      xmax = ymax = zmax = double.MinValue;
    }

    public JtBoundingBoxXyz( BoundingBoxXYZ bb )
    {
      xmin = bb.Min.X;
      ymin = bb.Min.Y;
      zmin = bb.Min.Z;
      xmax = bb.Max.X;
      ymax = bb.Max.Y;
      zmax = bb.Max.Z;
    }

    /// <summary>
    /// Return current lower left corner.
    /// </summary>
    public XYZ Min
    {
      get { return new XYZ( xmin, ymin, zmin ); }
    }

    /// <summary>
    /// Return current upper right corner.
    /// </summary>
    public XYZ Max
    {
      get { return new XYZ( xmax, ymax, zmax ); }
    }

    public XYZ MidPoint
    {
      get { return 0.5 * ( Min + Max ); }
    }

    //public XYZ Min { get; set; }
    //public XYZ Max { get; set; }

    //public XYZ Min
    //{
    //  get { return T1; }
    //}

    //public XYZ Max
    //{
    //  get { return T2; }
    //}

    /// <summary>
    /// Expand bounding box to contain 
    /// the given new point.
    /// </summary>
    public void ExpandToContain( XYZ p )
    {
      if( p.X < xmin ) { xmin = p.X; }
      if( p.Y < ymin ) { ymin = p.Y; }
      if( p.Z < zmin ) { zmin = p.Z; }
      if( p.X > xmax ) { xmax = p.X; }
      if( p.Y > ymax ) { ymax = p.Y; }
      if( p.Z > zmax ) { zmax = p.Z; }

      //int i = 0;
      //while( i < 3 )
      //{
      //  if( p[i] < _a[i] ) { _a[i] = p[i]; }
      //  ++i;
      //}
      //int j = 0;
      //while( i < 6 )
      //{
      //  if( p[j] > _a[i] ) { _a[i] = p[j]; }
      //  ++i;
      //  ++j;
      //}
    }

    //public JtBoundingBoxXyz( List<List<XYZ>> xyzarraylist )
    //{
    //  //Tuple<XYZ, XYZ> minmax = new Tuple<XYZ, XYZ>(
    //  //  new XYZ( double.MaxValue, double.MaxValue, double.MaxValue ),
    //  //  new XYZ( double.MinValue, double.MinValue, double.MinValue ) );

    //  //xyzarraylist.Aggregate<XYZ, Tuple<XYZ,XYZ>>( minmax, (a, p) => 
    //  //Accu

    //  foreach( List<XYZ> a in xyzarraylist )
    //  {
    //    foreach( XYZ p in a )
    //    {
    //      ExpandToContain( p );
    //    }
    //  }
    //}

    /// <summary>
    /// Return the four bounding box corners 
    /// projected onto the XY plane.
    /// </summary>
    public UV[] XyCorners
    {
      get
      {
        return new UV[] {
          new UV( xmin, ymin ),
          new UV( xmax, ymin ),
          new UV( xmax, ymax ),
          new UV( xmin, ymax )
        };
      }
    }
  }
#endif // NEED_BOUNDING_BOX_XYZ
}
