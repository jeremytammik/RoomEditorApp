#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using BoundarySegment = Autodesk.Revit.DB.BoundarySegment;
using ComponentManager = Autodesk.Windows.ComponentManager;
using IWin32Window = System.Windows.Forms.IWin32Window;
using DreamSeat;
#endregion

namespace RoomEditorApp
{
  [Transaction( TransactionMode.ReadOnly )]
  public class CmdUploadRooms : IExternalCommand
  {
    #region RoomSelectionFilter
    class RoomSelectionFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return e is Room;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return true;
      }
    }
    #endregion // RoomSelectionFilter

    static bool _debug_output = false;

    /// <summary>
    /// If curve tessellation is disabled, only
    /// straight line segments from start to end
    /// point are exported.
    /// </summary>
    static bool _tessellate_curves = true;

    /// <summary>
    /// Never tessellate a curve 
    /// shorter than this length.
    /// </summary>
    const double _min_tessellation_curve_length_in_feet = 0.2;

    /// <summary>
    /// Conversion factor from foot to quarter inch.
    /// </summary>
    const double _quarter_inch = 1.0 / (12 * 4);

    /// <summary>
    /// Retrieve the room plan view boundary 
    /// polygon loops and convert to 2D integer-based.
    /// For optimisation and consistency reasons, 
    /// convert all coordinates to integer values in
    /// millimetres. Revit precision is limited to 
    /// 1/16 of an inch, which is abaut 1.2 mm, anyway.
    /// </summary>
    static JtLoops GetRoomLoops( Room room )
    {
      SpatialElementBoundaryOptions opt 
        = new SpatialElementBoundaryOptions();

      opt.SpatialElementBoundaryLocation =
        SpatialElementBoundaryLocation.Center; // loops closed
        //SpatialElementBoundaryLocation.Finish; // loops not closed

      IList<IList<BoundarySegment>> loops = room.
        GetBoundarySegments( opt );

      int nLoops = loops.Count;

      JtLoops jtloops = new JtLoops( nLoops );

      foreach( IList<BoundarySegment> loop in loops )
      {
        int nSegments = loop.Count;

        JtLoop jtloop = new JtLoop( nSegments );

        XYZ p0 = null; // loop start point
        XYZ p; // segment start point
        XYZ q = null; // segment end point

        foreach( BoundarySegment seg in loop )
        {
          // Todo: handle non-linear curve.
          // Especially: if two long lines have a 
          // short arc in between them, skip the arc
          // and extend both lines.

          p = seg.Curve.GetEndPoint( 0 );

          jtloop.Add( new Point2dInt( p ) );

          Debug.Assert( null == q || q.IsAlmostEqualTo( p ),
            "expected last endpoint to equal current start point" );

          q = seg.Curve.GetEndPoint( 1 );

          if( _debug_output )
          {
            Debug.Print( "{0} --> {1}",
              Util.PointString( p ),
              Util.PointString( q ) );
          }
          if( null == p0 )
          {
            p0 = p; // save loop start point
          }
        }
        Debug.Assert( q.IsAlmostEqualTo( p0 ),
          "expected last endpoint to equal loop start point" );

        jtloops.Add( jtloop );
      }
      return jtloops;
    }

    //(9.03,10.13,0) --> (-14.59,10.13,0)
    //(-14.59,10.13,0) --> (-14.59,1.93,0)
    //(-14.59,1.93,0) --> (-2.45,1.93,0)
    //(-2.45,1.93,0) --> (-2.45,-3.98,0)
    //(-2.45,-3.98,0) --> (9.03,-3.98,0)
    //(9.03,-3.98,0) --> (9.03,10.13,0)
    //(0.98,-0.37,0) --> (0.98,1.93,0)
    //(0.98,1.93,0) --> (5.57,1.93,0)
    //(5.57,1.93,0) --> (5.57,-0.37,0)
    //(5.57,-0.37,0) --> (0.98,-0.37,0)

    //(9.03,10.13) --> (-14.59,10.13)
    //(-14.59,10.13) --> (-14.59,1.93)
    //(-14.59,1.93) --> (-2.45,1.93)
    //(-2.45,1.93) --> (-2.45,-3.98)
    //(-2.45,-3.98) --> (9.03,-3.98)
    //(9.03,-3.98) --> (9.03,10.13)
    //(0.98,-0.37) --> (0.98,1.93)
    //(0.98,1.93) --> (5.57,1.93)
    //(5.57,1.93) --> (5.57,-0.37)
    //(5.57,-0.37) --> (0.98,-0.37)

    //Room Rooms <212639 Room 1> has 2 loops:
    //  0: (2753,3087), (-4446,3087), (-4446,587), (-746,587), (-746,-1212), (2753,-1212)
    //  1: (298,-112), (298,587), (1698,587), (1698,-112)

    /// <summary>
    /// Return the element ids of all furniture and 
    /// equipment family instances contained in the 
    /// given room.
    /// </summary>
    static List<Element> GetFurniture( Room room )
    {
      BoundingBoxXYZ bb = room.get_BoundingBox( null );

      Outline outline = new Outline( bb.Min, bb.Max );

      BoundingBoxIntersectsFilter filter
        = new BoundingBoxIntersectsFilter( outline );

      Document doc = room.Document;

      // Todo: add category filters and other
      // properties to narrow down the results

      // what categories of family instances
      // are we interested in?

      BuiltInCategory[] bics = new BuiltInCategory[] {
        BuiltInCategory.OST_Furniture,
        BuiltInCategory.OST_PlumbingFixtures,
        BuiltInCategory.OST_SpecialityEquipment
      };

      LogicalOrFilter categoryFilter
        = new LogicalOrFilter( bics
          .Select<BuiltInCategory,ElementFilter>(
            bic => new ElementCategoryFilter( bic ) )
          .ToList<ElementFilter>() );

      FilteredElementCollector familyInstances 
        = new FilteredElementCollector( doc )
          .WhereElementIsNotElementType()
          .WhereElementIsViewIndependent()
          .OfClass( typeof( FamilyInstance ) )
          .WherePasses( categoryFilter )
          .WherePasses( filter );

      int roomid = room.Id.IntegerValue;

      List<Element> a = new List<Element>();
 
      foreach( FamilyInstance fi in familyInstances )
      {
        if( null != fi.Room
          && fi.Room.Id.IntegerValue.Equals( roomid ) )
        {
          Debug.Assert( fi.Location is LocationPoint,
            "expected all furniture to have a location point" );

          a.Add( fi );
        }
      }
      return a;
    }

    /// <summary>
    /// Add all plan view boundary loops from 
    /// given solid to the list of loops.
    /// The creation application argument is used to
    /// reverse the extrusion analyser output curves
    /// in case they are badly oriented.
    /// </summary>
    /// <returns>Number of loops added</returns>
    static int AddLoops( 
      Autodesk.Revit.Creation.Application creapp,
      JtLoops loops, 
      GeometryObject obj,
      ref int nExtrusionAnalysisFailures )
    {
      int nAdded = 0;

      Solid solid = obj as Solid;

      if( null != solid
        && 0 < solid.Faces.Size )
      {
        Plane plane = new Plane( XYZ.BasisX,
          XYZ.BasisY, XYZ.Zero );

        ExtrusionAnalyzer extrusionAnalyzer = null;

        try
        {
          extrusionAnalyzer = ExtrusionAnalyzer.Create(
            solid, plane, XYZ.BasisZ );
        }
        catch( Autodesk.Revit.Exceptions
          .InvalidOperationException )
        {
          ++nExtrusionAnalysisFailures;
          return nAdded;
        }

        Face face = extrusionAnalyzer
          .GetExtrusionBase();

        foreach( EdgeArray a in face.EdgeLoops )
        {
          int nEdges = a.Size;

          List<Curve> curves 
            = new List<Curve>( nEdges );

          XYZ p0 = null; // loop start point
          XYZ p; // edge start point
          XYZ q = null; // edge end point

          // Test ValidateCurveLoops

          //CurveLoop loopIfc = new CurveLoop();

          foreach( Edge e in a )
          {
            // This requires post-processing using
            // SortCurvesContiguous:

            Curve curve = e.AsCurve();

            if( _debug_output )
            {
              p = curve.GetEndPoint( 0 );
              q = curve.GetEndPoint( 1 );
              Debug.Print( "{0} --> {1}",
                Util.PointString( p ),
                Util.PointString( q ) );
            }

            // This returns the curves already
            // correctly oriented:

            curve = e.AsCurveFollowingFace( 
              face );

            if( _debug_output )
            {
              p = curve.GetEndPoint( 0 );
              q = curve.GetEndPoint( 1 );
              Debug.Print( "{0} --> {1} following face",
                Util.PointString( p ),
                Util.PointString( q ) );
            }

            curves.Add( curve );

            // Throws an exception saying "This curve 
            // will make the loop not contiguous. 
            // Parameter name: pCurve"

            //loopIfc.Append( curve );
          }

          // We never reach this point:

          //List<CurveLoop> loopsIfc 
          //  = new List<CurveLoop>( 1 );

          //loopsIfc.Add( loopIfc );

          //IList<CurveLoop> loopsIfcOut = ExporterIFCUtils
          //  .ValidateCurveLoops( loopsIfc, XYZ.BasisZ );

          // This is no longer needed if we use 
          // AsCurveFollowingFace instead of AsCurve:

          CurveUtils.SortCurvesContiguous( 
            creapp, curves, _debug_output );

          q = null;

          JtLoop loop = new JtLoop( nEdges );

          foreach( Curve curve in curves )
          {
            // Todo: handle non-linear curve.
            // Especially: if two long lines have a 
            // short arc in between them, skip the arc
            // and extend both lines.

            p = curve.GetEndPoint( 0 );

            Debug.Assert( null == q 
              || q.IsAlmostEqualTo( p, 1e-04 ),
              string.Format( 
                "expected last endpoint to equal current start point, not distance {0}", 
                (null == q ? 0 : p.DistanceTo( q ))  ) );

            q = curve.GetEndPoint( 1 );

            if( _debug_output )
            {
              Debug.Print( "{0} --> {1}",
                Util.PointString( p ),
                Util.PointString( q ) );
            }

            if( null == p0 )
            {
              p0 = p; // save loop start point
            }

            int n = -1;

            if( _tessellate_curves
              && _min_tessellation_curve_length_in_feet
                < q.DistanceTo( p ) )
            {
              IList<XYZ> pts = curve.Tessellate();
              n = pts.Count;

              Debug.Assert( 1 < n, "expected at least two points" );
              Debug.Assert( p.IsAlmostEqualTo( pts[0] ), "expected tessellation start equal curve start point" );
              Debug.Assert( q.IsAlmostEqualTo( pts[n-1] ), "expected tessellation end equal curve end point" );

              if( 2 == n )
              {
                n = -1; // this is a straight line
              }
              else
              {
                --n; // skip last point
                
                for( int i = 0; i < n; ++i )
                {
                  loop.Add( new Point2dInt( pts[i] ) );
                }
              }
            }

            // If tessellation is disabled,
            // or curve is too short to tessellate,
            // or has only two tessellation points,
            // just add the start point:

            if( -1 == n )
            {
              loop.Add( new Point2dInt( p ) );
            }
          }
          Debug.Assert( q.IsAlmostEqualTo( p0, 1e-05 ),
            string.Format( 
              "expected last endpoint to equal current start point, not distance {0}", 
              p0.DistanceTo( q ) ) );

          loops.Add( loop );

          ++nAdded;
        }
      }
      return nAdded;
    }

    /// <summary>
    /// Retrieve all plan view boundary loops from 
    /// all solids of given element. This initial 
    /// version passes each solid encountered in the 
    /// given element to the ExtrusionAnalyzer one
    /// at a time, which obviously results in multiple
    /// loops, many of which are contained within the 
    /// others. An updated version unites all the 
    /// solids first and then uses the ExtrusionAnalyzer
    /// once only to obtain the true outside shadow
    /// contour.
    /// </summary>
    static JtLoops GetPlanViewBoundaryLoopsMultiple( 
      Element e,
      ref int nFailures )
    {
      Autodesk.Revit.Creation.Application creapp 
        = e.Document.Application.Create;

      JtLoops loops = new JtLoops( 1 );

      //int nSolids = 0;

      Options opt = new Options();

      GeometryElement geo = e.get_Geometry( opt );

      if( null != geo )
      {
        Document doc = e.Document;

        if( e is FamilyInstance )
        {
          geo = geo.GetTransformed(
            Transform.Identity );
        }

        //GeometryInstance inst = null;

        foreach( GeometryObject obj in geo )
        {
          AddLoops( creapp, loops, obj, ref nFailures );

          //inst = obj as GeometryInstance;
        }

        //if( 0 == nSolids && null != inst )
        //{
        //  geo = inst.GetSymbolGeometry();

        //  foreach( GeometryObject obj in geo )
        //  {
        //    AddLoops( creapp, loops, obj, ref nFailures );
        //  }
        //}
      }
      return loops;
    }

    /// <summary>
    /// Retrieve all plan view boundary loops from 
    /// all solids of given element united together.
    /// If the element is a family instance, transform
    /// its loops from the instance placement 
    /// coordinate system back to the symbol 
    /// definition one.
    /// If no geometry can be determined, use the 
    /// bounding box instead.
    /// </summary>
    static JtLoops GetPlanViewBoundaryLoops(
      Element e,
      ref int nFailures )
    {
      Autodesk.Revit.Creation.Application creapp
        = e.Document.Application.Create;

      JtLoops loops = new JtLoops( 1 );

      Options opt = new Options();

      GeometryElement geo = e.get_Geometry( opt );

      if( null != geo )
      {
        Document doc = e.Document;

        if( e is FamilyInstance )
        {
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

        Solid union = null;

        Plane plane = new Plane( XYZ.BasisX,
          XYZ.BasisY, XYZ.Zero );

        foreach( GeometryObject obj in geo )
        {
          Solid solid = obj as Solid;

          if( null != solid
            && 0 < solid.Faces.Size )
          {
            // Some solids, e.g. in the standard 
            // content 'Furniture Chair - Office' 
            // cause an extrusion analyser failure,
            // so skip adding those.

            try
            {
              ExtrusionAnalyzer extrusionAnalyzer 
                = ExtrusionAnalyzer.Create(
                  solid, plane, XYZ.BasisZ );
            }
            catch( Autodesk.Revit.Exceptions
              .InvalidOperationException )
            {
              solid = null;
              ++nFailures;
            }

            if( null != solid )
            {
              if( null == union )
              {
                union = solid;
              }
              else
              {
                try
                {
                  union = BooleanOperationsUtils
                    .ExecuteBooleanOperation( union, solid,
                      BooleanOperationsType.Union );
                }
                catch( Autodesk.Revit.Exceptions
                  .InvalidOperationException )
                {
                  ++nFailures;
                }
              }
            }
          }
        }
        AddLoops( creapp, loops, union, ref nFailures );
      }
      if( 0 == loops.Count )
      {
        Debug.Print(
          "Unable to determine geometry for "
          + Util.ElementDescription( e )
          + "; using bounding box instead." );

        BoundingBoxXYZ bb;

        if( e is FamilyInstance )
        {
          bb = ( e as FamilyInstance ).Symbol
            .get_BoundingBox( null );
        }
        else
        {
          bb = e.get_BoundingBox( null );
        }
        JtLoop loop = new JtLoop( 4 );
        loop.Add( new Point2dInt( bb.Min ) );
        loop.Add( new Point2dInt( bb.Max.X, bb.Min.Y ) );
        loop.Add( new Point2dInt( bb.Max ) );
        loop.Add( new Point2dInt( bb.Min.X, bb.Max.Y ) );
        loops.Add( loop );
      }
      return loops;
    }

    /// <summary>
    /// List all the loops retrieved 
    /// from the given element.
    /// </summary>
    static void ListLoops( Element e, JtLoops loops )
    {
      int nLoops = loops.Count;

      Debug.Print( "{0} has {1} loop{2}{3}",
        Util.ElementDescription( e ), nLoops,
        Util.PluralSuffix( nLoops ),
        Util.DotOrColon( nLoops ) );

      int i = 0;

      foreach( JtLoop loop in loops )
      {
        Debug.Print( "  {0}: {1}", i++,
          loop.ToString() );
      }
    }

    public static void UploadRooms(
      Document doc,
      ICollection<ElementId> ids )
    {
      foreach( ElementId id in ids )
      {
        Element e = doc.GetElement( id );

        Debug.Assert( e is Room,
          "expected rooms only" );

        Room room = e as Room;

        BoundingBoxXYZ bb = room.get_BoundingBox( null );

        if( null == bb )
        {
          Util.ErrorMsg( string.Format( "Skipping room {0} "
            + "because it has no bounding box.",
            Util.ElementDescription( room ) ) );

          continue;
        }

        JtLoops roomLoops = GetRoomLoops( room );

        ListLoops( room, roomLoops );

        List<Element> furniture
          = GetFurniture( room );

        // Map symbol UniqueId to symbol loop

        Dictionary<string, JtLoop> furnitureLoops
          = new Dictionary<string, JtLoop>();

        // List of instances referring to symbols

        List<JtPlacement2dInt> furnitureInstances
          = new List<JtPlacement2dInt>(
            furniture.Count );

        int nFailures;

        foreach( FamilyInstance f in furniture )
        {
          FamilySymbol s = f.Symbol;

          string uid = s.UniqueId;

          if( !furnitureLoops.ContainsKey( uid ) )
          {
            nFailures = 0;

            JtLoops loops = GetPlanViewBoundaryLoops(
              f, ref nFailures );

            if( 0 < nFailures )
            {
              Debug.Print( "{0}: {1} extrusion analyser failure{2}",
                Util.ElementDescription( f ), nFailures,
                Util.PluralSuffix( nFailures ) );
            }
            ListLoops( f, loops );

            if( 0 < loops.Count )
            {
              // Assume first loop is outer one

              furnitureLoops.Add( uid, loops[0] );
            }
          }
          furnitureInstances.Add(
            new JtPlacement2dInt( f ) );
        }
        IWin32Window revit_window
          = new JtWindowHandle(
            ComponentManager.ApplicationWindow );

        string caption = doc.Title
          + " : " + doc.GetElement( room.LevelId ).Name
          + " : " + room.Name;

        GeoSnoop.DisplayLoops( revit_window,
          caption, false, roomLoops,
          furnitureLoops, furnitureInstances );

        DbUpload.DbUploadRoom( room, furniture, roomLoops,
          furnitureLoops, furnitureInstances );
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
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

      // Iterate over all pre-selected rooms

      List<ElementId> ids = null;

      Selection sel = uidoc.Selection;

      if( 0 < sel.Elements.Size )
      {
        foreach( Element e in sel.Elements )
        {
          if( !( e is Room ) )
          {
            Util.ErrorMsg( "Please pre-select only room"
              + " elements before running this command." );
            return Result.Failed;
          }

          if( null == ids )
          {
            ids = new List<ElementId>( 1 );
          }

          ids.Add( e.Id );
        }
      }

      // If no rooms were pre-selected, 
      // prompt for post-selection

      if( null == ids )
      {
        IList<Reference> refs = null;

        try
        {
          refs = sel.PickObjects( ObjectType.Element,
            new RoomSelectionFilter(),
            "Please select rooms." );
        }
        catch( Autodesk.Revit.Exceptions
          .OperationCanceledException )
        {
          return Result.Cancelled;
        }
        ids = new List<ElementId>(
          refs.Select<Reference, ElementId>(
            r => r.ElementId ) );
      }
      UploadRooms( doc, ids );

      DbUpdater.SetLastSequence();

      return Result.Succeeded;
    }
  }
}
