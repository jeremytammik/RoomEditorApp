#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Form = System.Windows.Forms.Form;
#endregion // Namespaces

namespace RoomEditorApp
{
  /// <summary>
  /// Interactive user selection of drawing sheets.
  /// </summary>
  public partial class FrmSelectSheets : Form
  {
    /// <summary>
    /// The Revit input project document.
    /// </summary>
    Document _doc;

    /// <summary>
    /// Constructor initialises the Revit 
    /// document and nothing else.
    /// </summary>
    /// <param name="doc"></param>
    public FrmSelectSheets( Document doc )
    {
      InitializeComponent();

      _doc = doc;
    }

    /// <summary>
    /// Candidate sheets are retrieved by a filtered
    /// element collector on loading the form.
    /// </summary>
    private void OnLoad(
      object sender,
      EventArgs e )
    {
      List<ViewSheet> sheets = new List<ViewSheet>(
        new FilteredElementCollector( _doc )
          .OfClass( typeof( ViewSheet ) )
          .Cast<ViewSheet>()
          .Where<ViewSheet>( v => v.CanBePrinted
            && ViewType.DrawingSheet == v.ViewType ) );

      checkedListBox1.DataSource = sheets;
      checkedListBox1.DisplayMember = "Name";

      // Set all entries to be initially checked.

      int n = checkedListBox1.Items.Count;

      for( int i = 0; i < n; ++i )
      {
        checkedListBox1.SetItemChecked( i, true );
      }
    }

    /// <summary>
    /// Selected sheets are accessible after the 
    /// form has been successfully completed.
    /// </summary>
    public List<ViewSheet> GetSelectedSheets()
    {
      return checkedListBox1.CheckedItems
        .Cast<ViewSheet>().ToList<ViewSheet>();
    }
  }
}
