#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DreamSeat;
#endregion

namespace RoomEditorApp
{
  /// <summary>
  /// External command to either set the initial 
  /// sequence number or, if already set, download
  /// and apply all changes from the cloud since 
  /// then to the BIM.
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  public class CmdUpdate : IExternalCommand
  {
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

      if( -1 == DbUpdater.LastSequence )
      {
        DbUpdater.SetLastSequence();

        return Result.Succeeded;
      }

      //// Retrieve all room unique ids in model:

      //FilteredElementCollector rooms 
      //  = new FilteredElementCollector( doc )
      //    .OfClass( typeof( SpatialElement ) )
      //    .OfCategory( BuiltInCategory.OST_Rooms );

      //IEnumerable<string> roomUniqueIds 
      //  = rooms.Select<Element, string>( 
      //    e => e.UniqueId );

      ////string ids = "?keys=[%22" + string.Join(
      ////  "%22,%22", roomUniqueIds ) + "%22]";

      //// Retrieve furniture transformations 
      //// after last sequence number:

      //CouchDatabase db = new RoomEditorDb().Db;

      //ChangeOptions opt = new ChangeOptions();

      //opt.IncludeDocs = true;
      //opt.Since = LastSequence;
      //opt.View = "roomedit/map_room_to_furniture";

      //// I tried to add a filter to this view, but 
      //// that is apparently not supported by the 
      //// CouchDB or DreamSeat GetChanges functionality.
      ////+ ids; // failed attempt to filter view by room id keys

      //// Specify filter function defined in 
      //// design document to get updates
      ////opt.Filter = 

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

      //  LastSequence = result.Sequence;
      //}

      //DbUpdater updater = new DbUpdater( doc );

      DbUpdater updater = new DbUpdater( uiapp );

      updater.UpdateBim();

      return Result.Succeeded;
    }
  }
}
