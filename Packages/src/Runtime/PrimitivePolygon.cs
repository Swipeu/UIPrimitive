using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Swipeu.UIPrimitive
{
    public class PrimitivePolygon : PrimitiveBase
    {
        public enum Alignment
        {
            Absolute,
            BottomLeft,
            TopLeft,
            TopRight,
            BottomRight,
        }

        [Serializable]
        public class PolygonPoint
        {
            public Vector2 Point;
            public Alignment Alignment;
        }

        public List<PolygonPoint> polygonPoints = new List<PolygonPoint>() 
        { 
            // Default rect
            new PolygonPoint(){Point = new Vector2(0,0)}, 
            new PolygonPoint(){Point = new Vector2(0,1)}, 
            new PolygonPoint(){Point = new Vector2(1,1)}, 
            new PolygonPoint(){Point = new Vector2(1,0)},
        };

        List<PolygonPoint> _cachedPolygonPoints = new List<PolygonPoint>();

        [ContextMenu("Reset points")]
        void ResetPoints()
        {
            polygonPoints = new List<PolygonPoint>() 
        { 
            // Default rect
            new PolygonPoint(){Point = new Vector2(0,0)}, 
            new PolygonPoint(){Point = new Vector2(0,1)}, 
            new PolygonPoint(){Point = new Vector2(1,1)}, 
            new PolygonPoint(){Point = new Vector2(1,0)},
        };

            RefreshCache();
        }

        Point LocalToPosition(Vector4 bounds, PolygonPoint polygonPoint)
        {
            switch (polygonPoint.Alignment){
                case Alignment.TopLeft:
                    return new Point(new Vector2(polygonPoint.Point.x + bounds.x, -polygonPoint.Point.y + bounds.w));
                case Alignment.TopRight:
                    return new Point(new Vector2(-polygonPoint.Point.x + bounds.z, -polygonPoint.Point.y + bounds.w));
                case Alignment.BottomLeft:
                    return new Point(new Vector2(polygonPoint.Point.x + bounds.x, polygonPoint.Point.y + bounds.y));
                case Alignment.BottomRight:
                    return new Point(new Vector2(-polygonPoint.Point.x + bounds.z, polygonPoint.Point.y + bounds.y));
                default:
                    return new Point(new Vector3(Mathf.Lerp(bounds.x, bounds.z, polygonPoint.Point.x), Mathf.Lerp(bounds.y, bounds.w, polygonPoint.Point.y)));
            }
        }

        public Vector4 GetBounds()
        {
            Rect rect = GetPixelAdjustedRect();
            return new Vector4(rect.x, rect.y, rect.x + rect.width, rect.y + rect.height);
        }

        //public void UpdatePoint(int index, Vector2 position)
        //{
        //    polygonPoints[index] = PositionToLocal(GetBounds(), position);
        //}

        public List<Point> GetPoints()
        {
            List<Point> ret = new List<Point>();
            var bounds = GetBounds();

            foreach (var point in polygonPoints)
            {
                var convertedPoint = LocalToPosition(bounds, point);
                ret.Add(convertedPoint);
            }

            return ret;
        }

        protected override void RefreshCache()
        {
            _cachedPolygonPoints = new List<PolygonPoint>(polygonPoints); // Copy

            points = GetPoints();
            triangles.Clear();

            for (int i = 0; i < points.Count; i++)
                points[i].OutlineIndex = i;

            // Quit here for invalid polygons
            if (points.Count < 3)
                return;

            List<Point> pointsCopy = new List<Point>(points);

            // Triangulate using ear clip algorithm
            while (pointsCopy.Count > 3)
            {
                bool anyConvexFound = false;

                // Find first convex point
                for (int i = 0; i < pointsCopy.Count; i++) 
                { 
                    var a = pointsCopy[i % pointsCopy.Count];
                    var b = pointsCopy[(i+1) % pointsCopy.Count];
                    var c = pointsCopy[(i+2) % pointsCopy.Count];

                    var ba = a.Position - b.Position;
                    var bc = c.Position - b.Position;
                                        
                    if (Vector3.Cross(bc, ba).z <= 0 && !AnyPointInTriangle(a, b, c, points)) // Concave check
                    {
                        anyConvexFound = true;
                        triangles.Add(new Triangle(a.OutlineIndex, b.OutlineIndex, c.OutlineIndex));
                        pointsCopy.Remove(b); // Remove extremity
                        break;
                    }
                }

                // Failsafe
                if (!anyConvexFound)
                    break;
            }

            // Add remaining triangle
            triangles.Add(new Triangle(pointsCopy[0].OutlineIndex, pointsCopy[1].OutlineIndex, pointsCopy[2].OutlineIndex));

            // Calculate UVs from bounds
            uvs.Clear();
            var uvBounds = GetPointsBounds();

            foreach (var point in points)
            {
                uvs.Add(GetUVFromBounds(point.Position, uvBounds));
            }
        }
        bool AnyPointInTriangle(Point a, Point b, Point c, List<Point> points)
        {
            foreach(var p in points)
            {
                if (p == a || p == b || p == c)
                    continue;

                Vector3 d, e;
                float w1, w2;
                d = b.Position - a.Position;
                e = c.Position - a.Position;
                w1 = (e.x * (a.Position.y - p.Position.y) + e.y * (p.Position.x - a.Position.x)) / (d.x * e.y - d.y * e.x);
                w2 = (p.Position.y - a.Position.y - w1 * d.y) / e.y;

                if ((w1 >= 0.0) && (w2 >= 0.0) && ((w1 + w2) <= 1.0))
                    return true;
            }

            return false;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/UI/Primitive/Polygon", false, 10)]
        static void CreateCustomGameObject(UnityEditor.MenuCommand menuCommand) => InstantiateNewObjectFromMenu<PrimitivePolygon>(menuCommand);
#endif
    }
}
