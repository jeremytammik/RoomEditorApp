#region Namespaces
using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Bitmap = System.Drawing.Bitmap;
using ComponentManager = Autodesk.Windows.ComponentManager;
using IWin32Window = System.Windows.Forms.IWin32Window;
using DialogResult = System.Windows.Forms.DialogResult;
using System.Diagnostics;
#endregion

namespace RoomEditorApp
{
  [Transaction( TransactionMode.ReadOnly )]
  public class CmdUploadSheets : IExternalCommand
  {
    #region JtViewSet - not used
    class JtViewSet : Dictionary<View, int>
    {
      public JtViewSet()
        : base( new ElementEqualityComparer() )
      {
      }

      public JtViewSet AddViews( ViewSet views )
      {
        foreach( View v in views )
        {
          if( !ContainsKey( v ) )
          {
            Add( v, 0 );
          }
          ++this[v];
        }
        return this;
      }
    }
    #endregion // JtViewSet - not used

    /// <summary>
    /// Predicate to determine whether 
    /// the given view is a floor plan.
    /// </summary>
    static bool IsFloorPlan( View v )
    {
      bool rc = ViewType.FloorPlan == v.ViewType;

      Debug.Assert( rc == ( v is ViewPlan ),
        "expected all views of type "
        + "floor plan to be plan views" );

      Debug.Assert( !rc || v.CanBePrinted,
        "expected floor plan views to be printable" );

      return rc;
    }

    #region Sheet and view transform - debugging code, currently not used
    static void GetViewTransform( View v )
    {
      XYZ origin = v.Origin;

      BoundingBoxXYZ cropbox = v.CropBox;
      BoundingBoxUV outline = v.Outline;

      UV dOutline = outline.Max - outline.Min;
      XYZ dCropbox = cropbox.Max - cropbox.Min;

      double scaleX = dCropbox.X / dOutline.U;
      double scaleY = dCropbox.Y / dOutline.V;

      Debug.Print(
        "{0} {1} origin {2} scale {3} sx {4} sy {5} cropbox {6} d {7} outline {8} d {9}",
        v.Name, v.Id,
        Util.PointString( origin ),
        Util.RealString( v.Scale ),
        Util.RealString( scaleX ),
        Util.RealString( scaleY ),
        Util.BoundingBoxString( cropbox ),
        Util.PointString( dCropbox ),
        Util.BoundingBoxString( outline ),
        Util.PointString( dOutline ) );
    }

    /// <summary>
    /// List the size and location of the given 
    /// sheet, the views it contains, and the 
    /// transforms from Revit model space to the view
    /// and from the view to the sheet.
    /// </summary>
    static void ListSheetAndViewTransforms(
      ViewSheet sheet )
    {
      Document doc = sheet.Document;

      // http://thebuildingcoder.typepad.com/blog/2010/09/view-location-on-sheet.html

      GetViewTransform( sheet );

      foreach( ElementId id in sheet.GetAllViewports() )
      {
        Viewport vp = doc.GetElement( id ) as Viewport;
        XYZ center = vp.GetBoxCenter();
        Outline outline = vp.GetBoxOutline();

        XYZ diff = outline.MaximumPoint
          - outline.MinimumPoint;

        Debug.Print(
          "viewport {0} for view {1} outline {2} "
          + "diff {3} label outline {4}",
          vp.Id, vp.ViewId,
          Util.OutlineString( outline ),
          Util.PointString( diff ),
          Util.OutlineString( vp.GetLabelOutline() ) );
      }

      foreach( View v in sheet.GetAllPlacedViews()
        .Select<ElementId, View>( id =>
          doc.GetElement( id ) as View ) )
      {
        GetViewTransform( v );
      }
    }
    #endregion // Sheet and view transform - debugging code, currently not used

