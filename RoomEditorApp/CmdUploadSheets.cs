#region Namespaces
using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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

    #region Sheet and view transform
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

      //foreach( View v in sheet.Views ) // 2014

      foreach( View v in sheet.GetAllPlacedViews() // 2015
        .Select<ElementId, View>( id =>
          doc.GetElement( id ) as View ) )
      {
        GetViewTransform( v );
      }
    }
    #endregion // Sheet and view transform

    /// <summary>
    /// Upload given sheet and the views it contains
    /// to the cloud repository, ignoring all elements
    /// not belonging to one of the selected categories.
    /// </summary>
    static void UploadSheet(
      ViewSheet sheet,
      ElementFilter categoryFilter )
    {
      bool list_ignored_elements = false;

      Document doc = sheet.Document;

      Autodesk.Revit.Creation.Application creapp
        = doc.Application.Create;

      Options opt = new Options();

      // Map symbol UniqueId to symbol geometry

      Dictionary<string, JtLoop> symbolGeometry
        = new Dictionary<string, JtLoop>();

      // List of instances referring to symbols

      List<JtPlacement2dInt> familyInstances
        = new List<JtPlacement2dInt>();

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

        opt.View = v;

        FilteredElementCollector els
          = new FilteredElementCollector( doc, v.Id )
            .WherePasses( categoryFilter );

        foreach( Element e in els )
        {
          //Debug.Print( "  " + e.Name );

          GeometryElement geo = e.get_Geometry( opt );

          // Call GetTransformed on family instance geo.
          // This converts it from GeometryInstance to ?

          FamilyInstance f = e as FamilyInstance;

          if( null != f )
          {
            Location loc = e.Location;

            // Simply ignore family instances that
            // have no valid location, e.g. panel.

            if( null == loc )
            {
              if( list_ignored_elements )
              {
                Debug.Print( "    ... ignored "
                  + e.Name );
              }
              continue;
            }

            familyInstances.Add(
              new JtPlacement2dInt( f ) );

            FamilySymbol s = f.Symbol;

            string uid = s.UniqueId;

            if( symbolGeometry.ContainsKey( uid ) )
            {
              if( list_ignored_elements )
              {
                Debug.Print( "    ... already handled "
                  + e.Name + " --> " + s.Name );
              }
              continue;
            }

            // Replace this later to add real geometry.

            symbolGeometry.Add( uid, null );

            // Retrieve family instance geometry 
            // transformed back to symbol definition
            // coordinate space by inverting the 
            // family instance placement transformation

            LocationPoint lp = e.Location
              as LocationPoint;

            Transform t = Transform.CreateTranslation(
              -lp.Point );

            Transform r = Transform.CreateRotationAtPoint(
              XYZ.BasisZ, -lp.Rotation, lp.Point );

            geo = geo.GetTransformed( t * r );
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

            Debug.Assert( obj is Solid || obj is Line, "expected only solids and lines after calling GetTransformed on instances" );

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
                Debug.Assert( obj is Line, "expected only lines and solids" );

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
              }
              else
              {
                int n = solid.Edges.Size;

                if( 0 < n )
                {
                  Debug.Print(
                    "      solid with {0} edges", n );

                  foreach( Edge edge in solid.Edges )
                  {
                    Curve c = edge.AsCurve();

                    Debug.Print( "        {0}: {1} {2}",
                      edge.GetType().Name,
                      c.GetType().Name,
                      Util.CurveEndpointString( c ) );
                  }
                }
              }
            }
          }
        }
      }
    }

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
            e => e.Name ) ) + ".";

        // Determine all views displayed 
        // in the selected sheets.

        //JtViewSet views = sheets
        //  .Aggregate<ViewSheet, JtViewSet>(
        //    new JtViewSet(),
        //    ( a, v ) => a.AddViews( v.Views ) );

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

          foreach( ViewSheet sheet in sheets )
          {
            ListSheetAndViewTransforms( sheet );
            UploadSheet( sheet, categoryFilter );
          }
        }
      }
      return Result.Succeeded;
    }
  }
}
