#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using DreamSeat;
#endregion

namespace GetLoops
{
  class RoomEditorDb
  {
    const string _url_web = "jt.iriscouch.com";
    const string _url_local = "localhost";
    const string _database_name = "roomedit";

    static CouchClient _client = null;
    static CouchDatabase _db = null;

    public RoomEditorDb()
    {
      if( null == _client )
      {
        _client = new CouchClient( _url_local, 5984 );
      }
      if( null == _db )
      {
        _db = _client.GetDatabase( _database_name, true );
      }
    }

    public CouchDatabase Db
    {
      get
      {
        return _db;
      }
    }

    /// <summary>
    /// Determine the last sequence number.
    /// </summary>
    public int LastSequenceNumber
    {
      get
      {
        ChangeOptions opt = new ChangeOptions();

        CouchChanges<DbFurniture> changes
          = _db.GetChanges<DbFurniture>( opt );

        CouchChangeResult<DbFurniture> r
          = changes.Results.Last<CouchChangeResult<DbFurniture>>();

        return r.Sequence;
      }
    }
  }
}
