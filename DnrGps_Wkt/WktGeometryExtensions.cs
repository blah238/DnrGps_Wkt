using System;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;

namespace DnrGps_Wkt
{
    /// <summary>
    /// Transform ESRI geometries to the Well-known Text representation, as defined in
    /// section 7 of http://www.opengeospatial.org/standards/sfa (version 1.2.1 dated 2011-05-28)
    /// </summary>
    public static class WktGeometryExtensions
    {

        public static string ToWellKnownText(this IGeometry geometry)
        {
            return BuildWellKnownText(geometry);
        }


        public static IGeometry ToGeometry(this string wkt)
        {
            return BuildGeometry(wkt, CreateDefaultSpatialReference());
        }

        public static IGeometry ToGeometry(this string wkt, ISpatialReference spatialReference)
        {
            return BuildGeometry(wkt, spatialReference);
        }


        #region Private methods for WKT construction

        private static string BuildWellKnownText(IGeometry geometry)
        {
            if (geometry is IPoint)
                return BuildWellKnownText(geometry as IPoint);
            if (geometry is IMultipoint)
                return BuildWellKnownText(geometry as IMultipoint);
            if (geometry is IPolyline)
                return BuildWellKnownText(geometry as IPolyline);
            if (geometry is IPolygon)
                return BuildWellKnownText(geometry as IPolygon);
            if (geometry is IGeometryBag)
                throw new NotImplementedException("Geometry bags to well known text is not yet supported");
            return string.Empty;
        }


        private static string BuildWellKnownText(IPoint point)
        {
            return string.Format("POINT ({0} {1})", point.X, point.Y);
        }


        private static string BuildWellKnownText(IMultipoint points)
        {
            //Example - MULTIPOINT ((10 40), (40 30), (20 20), (30 10))
            //Example - MULTIPOINT (10 40, 40 30, 20 20, 30 10)
            return "MULTIPOINT " + BuildWellKnownText(points as IPointCollection);
        }


        private static string BuildWellKnownText(IPolyline polyline)
        {
            //Example - LINESTRING (30 10, 10 30, 40 40)
            //Example - MULTILINESTRING ((10 10, 20 20, 10 40),(40 40, 30 30, 40 20, 30 10))
            var geometryCollection = polyline as IGeometryCollection;
            if (geometryCollection == null)
                return string.Empty;
            int partCount = geometryCollection.GeometryCount;
            if (partCount == 0)
                return string.Empty;
            if (partCount == 1)
                return "LINESTRING " + BuildWellKnownText(polyline as IPointCollection);
            return "MULTILINESTRING " + BuildWellKnownText(geometryCollection);
        }


        private static string BuildWellKnownText(IPolygon polygon)
        {
            //Example - POLYGON ((30 10, 10 20, 20 40, 40 40, 30 10))
            //Example - POLYGON ((35 10, 10 20, 15 40, 45 45, 35 10),(20 30, 35 35, 30 20, 20 30))
            //Example - MULTIPOLYGON (((30 20, 10 40, 45 40, 30 20)),((15 5, 40 10, 10 20, 5 10, 15 5)))
            //Example - MULTIPOLYGON (((40 40, 20 45, 45 30, 40 40)),((20 35, 45 20, 30 5, 10 10, 10 30, 20 35),(30 20, 20 25, 20 15, 30 20)))
            var geometryCollection = polygon as IGeometryCollection;
            if (geometryCollection == null)
                return string.Empty;
            int partCount = geometryCollection.GeometryCount;
            if (partCount == 0)
                return string.Empty;

            //FIXME - ArcObjects does not have a "multipolygon", however a polygon with multiple exterior rings needs to be a multipolygon in WKT
            //ArcGIS does not differentiate multi-ring polygons and multipolygons
            //Each polygon is simply a collection of rings in any order.
            //in ArcObjects a ring is clockwise for outer, and counterclockwise for inner (interior is on your right)
            //FIXME - In Wkt, exterior rings are counterclockwise, and interior are clockwise (interior is on your left)
            //FIXME - In Wkt, a polygon is one exterior, and zero or more interior rings

            //if (polygon.ExteriorRingCount > 1)
            //    return "MULTIPOLYGON " + BuildWellKnownText(geometryCollection);
            return "POLYGON " + BuildWellKnownText(geometryCollection);

        }


