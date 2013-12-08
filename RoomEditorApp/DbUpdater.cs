#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DreamSeat;
#endregion

namespace RoomEditorApp
{
  class DbUpdater : IExternalEventHandler
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
      LastSequence = new RoomEditorDb()
        .LastSequenceNumber;

      Util.InfoMsg( string.Format(
        "Last sequence number set to {0}."
        + "\nChanges from now on will be applied.",
        LastSequence ) );

      return LastSequence;
    }

    /// <summary>
    /// Current Revit project document.
    /// </summary>
    //Document _doc = null;

    /// <summary>
    /// Revit UI application.
    /// </summary>
    UIApplication _uiapp = null;

    /// <summary>
    /// Revit creation application for 
    /// generating transient geometry objects.
    /// </summary>
    Autodesk.Revit.Creation.Application _creapp = null;

    /// <summary>
    /// Delegate to raise a database modified event.
    /// </summary>
    //public delegate ExternalEventRequest EventRaiser();

    /// <summary>
    /// External event to raise event 
    /// for pending database changes.
    /// </summary>
    static ExternalEvent _event = null;

    /// <summary>
    /// Separate thread running loop to
    /// check for pending database changes.
    /// </summary>
    static Thread _thread = null;

    /// <summary>
    /// Store the unique ids of all room in this model
    /// in a dictionary for fast lookup to check 
    /// whether a given piece of furniture or 
    /// equipment really belongs to this model.
    /// </summary>
    Dictionary<string, int> _roomUniqueIdDict = null;

    public DbUpdater( UIApplication uiapp )
    {
      using( JtTimer pt = new JtTimer( "DbUpdater ctor" ) )
      {
        //_doc = doc;
        _uiapp = uiapp;
        _creapp = _uiapp.Application.Create;
      }
    }

    /// <summary>
    /// Update a piece of furniture.
    /// Return true if anything was changed.
    /// </summary>
    bool UpdateBimFurniture(
      DbFurniture f )
    {
      Document doc = _uiapp.ActiveUIDocument.Document;

      bool rc = false;

      if( !_roomUniqueIdDict.ContainsKey( f.RoomId ) )
      {
        Debug.Print( "Furniture instance '{0}' '{1}'"
          + " with UniqueId {2} belong to a room from"
          + " a different model, so ignore it.",
          f.Name, f.Description, f.Id );

        return rc;
      }

      Element e = doc.GetElement( f.Id );

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
        using( Transaction tx = new Transaction(
          doc ) )
        {
          tx.Start( "Update Furniture and "
            + "Equipmant Instance Placement" );

          if( .01 < translation.GetLength() )
          {
            ElementTransformUtils.MoveElement(
              doc, e.Id, translation );
          }
          if( .01 < Math.Abs( rotation ) )
          {
            Line axis = Line.CreateBound( lp.Point,
              lp.Point + XYZ.BasisZ );

            ElementTransformUtils.RotateElement(
              doc, e.Id, axis, rotation );
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
      Util.Log( "UpdateBim begin" );

      using( JtTimer pt = new JtTimer( "UpdateBim" ) )
      {
        Document doc = _uiapp.ActiveUIDocument.Document;

        // Retrieve all room unique ids in model:

        FilteredElementCollector rooms
          = new FilteredElementCollector( doc )
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
      Util.Log( "UpdateBim end" );
    }

    /// <summary>
    /// Execute method invoked by Revit via the 
    /// external event as a reaction to a call 
    /// to its Raise method.
    /// </summary>
    public void Execute( UIApplication a )
    {
      // As far as I can tell, the external event 
      // should work fine even when switching between
      // different documents. That, however, remains
      // to be tested in more depth (or at all).

      //Document doc = a.ActiveUIDocument.Document;

      //Debug.Assert( doc.Title.Equals( _doc.Title ),
      //  "oops ... different documents ... test this" );

      UpdateBim();
    }

    /// <summary>
    /// Required IExternalEventHandler interface 
    /// method returning a descriptive name.
    /// </summary>
    public string GetName()
    {
      return string.Format(
        "{0} DbUpdater", App.Caption );
    }

    /// <summary>
    /// Count total number of checks for
    /// database updates made so far.
    /// </summary>
    static int _nLoopCount = 0;

    /// <summary>
    /// Count total number of checks for
    /// database updates made so far.
    /// </summary>
    static int _nCheckCount = 0;

    /// <summary>
    /// Count total number of database 
    /// updates requested so far.
    /// </summary>
    static int _nUpdatesRequested = 0;

    /// <summary>
    /// Wait far a moment before requerying database.
    /// </summary>
    //static Stopwatch _stopwatch = null;

    /// <summary>
    /// Number of milliseconds to wait and relinquish
    /// CPU control before next check for pending
    /// database updates.
    /// </summary>
    static int _timeout = 100;

    /// <summary>
    /// Repeatedly check database status and raise 
    /// external event when updates are pending.
    /// Relinquish control and wait for timeout
    /// period between each attempt. Run in a 
    /// separate thread.
    /// </summary>
    static void CheckForPendingDatabaseChanges()
    {
      while( true )
      {
        ++_nLoopCount;

        Debug.Assert( null != _event,
        "expected non-null external event" );

        if( _event.IsPending )
        {
          Util.Log( string.Format(
            "CheckForPendingDatabaseChanges loop {0} - "
            + "database update event is pending",
            _nLoopCount ) );
        }
        else
        {
          using( JtTimer pt = new JtTimer(
            "CheckForPendingDatabaseChanges" ) )
          {
            ++_nCheckCount;

            Util.Log( string.Format(
              "CheckForPendingDatabaseChanges loop {0} - "
              + "check for changes {1}",
              _nLoopCount, _nCheckCount ) );

            RoomEditorDb rdb = new RoomEditorDb();

            if( rdb.LastSequenceNumberChanged(
              DbUpdater.LastSequence ) )
            {
              _event.Raise();

              ++_nUpdatesRequested;

              Util.Log( string.Format(
                "database update pending event raised {0} times",
                _nUpdatesRequested ) );
            }
          }
        }

        // Wait a moment and relinquish control before
        // next check for pending database updates.

        Thread.Sleep( _timeout );
      }
    }

    /// <summary>
    /// Toggle subscription to automatic database 
    /// updates. Forward the call to the external 
    /// application that creates the external event,
    /// store it and launch a separate thread checking 
    /// for database updates. When changes are pending,
    /// invoke the external event Raise method.
    /// </summary>
    public static void ToggleSubscription(
      UIApplication uiapp )
    {
      _event = App.ToggleSubscription(
        new DbUpdater( uiapp ) );

      if( null == _event )
      {
        _thread.Abort();
        _thread = null;
      }
      else
      {
        // Start a new thread to regularly check the
        // database status and raise the external event
        // when updates are pending.

        _thread = new Thread(
          CheckForPendingDatabaseChanges );

        _thread.Start();
      }
    }
  }
}
