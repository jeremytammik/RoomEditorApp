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
  /// Interactive user selection of plan views.
  /// </summary>
  public partial class FrmSelectViews : Form
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
    public FrmSelectViews( Document doc )
    {
      InitializeComponent();

      _doc = doc;
    }

    /// <summary>
    /// Candidate views are retrieved by a filtered 
    /// element collector on loading the form.
    /// </summary>
    private void FrmSelectViews_Load(
      object sender,
      EventArgs e )
    {
      List<ViewPlan> views = new List<ViewPlan>(
        new FilteredElementCollector( _doc )
          .OfClass( typeof( ViewPlan ) )
          .Cast<ViewPlan>()
          .Where<ViewPlan>( v => v.CanBePrinted
            && ViewType.FloorPlan == v.ViewType ) );

      checkedListBox1.DataSource = views;
      checkedListBox1.DisplayMember = "Name";

      // Set all entries to be initially checked.

      int n = checkedListBox1.Items.Count;

      for( int i = 0; i < n; ++i )
      {
        checkedListBox1.SetItemChecked( i, true );
      }
    }

    /// <summary>
    /// Selected views are accessible after the 
    /// form has been successfully completed.
    /// </summary>
    public List<ViewPlan> GetSelectedViews()
    {
      return checkedListBox1.CheckedItems
        .Cast<ViewPlan>().ToList<ViewPlan>();
    }
  }
}
