#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using DreamSeat;
#endregion

namespace RoomEditorApp
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdSubscribe : IExternalCommand
  {
    #region Obsolete Idling event handler replaced by external event
    /// <summary>
    /// How many Idling calls to wait before acting
    /// </summary>
    const int _update_interval = 100;

    /// <summary>
    /// How many Idling calls to wait before reporting
    /// </summary>
    const int _message_interval = 100;

    /// <summary>
    /// Number of Idling calls received in this session
    /// </summary>
    static int _counter = 0;

    /// <summary>
    /// Wait far a moment before requerying database.
    /// </summary>
    //static Stopwatch _stopwatch = null;

    void OnIdling(
      object sender,
      IdlingEventArgs ea )
    {
      using( JtTimer pt = new JtTimer( "OnIdling" ) )
      {
        // Use with care! This loads the CPU:

        ea.SetRaiseWithoutDelay();

        ++_counter;

        if( 0 == ( _counter % _update_interval ) )
        {
          if( 0 == ( _counter % _message_interval ) )
          {
            Util.Log( string.Format(
              "OnIdling called {0} times",
              _counter ) );
          }

          // Have we waited long enough since the last attempt?

          //if( null == _stopwatch
          //  || _stopwatch.ElapsedMilliseconds > 500 )

          RoomEditorDb rdb = new RoomEditorDb();
          //int n = rdb.LastSequenceNumber;

          if( rdb.LastSequenceNumberChanged(
            DbUpdater.LastSequence ) )
          {
            UIApplication uiapp = sender as UIApplication;
            Document doc = uiapp.ActiveUIDocument.Document;

            Util.Log( "furniture update begin" );

            //FilteredElementCollector rooms
            //  = new FilteredElementCollector( doc )
            //    .OfClass( typeof( SpatialElement ) )
            //    .OfCategory( BuiltInCategory.OST_Rooms );

            //IEnumerable<string> roomUniqueIds
            //  = rooms.Select<Element, string>(
            //    e => e.UniqueId );

            //CouchDatabase db = rdb.Db;

            //ChangeOptions opt = new ChangeOptions();

            //opt.IncludeDocs = true;
            //opt.Since = CmdUpdate.LastSequence;
            //opt.View = "roomedit/map_room_to_furniture";

            //CouchChanges<DbFurniture> changes
            //  = db.GetChanges<DbFurniture>( opt );

            //CouchChangeResult<DbFurniture>[] results
            //  = changes.Results;

            //DbUpdater updater = new DbUpdater(
            //  doc, roomUniqueIds );

            //foreach( CouchChangeResult<DbFurniture> result
            //  in results )
            //{
            //  updater.UpdateBimFurniture( result.Doc );

            //  CmdUpdate.LastSequence = result.Sequence;
            //}

            DbUpdater updater = new DbUpdater( uiapp );

            updater.UpdateBim();

            Util.Log( "furniture update end" );

            //  _stopwatch = new Stopwatch();
            //  _stopwatch.Start();
            //}
            //catch( Exception ex )
            //{
            //  //uiapp.Application.WriteJournalComment

            //  Debug.Print(
            //    "Room Editor: an error occurred "
            //    + "executing the OnIdling event:\r\n"
            //    + ex.ToString() );

            //  Debug.WriteLine( ex );
            //}
          }
        }
      }
    }
    #endregion // Obsolete Idling event handler replaced by external event

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      if( !App.Subscribed
        && -1 == DbUpdater.LastSequence )
      {
        DbUpdater.SetLastSequence();
      }

      DbUpdater.ToggleSubscription(
        commandData.Application );

      return Result.Succeeded;
    }
  }
}
