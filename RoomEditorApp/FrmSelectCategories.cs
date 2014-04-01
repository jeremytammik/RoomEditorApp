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
  /// Interactive category selection form.
  /// </summary>
  public partial class FrmSelectCategories : Form
  {
    IList<Category> _categories;

    /// <summary>
    /// Initialise the category selector
    /// with the given list of categories.
    /// </summary>
    /// <param name="categories"></param>
    public FrmSelectCategories(
      IList<Category> categories )
    {
      InitializeComponent();

      _categories = categories;
    }

    /// <summary>
    /// Initialise the category selector with
    /// the list of categories passed in to 
    /// the constructor and check them all.
    /// </summary>
    private void FrmSelectCategories_Load(
      object sender,
      EventArgs e )
    {
      checkedListBox1.DataSource = _categories;
      checkedListBox1.DisplayMember = "Name";

      // Set all entries to be initially checked.

      int n = checkedListBox1.Items.Count;

      for( int i = 0; i < n; ++i )
      {
        checkedListBox1.SetItemChecked( i, true );
      }
    }

    /// <summary>
    /// Access the selected categories after the
    /// form has been successfully completed.
    /// </summary>
    public List<Category> GetSelectedCategories()
    {
      return checkedListBox1.CheckedItems
        .Cast<Category>().ToList<Category>();
    }
  }
}
