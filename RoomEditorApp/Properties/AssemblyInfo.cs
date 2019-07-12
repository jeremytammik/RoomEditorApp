using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle( "Revit Room Editor Add-In" )]
[assembly: AssemblyDescription( "Cloud-based simplified BIM viewer and editor" )]
[assembly: AssemblyConfiguration( "" )]
[assembly: AssemblyCompany( "Autodesk Inc." )]
[assembly: AssemblyProduct( "Revit Room Editor Add-In" )]
[assembly: AssemblyCopyright( "Copyright 2013-2016 © Jeremy Tammik Autodesk Inc." )]
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
// 2014-03-20 - 2014.0.2.1 - implemented CmdUploadViews and FrmSelectViews
// 2014-03-24 - 2014.0.2.2 - implemented CategoryCollector and FrmSelectCategories
// 2014-04-01 - 2014.0.2.3 - switched from view selection to sheet selection, implemented FrmSelectSheets and CmdUploadSheets
// 2014-04-10 - 2014.0.2.4 - implemented categoryFilter and UploadSheet method outline
// 2014-04-10 - 2014.0.2.5 - split Point2dIntLoop.cs module into JtLoop.cs and JtLoops.cs, added support for open or closed loop to JtLoop
// 2014-04-10 - 2014.0.2.6 - examine geometry retrieved from floor plan views, refactored GetPlanViewBoundaryLoops, exposed GetPlanViewBoundaryLoopsGeo, ensure all geometry is either solid or curve, get JtLoops from solids, ensure curves are horizontal and co-planar
// 2014-04-14 - 2015.0.2.6 - migrated to Revit 2015
// 2014-04-15 - 2015.0.2.7 - eliminated compiler warnings on deprecated API usage of Selection.Elements and ViewSheet.Views
// 2014-04-16 - 2015.0.2.8 - implemented GetSheetViewportLoops
// 2014-04-18 - 2015.0.2.9 - refactored and exposed CmdUploadRooms.GetLoop, implemented GetBimGraphics
// 2014-04-29 - 2015.0.2.10 - renamed DisplayLoops to DisplayRoom, refactored to split off DisplayImageInForm as a separate method, implemented SheetModelCollections and GeoSnoop.DisplaySheet
// 2014-04-30 - 2015.0.2.11 - code cleanup, treat family instance with no location point as non-transformable part; this enables curtain walls
// 2014-05-05 - 2015.0.2.12 - completed initial upload of sheets to cloud repository
// 2014-05-05 - 2015.0.2.13 - implemented Util.GetElementProperties and upload of element properties
// 2014-05-07 - 2015.0.2.14 - added properties to version 1 furniture documents as well
// 2014-05-08 - 2015.0.2.15 - update Revit model with modified element properties from cloud database
// 2014-05-12 - 2015.0.2.16 - minor fixes updating BIM element properties from cloud database
// 2014-05-31 - 2015.0.2.17 - version used for tech summit 2014 pre-recording; implemented PluralString and replaced all calls to PluralSuffix
// 2014-06-01 - 2015.0.2.18 - minor edits for blog post
// 2014-06-05 - 2015.0.2.19 - edited Subscribed to accommodate additional button 3 changes to 4
// 2014-06-05 - 2015.0.2.20 - implemented _subscribeButtonIndex in both Subscribed and ToggleSubscription, check for null event in CheckForPendingDatabaseChanges
// 2014-07-21 - 2015.0.2.21 - break out of loop if external event _event is null
// 2015-10-28 - 2016.0.0.0 - flat migration from Revit 2015 to Revit 2016
// 2015-10-28 - 2016.0.0.1 - implemented RoomEditorDb.Url property and Boolean _use_local_db toggle, successfully tested on http://localhost:5984/roomedit/_design/roomedit/index.html
// 2015-10-28 - 2016.0.0.2 - fixed and tested subscription to database changes and auto-update via external event
// 2015-11-06 - 2016.0.0.3 - removed the experimental upload sheets command
// 2015-11-06 - 2016.0.0.4 - removed upload sheets command and looked at access violation using null _event when unsubscribing
// 2015-11-06 - 2016.0.0.5 - implemented App.Event property and eliminated superfluous DbUpdater._event variable
// 2015-11-06 - 2016.0.0.5 - eliminated obsolete API usage replace calls to BoundarySegment.Curve property by GetCurve method
// 2015-11-24 - 2016.0.0.7 - added installation instructions and set Copy Local to false on the Revit API assemblies
// 2016-01-26 - 2016.0.0.8 - incremented copyright year, tested for bim programming workshop in madrid
// 2016-03-03 - 2016.0.0.9 - eliminated unused _uiapp variable
// 2016-04-25 - 2017.0.0.0 - flat migration to Revit 2017, moved to Visual Studio 2015 and .NET framework 4.5.2
// 2016-04-25 - 2017.0.0.1 - additional manual project file cleanup after successful migration and first compilation
// 2016-04-25 - 2017.0.0.2 - eliminated deprecated Revit API usage warnings
// 2016-05-30 - 2017.0.0.3 - computer crashed, reinstalled, switched DreamSeat to NuGet, recompiled, still need to test
// 2016-05-30 - 2017.0.0.3 - ChangeOptions.View property disappeared and first test is successful
// 2017-10-16 - 2018.0.0.0 - flat migration to Revit 2018
// 2017-10-16 - 2018.0.0.1 - fix unsubscription on revit shutdown
// 2019-07-12 - 2020.0.0.0 - flat migration to Revit 2020
//
[assembly: AssemblyVersion( "2020.0.0.0" )]
[assembly: AssemblyFileVersion( "2020.0.0.0" )]
