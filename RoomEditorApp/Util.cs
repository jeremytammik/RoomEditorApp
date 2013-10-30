#region Namespaces
using System;
using System.Diagnostics;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace RoomEditorApp
{
  class Util
  {
    #region Unit conversion
    const double _feet_to_mm = 25.4 * 12;

    public static int ConvertFeetToMillimetres(
      double d )
    {
      //return (int) ( _feet_to_mm * d + 0.5 );
      return (int) Math.Round( _feet_to_mm * d,
        MidpointRounding.AwayFromZero );
    }

    public static double ConvertMillimetresToFeet( int d )
    {
      return d / _feet_to_mm;
    }

    const double _radians_to_degrees = 180.0 / Math.PI;

    public static double ConvertDegreesToRadians( int d )
    {
      return d * Math.PI / 180.0;
    }

    public static int ConvertRadiansToDegrees(
      double d )
    {
      //return (int) ( _radians_to_degrees * d + 0.5 );
      return (int) Math.Round( _radians_to_degrees * d,
        MidpointRounding.AwayFromZero );
    }
    #endregion // Unit conversion

    #region Formatting
    /// <summary>
    /// Return an English plural suffix for the given
    /// number of items, i.e. 's' for zero or more
    /// than one, and nothing for exactly one.
    /// </summary>
    public static string PluralSuffix( int n )
    {
      return 1 == n ? "" : "s";
    }

    /// <summary>
    /// Return an English plural suffix 'ies' or
    /// 'y' for the given number of items.
    /// </summary>
    public static string PluralSuffixY( int n )
    {
      return 1 == n ? "y" : "ies";
    }

    /// <summary>
    /// Return a dot (full stop) for zero
    /// or a colon for more than zero.
    /// </summary>
    public static string DotOrColon( int n )
    {
      return 0 < n ? ":" : ".";
    }

    /// <summary>
    /// Return a string for a real number
    /// formatted to two decimal places.
    /// </summary>
    public static string RealString( double a )
    {
      return a.ToString( "0.##" );
    }

    /// <summary>
    /// Return a string representation in degrees
    /// for an angle given in radians.
    /// </summary>
    public static string AngleString( double angle )
    {
      return RealString( angle * 180 / Math.PI ) + " degrees";
    }

    /// <summary>
    /// Return a string for a UV point
    /// or vector with its coordinates
    /// formatted to two decimal places.
    /// </summary>
    public static string PointString( UV p )
    {
      return string.Format( "({0},{1})",
        RealString( p.U ),
        RealString( p.V ) );
    }

    /// <summary>
    /// Return a string for an XYZ point
    /// or vector with its coordinates
    /// formatted to two decimal places.
    /// </summary>
    public static string PointString( XYZ p )
    {
      return string.Format( "({0},{1},{2})",
        RealString( p.X ),
        RealString( p.Y ),
        RealString( p.Z ) );
    }

    /// <summary>
    /// Return a string describing the given element:
    /// .NET type name,
    /// category name,
    /// family and symbol name for a family instance,
    /// element id and element name.
    /// </summary>
    public static string ElementDescription(
      Element e )
    {
      if( null == e )
      {
        return "<null>";
      }

      // For a wall, the element name equals the
      // wall type name, which is equivalent to the
      // family name ...

      FamilyInstance fi = e as FamilyInstance;

      string typeName = e.GetType().Name;

      string categoryName = ( null == e.Category )
        ? string.Empty
        : e.Category.Name + " ";

      string familyName = ( null == fi )
        ? string.Empty
        : fi.Symbol.Family.Name + " ";

      string symbolName = ( null == fi
        || e.Name.Equals( fi.Symbol.Name ) )
          ? string.Empty
          : fi.Symbol.Name + " ";

      return string.Format( "{0} {1}{2}{3}<{4} {5}>",
        typeName, categoryName, familyName,
        symbolName, e.Id.IntegerValue, e.Name );
    }
    #endregion // Formatting

    #region Messages
    /// <summary>
    /// Display a short big message.
    /// </summary>
    public static void InfoMsg( string msg )
    {
      Debug.Print( msg );
      TaskDialog.Show( App.Caption, msg );
    }

    /// <summary>
    /// Display a longer smaller message.
    /// </summary>
    public static void InfoMsg2(
      string instruction,
      string msg )
    {
      Debug.Print( msg );
      TaskDialog dlg = new TaskDialog( App.Caption );
      dlg.MainInstruction = instruction;
      dlg.MainContent = msg;
      dlg.Show();
    }

    /// <summary>
    /// Display an error message.
    /// </summary>
    public static void ErrorMsg( string msg )
    {
      Debug.Print( msg );
      TaskDialog dlg = new TaskDialog( App.Caption );
      dlg.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
      dlg.MainInstruction = msg;
      dlg.Show();
    }
    #endregion // Messages

    #region Browse for directory
    public static bool BrowseDirectory(
      ref string path,
      bool allowCreate )
    {
      FolderBrowserDialog browseDlg
        = new FolderBrowserDialog();

      browseDlg.SelectedPath = path;
      browseDlg.ShowNewFolderButton = allowCreate;

      bool rc = ( DialogResult.OK
        == browseDlg.ShowDialog() );

      if( rc )
      {
        path = browseDlg.SelectedPath;
      }
      return rc;
    }
    #endregion // Browse for directory

    #region Flip SVG Y coordinates
    public static bool SvgFlip = true;

    /// <summary>
    /// Flip Y coordinate for SVG export.
    /// </summary>
    public static int SvgFlipY( int y )
    {
      return SvgFlip ? -y : y;
    }
    #endregion // Flip SVG Y coordinates
  }
}
