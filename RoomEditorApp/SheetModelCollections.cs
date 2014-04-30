#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
#endregion

namespace RoomEditorApp
{
  /// <summary>
  /// Base class for symbol or BIM element, i.e.
  /// part or instance.
  /// </summary>
  class ObjData
  {
    public ElementId Id { get; set; }
  }

  /// <summary>
  /// Part or symbol geometry.
  /// </summary>
  class ViewData : ObjData
  {
    public JtBoundingBox2dInt BimBoundingBox { get; set; }
    public JtBoundingBox2dInt ViewportBoundingBox { get; set; }

    /// <summary>
    /// Display as a string.
    /// </summary>
    public override string ToString()
    {
      return string.Format( "view data {0} ({1},{2})", 
        Id, BimBoundingBox, ViewportBoundingBox );
    }
  }

  /// <summary>
  /// Part or symbol geometry.
  /// </summary>
  class GeomData : ObjData
  {
    public JtLoop Loop { get; set; }
  }

  /// <summary>
  /// Family instance defining placement and referring 
  /// to symbol. Can live in several views.
  /// </summary>
  class InstanceData : ObjData
  {
    public ElementId Symbol { get; set; }
    public JtPlacement2dInt Placement { get; set; }
    //public List<ElementId> Views { get; set; }
  }

  /// <summary>
  /// Package the collections of objects required
  /// to represent the model, sheets, views and BIM
  /// elements to export.
  /// </summary>
  class SheetModelCollections
  {
    public ElementId ProjectInfoId { get; set; }
    public List<ElementId> SheetIds { get; set; }
    public Dictionary<ElementId, List<ViewData>> ViewsInSheet { get; set; }
    public Dictionary<ElementId, GeomData> Symbols;
    //public Dictionary<ElementId, List<InstanceData>> InstancesInViews;
    //public Dictionary<ElementId, List<GeomData>> PartsInViews { get; set; }
    public Dictionary<ElementId, List<ObjData>> BimelsInViews;

    public SheetModelCollections( ElementId projectInfoId )
    {
      ProjectInfoId = projectInfoId;
      SheetIds = new List<ElementId>();
      ViewsInSheet = new Dictionary<ElementId, List<ViewData>>();
      Symbols = new Dictionary<ElementId, GeomData>();
      BimelsInViews = new Dictionary<ElementId, List<ObjData>>();
    }
  }
}
