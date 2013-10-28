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
    void OnIdling(
      object sender,
      IdlingEventArgs ea )
    {
      //Debug.Print( "OnIdling: {0}",
      //  DateTime.Now.ToString( "HH:mm:ss.fff" ) );

      // Use with care! This loads the CPU:

      ea.SetRaiseWithoutDelay();

      try
      {
        RoomEditorDb rdb = new RoomEditorDb();
        int n = rdb.LastSequenceNumber;

        if( n != DbUpdater.LastSequence )
        {
          UIApplication uiApp = sender as UIApplication;
          Document doc = uiApp.ActiveUIDocument.Document;

          Debug.Print( "Start furniture update: {0}",
            DateTime.Now.ToString( "HH:mm:ss.fff" ) );

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

          DbUpdater updater = new DbUpdater( doc );

          updater.UpdateBim();

          Debug.Print( "End furniture update: {0}",
            DateTime.Now.ToString( "HH:mm:ss.fff" ) );
        }
      }
      catch( Exception ex )
      {
        //uiApp.Application.WriteJournalComment

        Debug.Print(
          "Room Editor: an error occurred "
          + "executing the OnIdling event:\r\n"
          + ex.ToString() );

        Debug.WriteLine( ex );
      }
    }

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

      App.ToggleSubscription( 
        new EventHandler<IdlingEventArgs>( 
          OnIdling ) );

      return Result.Succeeded;
    }
  }
}
