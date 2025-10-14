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

            RoundedBottomLeft,
            RoundedTopLeft,
            RoundedTopRight,
            RoundedBottomRight,

            RoundedStretchedBottomLeft,
            RoundedStretchedTopLeft,
            RoundedStretchedTopRight,
            RoundedStretchedBottomRight,
        }

        [Serializable]
        public class PolygonPoint
        {
            public Vector2 Point;
            public Alignment Alignment;
            public int Smoothness;
            public int Radius;
            public Vector2 Radius_vec;
        }

        public List<PolygonPoint> polygonPoints = new List<PolygonPoint>()
        { 
            // Default rect
            new PolygonPoint(){Point = new Vector2(0,0)},
            new PolygonPoint(){Point = new Vector2(0,1)},
            new PolygonPoint(){Point = new Vector2(1,1)},
            new PolygonPoint(){Point = new Vector2(1,0)},
        };

        [SerializeField] bool clampCorners = false;

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

        IEnumerable<Point> LocalToPosition(Vector4 bounds, PolygonPoint polygonPoint)
        {
            float width = (bounds.z - bounds.x);
            float height = (bounds.w - bounds.y);

            // Clamp the radius to ensure it doesn't exceed half the width or height of the rectangle
            float clampedRadius = Mathf.Min(polygonPoint.Radius, width / 2, height / 2);

            var points = new List<Point>();
            switch (polygonPoint.Alignment)
            {
                case Alignment.TopLeft:
                    points.Add(new Point(new Vector2(polygonPoint.Point.x + bounds.x, -polygonPoint.Point.y + bounds.w)));
                    break;
                case Alignment.TopRight:
                    points.Add(new Point(new Vector2(-polygonPoint.Point.x + bounds.z, -polygonPoint.Point.y + bounds.w)));
                    break;
                case Alignment.BottomLeft:
                    points.Add(new Point(new Vector2(polygonPoint.Point.x + bounds.x, polygonPoint.Point.y + bounds.y)));
                    break;
                case Alignment.BottomRight:
                    points.Add(new Point(new Vector2(-polygonPoint.Point.x + bounds.z, polygonPoint.Point.y + bounds.y)));
                    break;

                case Alignment.RoundedBottomLeft:
                    AddCorner(points, new Vector2(bounds.x, bounds.y), bounds, Quaternion.identity, polygonPoint.Radius, height / 2, polygonPoint.Smoothness);
                    break;
                case Alignment.RoundedTopLeft:
                    AddCorner(points, new Vector2(bounds.x, bounds.y + height), bounds, Quaternion.Euler(0, 0, -90), polygonPoint.Radius, width / 2, polygonPoint.Smoothness);
                    break;
                case Alignment.RoundedTopRight:
                    AddCorner(points, new Vector2(bounds.x + width, bounds.y + height), bounds, Quaternion.Euler(0, 0, -180), polygonPoint.Radius, height / 2, polygonPoint.Smoothness);
                    break;
                case Alignment.RoundedBottomRight:
                    AddCorner(points, new Vector2(bounds.x + width, bounds.y), bounds, Quaternion.Euler(0, 0, -270), polygonPoint.Radius, width / 2, polygonPoint.Smoothness);
                    break;

                case Alignment.RoundedStretchedBottomLeft:
                    AddCorner(points, new Vector2(bounds.x, bounds.y), bounds, Quaternion.identity, polygonPoint.Radius_vec, height / 2, polygonPoint.Smoothness);
                    break;
                case Alignment.RoundedStretchedTopLeft:
                    AddCorner(points, new Vector2(bounds.x, bounds.y + height), bounds, Quaternion.Euler(0, 0, -90), polygonPoint.Radius_vec, width / 2, polygonPoint.Smoothness);
                    break;
                case Alignment.RoundedStretchedTopRight:
                    AddCorner(points, new Vector2(bounds.x + width, bounds.y + height), bounds, Quaternion.Euler(0, 0, -180), polygonPoint.Radius_vec, height / 2, polygonPoint.Smoothness);
                    break;
                case Alignment.RoundedStretchedBottomRight:
                    AddCorner(points, new Vector2(bounds.x + width, bounds.y), bounds, Quaternion.Euler(0, 0, -270), polygonPoint.Radius_vec, width / 2, polygonPoint.Smoothness);
                    break;

                default:
                    points.Add(new Point(new Vector3(Mathf.Lerp(bounds.x, bounds.z, polygonPoint.Point.x), Mathf.Lerp(bounds.y, bounds.w, polygonPoint.Point.y))));
                    break;
            }

            return points;
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
                ret.AddRange(convertedPoint);
            }

            for (int i = ret.Count - 2; i >= 0; i--)
            {
                if (ret[i + 1].Position != ret[i].Position)
                    continue;

                ret.RemoveAt(i + 1); // Remove duplicates
            }

            for (int i = 0; i < ret.Count; i++)
            {
                ret[i].OutlineIndex = i; // Set outline index
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
                    var b = pointsCopy[(i + 1) % pointsCopy.Count];
                    var c = pointsCopy[(i + 2) % pointsCopy.Count];

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
            foreach (var p in points)
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

        void AddCorner(List<Point> points, Vector2 point, Vector4 bounds, Quaternion rotation, float clampedRadius, float valueToCompare, int smoothness)
            => AddCorner(points, point, bounds, rotation, new Vector2(clampedRadius, clampedRadius), valueToCompare, smoothness);
        void AddCorner(List<Point> points, Vector2 point, Vector4 bounds, Quaternion rotation, Vector2 clampedRadius, float valueToCompare, int smoothness)
        {
            if (smoothness <= 1 || clampedRadius.x <= 0 || clampedRadius.y <= 0)
            {
                points.Add(new Point(point, points.Count - 1));
                return;
            }

            // Clamp 
            if (clampCorners)
            {
                float width = (bounds.z - bounds.x);
                float height = (bounds.w - bounds.y);

                clampedRadius = Vector2.Min(clampedRadius, new Vector2(width - 0.01f, height - 0.01f) / 2);
            }

            Vector2 offsetPoint = point + (Vector2)(rotation * Vector2.one * clampedRadius);
            int currentCount = points.Count;

            for (int i = 0; i < smoothness; i++)
            {
                // Skip the last point if the radius is exactly half the width or height
                //if (i == smoothness - 1 && Mathf.Approximately(clampedRadius, valueToCompare))
                //    continue;

                Vector2 direction = rotation * Quaternion.Euler(0, 0, -90 * (i / ((float)smoothness - 1))) * Vector2.down;
                points.Add(new Point(offsetPoint + direction * clampedRadius, points.Count - 1));
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/UI/Primitive/Polygon", false, 10)]
        static void CreateCustomGameObject(UnityEditor.MenuCommand menuCommand) => InstantiateNewObjectFromMenu<PrimitivePolygon>(menuCommand);

        [ContextMenu("Preset/Absolute")]
        private void MakeAbsolute()
        {
            polygonPoints = new List<PolygonPoint>()
            {
                new PolygonPoint(){Point = new Vector2(0,0), Alignment = Alignment.Absolute},
                new PolygonPoint(){Point = new Vector2(0,1), Alignment = Alignment.Absolute},
                new PolygonPoint(){Point = new Vector2(1,1), Alignment = Alignment.Absolute},
                new PolygonPoint(){Point = new Vector2(1,0), Alignment = Alignment.Absolute},
            };

            RefreshCache();
        }

        [ContextMenu("Preset/Relative")]
        private void MakeRelative()
        {
            polygonPoints = new List<PolygonPoint>()
            {
                new PolygonPoint(){Point = new Vector2(0,0), Alignment = Alignment.BottomLeft},
                new PolygonPoint(){Point = new Vector2(0,1), Alignment = Alignment.TopLeft},
                new PolygonPoint(){Point = new Vector2(1,1), Alignment = Alignment.TopRight},
                new PolygonPoint(){Point = new Vector2(1,0), Alignment = Alignment.BottomRight},
            };

            RefreshCache();
        }

        [ContextMenu("Preset/Rounded/0")]
        private void MakeRounded() => MakeRounded(0);
        [ContextMenu("Preset/Rounded/8")]
        private void MakeRounded8() => MakeRounded(8);
        [ContextMenu("Preset/Rounded/16")]
        private void MakeRounded16() => MakeRounded(16);
        private void MakeRounded(int radius)
        {
            polygonPoints = new List<PolygonPoint>()
            {
                new PolygonPoint(){Point = new Vector2(0,0), Alignment = Alignment.RoundedBottomLeft, Radius = radius, Smoothness = radius},
                new PolygonPoint(){Point = new Vector2(0,1), Alignment = Alignment.RoundedTopLeft, Radius = radius, Smoothness = radius},
                new PolygonPoint(){Point = new Vector2(1,1), Alignment = Alignment.RoundedTopRight, Radius = radius, Smoothness = radius},
                new PolygonPoint(){Point = new Vector2(1,0), Alignment = Alignment.RoundedBottomRight, Radius = radius, Smoothness = radius},
            };

            RefreshCache();
        }

        [ContextMenu("Preset/Stretched")]
        private void MakeStretched()
        {
            polygonPoints = new List<PolygonPoint>()
            {
                new PolygonPoint(){Point = new Vector2(0,0), Alignment = Alignment.RoundedStretchedBottomLeft},
                new PolygonPoint(){Point = new Vector2(0,1), Alignment = Alignment.RoundedStretchedTopLeft},
                new PolygonPoint(){Point = new Vector2(1,1), Alignment = Alignment.RoundedStretchedTopRight},
                new PolygonPoint(){Point = new Vector2(1,0), Alignment = Alignment.RoundedStretchedBottomRight},
            };

            RefreshCache();
        }


#endif
    }
}