    #region Determine sheet and viewport size and location
    /// <summary>
    /// Return polygon loops representing the size 
    /// and location of given sheet and all the 
    /// viewports it contains, regardless of type.
    /// </summary>
    static JtLoops GetSheetViewportLoops(
      SheetModelCollections modelCollections,
      ViewSheet sheet )
    {
      Document doc = sheet.Document;

      List<Viewport> viewports = sheet
        .GetAllViewports()
        .Select<ElementId, Viewport>(
          id => doc.GetElement( id ) as Viewport )
        .ToList<Viewport>();

      int n = viewports.Count;

      modelCollections.ViewsInSheet[sheet.Id] 
        = new List<ViewData>( n );

      JtLoops sheetViewportLoops = new JtLoops( n + 1 );

      // sheet.get_BoundingBox( null ) returns (-100,-100),(100,100)

      BoundingBoxUV bb = sheet.Outline; // model coordinates (0,0), (2.76,1.95)

      JtBoundingBox2dInt ibb = new JtBoundingBox2dInt(); // millimeters (0,0),(840,...)

      ibb.ExpandToContain( new Point2dInt( bb.Min ) );
      ibb.ExpandToContain( new Point2dInt( bb.Max ) );

      JtLoop loop = new JtLoop( ibb.Corners );

      sheetViewportLoops.Add( loop );

      foreach( Viewport vp in viewports )
      {
        XYZ center = vp.GetBoxCenter(); // not used

        Outline outline = vp.GetBoxOutline();

        ibb.Init();

        ibb.ExpandToContain(
          new Point2dInt( outline.MinimumPoint ) );

        ibb.ExpandToContain(
          new Point2dInt( outline.MaximumPoint ) );

        loop = new JtLoop( ibb.Corners );

        sheetViewportLoops.Add( loop );

        ViewData d = new ViewData();
        d.Id = vp.ViewId;
        d.ViewportBoundingBox = loop.BoundingBox;

        modelCollections.ViewsInSheet[sheet.Id].Add( 
          d );
      }
      return sheetViewportLoops;
    }
    #endregion // Determine sheet and viewport size and location

