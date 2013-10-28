#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System.IO;
using System.Windows.Media.Imaging;
using System.Reflection;
#endregion

namespace RoomEditorApp
{
  class App : IExternalApplication
  {
    /// <summary>
    /// Idling event handler delegate
    /// </summary>
    public delegate void IdlingHandler(
      object sender,
      IdlingEventArgs e );

    /// <summary>
    /// Caption
    /// </summary>
    const string _caption = "Room Editor";

    /// <summary>
    /// Switch between subscribe 
    /// and unsubscribe commands.
    /// </summary>
    const string _subscribe = "Subscribe";
    const string _unsubscribe = "Unsubscribe";

    /// <summary>
    /// Executing assembly namespace
    /// </summary>
    static string _namespace = typeof( App ).Namespace;

    /// <summary>
    /// Command name prefix
    /// </summary>
    const string _cmd_prefix = "Cmd";

    /// <summary>
    /// Currently executing assembly path
    /// </summary>
    static string _path = typeof( App )
      .Assembly.Location;

    /// <summary>
    /// Keep track of our ribbon buttons to toggle
    /// them on and off later and change their text.
    /// </summary>
    static RibbonItem[] _buttons;

    /// <summary>
    /// Singleton external application class instance.
    /// </summary>
    internal static App _app = null;

    /// <summary>
    /// Provide access to singleton class instance.
    /// </summary>
    //public static App Instance
    //{
    //  get { return _app; }
    //}

    /// <summary>
    /// Leep track of our Idling status.
    /// </summary>
    //internal static bool _idling = false;

    /// <summary>
    /// Our one and only Revit-provided 
    /// UIControlledApplication instance.
    /// </summary>
    static UIControlledApplication _uiapp;

    /// <summary>
    /// Return path to embedded resource icon
    /// </summary>
    static string IconResourcePath( 
      string name, 
      string size )
    {
      return _namespace 
        + "." + "Icon" // folder name
        + "." + name + size // icon name
        + ".png"; // filename extension
    }

    /// <summary>
    /// Load a new icon bitmap from embedded resources.
    /// For the BitmapImage, make sure you reference 
    /// WindowsBase and PresentationCore, and import 
    /// the System.Windows.Media.Imaging namespace. 
    /// </summary>
    static BitmapImage GetBitmapImage(
      Assembly a,
      string path )
    {
      // to read from an external file:
      //return new BitmapImage( new Uri(
      //  Path.Combine( _imageFolder, imageName ) ) );

      string [] names = a.GetManifestResourceNames();

      Stream s = a.GetManifestResourceStream( path );

      Debug.Assert( null != s, 
        "expected valid icon resource" );

      BitmapImage img = new BitmapImage();

      img.BeginInit();
      img.StreamSource = s;
      img.EndInit();

      return img;
    }

    /// <summary>
    /// Create a ribbon panel for the MEP sample application.
    /// We present a column of three buttons: Electrical, HVAC and About.
    /// The first two include subitems, the third does not.
    /// </summary>
    static void AddRibbonPanel(
      UIControlledApplication a )
    {
      // Upload selection to cloud
      // Upload all to cloud
      // Update from cloud
      // Subscribe-Unsubscribe toggle, disabling Update when subscribed

      string[] tooltip = new string[] {
        "Upload selected rooms to cloud.",
        "Upload all rooms to cloud.",
        "Update furniture from the last cloud edit.",
        "Subscribe to or unsubscribe from updates.",
        "About " + _caption + ": ..."
      };

      string[] text = new string[] {
        "Upload Selected",
        "Upload All",
        "Update Furniture",
        "Subscribe",
        "About..."
      };

      string[] classNameStem = new string[] {
        "Upload",
        "UploadAll",
        "Update",
        "Subscribe",
        "About"
      };

      string[] iconName = new string[] {
        "1Up",
        "2Up",
        "1Down",
        "ZigZagRed",
        "Question"
      };

      int n = classNameStem.Length;

      Debug.Assert( text.Length == n, 
        "expected equal number of text and class name entries" );

      _buttons = new RibbonItem[n];

      RibbonPanel panel 
        = a.CreateRibbonPanel( _caption );

      #region Use separate push buttons
#if USE_SEPARATE_PUSH_BUTTONS
      PushButtonData[] pbd = new PushButtonData[n];

      for( int i = 0; i < n; ++i )
      {
        pbd[i] = new PushButtonData(
          classNameStem[i], text[i], _path,
          _cmd_prefix + classNameStem[i] );

        pbd[i].ToolTip = text[i];
      }

      IList<RibbonItem> b = panel.AddStackedItems( 
        pbd[0], pbd[1] );

      _buttons[0] = b[0];
      _buttons[1] = b[1];

      _buttons[2] = panel.AddItem( pbd[2] );
      _buttons[3] = panel.AddItem( pbd[3] );
      _buttons[4] = panel.AddItem( pbd[4] );
#endif // USE_SEPARATE_PUSH_BUTTONS
      #endregion // Use separate push buttons

      SplitButtonData splitBtnData
        = new SplitButtonData( _caption, _caption );

      SplitButton splitBtn = panel.AddItem( 
        splitBtnData ) as SplitButton;

      Assembly asm = typeof( App ).Assembly;

      for( int i = 0; i < n; ++i )
      {
        PushButtonData d = new PushButtonData(
          classNameStem[i], text[i], _path,
          _namespace + "." + _cmd_prefix 
          + classNameStem[i] );

        d.ToolTip = tooltip[i];

        d.Image = GetBitmapImage( asm,
          IconResourcePath( iconName[i], "16" ) );

        d.LargeImage = GetBitmapImage( asm,
          IconResourcePath( iconName[i], "32" ) );

        d.ToolTipImage = GetBitmapImage( asm,
          IconResourcePath( iconName[i], "" ) );

        _buttons[i] = splitBtn.AddPushButton( d );
      }
    }

    public Result OnStartup(
      UIControlledApplication a )
    {
      _app = this;
      _uiapp = a;

      AddRibbonPanel( a );

      return Result.Succeeded;
    }

    public Result OnShutdown( 
      UIControlledApplication a )
    {
      if( Subscribed )
      {
        _uiapp.Idling 
          -= new EventHandler<IdlingEventArgs>( 
            ( o, e ) => { } );
      }
      return Result.Succeeded;
    }

    /// <summary>
    /// Are we currently subscribed 
    /// to automatic cloud updates?
    /// </summary>
    public static bool Subscribed
    {
      get
      {
        return _buttons[3].ItemText.Equals( 
          _unsubscribe );
      }
    }

    /// <summary>
    /// Toggle on and off subscription to 
    /// automatic cloud updates.
    /// </summary>
    public static void ToggleSubscription( 
      EventHandler<IdlingEventArgs> h )
    {
      if( Subscribed )
      {
        _uiapp.Idling -= h;
        _buttons[3].ItemText = _subscribe;
      }
      else
      {
        _uiapp.Idling += h;
        _buttons[3].ItemText = _unsubscribe;
      }
    }
  }
}
