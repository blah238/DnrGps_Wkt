using System;

namespace DnrGps_Wkt
{
    internal class WktText
    {
        internal WktText(string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentException("WKT is empty");
            ParsePrefix(s);
        }

        internal bool HasZ { get; private set; }
        internal bool HasM { get; private set; }
        internal int CoordinateCount { get; private set; }
        internal WktType Type { get; private set; }
        internal WktToken Token { get; private set; }

        internal string ZmText
        {
            get { return (HasZ ? "Z" : "") + (HasM ? "M" : ""); }
        }

        private void ParsePrefix(string s)
        {
            int startIndex = s.IndexOf('(');
            int endIndex = s.LastIndexOf(')');
            string prefix = startIndex == -1 ? s : s.Substring(0, startIndex);

            HasZ = false;
            HasM = false;
            CoordinateCount = 2;
            prefix = prefix.Trim().ToLower();
            if (prefix.EndsWith(" z"))
            {
                HasZ = true;
                CoordinateCount = 3;
            }
            if (prefix.EndsWith(" m"))
            {
                HasM = true;
                CoordinateCount = 3;
            }
            if (prefix.EndsWith(" zm"))
            {
                HasM = true;
                HasZ = true;
                CoordinateCount = 4;
            }

            Type = WktType.None;
            if (prefix.StartsWith("point"))
                Type = WktType.Point;
            if (prefix.StartsWith("linestring"))
                Type = WktType.LineString;
            if (prefix.StartsWith("polygon"))
                Type = WktType.Polygon;
            if (prefix.StartsWith("polyhedralsurface"))
                Type = WktType.PolyhedralSurface;
            if (prefix.StartsWith("triangle"))
                Type = WktType.Triangle;
            if (prefix.StartsWith("tin"))
                Type = WktType.Tin;
            if (prefix.StartsWith("multipoint"))
                Type = WktType.MultiPoint;
            if (prefix.StartsWith("multilinestring"))
                Type = WktType.MultiLineString;
            if (prefix.StartsWith("multipolygon"))
                Type = WktType.MultiPolygon;
            if (prefix.StartsWith("geometrycollection"))
                Type = WktType.GeometryCollection;

            Token = new WktToken(s, startIndex, endIndex);
        }
    }
}
