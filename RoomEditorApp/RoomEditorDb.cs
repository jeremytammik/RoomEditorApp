#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using DreamSeat;
#endregion

namespace RoomEditorApp
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
      using( JtTimer pt = new JtTimer( "RoomEditorDb ctor" ) )
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
    }

    public CouchDatabase Db
    {
      get
      {
        return _db;
      }
    }

    public TDocument GetOrCreate<TDocument>(
      ref bool pre_existing,
      string uid ) where TDocument : DbObj
    {
      //return _db.GetDocument<TDocument>( uid );

      TDocument doc;

      if( _db.DocumentExists( uid ) )
      {
        pre_existing = true;

        doc = _db.GetDocument<TDocument>( uid );

        Debug.Assert(
          doc.Id.Equals( uid ),
          "expected equal ids" );
      }
      else
      {
        pre_existing = false;
        doc = (TDocument) Activator.CreateInstance(
          typeof( TDocument ), uid );
      }
      return doc;
    }

    /// <summary>
    /// Return the last sequence number.
    /// </summary>
    public int LastSequenceNumber
    {
      get
      {
        using( JtTimer pt = new JtTimer(
          "LastSequenceNumber" ) )
        {
          ChangeOptions opt = new ChangeOptions();

          //opt.Limit = 1;
          //opt.Descending = true;

          //opt.IncludeDocs = true;
          //opt.View = "roomedit/_changes?descending=true&limit=1";

          CouchChanges<DbFurniture> changes
            = _db.GetChanges<DbFurniture>( opt );

          CouchChangeResult<DbFurniture> r
            = changes.Results.Last<
              CouchChangeResult<DbFurniture>>();

          return r.Sequence;
        }
      }
    }

    /// <summary>
    /// Determine whether the given sequence number
    /// matches the most up-to-date status.
    /// </summary>
    public bool LastSequenceNumberChanged( int since )
    {
      using( JtTimer pt = new JtTimer(
        "LastSequenceNumberChanged" ) )
      {
        ChangeOptions opt = new ChangeOptions();

        opt.Since = since;
        opt.IncludeDocs = false;

        CouchChanges<DbFurniture> changes
          = _db.GetChanges<DbFurniture>( opt );

        CouchChangeResult<DbFurniture> r
          = changes.Results.LastOrDefault<
            CouchChangeResult<DbFurniture>>();

        Debug.Assert( null == r || since < r.Sequence,
          "expected monotone growing sequence number" );

        return null != r && since < r.Sequence;
      }
    }
  }
}
