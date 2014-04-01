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
  /// Elements with the same element id equate to
  /// the same element. Without this, many, many,
  /// many duplicates.
  /// </summary>
  class ElementEqualityComparer
    : IEqualityComparer<Element>
  {
    public bool Equals( Element x, Element y )
    {
      return x.Id.IntegerValue.Equals(
        y.Id.IntegerValue );
    }

    public int GetHashCode( Element obj )
    {
      return obj.Id.IntegerValue.GetHashCode();
    }
  }
}
