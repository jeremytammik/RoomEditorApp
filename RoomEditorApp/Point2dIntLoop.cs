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
  /// A closed polygon boundary loop.
  /// </summary>
  class JtLoop : List<Point2dInt>
  {
    public JtLoop( int capacity )
      : base( capacity )
    {
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
      Point[] loop = new Point[n + 1];
      i = 0;
      foreach( Point2dInt p in this )
      {
        loop[i++] = new Point( p.X, p.Y );
      }
      loop[i] = loop[0];
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
        return string.Join( " ", 
          this.Select<Point2dInt,string>( 
            (p,i) => p.SvgPath( i ) ) ) 
          + "Z";
      }
    }
  }

  /// <summary>
  /// A list of boundary loops.
  /// </summary>
  class JtLoops : List<JtLoop>
  {
    public JtLoops( int capacity )
      : base( capacity )
    {
    }

    /// <summary>
    /// Unite two collections of boundary 
    /// loops into one single one.
    /// </summary>
    public static JtLoops operator+( JtLoops a, JtLoops b )
    {
      int na = a.Count;
      int nb = b.Count;
      JtLoops sum = new JtLoops( na + nb );
      sum.AddRange( a );
      sum.AddRange( b );
      return sum;
    }

    /// <summary>
    /// Instantiate a new bounding box 
    /// containing these loops.
    /// </summary>
    public JtBoundingBox2dInt BoundingBox
    {
      get
      {
        JtBoundingBox2dInt bb = new JtBoundingBox2dInt();

        foreach( JtLoop loop in this )
        {
          foreach( Point2dInt p in loop )
          {
            bb.ExpandToContain( p );
          }
        }
        return bb;
      }
    }

    /// <summary>
    /// Return suitable input for the .NET 
    /// GraphicsPath.AddLines method to display the 
    /// loops in a form. Note that a closing segment 
    /// to connect the last point back to the first
    /// is added.
    /// </summary>
    public List<Point[]> GetGraphicsPathLines()
    {
      List<Point[]> loops 
        = new List<Point[]>( Count );
      
      foreach( JtLoop jloop in this )
      {
        loops.Add( jloop.GetGraphicsPathLines() );
      }
      return loops;
    }

    /// <summary>
    /// Return the concatenated SVG path 
    /// specifications for all the loops.
    /// </summary>
    public string SvgPath
    {
      get
      {
        return string.Join( " ",
          this.Select<JtLoop, string>(
            a => a.SvgPath ) );
      }
    }
  }
}
