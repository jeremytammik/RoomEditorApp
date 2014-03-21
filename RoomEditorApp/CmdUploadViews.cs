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
#endregion

namespace RoomEditorApp
{
  [Transaction( TransactionMode.ReadOnly )]
  public class CmdUploadViews : IExternalCommand
  {
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

      FrmSelectViews form = new FrmSelectViews( doc );

      if( DialogResult.OK == form.ShowDialog(
        revit_window ) )
      {
        List<ViewPlan> views = form.GetSelectedViews();

        int n = views.Count;

        string caption = string.Format(
          "{0} Plan View{1} Selected",
          n, Util.PluralSuffix( n ) );

        string list = string.Join( ", ",
          views.Select<Element, string>(
            e => e.Name ) );

        TaskDialog.Show( caption, list );

        //List<Category> categories = new List<Category>();
      }
      return Result.Succeeded;
    }
  }
}
