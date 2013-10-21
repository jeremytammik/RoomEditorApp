#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using DreamSeat;
#endregion

namespace GetLoops
{
  class DbUpload
  {
    static Element GetProjectInfo( Document doc )
    {
      return new FilteredElementCollector( doc )
        .OfClass( typeof( ProjectInfo ) )
        .FirstElement();
    }

    /// <summary>
    /// Upload model, level, room and furniture data 
    /// to an IrisCouch hosted CouchDB data repository.
    /// </summary>
    static public void DbUploadRoom(
      Room room,
      List<Element> furniture,
      JtLoops roomLoops,
      Dictionary<string, JtLoop> furnitureLoops,
      List<JtPlacement2dInt> furnitureInstances )
    {
      CouchDatabase db = new RoomEditorDb().Db;

      Document doc = room.Document;

      Element projectInfo 
        = GetProjectInfo( doc );

      string uid = projectInfo.UniqueId;

      DbModel dbModel;

      if( db.DocumentExists( uid ) )
      {
        dbModel = db.GetDocument<DbModel>( uid );

        Debug.Assert(
          dbModel.Id.Equals( projectInfo.UniqueId ),
          "expected equal ids" );

        dbModel.Description = Util.ElementDescription(
          projectInfo );

        dbModel.Name = projectInfo.Document.Title;

        dbModel = db.UpdateDocument<DbModel>(
          dbModel );
      }
      else
      {
        dbModel = new DbModel();

        dbModel.Id = uid;
        dbModel.Description = Util.ElementDescription(
          projectInfo );

        dbModel.Name = projectInfo.Name;
        dbModel = db.CreateDocument<DbModel>( dbModel );
      }

      Level level = room.Level;

      uid = level.UniqueId;

      DbLevel dbLevel;

      if( db.DocumentExists( uid ) )
      {
        dbLevel = db.GetDocument<DbLevel>( uid );

        Debug.Assert(
          dbLevel.Id.Equals( level.UniqueId ),
          "expected equal ids" );

        dbLevel.Description = Util.ElementDescription(
          level );

        dbLevel.Name = level.Name;
        dbLevel.ModelId = projectInfo.UniqueId;

        dbLevel = db.UpdateDocument<DbLevel>(
          dbLevel );
      }
      else
      {
        dbLevel = new DbLevel();

        dbLevel.Id = uid;
        dbLevel.Description = Util.ElementDescription(
          level );

        dbLevel.Name = level.Name;
        dbLevel.ModelId = projectInfo.UniqueId;

        dbLevel = db.CreateDocument<DbLevel>(
          dbLevel );
      }

      uid = room.UniqueId;

      DbRoom dbRoom;

      if( db.DocumentExists( uid ) )
      {
        dbRoom = db.GetDocument<DbRoom>( uid );

        Debug.Assert(
          dbRoom.Id.Equals( room.UniqueId ),
          "expected equal ids" );

        dbRoom.Description = Util.ElementDescription(
          room );

        dbRoom.Name = room.Name;
        dbRoom.LevelId = level.UniqueId;
        dbRoom.Loops = roomLoops.SvgPath;
        dbRoom.ViewBox = roomLoops.BoundingBox.SvgViewBox;

        dbRoom = db.UpdateDocument<DbRoom>( dbRoom );
      }
      else
      {
        dbRoom = new DbRoom();

        dbRoom.Id = uid;
        dbRoom.Description = Util.ElementDescription(
          room );

        dbRoom.Name = room.Name;
        dbRoom.LevelId = level.UniqueId;
        dbRoom.Loops = roomLoops.SvgPath;
        dbRoom.ViewBox = roomLoops.BoundingBox.SvgViewBox;

        dbRoom = db.CreateDocument<DbRoom>( dbRoom );
      }

      foreach( KeyValuePair<string, JtLoop> p in furnitureLoops )
      {
        uid = p.Key;
        Element e = doc.GetElement( uid );
        if( db.DocumentExists( uid ) )
        {
          DbSymbol symbol = db.GetDocument<DbSymbol>(
            uid );

          symbol.Description = Util.ElementDescription( e );
          symbol.Name = e.Name;
          symbol.Loop = p.Value.SvgPath;

          symbol = db.UpdateDocument<DbSymbol>( symbol );
        }
        else
        {
          DbSymbol symbol = new DbSymbol();
          symbol.Id = uid;
          symbol.Description = Util.ElementDescription( e );
          symbol.Name = e.Name;
          symbol.Loop = p.Value.SvgPath;

          symbol = db.CreateDocument<DbSymbol>( symbol );
        }
      }

      foreach( FamilyInstance f in furniture )
      {
        uid = f.UniqueId;
        if( db.DocumentExists( uid ) )
        {
          DbFurniture dbf = db.GetDocument<DbFurniture>(
            uid );

          dbf.Description = Util.ElementDescription( f );
          dbf.Name = f.Name;
          dbf.RoomId = room.UniqueId;
          dbf.SymbolId = f.Symbol.UniqueId;
          dbf.Transform = new JtPlacement2dInt( f )
            .SvgTransform;

          dbf = db.UpdateDocument<DbFurniture>( dbf );
        }
        else
        {
          DbFurniture dbf = new DbFurniture();
          dbf.Id = uid;
          dbf.Description = Util.ElementDescription( f );
          dbf.Name = f.Name;
          dbf.RoomId = room.UniqueId;
          dbf.SymbolId = f.Symbol.UniqueId;
          dbf.Transform = new JtPlacement2dInt( f )
            .SvgTransform;

          dbf = db.CreateDocument<DbFurniture>( dbf );
        }
      }
    }
  }
}
