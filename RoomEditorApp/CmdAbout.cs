#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace GetLoops
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdAbout : IExternalCommand
  {
    const string _description
      = "Demonstrate round-trip editing a 2D rendering "
      + "of a Revit model on any mobile device with no "
      + "need for installation of any additional software "
      + "whatsoever beyond a browser. How can this be "
      + "achieved? A Revit add-in exports polygon "
      + "renderings of room boundaries and other elements "
      + "such as furniture and equipment to a cloud-based "
      + "repository implemented using a CouchDB NoSQL "
      + "database. On the mobile device, the repository "
      + "is queried and the data is rendered in a standard "
      + "browser using server-side generated JavaScript "
      + "and SVG. The rendering supports graphical editing, "
      + "specifically translation and rotation of the "
      + "furniture and equipment. Modified transformations "
      + "are saved back to the cloud database. The Revit "
      + "add-in picks up these changes and updates the "
      + "Revit model in real-time. All of the components "
      + "used are completely open source, except for Revit "
      + "itself.\r\n\r\n"
      + "Jeremy Tammik, Autodesk Inc., Tech Summit June 2013";

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      Util.InfoMsg2( 
        "Room furniture and equipment editor",
        _description );
      
      return Result.Succeeded;
    }
  }
}
