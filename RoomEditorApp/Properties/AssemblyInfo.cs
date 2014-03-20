using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle( "Revit Room Editor Add-In" )]
[assembly: AssemblyDescription( "Cloud based furniture and equipment editor" )]
[assembly: AssemblyConfiguration( "" )]
[assembly: AssemblyCompany( "Autodesk Inc." )]
[assembly: AssemblyProduct( "Revit Room Editor Add-In" )]
[assembly: AssemblyCopyright( "Copyright 2013-2014 © Jeremy Tammik Autodesk Inc." )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible( false )]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid( "321044f7-b0b2-4b1c-af18-e71a19252be0" )]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

//
// History:
//
// 2013-03-26 GetRoomLoops.zip -- http://thebuildingcoder.typepad.com/blog/2013/03/revit-2014-api-and-room-plan-view-boundary-polygon-loops.html#3
// 2013-04-03 GetFurnitureLoops.zip -- http://thebuildingcoder.typepad.com/blog/2013/04/extrusion-analyser-and-plan-view-boundaries.html
// 2013-04-08 GeoSnoopLoops.zip -- http://thebuildingcoder.typepad.com/blog/2013/04/geosnoop-net-boundary-curve-loop-visualisation.html
// 2013-04-24 rooms_kanso_02.zip -- SVG display is working, dragging, rotation buttons
// 2013-04-24 rooms_kanso_03.zip -- save is working, removed handlebars and views and shows
// 2013-04-28 rooms_kanso_04.zip -- raphael buttons are working, also on ipad
// 2013-05-01 rooms_kanso_05.zip -- experimenting with furniture access to symbols, before map_room_to_furniture_plus_symbol
// 2013-05-01 rooms_kanso_06.zip -- implemented symbols view, map_symbid_to_loop, etc., all_furniture, save_all ... editing different bits of furniture and saving works now
// 2013-05-03 RoomEditUpload01.zip -- implemented external app and first update command to update last edit only
// 2013-05-03 RoomEditUpload02.zip -- implemented updating all instances since last sequence number, and setting sequence number on upload, before
// 2013-05-08 RoomEditIdling.zip -- idling event works for auto-update
// 2013-05-08 roomedit01.zip -- auto-update, disabled save button, tooltip, enhanced current item notification text
// 2013-05-09 RoomEditUpload03.zip -- icons added
// 2013-05-09 RoomEditUpload04.zip -- external event attempts removed, just using pure idling event, DbUpdater expanded to handle both Update and Subscribe
// 2013-05-29 room_model_7.json -- simple room with one desk, correctly flipped
// 2013-10-21 - 2014.0.0.10 - implemented RoomEditorApp and migrated GetLoops source from Revit 2013 to 2014
// 2013-10-28 - 2014.0.0.11 - replaced GetLoops namespace by RoomEditorApp and installed CouchDB
// 2013-10-28 - 2014.0.0.12 - added icon resources to project
// 2013-10-29 - 2014.0.0.13 - explored why this is slower in Revit 2014 than in 2013 and removed call to SetRaiseWithoutDelay
// 2013-10-30 - 2014.0.0.14 - added early history, documented external application implementation and cleaned up
// 2013-10-31 - 2014.0.0.15 - debugging the less repsonsive Idling event management
// 2013-11-18 - 2014.0.0.16 - fixed unsubscription as described in http://thebuildingcoder.typepad.com/blog/2013/11/singleton-application-versus-multiple-command-instances.html
// 2013-11-18 - 2014.0.0.17 - added JtTimer, implemented LastSequenceNumberChanged, Idling event handling is now responsive and snappy
// 2013-12-05 - 2014.0.0.18 - replaced Idling event by external event
// 2013-12-10 - 2014.0.0.19 - set focus to Revit after raising external event to trigger immediate call to Execute method
// 2014-03-20 - 2014.0.0.20 - updated copyright notice to year 2014 in preparation for release 2
//
[assembly: AssemblyVersion( "2014.0.0.20" )]
[assembly: AssemblyFileVersion( "2014.0.0.20" )]
