#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using System.Diagnostics;
#endregion

namespace GetLoops
{
  static class CurveGetEnpointExtension
  {
    static public XYZ GetEndPoint(
      this Curve curve,
      int i )
    {
      return curve.GetEndPoint( i );
    }
  }

  /// <summary>
  /// Curve loop utilities supporting resorting and 
  /// orientation of curves to form a contiguous 
  /// closed loop.
  /// </summary>
  class CurveUtils
  {
    const double _inch = 1.0 / 12.0;
    const double _sixteenth = _inch / 16.0;

    public enum FailureCondition
    {
      Success,
      CurvesNotContigous,
      CurveLoopAboveTarget,
      NoIntersection
    };

    /// <summary>
    /// Predicate to report whether the given curve 
    /// type is supported by this utility class.
    /// </summary>
    /// <param name="curve">The curve.</param>
    /// <returns>True if the curve type is supported, 
    /// false otherwise.</returns>
    public static bool IsSupported(
      Curve curve )
    {
      return curve is Line || curve is Arc;
    }

    /// <summary>
    /// Create a new curve with the same 
    /// geometry in the reverse direction.
    /// </summary>
    /// <param name="orig">The original curve.</param>
    /// <returns>The reversed curve.</returns>
    /// <throws cref="NotImplementedException">If the 
    /// curve type is not supported by this utility.</throws>
    static Curve CreateReversedCurve(
      Autodesk.Revit.Creation.Application creapp,
      Curve orig )
    {
      if( !IsSupported( orig ) )
      {
        throw new NotImplementedException(
          "CreateReversedCurve for type "
          + orig.GetType().Name );
      }

      if( orig is Line )
      {
        return Line.CreateBound( 
          orig.GetEndPoint( 1 ),
          orig.GetEndPoint( 0 ) );
      }
      else if( orig is Arc )
      {
        return Arc.Create( orig.GetEndPoint( 1 ), 
          orig.GetEndPoint( 0 ), 
          orig.Evaluate( 0.5, true ) );
      }
      else
      {
        throw new Exception(
          "CreateReversedCurve - Unreachable" );
      }
    }

    /// <summary>
    /// Sort a list of curves to make them correctly 
    /// ordered and oriented to form a closed loop.
    /// </summary>
    public static void SortCurvesContiguous(
      Autodesk.Revit.Creation.Application creapp,
      IList<Curve> curves,
      bool debug_output )
    {
      int n = curves.Count;

      // Walk through each curve (after the first) 
      // to match up the curves in order

      for( int i = 0; i < n; ++i )
      {
        Curve curve = curves[i];
        XYZ endPoint = curve.GetEndPoint( 1 );

        if( debug_output ) 
        { 
          Debug.Print( "{0} endPoint {1}", i, 
            Util.PointString( endPoint ) ); 
        }

        XYZ p;

        // Find curve with start point = end point

        bool found = (i + 1 >= n);

        for( int j = i + 1; j < n; ++j )
        {
          p = curves[j].GetEndPoint( 0 );

          // If there is a match end->start, 
          // this is the next curve

          if( _sixteenth > p.DistanceTo( endPoint ) )
          {
            if( i + 1 == j )
            {
              if( debug_output )
              {
                Debug.Print(
                  "{0} start point match, no need to swap",
                  j, i + 1 );
              }
            }
            else
            {
              if( debug_output )
              {
                Debug.Print(
                  "{0} start point, swap with {1}",
                  j, i + 1 );
              }
              Curve tmp = curves[i + 1];
              curves[i + 1] = curves[j];
              curves[j] = tmp;
            }
            found = true;
            break;
          }

          p = curves[j].GetEndPoint( 1 );

          // If there is a match end->end, 
          // reverse the next curve

          if( _sixteenth > p.DistanceTo( endPoint ) )
          {
            if( i + 1 == j )
            {
              if( debug_output )
              {
                Debug.Print( 
                  "{0} end point, reverse {1}", 
                  j, i + 1 );
              }

              curves[i + 1] = CreateReversedCurve(
                creapp, curves[j] );
            }
            else
            {
              if( debug_output )
              {
                Debug.Print( 
                  "{0} end point, swap with reverse {1}", 
                  j, i + 1 );
              }

              Curve tmp = curves[i + 1];
              curves[i + 1] = CreateReversedCurve(
                creapp, curves[j] );
              curves[j] = tmp;
            }
            found = true;
            break;
          }
        }
        if( !found )
        {
          throw new Exception( "SortCurvesContiguous:"
            + " non-contiguous input curves" );
        }
      }
    }

    /// <summary>
    /// Return a list of curves which are correctly 
    /// ordered and oriented to form a closed loop.
    /// </summary>
    /// <param name="doc">The document.</param>
    /// <param name="boundaries">The list of curve element references which are the boundaries.</param>
    /// <returns>The list of curves.</returns>
    public static IList<Curve> GetContiguousCurvesFromSelectedCurveElements(
      Document doc,
      IList<Reference> boundaries,
      bool debug_output )
    {
      List<Curve> curves = new List<Curve>();

      // Build a list of curves from the curve elements

      foreach( Reference reference in boundaries )
      {
        CurveElement curveElement = doc.GetElement( 
          reference ) as CurveElement;

        curves.Add( curveElement.GeometryCurve.Clone() );
      }

      SortCurvesContiguous( doc.Application.Create, 
        curves, debug_output );

      return curves;
    }

    /// <summary>
    /// Identifies if the curve lies entirely in an XY plane (Z = constant)
    /// </summary>
    /// <param name="curve">The curve.</param>
    /// <returns>True if the curve lies in an XY plane, false otherwise.</returns>
    public static bool IsCurveInXYPlane( Curve curve )
    {
      // quick reject - are endpoints at same Z

      double zDelta = curve.GetEndPoint( 1 ).Z 
        - curve.GetEndPoint( 0 ).Z;

      if( Math.Abs( zDelta ) > 1e-05 )
        return false;

      if( !( curve is Line ) && !curve.IsCyclic )
      {
        // Create curve loop from curve and 
        // connecting line to get plane

        List<Curve> curves = new List<Curve>();
        curves.Add( curve );

        //curves.Add(Line.CreateBound(curve.GetEndPoint(1), curve.GetEndPoint(0)));
        
        CurveLoop curveLoop = CurveLoop.Create( curves );

        XYZ normal = curveLoop.GetPlane().Normal
          .Normalize();

        if( !normal.IsAlmostEqualTo( XYZ.BasisZ )
          && !normal.IsAlmostEqualTo( XYZ.BasisZ.Negate() ) )
        {
          return false;
        }
      }
      return true;
    }
  }
}
