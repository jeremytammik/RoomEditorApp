#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace RoomEditorApp
{
  class Util
  {
    #region Geometrical Comparison
    const double _eps = 1.0e-9;

    public static bool IsZero(
      double a,
      double tolerance )
    {
      return tolerance > Math.Abs( a );
    }

    public static bool IsZero( double a )
    {
      return IsZero( a, _eps );
    }

    public static bool IsEqual( double a, double b )
    {
      return IsZero( b - a );
    }
    #endregion // Geometrical Comparison

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

    /// <summary>
    /// Return true if the type b is either a 
    /// subclass of OR equal to the base class itself.
    /// IsSubclassOf returns false if the two types
    /// are the same. It only returns true for true
    /// non-equal subclasses.
    /// </summary>
    public static bool IsSameOrSubclassOf(
      Type a,
      Type b )
    {
      // http://stackoverflow.com/questions/2742276/in-c-how-do-i-check-if-a-type-is-a-subtype-or-the-type-of-an-object

      return a.IsSubclassOf( b ) || a == b;
    }
    #endregion // Unit conversion

    #region Formatting
    /// <summary>
    /// Uncapitalise string, i.e. 
    /// lowercase its first character.
    /// </summary>
    public static string Uncapitalise( string s )
    {
      return Char.ToLowerInvariant( s[0] )
        + s.Substring( 1 );
    }

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
  /// Return an English pluralised string for the 
  /// given thing or things. If the thing ends with
  /// 'y', the plural is assumes to end with 'ies', 
  /// e.g. 
  /// (2, 'chair') -- '2 chairs'
  /// (2, 'property') -- '2 properties'
  /// (2, 'furniture item') -- '2 furniture items'
  /// If in doubt, appending 'item' or 'entry' to 
  /// the thing description is normally a pretty 
  /// safe bet. Replaces calls to PluralSuffix 
  /// and PluralSuffixY.
  /// </summary>
  public static string PluralString(
    int n,
    string thing )
  {
    if( 1 == n )
    {
      return "1 " + thing;
    }

    int i = thing.Length - 1;
    char cy = thing[i];

    return n.ToString() + " " + ( ( 'y' == cy )
      ? thing.Substring( 0, i ) + "ies"
      : thing + "s" );
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
    /// Return a string for an XYZ 
    /// point or vector with its coordinates
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
    /// Return a string for the XY values of an XYZ 
    /// point or vector with its coordinates
    /// formatted to two decimal places.
    /// </summary>
    public static string PointString2d( XYZ p )
    {
      return string.Format( "({0},{1})",
        RealString( p.X ),
        RealString( p.Y ) );
    }

    /// <summary>
    /// Return a string displaying the two XYZ 
    /// endpoints of a geometry curve element.
    /// </summary>
    public static string CurveEndpointString( Curve c )
    {
      return string.Format( "({0},{1})",
        PointString2d( c.GetEndPoint( 0 ) ),
        PointString2d( c.GetEndPoint( 1 ) ) );
    }

    /// <summary>
    /// Return a string displaying only the XY values
    /// of the two XYZ endpoints of a geometry curve 
    /// element.
    /// </summary>
    public static string CurveEndpointString2d( Curve c )
    {
      return string.Format( "({0},{1})",
        PointString( c.GetEndPoint( 0 ) ),
        PointString( c.GetEndPoint( 1 ) ) );
    }

    /// <summary>
    /// Return a string for a 2D bounding box
    /// formatted to two decimal places.
    /// </summary>
    public static string BoundingBoxString(
      BoundingBoxUV b )
    {
      //UV d = b.Max - b.Min;

      return string.Format( "({0},{1})",
        PointString( b.Min ),
        PointString( b.Max ) );
    }

    /// <summary>
    /// Return a string for a 3D bounding box
    /// formatted to two decimal places.
    /// </summary>
    public static string BoundingBoxString(
      BoundingBoxXYZ b )
    {
      //XYZ d = b.Max - b.Min;

      return string.Format( "({0},{1})",
        PointString( b.Min ),
        PointString( b.Max ) );
    }

    /// <summary>
    /// Return a string for an Outline
    /// formatted to two decimal places.
    /// </summary>
    public static string OutlineString( Outline o )
    {
      //XYZ d = o.MaximumPoint - o.MinimumPoint;

      return string.Format( "({0},{1})",
        PointString( o.MinimumPoint ),
        PointString( o.MaximumPoint ) );
    }
    #endregion // Formatting

    #region Element properties
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

    /// <summary>
    /// Return a string describing the given sheet:
    /// sheet number and name.
    /// </summary>
    public static string SheetDescription(
      Element e )
    {
      string sheet_number = e.get_Parameter(
        BuiltInParameter.SHEET_NUMBER )
          .AsString();

      return string.Format( "{0} - {1}",
        sheet_number, e.Name );
    }

    /// <summary>
    /// Return a dictionary of all the given 
    /// element parameter names and values.
    /// </summary>
    public static bool IsModifiable( Parameter p )
    {
      StorageType st = p.StorageType;

      return !( p.IsReadOnly )
        // && p.UserModifiable // ignore this
        && ( ( StorageType.Integer == st )
          || ( StorageType.String == st ) );
    }

    /// <summary>
    /// Return a dictionary of all the given 
    /// element parameter names and values.
    /// </summary>
    public static Dictionary<string, string>
      GetElementProperties(
        Element e )
    {
      IList<Parameter> parameters
        = e.GetOrderedParameters();

      Dictionary<string, string> a
        = new Dictionary<string, string>(
          parameters.Count );

      StorageType st;
      string s;

      foreach( Parameter p in parameters )
      {
        st = p.StorageType;

        s = string.Format( "{0} {1}",
          ( IsModifiable( p ) ? "w" : "r" ),
          ( StorageType.String == st
            ? p.AsString()
            : p.AsInteger().ToString() ) );

        a.Add( p.Definition.Name, s );
      }
      return a;
    }
    #endregion // Element properties

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
    /// Display a longer message in smaller font.
    /// </summary>
    public static void InfoMsg2(
      string instruction,
      string msg,
      bool prompt = true )
    {
      Debug.Print( "{0}: {1}", instruction, msg );
      if( prompt )
      {
        TaskDialog dlg = new TaskDialog( App.Caption );
        dlg.MainInstruction = instruction;
        dlg.MainContent = msg;
        dlg.Show();
      }
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

    /// <summary>
    /// Print a debug log message with a time stamp
    /// to the Visual Studio debug output window.
    /// </summary>
    public static void Log( string msg )
    {
      string timestamp = DateTime.Now.ToString(
        "HH:mm:ss.fff" );

      Debug.Print( timestamp + " " + msg );
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
