#region Namespaces
using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ComponentManager = Autodesk.Windows.ComponentManager;
using IWin32Window = System.Windows.Forms.IWin32Window;
using DialogResult = System.Windows.Forms.DialogResult;
using System.Diagnostics;
#endregion

namespace RoomEditorApp
{
  [Transaction( TransactionMode.ReadOnly )]
  public class CmdUploadSheets : IExternalCommand
  {
    #region JtViewSet - not used
    class JtViewSet : Dictionary<View, int>
    {
      public JtViewSet()
        : base( new ElementEqualityComparer() )
      {
      }

      public JtViewSet AddViews( ViewSet views )
      {
        foreach( View v in views )
        {
          if( !ContainsKey( v ) )
          {
            Add( v, 0 );
          }
          ++this[v];
        }
        return this;
      }
    }
    #endregion // JtViewSet - not used

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      IWin32Window revit_window
        = new JtWindowHandle(
          ComponentManager.ApplicationWindow );

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

      // Interactive sheet selection.

      FrmSelectSheets form = new FrmSelectSheets( doc );

      if( DialogResult.OK == form.ShowDialog(
        revit_window ) )
      {
        List<ViewSheet> sheets
          = form.GetSelectedSheets();

        int n = sheets.Count;

        string caption = string.Format(
          "{0} Sheet{1} Selected",
          n, Util.PluralSuffix( n ) );

        string msg = string.Join( ", ",
          sheets.Select<Element, string>(
            e => e.Name ) ) + ".";

        // Determine all views displayed 
        // in the selected sheets.

        //JtViewSet views = sheets
        //  .Aggregate<ViewSheet, JtViewSet>(
        //    new JtViewSet(),
        //    ( a, v ) => a.AddViews( v.Views ) );

        Dictionary<View, int> views
          = new Dictionary<View, int>(
            new ElementEqualityComparer() );

        int nFloorPlans = 0;

        foreach( ViewSheet sheet in sheets )
        {
          foreach( View v in sheet.Views )
          {
            if( !views.ContainsKey( v ) )
            {
              if( Util.IsSameOrSubclassOf( 
                  v.GetType(), typeof( ViewPlan ) )
                && v.CanBePrinted
                && ViewType.FloorPlan == v.ViewType )
              {
                ++nFloorPlans;
              }
              views.Add( v, 0 );
            }
            ++views[v];
          }
        }

        msg += ( 1 == n )
          ? "\nIt contains"
          : "\nThey contain";

        n = views.Count;

        msg += string.Format(
          " {0} view{1} including {2} floor plan{3}: ",
          n, Util.PluralSuffix( n ), nFloorPlans,
          Util.PluralSuffix( nFloorPlans ) );

        msg += string.Join( ", ",
          views.Keys.Select<Element, string>(
            e => e.Name ) ) + ".";

        Util.InfoMsg2( caption, msg );

        // Determine all categories occurring
        // in the views displayed by the sheets.

        List<Category> categories
          = new List<Category>(
            new CategoryCollector( views.Keys ).Keys );

        // Sort categories alphabetically by name
        // to display them in selection form.

        categories.Sort(
          delegate( Category c1, Category c2 )
          {
            return string.Compare( c1.Name, c2.Name );
          } );

        // Interactive category selection.

        FrmSelectCategories form2
          = new FrmSelectCategories( categories );

        if( DialogResult.OK == form2.ShowDialog(
          revit_window ) )
        {
          categories = form2.GetSelectedCategories();

          n = categories.Count;

          caption = string.Format(
            "{0} Categor{1} Selected",
            n, Util.PluralSuffixY( n ) );

          msg = string.Join( ", ",
            categories.Select<Category, string>(
              e => e.Name ) ) + ".";

          Util.InfoMsg2( caption, msg );
        }
      }
      return Result.Succeeded;
    }
  }
}

// 3 Plan Views Selected: Sheet view of Level 0 and 1, 3D, Level 0 Duplicate.
// They contain 4 views: Level 0, Level 1, {3D}, Dependent on Level 0.
// Selected 5 categories from 4 views displaying 1692 elements, 821 with HasMaterialQuantities=true
// 5 Categories Selected: Curtain Panels, Doors, Furniture, Structural Columns, Walls.
