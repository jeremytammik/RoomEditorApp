#region Namespaces
using System;
using System.Diagnostics;
using System.Drawing;
#endregion

namespace RoomEditorApp
{
  /// <summary>
  /// A bounding box for a collection 
  /// of 2D integer points.
  /// </summary>
  class JtBoundingBox2dInt
  {
    /// <summary>
    /// Margin around graphics when 
    /// exporting SVG view box.
    /// </summary>
    const int _margin = 10;

    /// <summary>
    /// Minimum and maximum X and Y values.
    /// </summary>
    int xmin, ymin, xmax, ymax;

    /// <summary>
    /// Initialise to infinite values, e.g. empty box.
    /// </summary>
    public JtBoundingBox2dInt()
    {
      Init();
    }

    /// <summary>
    /// Initialise to infinite values, e.g. empty box.
    /// </summary>
    public void Init()
    {
      xmin = ymin = int.MaxValue;
      xmax = ymax = int.MinValue;
    }

    /// <summary>
    /// Return current lower left corner.
    /// </summary>
    public Point2dInt Min
    {
      get { return new Point2dInt( xmin, ymin ); }
    }

    /// <summary>
    /// Return current upper right corner.
    /// </summary>
    public Point2dInt Max
    {
      get { return new Point2dInt( xmax, ymax ); }
    }

    /// <summary>
    /// Return current center point.
    /// </summary>
    public Point2dInt MidPoint
    {
      get 
      { 
        return new Point2dInt( 
          (int)(0.5 * ( xmin + xmax )), 
          (int)(0.5 * ( ymin + ymax )) ); 
      }
    }

    /// <summary>
    /// Return current width.
    /// </summary>
    public int Width
    {
      get { return xmax - xmin; }
    }

    /// <summary>
    /// Return current height.
    /// </summary>
    public int Height
    {
      get { return ymax - ymin; }
    }

    /// <summary>
    /// Return aspect ratio, i.e. Height/Width.
    /// </summary>
    public double AspectRatio
    {
      get
      {
        return (double) Height / (double) Width;
      }
    }

    /// <summary>
    /// Return a System.Drawing.Rectangle for this.
    /// </summary>
    public Rectangle Rectangle
    {
      get
      {
        return new Rectangle( xmin, ymin,
          Width, Height );
      }
    }

    /// <summary>
    /// Expand bounding box to contain 
    /// the given new point.
    /// </summary>
    public void ExpandToContain( Point2dInt p )
    {
      if( p.X < xmin ) { xmin = p.X; }
      if( p.Y < ymin ) { ymin = p.Y; }
      if( p.X > xmax ) { xmax = p.X; }
      if( p.Y > ymax ) { ymax = p.Y; }
    }

    /// <summary>
    /// Expand bounding box to contain 
    /// the given other bounding box.
    /// </summary>
    public void ExpandToContain( JtBoundingBox2dInt b )
    {
      ExpandToContain( b.Min );
      ExpandToContain( b.Max );
    }

    ///// <summary>
    ///// Instantiate a new bounding box containing
    ///// the given loops.
    ///// </summary>
    //public JtBoundingBox2dInt( JtLoops loops )
    //{
    //  foreach( JtLoop loop in loops )
    //  {
    //    foreach( Point2dInt p in loop )
    //    {
    //      ExpandToContain( p );
    //    }
    //  }
    //}

    /// <summary>
    /// Return the four bounding box corners.
    /// </summary>
    public Point2dInt[] Corners
    {
      get
      {
        return new Point2dInt[] {
          Min,
          new Point2dInt( xmax, ymin ),
          Max,
          new Point2dInt( xmin, ymax )
        };
      }
    }

    /// <summary>
    /// Display as a string.
    /// </summary>
    public override string ToString()
    {
      return string.Format( "({0},{1})", Min, Max );
    }

    /// <summary>
    /// Return the SVG viewBox 
    /// of this bounding box.
    /// </summary>
    public string SvgViewBox
    {
      get
      {
        int left = xmin - _margin;
        int bottom = ymin - _margin;
        int w = Width + _margin + _margin;
        int h = Height + _margin + _margin;
        if( Util.SvgFlip )
        {
          bottom = Util.SvgFlipY( bottom ) - h;
        }
        return string.Format( 
          "{0} {1} {2} {3}",
          left, bottom, w, h );
      }
    }
  }
}
