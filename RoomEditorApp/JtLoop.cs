#region Namespaces
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
#endregion

namespace RoomEditorApp
{
  /// <summary>
  /// A closed or open polygon boundary loop.
  /// </summary>
  class JtLoop : List<Point2dInt>
  {
    public bool Closed { get; set; }

    public JtLoop( int capacity )
      : base( capacity )
    {
      Closed = true;
    }

    /// <summary>
    /// Add another point to the collection.
    /// If the new point is identical to the last,
    /// ignore it. This will automatically suppress
    /// really small boundary segment fragments.
    /// </summary>
    public new void Add( Point2dInt p )
    {
      if( 0 == Count
        || 0 != p.CompareTo( this[Count - 1] ) )
      {
        base.Add( p );
      }
    }

    /// <summary>
    /// Add a point array to the collection.
    /// If the new point is identical to the last,
    /// ignore it. This will automatically suppress
    /// really small boundary segment fragments.
    /// </summary>
    public void Add( Point2dInt [] pts )
    {
      foreach( Point2dInt p in pts )
      {
        Add( p );
      }
    }

    /// <summary>
    /// Return a bounding box 
    /// containing this loop.
    /// </summary>
    public JtBoundingBox2dInt BoundingBox
    {
      get
      {
        JtBoundingBox2dInt bb = new JtBoundingBox2dInt();

        foreach( Point2dInt p in this )
        {
          bb.ExpandToContain( p );
        }
        return bb;
      }
    }

    /// <summary>
    /// Display as a string.
    /// </summary>
    public override string ToString()
    {
      return string.Join( ", ", this );
    }

    /// <summary>
    /// Return suitable input for the .NET 
    /// GraphicsPath.AddLines method to display this 
    /// loop in a form. Note that a closing segment 
    /// to connect the last point back to the first
    /// is added.
    /// </summary>
    public Point[] GetGraphicsPathLines()
    {
      int i, n;

      n = Count;

      if( Closed ) { ++n; }

      Point[] loop = new Point[n];

      i = 0;
      foreach( Point2dInt p in this )
      {
        loop[i++] = new Point( p.X, p.Y );
      }

      if( Closed ) { loop[i] = loop[0]; }

      return loop;
    }

    /// <summary>
    /// Return an SVG path specification, c.f.
    /// http://www.w3.org/TR/SVG/paths.html
    /// M [0] L [1] [2] ... [n-1] Z
    /// </summary>
    public string SvgPath
    {
      get
      {
        return
          string.Join( " ",
            this.Select<Point2dInt, string>(
              ( p, i ) => p.SvgPath( i ) ) )
          + ( Closed ? "Z" : "" );
      }
    }
  }
}
