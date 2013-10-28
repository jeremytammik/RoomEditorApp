#region Namespaces
using System;
using System.Collections.Generic;
using DreamSeat;
#endregion

namespace RoomEditorApp
{
  /// <summary>
  /// Base class for all Jeremy Room Editor classes.
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
}