        private static string BuildWellKnownText(IGeometryCollection geometries)
        {
            //Example ((10 10, 20 20, 10 40))
            //Example ((10 10, 20 20, 10 40),(40 40, 30 30, 40 20, 30 10))
            var sb = new StringBuilder();
            int partCount = geometries.GeometryCount;
            if (partCount < 1)
                return string.Empty;
            sb.AppendFormat("({0}", BuildWellKnownText(geometries.Geometry[0] as IPointCollection));
            for (int i = 1; i < partCount; i++)
                sb.AppendFormat(",{0}", BuildWellKnownText(geometries.Geometry[i] as IPointCollection));
            sb.Append(")");
            return sb.ToString();
        }


        private static string BuildWellKnownText(IPointCollection points)
        {
            //Example - (10 40)
            //Example - (10 40, 40 30, 20 20, 30 10)
            var sb = new StringBuilder();
            int pointCount = points.PointCount;
            if (pointCount < 1)
                return string.Empty;
            sb.AppendFormat("({0} {1}", points.Point[0].X, points.Point[0].Y);
            for (int i = 1; i < pointCount; i++)
                sb.AppendFormat(",{0} {1}", points.Point[i].X, points.Point[i].Y);
            sb.Append(")");
            return sb.ToString();
        }

        #endregion


        #region Private methods for IGeometry construction

        private static IGeometry BuildGeometry(string s, ISpatialReference spatialReference)
        {
            var wkt = new WktText(s);

            switch (wkt.Type)
            {
                case WktType.None:
                    return null;
                case WktType.Point:
                    return BuildPoint(wkt, spatialReference);
                case WktType.LineString:
                    return BuildPolyline(wkt, spatialReference);
                case WktType.Polygon:
                    return BuildPolygon(wkt, spatialReference);
                case WktType.Triangle:
                    return BuildPolygon(wkt, spatialReference);
                case WktType.PolyhedralSurface:
                    return BuildMultiPatch(wkt, spatialReference);
                case WktType.Tin:
                    return BuildMultiPolygon(wkt, spatialReference);
                case WktType.MultiPoint:
                    return BuildMultiPoint(wkt, spatialReference);
                case WktType.MultiLineString:
                    return BuildMultiPolyline(wkt, spatialReference);
                case WktType.MultiPolygon:
                    return BuildMultiPolygon(wkt, spatialReference);
                case WktType.GeometryCollection:
                    return BuildGeometryCollection(wkt, spatialReference);
                default:
                    throw new ArgumentOutOfRangeException("s", "Unsupported geometry type: " + wkt.Type);
            }
        }

        private static IGeometry BuildPoint(WktText wkt, ISpatialReference spatialReference)
        {
            return BuildPoint(wkt.Token, wkt, spatialReference);
        }

        private static IGeometry BuildMultiPoint(WktText wkt, ISpatialReference spatialReference)
        {
            var multiPoint = (IPointCollection)new MultipointClass();
            if (spatialReference != null)
                ((IGeometry)multiPoint).SpatialReference = spatialReference;

            foreach (var point in wkt.Token.Tokens)
            {
                multiPoint.AddPoint(BuildPoint(point, wkt, spatialReference));
            }
            ((ITopologicalOperator)multiPoint).Simplify();
            var geometry = multiPoint as IGeometry;
            MakeZmAware(geometry, wkt.HasZ, wkt.HasM);
            return geometry;
        }