    #region Determine visible elements, their graphics and placements
    /// <summary>
    /// Determine the visible elements belonging to the 
    /// specified categories in the views displayed by
    /// the given sheet and return their graphics and 
    /// instance placements.
    /// Ignore all but the first geometry loop retrieved.
    /// </summary>
    /// <param name="modelCollections">Data container</param>
    /// <param name="sheet">The view sheet</param>
    /// <param name="categoryFilter">The desired categories</param>
    static void GetBimGraphics(
      SheetModelCollections modelCollections,
      ViewSheet sheet,
      ElementFilter categoryFilter )
    {
      bool list_ignored_elements = false;

      Document doc = sheet.Document;

      Autodesk.Revit.Creation.Application creapp
        = doc.Application.Create;

      Options opt = new Options();

      // There is no need and no possibility to set 
      // the detail level when retrieving view geometry.
      // An attempt to specify the detail level will 
      // cause writing the opt.View property to throw
      // "DetailLevel is already set. When DetailLevel 
      // is set view-specific geometry can't be 
      // extracted."
      //
      //opt.DetailLevel = ViewDetailLevel.Coarse;

      Debug.Print( sheet.Name );

      foreach( ViewPlan v in sheet.GetAllPlacedViews()
        .Select<ElementId, View>( id =>
          doc.GetElement( id ) as View )
        .OfType<ViewPlan>()
        .Where<ViewPlan>( v => IsFloorPlan( v ) ) )
      {
        Debug.Print( "  " + v.Name );

        modelCollections.BimelsInViews.Add( 
          v.Id, new List<ObjData>() );

        opt.View = v;

        JtBoundingBox2dInt bimelBb 
          = new JtBoundingBox2dInt();

        FilteredElementCollector els
          = new FilteredElementCollector( doc, v.Id )
            .WherePasses( categoryFilter );

        foreach( Element e in els )
        {
          GeometryElement geo = e.get_Geometry( opt );

          FamilyInstance f = e as FamilyInstance;

          if( null != f )
          {
            LocationPoint lp = e.Location
              as LocationPoint;

            // Simply ignore family instances that
            // have no location point or no location at 
            // all, e.g. panel.
            // No, we should not ignore them, but 
            // treat tham as non-transformable parts.

            if( null == lp )
            {
              if( list_ignored_elements )
              {
                Debug.Print( string.Format( 
                  "    ...  {0} has no location",
                  e.Name ) );
              }
              f = null;

              geo = geo.GetTransformed( 
                Transform.Identity );
            }
            else
            {
              FamilySymbol s = f.Symbol;

              if( modelCollections.Symbols.ContainsKey( s.Id ) )
              {
                if( list_ignored_elements )
                {
                  Debug.Print( "    ... symbol already handled "
                    + e.Name + " --> " + s.Name );
                }

                // Symbol already defined, just add instance

                JtPlacement2dInt placement 
                  = new JtPlacement2dInt( f );

                // Expand bounding box around all BIM 
                // elements, ignoring the size of the 
                // actual geometry, assuming is is small
                // in comparison and the insertion point
                // lies within it.

                bimelBb.ExpandToContain( 
                  placement.Translation );

                InstanceData d = new InstanceData();
                d.Id = f.Id;
                d.Symbol = f.Symbol.Id;
                d.Placement = placement;

                modelCollections.BimelsInViews[v.Id]
                  .Add( d );

                continue;
              }

              // Retrieve family instance geometry 
              // transformed back to symbol definition
              // coordinate space by inverting the 
              // family instance placement transformation

              Transform t = Transform.CreateTranslation(
                -lp.Point );

              Transform r = Transform.CreateRotationAtPoint(
                XYZ.BasisZ, -lp.Rotation, lp.Point );

              geo = geo.GetTransformed( t * r );
            }
          }

          int nEmptySolids = 0;
          int nNonEmptySolids = 0;
          int nCurves = 0;
          int nOther = 0;

          foreach( GeometryObject obj in geo )
          {
            // This was true before calling GetTransformed.
            //Debug.Assert( obj is Solid || obj is GeometryInstance, "expected only solids and instances" );

            // This was true before calling GetTransformed.
            //Debug.Assert( ( obj is GeometryInstance ) == ( e is FamilyInstance ), "expected all family instances to have geometry instance" ); 

            Debug.Assert( obj is Solid || obj is Line || obj is Arc, "expected only solids, lines and arcs after calling GetTransformed on instances" );

            // Todo: handle arcs, e.g. tessellate

            Debug.Assert( Visibility.Visible == obj.Visibility, "expected only visible geometry objects" );

            Debug.Assert( obj.IsElementGeometry, "expected only element geometry" );
            //bool isElementGeometry = obj.IsElementGeometry;

            Solid solid = obj as Solid;

            if( null != solid )
            {
              if( 0 < solid.Edges.Size )
              {
                ++nNonEmptySolids;
              }
              else
              {
                ++nEmptySolids;
              }
            }
            else if( obj is Curve )
            {
              ++nCurves;
            }
            else
            {
              ++nOther;
            }
          }

          Debug.Print( "    {0}: {1} non-emtpy solids, "
            + "{2} empty, {3} curves, {4} other",
            e.Name, nNonEmptySolids, nEmptySolids,
            nCurves, nOther );

          JtLoops loops = null;

          if( 1 == nNonEmptySolids
            && 0 == nEmptySolids + nCurves + nOther )
          {
            int nFailures = 0;

            loops = CmdUploadRooms
              .GetPlanViewBoundaryLoopsGeo(
                creapp, geo, ref nFailures );
          }
          else
          {
            double z = double.MinValue;
            bool first = true;

            foreach( GeometryObject obj in geo )
            {
              // Do we need the graphics style?
              // It might give us horrible things like
              // colours etc.

              ElementId id = obj.GraphicsStyleId;

              //Debug.Print( "      " + obj.GetType().Name );

              Solid solid = obj as Solid;

              if( null == solid )
              {
                #region Debug code to ensure horizontal co-planar curves
#if DEBUG
                Debug.Assert( obj is Line || obj is Arc, "expected only lines and arcs here" );

                Curve c = obj as Curve;

                if( first )
                {
                  z = c.GetEndPoint( 0 ).Z;

                  Debug.Assert( Util.IsEqual( z, c.GetEndPoint( 1 ).Z ),
                    "expected a plan view with all Z values equal" );

                  first = false;
                }
                else
                {
                  Debug.Assert( Util.IsEqual( z, c.GetEndPoint( 0 ).Z ),
                    "expected a plan view with all Z values equal" );

                  Debug.Assert( Util.IsEqual( z, c.GetEndPoint( 1 ).Z ),
                    "expected a plan view with all Z values equal" );
                }

                Debug.Print( "      {0} {1}",
                  obj.GetType().Name,
                  Util.CurveEndpointString( c ) );
#endif // DEBUG
                #endregion // Debug code to ensure horizontal co-planar curves
              }
              else if( 1 == solid.Faces.Size )
              {
                Debug.Print(
                  "      solid with 1 face" );

                foreach( Face face in solid.Faces )
                {
                  #region Debug code to print out face edges
#if DEBUG
                  foreach( EdgeArray loop in
                    face.EdgeLoops )
                  {
                    foreach( Edge edge in loop )
                    {
                      // This returns the curves already
                      // correctly oriented:

                      Curve c = edge
                        .AsCurveFollowingFace( face );

                      Debug.Print( "        {0}: {1} {2}",
                        edge.GetType().Name,
                        c.GetType().Name,
                        Util.CurveEndpointString( c ) );
                    }
                  }
#endif // DEBUG
                  #endregion // Debug code to print out face edges

                  if( null == loops )
                  {
                    loops = new JtLoops( 1 );
                  }
                  loops.Add( CmdUploadRooms.GetLoop(
                    creapp, face ) );
                }
              }
              else
              {
                #region Debug code for exceptional cases
#if DEBUG_2
                Debug.Assert( 1 >= solid.Faces.Size, "expected at most one visible face in plan view for my simple solids" );

                int n = solid.Edges.Size;

                if( 0 < n )
                {
                  Debug.Print(
                    "      solid with {0} edges", n );

                  Face[] face2 = new Face[] { null, null };
                  Face face = null;

                  foreach( Edge edge in solid.Edges )
                  {
                    if( null == face2[0] )
                    {
                      face2[0] = edge.GetFace( 0 );
                      face2[1] = edge.GetFace( 1 );
                    }
                    else if( null == face )
                    {
                      if( face2.Contains<Face>( edge.GetFace( 0 ) ) )
                      {
                        face = edge.GetFace( 0 );
                      }
                      else if( face2.Contains<Face>( edge.GetFace( 1 ) ) )
                      {
                        face = edge.GetFace( 1 );
                      }
                      else
                      {
                        Debug.Assert( false,
                          "expected all edges to belong to one face" );
                      }
                    }
                    else
                    {
                      Debug.Assert( face == edge.GetFace( 0 )
                        || face == edge.GetFace( 1 ),
                        "expected all edges to belong to one face" );
                    }

                    Curve c = edge.AsCurve();

                    // This returns the curves already
                    // correctly oriented:

                    //Curve curve = e.AsCurveFollowingFace(
                    //  face );

                    Debug.Print( "        {0}: {1} {2}",
                      edge.GetType().Name,
                      c.GetType().Name,
                      Util.CurveEndpointString( c ) );
                  }
                }
#endif // DEBUG
                #endregion // Debug code for exceptional cases
              }
            }
          }

          // Save the part or instance and 
          // the geometry retrieved for it.
          // This is where we drop all geometry but
          // the first loop.

          if( null != loops )
          {
            GeomData gd = new GeomData();
            gd.Loop = loops[0];

            if( null == f )
            {
              // Add part with absolute geometry

              gd.Id = e.Id;

              modelCollections.BimelsInViews[v.Id].Add( 
                gd );

              // Expand bounding box around all BIM 
              // elements.

              bimelBb.ExpandToContain( 
                gd.Loop.BoundingBox );
            }
            else
            {
              // Define symbol and add instance

              JtPlacement2dInt placement
                = new JtPlacement2dInt( f );

              InstanceData id = new InstanceData();
              id.Id = f.Id;
              id.Symbol = f.Symbol.Id;
              id.Placement = placement;

              modelCollections.BimelsInViews[v.Id].Add( 
                id );

              gd.Id = f.Symbol.Id;

              modelCollections.Symbols.Add( 
                f.Symbol.Id,  gd );

              // Expand bounding box around all BIM 
              // elements.

              JtBoundingBox2dInt bb = gd.Loop.BoundingBox;
              Point2dInt vtrans = placement.Translation;
              bimelBb.ExpandToContain( bb.Min + vtrans );
              bimelBb.ExpandToContain( bb.Max + vtrans );
            }
          }
        }

        // Set up BIM bounding box for this view

        modelCollections.ViewsInSheet[sheet.Id].Find( 
          v2 => v2.Id.IntegerValue.Equals( 
            v.Id.IntegerValue ) ).BimBoundingBox 
              = bimelBb;
      }
    }
    #endregion // Determine visible elements, their graphics and placements

