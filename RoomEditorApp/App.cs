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
    /// Caption
    /// </summary>
    public const string Caption = "Room Editor";

    /// <summary>
    /// Switch between subscribe 
    /// and unsubscribe commands.
    /// </summary>
    const string _subscribe = "Subscribe";
    const string _unsubscribe = "Unsubscribe";

    /// <summary>
    /// Subscription debugging benchmark timer.
    /// </summary>
    static JtTimer _timer = null;

    /// <summary>
    /// Store the Idling event handler when subscribed.
    /// </summary>
    //static EventHandler<IdlingEventArgs> _handler = null;

    /// <summary>
    /// Store the external event.
    /// </summary>
    static ExternalEvent _event = null;

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
    /// Our one and only Revit-provided 
    /// UIControlledApplication instance.
    /// </summary>
    static UIControlledApplication _uiapp;

    #region Icon resource, bitmap image and ribbon panel stuff
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

      string[] names = a.GetManifestResourceNames();

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
    /// Create a custom ribbon panel and populate
    /// it with our commands, saving the resulting
    /// ribbon items for later access.
    /// </summary>
    static void AddRibbonPanel(
      UIControlledApplication a )
    {
      string[] tooltip = new string[] {
        "Upload selected sheets and categories to cloud.",
        "Upload selected rooms to cloud.",
        "Upload all rooms to cloud.",
        "Update furniture from the last cloud edit.",
        "Subscribe to or unsubscribe from updates.",
        "About " + Caption + ": ..."
      };

      string[] text = new string[] {
        "Upload Sheets",
        "Upload Rooms",
        "Upload All Rooms",
        "Update Furniture",
        "Subscribe",
        "About..."
      };

      string[] classNameStem = new string[] {
        "UploadSheets",
        "UploadRooms",
        "UploadAllRooms",
        "Update",
        "Subscribe",
        "About"
      };

      string[] iconName = new string[] {
        "2Up",
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
        = a.CreateRibbonPanel( Caption );

      SplitButtonData splitBtnData
        = new SplitButtonData( Caption, Caption );

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
    #endregion // Icon resource, bitmap image and ribbon panel stuff

    #region Idling subscription and external event creation
    /// <summary>
    /// Are we currently subscribed 
    /// to automatic cloud updates?
    /// </summary>
    public static bool Subscribed
    {
      get
      {
        bool rc = _buttons[3].ItemText.Equals(
          _unsubscribe );

        Debug.Assert( ( _event != null ) == rc, 
          "expected synchronised handler and button text" );

        return rc;
      }
    }

    /// <summary>
    /// Toggle on and off subscription to 
    /// automatic cloud updates.
    /// </summary>
    public static ExternalEvent ToggleSubscription(
      // EventHandler<IdlingEventArgs> handler
      IExternalEventHandler handler ) 
    {
      if( Subscribed )
      {
        Debug.Print( "Unsubscribing..." );
        //_uiapp.Idling -= _handler; 
        //_handler = null; 
        _event.Dispose();
        _event = null;
        _buttons[3].ItemText = _subscribe;
        _timer.Stop();
        _timer.Report( "Subscription timing" );
        _timer = null;
        Debug.Print( "Unsubscribed." );
      }
      else
      {
        Debug.Print( "Subscribing..." );
        //_uiapp.Idling += handler;
        //_handler = handler;
        _event = ExternalEvent.Create( handler );
        _buttons[3].ItemText = _unsubscribe;
        _timer = new JtTimer( "Subscription" );
        Debug.Print( "Subscribed." );
      }
      return _event;
    }
    #endregion // Idling subscription and external event creation

    public Result OnStartup(
      UIControlledApplication a )
    {
      _uiapp = a;

      AddRibbonPanel( a );

      return Result.Succeeded;
    }

    public Result OnShutdown(
      UIControlledApplication a )
    {
      if( Subscribed )
      {
        //_uiapp.Idling -= _handler;
        _event.Dispose();
      }
      return Result.Succeeded;
    }
  }
}
