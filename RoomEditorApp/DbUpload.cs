#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using DreamSeat;
#endregion

namespace RoomEditorApp
{
  class DbUpload
  {
    #region GetProjectInfo
    public static Element GetProjectInfo( Document doc )
    {
      return new FilteredElementCollector( doc )
        .OfClass( typeof( ProjectInfo ) )
        .FirstElement();
    }
    #endregion // GetProjectInfo

    #region GetDbModel
    static DbModel GetDbModel(
      CouchDatabase db,
      Element projectInfo )
    {
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
        dbModel = new DbModel( uid );

        dbModel.Description = Util.ElementDescription(
          projectInfo );

        dbModel.Name = projectInfo.Name;
        dbModel = db.CreateDocument<DbModel>( dbModel );
      }

      return dbModel;
    }
    #endregion // GetDbModel

    #region DbUploadRoom
    /// <summary>
    /// Upload model, level, room and furniture data 
    /// to an IrisCouch hosted CouchDB data repository.
    /// </summary>
    static public void DbUploadRoom(
      Room room,
      List<Element> furniture,
      JtLoops roomLoops,
      Dictionary<string, JtLoop> furnitureLoops )
    {
      CouchDatabase db = new RoomEditorDb().Db;

      Document doc = room.Document;

      Element projectInfo = GetProjectInfo( doc );

      DbModel dbModel = GetDbModel( db, projectInfo );

      Element level = doc.GetElement( room.LevelId );

      string uid = level.UniqueId;

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
        dbLevel = new DbLevel( uid );

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
        dbRoom = new DbRoom( uid );

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
          DbSymbol symbol = new DbSymbol( uid );

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
          DbFurniture dbf = new DbFurniture( uid );

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
    #endregion // DbUploadRoom

    #region DbUploadSheet
    #region JtUidSet
    /// <summary>
    /// String list uniquifier.
    /// Helper class to ensure that the list of views
    /// that a BIM element appears in contains no 
    /// duplicate entries.
    /// </summary>
    class JtUidSet : Dictionary<string, int>
    {
      public JtUidSet( List<string> uids )
      {
        foreach( string uid in uids )
        {
          Add( uid );
        }
      }

      public void Add( string uid )
      {
        if( ContainsKey( uid ) )
        {
          ++this[uid];
        }
        else
        {
          this[uid] = 1;
        }
      }

      public List<string> Uids
      {
        get
        {
          List<string> uids = new List<string>( Keys );
          uids.Sort();
          return uids;
        }
      }
    }
    #endregion // JtUidSet

    /// <summary>
    /// Upload model, sheet, views it contains and
    /// their BIM elements to a CouchDB data repository.
    /// </summary>
    static public void DbUploadSheet(
      ViewSheet sheet,
      JtLoops sheetViewportLoops,
      SheetModelCollections modelCollections )
    {
      bool pre_existing = false;

      RoomEditorDb rdb = new RoomEditorDb();
      CouchDatabase db = rdb.Db;

      // Sheet

      Document doc = sheet.Document;

      Element e = GetProjectInfo( doc );

      DbModel dbModel = GetDbModel( db, e );

      DbSheet dbSheet = rdb.GetOrCreate<DbSheet>(
        ref pre_existing, sheet.UniqueId );

      dbSheet.Description = Util.SheetDescription( sheet );
      dbSheet.Name = sheet.Name;
      dbSheet.ModelId = e.UniqueId;
      dbSheet.Width = sheetViewportLoops[0].BoundingBox.Width;
      dbSheet.Height = sheetViewportLoops[0].BoundingBox.Height;

      dbSheet = pre_existing
        ? db.UpdateDocument<DbSheet>( dbSheet )
        : db.CreateDocument<DbSheet>( dbSheet );

      // Symbols

      Dictionary<ElementId, GeomData> geometryLookup
        = modelCollections.Symbols;

      foreach( KeyValuePair<ElementId, GeomData> p 
        in geometryLookup )
      {
        ElementId id = p.Key;

        e = doc.GetElement( id );

        DbSymbol symbol = rdb.GetOrCreate<DbSymbol>(
          ref pre_existing, e.UniqueId );

        symbol.Description = Util.ElementDescription( e );
        symbol.Name = e.Name;
        symbol.Loop = p.Value.Loop.SvgPath;

        symbol = pre_existing
          ? db.UpdateDocument<DbSymbol>( symbol )
          : db.CreateDocument<DbSymbol>( symbol );
      }

      // Views and BIM elements

      List<ViewData> views = modelCollections
        .ViewsInSheet[sheet.Id];

      View view;
      DbView dbView;
      DbBimel dbBimel;
      DbInstance dbInstance = null;
      DbPart dbPart = null;
      JtBoundingBox2dInt bbFrom;
      JtBoundingBox2dInt bbTo;

      foreach( ViewData viewData in views )
      {
        ElementId vid = viewData.Id;

        if( !modelCollections.BimelsInViews
          .ContainsKey( vid ) )
        {
          // This is not a floor plan view, so
          // we have nothing to display in it.

          continue;
        }

        view = doc.GetElement( vid ) as View;

        dbView = rdb.GetOrCreate<DbView>( 
          ref pre_existing, view.UniqueId );

        dbView.Description = Util.ElementDescription( view );
        dbView.Name = view.Name;
        dbView.SheetId = dbSheet.Id;

        bbFrom = viewData.BimBoundingBox;
        bbTo = viewData.ViewportBoundingBox;

        dbView.X = bbTo.Min.X;
        dbView.Y = bbTo.Min.Y;
        dbView.Width = bbTo.Width;
        dbView.Height = bbTo.Height;

        dbView.BimX = bbFrom.Min.X;
        dbView.BimY = bbFrom.Min.Y;
        dbView.BimWidth = bbFrom.Width;
        dbView.BimHeight = bbFrom.Height;

        dbView = pre_existing
          ? db.UpdateDocument<DbView>( dbView )
          : db.CreateDocument<DbView>( dbView );

        // Retrieve the list of BIM elements  
        // displayed in this view.

        List<ObjData> bimels = modelCollections
          .BimelsInViews[vid];

        foreach( ObjData bimel in bimels )
        {
          e = doc.GetElement( bimel.Id );

          InstanceData inst = bimel as InstanceData;

          if( null != inst )
          {
            dbInstance = rdb.GetOrCreate<DbInstance>( 
              ref pre_existing, e.UniqueId );

            dbInstance.SymbolId = doc.GetElement( 
              inst.Symbol ).UniqueId;

            dbInstance.Transform = inst.Placement
              .SvgTransform;

            dbBimel = dbInstance;
          }
          else
          {
            Debug.Assert( bimel is GeomData, 
              "expected part with geometry" );

            dbPart = rdb.GetOrCreate<DbPart>(
              ref pre_existing, e.UniqueId );

            dbPart.Loop = ((GeomData) bimel ).Loop
              .SvgPath;

            dbBimel = dbPart;
          }
          dbBimel.Description = Util.ElementDescription( e );
          dbBimel.Name = e.Name;
          JtUidSet uids = new JtUidSet( dbBimel.ViewIds );
          uids.Add( view.UniqueId );
          dbBimel.ViewIds = uids.Uids;

          // Todo:
          //GetElementProperties( dbBimel.Properties, e );

          if( null != inst )
          {
            dbInstance = pre_existing
              ? db.UpdateDocument<DbInstance>( dbInstance )
              : db.CreateDocument<DbInstance>( dbInstance );
          }
          else
          {
            dbPart = pre_existing
              ? db.UpdateDocument<DbPart>( dbPart )
              : db.CreateDocument<DbPart>( dbPart );
          }

        }
      }
    }
    #endregion // DbUploadSheet
  }
}