    #region External command mainline Execute method
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      IWin32Window revit_window
        = new JtWindowHandle(
          ComponentManager.ApplicationWindow );

      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      if( null == doc )
      {
        Util.ErrorMsg( "Please run this command in a valid"
          + " Revit project document." );
        return Result.Failed;
      }

      // Interactive sheet selection.

      FrmSelectSheets form = new FrmSelectSheets( doc );

      if( DialogResult.OK == form.ShowDialog(
        revit_window ) )
      {
        List<ViewSheet> sheets
          = form.GetSelectedSheets();

        int n = sheets.Count;

        string caption = string.Format(
          "{0} Sheet{1} Selected",
          n, Util.PluralSuffix( n ) );

        string msg = string.Join( ", ",
          sheets.Select<Element, string>(
            e => Util.SheetDescription( e ) ) ) + ".";

        // Determine all floor plan views displayed 
        // in the selected sheets.

        Dictionary<View, int> views
          = new Dictionary<View, int>(
            new ElementEqualityComparer() );

        int nFloorPlans = 0;

        foreach( ViewSheet sheet in sheets )
        {
          foreach( View v in sheet.GetAllPlacedViews()
            .Select<ElementId, View>( id =>
              doc.GetElement( id ) as View ) )
          {
            if( !views.ContainsKey( v ) )
            {
              if( IsFloorPlan( v ) )
              {
                ++nFloorPlans;
              }
              views.Add( v, 0 );
            }
            ++views[v];
          }
        }

        msg += ( 1 == n )
          ? "\nIt contains"
          : "\nThey contain";

        n = views.Count;

        msg += string.Format(
          " {0} view{1} including {2} floor plan{3}: ",
          n, Util.PluralSuffix( n ), nFloorPlans,
          Util.PluralSuffix( nFloorPlans ) );

        msg += string.Join( ", ",
          views.Keys.Select<Element, string>(
            e => e.Name ) ) + ".";

        Util.InfoMsg2( caption, msg, false );

        // Determine all categories occurring
        // in the views displayed by the sheets.

        List<Category> categories
          = new List<Category>(
            new CategoryCollector( views.Keys ).Keys );

        // Sort categories alphabetically by name
        // to display them in selection form.

        categories.Sort(
          delegate( Category c1, Category c2 )
          {
            return string.Compare( c1.Name, c2.Name );
          } );

        // Interactive category selection.

        FrmSelectCategories form2
          = new FrmSelectCategories( categories );

        if( DialogResult.OK == form2.ShowDialog(
          revit_window ) )
        {
          categories = form2.GetSelectedCategories();

          n = categories.Count;

          caption = string.Format(
            "{0} Categor{1} Selected",
            n, Util.PluralSuffixY( n ) );

          msg = string.Join( ", ",
            categories.Select<Category, string>(
              e => e.Name ) ) + ".";

          Util.InfoMsg2( caption, msg, false );

          // Convert category list to a dictionary for 
          // more effective repeated lookup.
          //
          //Dictionary<ElementId, Category> catLookup =
          //  categories.ToDictionary<Category, ElementId>(
          //    c => c.Id );
          //
          // No, much better: set up a reusable element 
          // filter for the categories of interest:

          ElementFilter categoryFilter
            = new LogicalOrFilter( categories
              .Select<Category, ElementCategoryFilter>(
                c => new ElementCategoryFilter( c.Id ) )
              .ToList<ElementFilter>() );

          // Instantiate a container for all
          // cloud data repository content.

          SheetModelCollections modelCollections 
            = new SheetModelCollections( 
              DbUpload.GetProjectInfo( doc ).Id );

          foreach( ViewSheet sheet in sheets )
          {
            // Define preview form caption.

            caption = "Sheet and Viewport Loops - " 
              + Util.SheetDescription( sheet );

            // This is currently not used for anything.

            ListSheetAndViewTransforms( sheet );

            // Determine the polygon loops representing 
            // the size and location of given sheet and 
            // the viewports it contains.

            JtLoops sheetViewportLoops 
              = GetSheetViewportLoops( 
                modelCollections, sheet );

            // Determine graphics for family instances,
            // their symbols and other BIM parts.

            GetBimGraphics( modelCollections, 
              sheet, categoryFilter );

            // Display sheet and viewports with the 
            // geometry retrieved in a temporary GeoSnoop 
            // form generated on the fly for debugging 
            // purposes.

            Bitmap bmp = GeoSnoop.DisplaySheet( 
              sheet.Id, sheetViewportLoops, 
              modelCollections );

            GeoSnoop.DisplayImageInForm( 
              revit_window, caption, false, bmp );

            // Upload data to the cloud database.

            DbUpload.DbUploadSheet( sheet, 
              sheetViewportLoops, modelCollections );
          }
          DbUpdater.SetLastSequence();
        }
      }
      return Result.Succeeded;
    }
    #endregion // External command mainline Execute method
  }
}
