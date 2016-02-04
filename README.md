# RoomEditorApp

Revit add-in part of cloud-based, real-time, round-trip, 2D Revit model editor.

RoomEditorApp implements export of Revit BIM data to a web based cloud database, reimport, and subscription to automatic changes to update the BIM in real time as the user edits a simplified graphical 2D view in any browser on any device.

The database part is implemented by
the [roomedit](https://github.com/jeremytammik/roomedit)
[CouchDB](https://couchdb.apache.org) app.

Please refer to [The Building Coder](http://thebuildingcoder.typepad.com) for
more information, especially in
the [cloud](http://thebuildingcoder.typepad.com/blog/cloud)
and [desktop](http://thebuildingcoder.typepad.com/blog/desktop) categories.

Here is a
recent [summary and overview description](http://thebuildingcoder.typepad.com/blog/2015/11/connecting-desktop-and-cloud-room-editor-update.html#3) of
this project.


## Installation

RoomEditorApp is a Revit add-in.

To install it, fork the repository, clone to your local system, load the solution file in Visual Studio, compile and install in the standard Revit add-in location, for example by copying the add-in manifest file and the .NET DLL assembly to `C:\Users\tammikj\AppData\Roaming\Autodesk\Revit\Addins\2016`

In order to build RoomEditorApp, you will first need to download and compile the [DreamSeat CouchDB wrapper library](https://github.com/vdaron/DreamSeat) and reference the its .NET assembly `DreamSeat.dll`. This will pull in these additional DLLs:

- Autofac.dll
- DreamSeat.dll
- Newtonsoft.Json.dll
- SgmlReaderDll.dll
- log4net.dll
- mindtouch.dream.dll

They all need to be accessible to run the add-in. On way to achieve by copying them to the `RoomEditorApp\bin\Debug` folder and running the Revit add-in from there, cf. the path specified by
the [`Assembly` tag in the add-in manifest file](./RoomEditorApp/RoomEditorApp.addin#L5).

If you do not know what this means, please refer to the GitHub
and [Revit programming getting started](http://thebuildingcoder.typepad.com/blog/about-the-author.html#2) guides.

As said above, RoomEditorApp interacts with
the [roomedit](https://github.com/jeremytammik/roomedit) CouchDB app.

You can run that either locally, on your own system, or on the web, e.g., hosted by the CouchDB hosting
site [Iris Couch](http://www.iriscouch.com).

The choice between these two options is made by the Boolean variable `RoomEditorDb._use_local_db`.

If you use the web hosted system, i.e., `_use_local_db` is set to `false`, you have no more to set up.

In the former case, you need to install and
run both [Apache CouchDB](http://couchdb.apache.org) and
the roomedit app itself locally on your system.

Good luck and have fun!


## Todo

- Reimplement the roomeditor database using node.js and MongoDB instead of CouchDB, like
the [FireRatingCloud](https://github.com/jeremytammik/FireRatingCloud)
[fireratingdb](https://github.com/jeremytammik/firerating)
[node.js](https://nodejs.org)
[MongoDB](https://www.mongodb.org) web server.


## Author

Jeremy Tammik,
[The Building Coder](http://thebuildingcoder.typepad.com) and
[The 3D Web Coder](http://the3dwebcoder.typepad.com),
[ADN](http://www.autodesk.com/adn)
[Open](http://www.autodesk.com/adnopen),
[Autodesk Inc.](http://www.autodesk.com)


## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT).
Please see the [LICENSE](LICENSE) file for full details.