        private static IGeometry BuildPolyline(WktText wkt, ISpatialReference spatialReference)
        {
            var multiPath = (IGeometryCollection)new PolylineClass();
            if (spatialReference != null)
                ((IGeometry)multiPath).SpatialReference = spatialReference;

            var path = BuildPath(wkt.Token, wkt, spatialReference);
            ((ITopologicalOperator)multiPath).Simplify();
            multiPath.AddGeometry(path);
            var geometry = multiPath as IGeometry;
            MakeZmAware(geometry, wkt.HasZ, wkt.HasM);
            return geometry;
        }

        private static IGeometry BuildMultiPolyline(WktText wkt, ISpatialReference spatialReference)
        {
            var multiPath = (IGeometryCollection)new PolylineClass();
            if (spatialReference != null)
                ((IGeometry)multiPath).SpatialReference = spatialReference;

            foreach (var lineString in wkt.Token.Tokens)
            {
                var path = BuildPath(lineString, wkt, spatialReference);
                multiPath.AddGeometry(path);
            }
            ((ITopologicalOperator)multiPath).Simplify();
            var geometry = multiPath as IGeometry;
            MakeZmAware(geometry, wkt.HasZ, wkt.HasM);
            return geometry;
        }

        private static IGeometry BuildPolygon(WktText wkt, ISpatialReference spatialReference)
        {
            var multiRing = (IGeometryCollection)new PolygonClass();
            if (spatialReference != null)
                ((IGeometry)multiRing).SpatialReference = spatialReference;

            foreach (var ringString in wkt.Token.Tokens)
            {
                var ring = BuildRing(ringString, wkt, spatialReference);
                multiRing.AddGeometry(ring);
            }
            ((ITopologicalOperator)multiRing).Simplify();
            var geometry = multiRing as IGeometry;
            MakeZmAware(geometry, wkt.HasZ, wkt.HasM);
            return geometry;
        }

        private static IGeometry BuildMultiPolygon(WktText wkt, ISpatialReference spatialReference)
        {
            var multiRing = (IGeometryCollection)new PolygonClass();
            if (spatialReference != null)
                ((IGeometry)multiRing).SpatialReference = spatialReference;

            foreach (var polygonString in wkt.Token.Tokens)
            {
                foreach (var ringString in polygonString.Tokens)
                {
                    var ring = BuildRing(ringString, wkt, spatialReference);
                    multiRing.AddGeometry(ring);
                }
            }
            ((ITopologicalOperator)multiRing).Simplify();
            var geometry = multiRing as IGeometry;
            MakeZmAware(geometry, wkt.HasZ, wkt.HasM);
            return geometry;
        }

        private static IGeometry BuildMultiPatch(WktText wkt, ISpatialReference spatialReference)
        {
            var multiPatch = (IGeometryCollection)new MultiPatchClass();
            if (spatialReference != null)
                ((IGeometry)multiPatch).SpatialReference = spatialReference;

            foreach (var polygonString in wkt.Token.Tokens)
            {
                bool isOuter = true;
                foreach (var ringString in polygonString.Tokens)
                {
                    var ring = BuildRing(ringString, wkt, spatialReference);
                    multiPatch.AddGeometry(ring);
                    ((IMultiPatch)multiPatch).PutRingType(ring, isOuter
                                                                  ? esriMultiPatchRingType.esriMultiPatchOuterRing
                                                                  : esriMultiPatchRingType.esriMultiPatchInnerRing);
                    isOuter = false;
                }
            }
            ((ITopologicalOperator)multiPatch).Simplify();
            var geometry = multiPatch as IGeometry;
            MakeZmAware(geometry, wkt.HasZ, wkt.HasM);
            return geometry;
        }

