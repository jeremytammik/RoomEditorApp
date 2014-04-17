#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
#endregion

namespace RoomEditorApp
{
  // based on 06972592 [How to get the shape of Structural Framing objects]
  // /a/j/adn/case/sfdc/06972592/src/FramingXsecAnalyzer/FramingXsecAnalyzer/GeoSnoop.cs

  /// <summary>
  /// Display a collection of loops in a .NET form.
  /// </summary>
  class GeoSnoop
  {
    /// <summary>
    /// Pen size.
    /// </summary>
    const int _pen_size = 1;

    /// <summary>
    /// Pen colour.
    /// </summary>
    static Color _pen_color = Color.Black;

    /// <summary>
    /// Margin around graphics.
    /// </summary>
    const int _margin = 10;

    /// <summary>
    /// Draw loops on graphics with the specified
    /// transform and graphics attributes.
    /// </summary>
    static void DrawLoopsOnGraphics(
      Graphics graphics,
      List<Point[]> loops,
      Matrix transform )
    {
      Pen pen = new Pen( _pen_color, _pen_size );

      foreach( Point[] loop in loops )
      {
        GraphicsPath path = new GraphicsPath();

        transform.TransformPoints( loop );

        path.AddLines( loop );
        
        graphics.DrawPath( pen, path );
      }
    }

    /// <summary>
    /// Display loops in a temporary form generated
    /// on the fly.
    /// </summary>
    /// <param name="owner">Owner window</param>
    /// <param name="caption">Form caption</param>
    /// <param name="modal">Modal versus modeless</param>
    /// <param name="roomLoops">Room boundary loops</param>
    /// <param name="furnitureLoops">Furniture symbol boundary loops</param>
    /// <param name="furnitureInstances">Furniture instances</param>
    public static void DisplayLoops(
      IWin32Window owner,
      string caption, 
      bool modal,
      JtLoops roomLoops,
      Dictionary<string, JtLoop> furnitureLoops = null,
      List<JtPlacement2dInt> furnitureInstances = null )
    {
      JtBoundingBox2dInt bb = roomLoops.BoundingBox;

      // Adjust target rectangle height to the 
      // displayee loop height.

      int width = 400;
      int height = (int) (width * bb.AspectRatio + 0.5);

      //SizeF fsize = new SizeF( width, height );

      //SizeF scaling = new SizeF( 1, 1 );
      //PointF translation = new PointF( 0, 0 );

      //GetTransform( fsize, bb, 
      //  ref scaling, ref translation, true );

      //Matrix transform1 = new Matrix( 
      //  new Rectangle(0,0,width,height),
      //  bb.GetParallelogramPoints());
      //transform1.Invert();

      // the bounding box fills the rectangle 
      // perfectly and completely, inverted and
      // non-uniformly distorted:

      //Point2dInt pmin = bb.Min;
      //Rectangle rect = new Rectangle( 
      //  pmin.X, pmin.Y, bb.Width, bb.Height );
      //Point[] parallelogramPoints = new Point [] {
      //  new Point( 0, 0 ), // upper left
      //  new Point( width, 0 ), // upper right
      //  new Point( 0, height ) // lower left
      //};

      // the bounding box fills the rectangle 
      // perfectly and completely, inverted and
      // non-uniformly distorted:

      // Specify transformation target rectangle 
      // including a margin.

      int bottom = height - (_margin + _margin);

      Point[] parallelogramPoints = new Point[] {
        new Point( _margin, bottom ), // upper left
        new Point( width - _margin, bottom ), // upper right
        new Point( _margin, _margin ) // lower left
      };

      // Transform from native loop coordinate system
      // to target display coordinates.

      Matrix transform = new Matrix( 
        bb.Rectangle, parallelogramPoints );

      Bitmap bmp = new Bitmap( width, height );
      Graphics graphics = Graphics.FromImage( bmp );

      graphics.Clear( System.Drawing.Color.White );

      DrawLoopsOnGraphics( graphics,
        roomLoops.GetGraphicsPathLines(), transform );

      if( null != furnitureLoops )
      {
        List<Point[]> loops = new List<Point[]>( 1 );
        loops.Add( new Point[] { } );

        foreach( JtPlacement2dInt i in furnitureInstances )
        {
          Point2dInt v = i.Translation;
          Matrix placement = new Matrix();
          placement.Rotate( i.Rotation );
          placement.Translate( v.X, v.Y, MatrixOrder.Append );
          placement.Multiply( transform, MatrixOrder.Append );
          loops[0] = furnitureLoops[i.SymbolId]
            .GetGraphicsPathLines();

          DrawLoopsOnGraphics( graphics, loops, placement );
        }
      }

      Form form = new Form();
      form.Text = caption;
      form.Size = new Size( width + 7, height + 13 );
      form.FormBorderStyle = FormBorderStyle
        .FixedToolWindow;

      PictureBox pb = new PictureBox();
      pb.Location = new System.Drawing.Point( 0, 0 );
      pb.Dock = System.Windows.Forms.DockStyle.Fill;
      pb.Size = bmp.Size;
      pb.Parent = form;
      pb.Image = bmp;

      if( modal )
      {
        form.ShowDialog( owner );
      }
      else
      {
        form.Show( owner );
      }
    }
  }
}
