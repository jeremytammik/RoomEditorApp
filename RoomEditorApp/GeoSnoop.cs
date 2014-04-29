#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ElementId = Autodesk.Revit.DB.ElementId;
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
    /// Width of the form to generate.
    /// </summary>
    const int _form_width = 400;

    /// <summary>
    /// Pen size.
    /// </summary>
    const int _pen_size = 1;

    /// <summary>
    /// Pen colour.
    /// </summary>
    static Color _pen_color = Color.Black;

    /// <summary>
    /// Margin around graphics between 
    /// sheet and form edge.
    /// </summary>
    const int _margin = 10;

    /// <summary>
    /// Margin around graphics between 
    /// BIM elements and viewport edge.
    /// </summary>
    const int _margin2 = 10;

    /// <summary>
    /// Our one and only pen. 
    /// </summary>
    static Pen _pen = null;

    /// <summary>
    /// Set up and return our one and only pen.
    /// </summary>
    static Pen Pen
    {
      get
      {
        if( null == _pen )
        {
          _pen = new Pen( _pen_color, _pen_size );
        }
        return _pen;
      }
    }

    /// <summary>
    /// Draw loops on graphics with the specified
    /// transform and graphics attributes.
    /// </summary>
    static void DrawLoopsOnGraphics(
      Graphics graphics,
      List<Point[]> loops,
      Matrix transform )
    {
      foreach( Point[] loop in loops )
      {
        GraphicsPath path = new GraphicsPath();

        transform.TransformPoints( loop );

        path.AddLines( loop );
        
        graphics.DrawPath( Pen, path );
      }
    }

    /// <summary>
    /// Display room and furniture in a temporary form
    /// generated on the fly.
    /// </summary>
    /// <param name="roomLoops">Room boundary loops</param>
    /// <param name="geometryLoops">Family symbol geometry</param>
    /// <param name="familyInstances">Family instances</param>
    public static Bitmap DisplayRoom(
      JtLoops roomLoops,
      Dictionary<string, JtLoop> geometryLoops,
      List<JtPlacement2dInt> familyInstances )
    {
      JtBoundingBox2dInt bbFrom = roomLoops.BoundingBox;

      // Adjust target rectangle height to the 
      // displayee loop height.

      int width = _form_width;
      int height = (int) (width * bbFrom.AspectRatio + 0.5);

      //SizeF fsize = new SizeF( width, height );

      //SizeF scaling = new SizeF( 1, 1 );
      //PointF translation = new PointF( 0, 0 );

      //GetTransform( fsize, bbFrom, 
      //  ref scaling, ref translation, true );

      //Matrix transform1 = new Matrix( 
      //  new Rectangle(0,0,width,height),
      //  bbFrom.GetParallelogramPoints());
      //transform1.Invert();

      // the bounding box fills the rectangle 
      // perfectly and completely, inverted and
      // non-uniformly distorted:

      //Point2dInt pmin = bbFrom.Min;
      //Rectangle rect = new Rectangle( 
      //  pmin.X, pmin.Y, bbFrom.Width, bbFrom.Height );
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
        bbFrom.Rectangle, parallelogramPoints );

      Bitmap bmp = new Bitmap( width, height );
      Graphics graphics = Graphics.FromImage( bmp );

      graphics.Clear( System.Drawing.Color.White );

      DrawLoopsOnGraphics( graphics,
        roomLoops.GetGraphicsPathLines(), transform );

      if( null != familyInstances )
      {
        List<Point[]> loops = new List<Point[]>( 1 );
        loops.Add( new Point[] { } );

        foreach( JtPlacement2dInt i in familyInstances )
        {
          Point2dInt v = i.Translation;
          Matrix placement = new Matrix();
          placement.Rotate( i.Rotation );
          placement.Translate( v.X, v.Y, MatrixOrder.Append );
          placement.Multiply( transform, MatrixOrder.Append );
          loops[0] = geometryLoops[i.SymbolId]
            .GetGraphicsPathLines();

          DrawLoopsOnGraphics( graphics, loops, placement );
        }
      }
      return bmp;
    }

    /// <summary>
    /// Display sheet, the views it contains, the BIM 
    /// parts and  family instances they display in a 
    /// temporary form generated on the fly.
    /// </summary>
    /// <param name="owner">Owner window</param>
    /// <param name="caption">Form caption</param>
    /// <param name="modal">Modal versus modeless</param>
    /// <param name="roomLoops">Sheet and viewport boundary loops</param>
    /// <param name="geometryLoopss">Family symbol and part geometry</param>
    /// <param name="familyInstances">Family instances</param>
    public static Bitmap DisplaySheet(
      ElementId sheetId,
      JtLoops sheetViewportLoops,
      SheetModelCollections modelCollections )
    {
      // Source rectangle.
 
      JtBoundingBox2dInt bbFrom = sheetViewportLoops
        .BoundingBox;

      // Adjust target rectangle height to the 
      // displayee loop height.

      int width = _form_width;
      int height = (int) ( width * bbFrom.AspectRatio + 0.5 );

      // Specify transformation target rectangle 
      // including a margin.

      int top = 0;
      int left = 0;
      int bottom = height - ( _margin + _margin );

      Point[] parallelogramPoints = new Point[] {
        new Point( left + _margin, bottom ), // upper left
        new Point( left + width - _margin, bottom ), // upper right
        new Point( left + _margin, top + _margin ) // lower left
      };

      // Transform from native loop coordinate system
      // (sheet) to target display coordinates form).

      Matrix transformSheetBbToForm = new Matrix(
        bbFrom.Rectangle, parallelogramPoints );

      Bitmap bmp = new Bitmap( width, height );
      Graphics graphics = Graphics.FromImage( bmp );

      graphics.Clear( System.Drawing.Color.White );

      // Display sheet and viewport rectangles.

      DrawLoopsOnGraphics( graphics,
        sheetViewportLoops.GetGraphicsPathLines(),
        transformSheetBbToForm );

      // Iterate over the views and display the 
      // elements for each one appropriately 
      // scaled and translated to fit.

      List<ViewData> views = modelCollections
        .ViewsInSheet[sheetId];

      Dictionary<ElementId, GeomData> geometryLookup 
        = modelCollections.Symbols;

      Matrix transformBimToViewport;
      JtBoundingBox2dInt bbTo;
      JtLoop loop;

      foreach( ViewData view in views )
      {
        ElementId vid = view.Id;

        if( !modelCollections.BimelsInViews
          .ContainsKey( vid ) )
        {
          // This is not a floor plan view, so
          // we have nothing to display in it.

          continue;
        }

        // Determine transform from model space to
        // the viewport associated with this view.

        bbFrom = view.BimBoundingBox;
        bbTo = view.ViewportBoundingBox;

        Debug.Print( view.ToString() );

        // Adjust target rectangle height to the 
        // displayee loop height.

        //height = (int) ( width * bbFrom.AspectRatio + 0.5 );

        // Specify transformation target rectangle 
        // including a margin, and center the target 
        // rectangle vertically.

        top = bbTo.Min.Y + _margin2;
        left = bbTo.Min.X + _margin2;
        bottom = bbTo.Max.Y - _margin2;
        width = bbTo.Width - (_margin2 + _margin2);

        parallelogramPoints = new Point[] {
          new Point( left, top ), // upper left
          new Point( left + width, top ), // upper right
          new Point( left, bottom ) // lower left
        };

        // Transform from native loop coordinate system
        // (sheet) to target display coordinates form).

        transformBimToViewport = new Matrix(
          bbFrom.Rectangle, parallelogramPoints );

        // Retrieve the list of BIM elements  
        // displayed in this view.

        List<ObjData> bimels = modelCollections
          .BimelsInViews[vid];

        List<Point[]> loops = new List<Point[]>( 1 );
        loops.Add( new Point[] { } );

        Matrix placement = new Matrix();

        foreach( ObjData bimel in bimels )
        {
          placement.Reset();

          InstanceData inst = bimel as InstanceData;

          if( null != inst )
          {
            loop = geometryLookup[inst.Symbol].Loop;
            Point2dInt v = inst.Placement.Translation;
            placement.Rotate( inst.Placement.Rotation );
            placement.Translate( v.X, v.Y, MatrixOrder.Append );
          }
          else
          {
            Debug.Assert( bimel is GeomData, "expected part with geometry" );

            loop = ((GeomData) bimel).Loop;
          }
          loops[0] = loop.GetGraphicsPathLines();

          placement.Multiply( transformBimToViewport, MatrixOrder.Append );
          placement.Multiply( transformSheetBbToForm, MatrixOrder.Append );

          DrawLoopsOnGraphics( graphics, loops, placement );
        }
      }
      return bmp;
    }

    /// <summary>
    /// Generate a form on the fly and display the 
    /// given bitmap image in it in a picture box.
    /// </summary>
    /// <param name="owner">Owner window</param>
    /// <param name="caption">Form caption</param>
    /// <param name="modal">Modal versus modeless</param>
    /// <param name="bmp">Bitmap image to display</param>
    public static void DisplayImageInForm(
      IWin32Window owner,
      string caption,
      bool modal,
      Bitmap bmp )
    {
      Form form = new Form();
      form.Text = caption;

      form.Size = new Size( bmp.Width + 7,
        bmp.Height + 13 );

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