        private static IGeometry BuildGeometryCollection(WktText wkt, ISpatialReference spatialReference)
        {
            var geometryBag = (IGeometryCollection)new GeometryBagClass();
            if (spatialReference != null)
                ((IGeometryBag)geometryBag).SpatialReference = spatialReference;

            foreach (var geomToken in wkt.Token.Tokens)
            {
                var geom = BuildGeometry(geomToken.ToString(), spatialReference);
                geometryBag.AddGeometry(geom);
            }
            var geometry = geometryBag as IGeometry;
            MakeZmAware(geometry, wkt.HasZ, wkt.HasM);
            return geometry;
        }

        private static IPath BuildPath(WktToken token, WktText wkt, ISpatialReference spatialReference)
        {
            var path = (IPointCollection)new PathClass();
            if (spatialReference != null)
                ((IGeometry)path).SpatialReference = spatialReference;

            foreach (var point in token.Tokens)
            {
                path.AddPoint(BuildPoint(point, wkt, spatialReference));
            }
            var geometry = path as IGeometry;
            MakeZmAware(geometry, wkt.HasZ, wkt.HasM);
            return (IPath)path;
        }

        private static IRing BuildRing(WktToken token, WktText wkt, ISpatialReference spatialReference)
        {
            var ring = (IPointCollection)new RingClass();
            if (spatialReference != null)
                ((IGeometry)ring).SpatialReference = spatialReference;

            foreach (var point in token.Tokens)
            {
                ring.AddPoint(BuildPoint(point, wkt, spatialReference));
            }
            MakeZmAware((IGeometry)ring, wkt.HasZ, wkt.HasM);
            return (IRing)ring;
        }

        private static IPoint BuildPoint(WktToken token, WktText wkt, ISpatialReference spatialReference)
        {
            var coordinates = token.Coords.ToArray();

            int partCount = coordinates.Length;
            if (!wkt.HasZ && !wkt.HasM && partCount != 2)
                throw new ArgumentException("Mal-formed WKT, wrong number of elements, expecting x and y");
            if (wkt.HasZ && !wkt.HasM && partCount != 3)
                throw new ArgumentException("Mal-formed WKT, wrong number of elements, expecting x y z");
            if (!wkt.HasZ && wkt.HasM && partCount != 3)
                throw new ArgumentException("Mal-formed WKT, wrong number of elements, expecting x y m");
            if (wkt.HasZ && wkt.HasM && partCount != 4)
                throw new ArgumentException("Mal-formed WKT, wrong number of elements, expecting x y z m");

            var point = (IPoint)new PointClass();
            if (spatialReference != null)
                point.SpatialReference = spatialReference;

            point.PutCoords(coordinates[0], coordinates[1]);

            if (wkt.HasZ)
                point.Z = coordinates[2];
            if (wkt.HasM && !wkt.HasZ)
                point.M = coordinates[2];
            if (wkt.HasZ && wkt.HasM)
                point.M = coordinates[3];

            MakeZmAware(point, wkt.HasZ, wkt.HasM);
            return point;
        }

        private static void MakeZmAware(IGeometry geometry, bool hasZ, bool hasM)
        {
            if (hasZ)
                ((IZAware)geometry).ZAware = true;
            if (hasM)
                ((IMAware)geometry).MAware = true;
        }

        private static ISpatialReference CreateDefaultSpatialReference()
        {
        	ISpatialReferenceFactory spatialReferenceFactory = new SpatialReferenceEnvironmentClass();
        	var spatialReference = (ISpatialReference)spatialReferenceFactory.CreateProjectedCoordinateSystem((int)esriSRProjCSType.esriSRProjCS_World_PlateCarree);
        	var controlPrecision = spatialReference as IControlPrecision2;
    		controlPrecision.IsHighPrecision = false;
        	var spatialReferenceResolution = (ISpatialReferenceResolution)spatialReference;
        	spatialReferenceResolution.ConstructFromHorizon();
		    var spatialReferenceTolerance = (ISpatialReferenceTolerance)spatialReference;
		    spatialReferenceTolerance.SetDefaultXYTolerance();
		    return spatialReference;
        }

        #endregion
    }
}
