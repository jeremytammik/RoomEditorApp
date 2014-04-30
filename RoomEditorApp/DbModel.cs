#region Namespaces
using System;
using System.Collections.Generic;
using DreamSeat;
#endregion

namespace RoomEditorApp
{
  /// <summary>
  /// Base class for all Room Editor database classes.
  /// </summary>
  class DbObj : CouchDocument
  {
    protected DbObj()
    {
      Type = "obj";
    }
    public string Type { get; protected set; }
    public string Description { get; set; }
    public string Name { get; set; }
  }

  #region Model - Level - Room - Symbol - Furniture
  /// <summary>
  /// Current model, i.e. Revit project.
  /// </summary>
  class DbModel : DbObj
  {
    public DbModel()
    {
      Type = "model";
    }
  }

  /// <summary>
  /// Level.
  /// </summary>
  class DbLevel : DbObj
  {
    public DbLevel()
    {
      Type = "level";
    }
    public string ModelId { get; set; }
  }

  /// <summary>
  /// Room
  /// </summary>
  class DbRoom : DbObj
  {
    public DbRoom()
    {
      Type = "room";
    }
    public string LevelId { get; set; }
    public string Loops { get; set; }
    public string ViewBox { get; set; }
  }

  /// <summary>
  /// Family symbol, i.e. element type defining 
  /// the geometry, i.e. the 2D boundary loop.
  /// </summary>
  class DbSymbol : DbObj
  {
    public DbSymbol()
    {
      Type = "symbol";
    }
    public string Loop { get; set; }
  }

  /// <summary>
  /// Family instance, defining placement, i.e.
  /// transform, i.e. translation and rotation,
  /// and referring to the symbol geometry.
  /// </summary>
  class DbFurniture : DbObj
  {
    public DbFurniture()
    {
      Type = "furniture";
    }
    public string RoomId { get; set; }
    public string SymbolId { get; set; }
    public string Transform { get; set; }
  }
  #endregion // Model - Level - Room - Symbol - Furniture

  #region Obj2 - Sheet - View - Part - Instance
  /// <summary>
  /// Base class for all second-generation Room Editor classes.
  /// </summary>
  class DbObj2 : DbObj
  {
    protected DbObj2()
    {
      Type = "obj2";
    }
    public Dictionary<string, string> Properties { get; set; }
  }

  /// <summary>
  /// Sheet. Lives in a model. Contains views.
  /// </summary>
  class DbSheet : DbObj2
  {
    public DbSheet()
    {
      Type = "sheet";
    }
    public string ModelId { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
  }

  /// <summary>
  /// View. Lives on a sheet. Displays BIM elements.
  /// </summary>
  class DbView : DbObj2
  {
    public DbView()
    {
      Type = "view";
    }
    public string SheetId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string ViewBox { get; set; }
  }

  /// <summary>
  /// Part of the building, cannot move.
  /// A building part can appear in multiple views.
  /// It has its own graphical representation in 
  /// absolute global coordinates hence no placement.
  /// </summary>
  class DbPart : DbObj2
  {
    public DbPart()
    {
      Type = "part";
    }
    public string [] ViewIds { get; set; }
    public string Loop { get; set; }
  }

  /// <summary>
  /// Family instance, defining placement, i.e.
  /// transform, i.e. translation and rotation,
  /// and referring to the symbol geometry.
  /// </summary>
  class DbInstance : DbObj2
  {
    public DbInstance()
    {
      Type = "instance";
    }
    public string[] ViewIds { get; set; }
    public string SymbolId { get; set; }
    public string Transform { get; set; }
  }
  #endregion // Obj2 - Sheet - View - Part - Instance
}
