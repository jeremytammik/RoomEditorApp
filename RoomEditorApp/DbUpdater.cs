#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using DreamSeat;
#endregion

namespace GetLoops
{

  class DbUpdater
  {
    static public int LastSequence
    {
      get;
      set;
    }

    /// <summary>
    /// Determine and set the last sequence 
    /// number after updating database.
    /// </summary>
    static public int SetLastSequence()
    {
      LastSequence
        = new RoomEditorDb().LastSequenceNumber;

      Util.InfoMsg( string.Format(
        "Last sequence number set to {0}."
        + "\nChanges from now on will be applied.",
        LastSequence ) );

      return LastSequence;
    }

    /// <summary>
    /// Current Revit project document.
    /// </summary>
    Document _doc = null;

    /// <summary>
    /// Revit creation application for 
    /// generating transient geometry objects.
    /// </summary>
    Autodesk.Revit.Creation.Application _creapp = null;

    /// <summary>
    /// Store the unique ids of all room in this model
    /// in a dictionary for fast lookup to check 
    /// whether a given piece of furniture or 
    /// equipment really belongs to this model.
    /// </summary>
    Dictionary<string, int> _roomUniqueIdDict = null;

    public DbUpdater( Document doc )
    {
      _doc = doc;
      _creapp = doc.Application.Create;
    }

    /// <summary>
    /// Update a piece of furniture.
    /// Return true if anything was changed.
    /// </summary>
    bool UpdateBimFurniture( 
      DbFurniture f )
    {
      bool rc = false;

      if( !_roomUniqueIdDict.ContainsKey( f.RoomId ) )
      {
        Debug.Print( "Furniture instance '{0}' '{1}'"
          + " with UniqueId {2} belong to a room from"
          + " a different model, so ignore it.",
          f.Name, f.Description, f.Id );

        return rc;
      }

      Element e = _doc.GetElement( f.Id );

      if( null == e )
      {
        Util.ErrorMsg( string.Format(
          "Unable to retrieve element '{0}' '{1}' "
          + "with UniqueId {2}. Are you in the right "
          + "Revit model?", f.Name,
          f.Description, f.Id ) );

        return rc;
      }

      if( !( e is FamilyInstance ) )
      {
        Debug.Print( "Strange, we received an "
          + "updated '{0}' '{1}' with UniqueId {2}, "
          + "which we ignore.", f.Name,
          f.Description, f.Id );

        return rc;
      }

      // Convert SVG transform from string to int
      // to XYZ point and rotation in radians 
      // including flipping of Y coordinates.

      string svgTransform = f.Transform;

      char[] separators = new char[] { ',', 'R', 'T' };
      string[] a = svgTransform.Substring( 1 ).Split( separators );
      int[] trxy = a.Select<string, int>( s => int.Parse( s ) ).ToArray();

      double r = Util.ConvertDegreesToRadians(
        Util.SvgFlipY( trxy[0] ) );

      XYZ p = new XYZ(
        Util.ConvertMillimetresToFeet( trxy[1] ),
        Util.ConvertMillimetresToFeet( Util.SvgFlipY( trxy[2] ) ),
        0.0 );

      LocationPoint lp = e.Location as LocationPoint;

      XYZ translation = p - lp.Point;
      double rotation = r - lp.Rotation;

      if( .01 < translation.GetLength()
        || .01 < Math.Abs( rotation ) )
      {
        using( Transaction tx = new Transaction( _doc ) )
        {
          tx.Start( "Update Furniture and Equipmant Instance Placement" );

          if( .01 < translation.GetLength() )
          {
            ElementTransformUtils.MoveElement( _doc, e.Id, translation );
          }
          if( .01 < Math.Abs( rotation ) )
          {
            Line axis = _creapp.NewLineBound( lp.Point, lp.Point + XYZ.BasisZ );
            ElementTransformUtils.RotateElement( _doc, e.Id, axis, rotation );
          }
          tx.Commit();
          rc = true;
        }
      }
      return rc;
    }

    /// <summary>
    /// Apply all current cloud database 
    /// changes to the BIM.
    /// </summary>
    public void UpdateBim()
    {
      // Retrieve all room unique ids in model:

      FilteredElementCollector rooms
        = new FilteredElementCollector( _doc )
          .OfClass( typeof( SpatialElement ) )
          .OfCategory( BuiltInCategory.OST_Rooms );

      IEnumerable<string> roomUniqueIds
        = rooms.Select<Element, string>(
          e => e.UniqueId );

      // Convert to a dictionary for faster lookup:

      _roomUniqueIdDict
        = new Dictionary<string, int>(
          roomUniqueIds.Count() );

      foreach( string s in roomUniqueIds )
      {
        _roomUniqueIdDict.Add( s, 1 );
      }

      //string ids = "?keys=[%22" + string.Join(
      //  "%22,%22", roomUniqueIds ) + "%22]";

      // Retrieve all furniture transformations 
      // after the last sequence number:

      CouchDatabase db = new RoomEditorDb().Db;

      ChangeOptions opt = new ChangeOptions();

      opt.IncludeDocs = true;
      opt.Since = LastSequence;
      opt.View = "roomedit/map_room_to_furniture";

      // I tried to add a filter to this view, but 
      // that is apparently not supported by the 
      // CouchDB or DreamSeat GetChanges functionality.
      //+ ids; // failed attempt to filter view by room id keys

      // Specify filter function defined in 
      // design document to get updates
      //opt.Filter = 

      CouchChanges<DbFurniture> changes
        = db.GetChanges<DbFurniture>( opt );

      CouchChangeResult<DbFurniture>[] results
        = changes.Results;

      foreach( CouchChangeResult<DbFurniture> result
        in results )
      {
        UpdateBimFurniture( result.Doc );

        LastSequence = result.Sequence;
      }
    }
  }
}
