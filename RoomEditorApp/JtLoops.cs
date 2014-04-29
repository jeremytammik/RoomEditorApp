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
    /// Return a bounding box 
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
