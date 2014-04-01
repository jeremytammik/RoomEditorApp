#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
#endregion

namespace RoomEditorApp
{
  /// <summary>
  /// Collect all categories of all visible
  /// elements in a given set of views.
  /// </summary>
  class CategoryCollector : Dictionary<Category, int>
  {
    #region CategoryEqualityComparer
    /// <summary>
    /// Categories with the same element id equate to
    /// the same category. Without this, many, many,
    /// many duplicates.
    /// </summary>
    class CategoryEqualityComparer
      : IEqualityComparer<Category>
    {
      public bool Equals( Category x, Category y )
      {
        return x.Id.IntegerValue.Equals(
          y.Id.IntegerValue );
      }

      public int GetHashCode( Category obj )
      {
        return obj.Id.IntegerValue.GetHashCode();
      }
    }
    #endregion // CategoryEqualityComparer

    /// <summary>
    /// Number of view selected.
    /// </summary>
    int _nViews;

    /// <summary>
    /// Number of elements in all selected views 
    /// including repetitions.
    /// </summary>
    int _nElements;

    /// <summary>
    /// Number of elements whose category have 
    /// material quantities in all selected views
    /// including repetitions.
    /// </summary>
    int _nElementsWithCategorMaterialQuantities;

    public CategoryCollector( ICollection<View> views )
      : base( new CategoryEqualityComparer() )
    {
      _nViews = views.Count;
      _nElements = 0;
      _nElementsWithCategorMaterialQuantities = 0;

      if( 0 < _nViews )
      {
        FilteredElementCollector a;

        foreach( View v in views )
        {
          a = new FilteredElementCollector( v.Document, v.Id )
            .WhereElementIsViewIndependent();

          foreach( Element e in a )
          {
            ++_nElements;

            #region Research code
#if RESEARCH_CODE
            // Categories of all elements in the given set of views:
            // 14 Categories Selected: Cameras, Curtain Panels, Curtain Wall Grids, Curtain Wall Mullions, Doors, Elevations, Furniture, Project Base Point, Room Tags, Rooms, Structural Columns, Survey Point, Views, Walls

            // suppressing view specific elements eliminated Room Tags
            //if( !e.ViewSpecific )
            // 13 Categories Selected: Cameras, Curtain Panels, Curtain Wall Grids, Curtain Wall Mullions, Doors, Elevations, Furniture, Project Base Point, Rooms, Structural Columns, Survey Point, Views, Walls

            // suppressing elements with an empty bounding box eliminated Project Base Point and Survey Point
            //BoundingBoxXYZ box = e.get_BoundingBox( v );
            //if( null != box
            //  && !box.Max.IsAlmostEqualTo( box.Min ) )
            // 13 Categories Selected: Cameras, Curtain Panels, Curtain Wall Grids, Curtain Wall Mullions, Doors, Elevations, Furniture, Project Base Point, Rooms, Structural Columns, Survey Point, Views, Walls
#endif // RESEARCH_CODE
            #endregion // Research code

            Category cat = e.Category;

            if( null != cat
              && cat.HasMaterialQuantities )
            {
              ++_nElementsWithCategorMaterialQuantities;

              if( !ContainsKey( cat ) )
              {
                Add( cat, 0 );
              }
              ++this[cat];
            }
          }
        }
      }
      Debug.Print( "Selected {0} categor{1} from "
        + "{2} view{3} displaying {4} element{5}, "
        + "{6} with HasMaterialQuantities=true",
        Count, Util.PluralSuffixY( Count ),
        _nViews, Util.PluralSuffix( _nViews ),
        _nElements, Util.PluralSuffix( _nElements ),
        _nElementsWithCategorMaterialQuantities );
    }
  }
}
