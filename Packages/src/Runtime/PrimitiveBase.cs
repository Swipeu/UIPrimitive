using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Swipeu.UIPrimitive
{
    [RequireComponent(typeof(CanvasRenderer))]
    public abstract class PrimitiveBase : MaskableGraphic, ICanvasRaycastFilter
    {
        [SerializeField] VisualSettings visualSettings;
        [SerializeField] TransformSettings transformSettings;

        protected List<Vector2> uvs = new List<Vector2>();
        protected List<Point> points = new List<Point>();
        protected List<Triangle> triangles = new List<Triangle>();

        List<Point> visualPoints = new List<Point>();
        List<Triangle> visualTriangles = new List<Triangle>();

        public VisualSettings VisualSettings
        {
            get
            {
                if (visualSettings == null)
                    visualSettings = new VisualSettings();

                return visualSettings;
            }
        }

        public TransformSettings TransformSettings
        {
            get
            {
                if (transformSettings == null)
                    transformSettings = new TransformSettings();

                return transformSettings;
            }
        }

        public List<Point> Points
        {
            set
            {
                points = value;
            }
        }
        public List<Triangle> Triangles
        {
            set
            {
                triangles = value;
            }
        }

        public override Color color
        {
            get
            {
                return visualSettings.color;
            }
            set
            {
                visualSettings.color = value;
                TriggerRefreshCache(true);
            }
        }

        public List<Point> CopyPoints()
        {
            List<Point> copyPoints = new List<Point>();
            copyPoints.AddRange(points.Select(p => p.Copy()));
            return copyPoints;
        }
        public List<Triangle> CopyTriangles()
        {
            List<Triangle> copyTriangles = new List<Triangle>();
            copyTriangles.AddRange(triangles.Select(t => t.Copy()));
            return copyTriangles;
        }

        bool initiated = false;

        protected const double approachingZero = 1e-13;

        static VisualSettings defaultVisualSettings = new VisualSettings();
        static TransformSettings defaultTransformSettings = new TransformSettings();

        /// <summary>
        /// Used to clear and refresh points and triangles
        /// </summary>
        abstract protected void RefreshCache();

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            TriggerRefreshCache(true);
        }
#endif
        protected override void OnEnable()
        {
            TriggerRefreshCache(true);
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            foreach (var copyPrimitive in GetComponents<AdditionalGraphic>())
            {
                copyPrimitive.DisableInstance();
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            TriggerRefreshCache(true);
            base.OnRectTransformDimensionsChange();
        }

        public void TriggerRefreshCache(bool setDirty)
        {
            if (rectTransform.rect.width == 0 || rectTransform.rect.height == 0)
            {
                uvs.Clear();
                points.Clear();
                triangles.Clear();
                visualPoints.Clear();
                visualTriangles.Clear();

                if (setDirty)
                    SetVerticesDirty();

                return;
            }

            RefreshCache();

            visualPoints.Clear();
            visualTriangles.Clear();
            uvs.Clear();

            GetModifiedPointsAndTriangles(points, visualSettings, transformSettings, 0, out visualPoints, out visualTriangles, out uvs);

            if (setDirty)
            {
                foreach (var copyPrimitive in GetComponents<AdditionalGraphic>())
                {
                    copyPrimitive.SetDirty();
                }

                SetVerticesDirty();
            }
        }

        void GetModifiedPointsAndTriangles(List<Point> originalPoints, VisualSettings visualSettings, TransformSettings transformSettings, int indexModifier, out List<Point> modifiedPoints, out List<Triangle> modifiedTriangles, out List<Vector2> modifiedUVs)
        {
            visualSettings = visualSettings ?? defaultVisualSettings;
            transformSettings = transformSettings ?? defaultTransformSettings;

            GetSizeOffsetPoints(originalPoints, -transformSettings.sizeOffset, out List<Point> sizeOffsetPoints);

            List<Point> positionOffsetPoints = new List<Point>();
            if (visualSettings.innerShadow)
            {
                positionOffsetPoints = new List<Point>(sizeOffsetPoints);
            }
            else
            {
                GetPositionOffsetPoints(sizeOffsetPoints, transformSettings.positionOffset, out positionOffsetPoints);
            }

            if (visualSettings.outline && !visualSettings.glow)
            {
                GetOutline(positionOffsetPoints, visualSettings, out modifiedPoints, out modifiedTriangles, out modifiedUVs);
                modifiedPoints.ForEach(p => p.Color = visualSettings.color);
            }
            else if (visualSettings.glow && !visualSettings.outline)
            {
                GetGlow(positionOffsetPoints, visualSettings, out modifiedPoints, out modifiedTriangles, out modifiedUVs);
                modifiedPoints.ForEach(p => p.Color = new Color(visualSettings.color.r, visualSettings.color.g, visualSettings.color.b, p.Color.a * visualSettings.color.a));
            }
            else
            {
                modifiedPoints = new List<Point>();
                modifiedTriangles = new List<Triangle>();
                modifiedUVs = new List<Vector2>();

                var bounds = GetPointsBounds();

                foreach (var point in positionOffsetPoints)
                {
                    modifiedPoints.Add(point);
                    modifiedUVs.Add(GetUVFromBounds(point.Position, bounds));
                }

                foreach (var triangle in triangles)
                {
                    modifiedTriangles.Add(triangle.Copy());
                }
                modifiedPoints.ForEach(p => p.Color = visualSettings.color);
            }

            if (!visualSettings.glow && visualSettings.antialiasing)
            {
                var outlinePoints = new List<Point>(modifiedPoints);

                if (visualSettings.outline)
                {
                    var outerPoints = modifiedPoints.Where((p, index) => index % 2 == 0).ToList();
                    var innerPoints = modifiedPoints.Where((p, index) => index % 2 == 1).Reverse().ToList();

                    AntialiasingPass(.225f, .25f, outerPoints, modifiedPoints, modifiedTriangles);
                    AntialiasingPass(.225f, .25f, innerPoints, modifiedPoints, modifiedTriangles);

                    AntialiasingPass(.45f, .5f, outerPoints, modifiedPoints, modifiedTriangles);
                    AntialiasingPass(.45f, .5f, innerPoints, modifiedPoints, modifiedTriangles);
                }
                else
                {
                    List<Point> orderedPointsWithOutline = modifiedPoints.Where(p => p.OutlineIndex >= 0).OrderBy(p => p.OutlineIndex).ToList();
                    AntialiasingPass(.225f, .25f, orderedPointsWithOutline, modifiedPoints, modifiedTriangles);
                    AntialiasingPass(.45f, .5f, orderedPointsWithOutline, modifiedPoints, modifiedTriangles);
                }
            }

            modifiedTriangles.ForEach(t =>
            {
                t.PointAIndex += indexModifier;
                t.PointBIndex += indexModifier;
                t.PointCIndex += indexModifier;
            });
        }

        void AntialiasingPass(float width, float alpha, List<Point> points, List<Point> allPoints, List<Triangle> allTriangles)
        {
            var antialiasingPoints = new List<Point>();
            var antialiasingTriangles = new List<Triangle>();

            if (points.Count <= 2 || width <= 0)
                return;

            GetSizeOffsetPoints(points, -width, out List<Point> pointsOffset, true);

            for (int i = 0; i < points.Count; i++)
            {
                var newPoint = points[i].Copy();
                pointsOffset[i].Color = newPoint.Color = new Color(points[i].Color.r, points[i].Color.g, points[i].Color.b, points[i].Color.a * alpha);
                antialiasingPoints.Add(newPoint);
                antialiasingPoints.Add(pointsOffset[i]);
            }

            for (int i = 0; i < antialiasingPoints.Count; i += 2)
            {
                int point1Index = i;
                int point2Index = i + 1;
                int point3Index = i + 2 >= antialiasingPoints.Count ? 0 : i + 2;
                int point4Index = i + 3 >= antialiasingPoints.Count ? 1 : i + 3;

                antialiasingTriangles.Add(new Triangle(point1Index, point3Index, point2Index));
                antialiasingTriangles.Add(new Triangle(point2Index, point3Index, point4Index));
            }

            allTriangles.ForEach(t =>
            {
                t.PointAIndex += antialiasingPoints.Count;
                t.PointBIndex += antialiasingPoints.Count;
                t.PointCIndex += antialiasingPoints.Count;
            });

            allPoints.InsertRange(0, antialiasingPoints);
            allTriangles.InsertRange(0, antialiasingTriangles);
        }

        void GetOutline(List<Point> originalPoints, VisualSettings visualSettings, out List<Point> outlinePoints, out List<Triangle> outlineTriangles, out List<Vector2> outlineUVs)
        {
            outlinePoints = new List<Point>();
            outlineTriangles = new List<Triangle>();
            outlineUVs = new List<Vector2>();

            List<Point> orderedPointsWithOutline = originalPoints.Where(p => p.OutlineIndex >= 0).OrderBy(p => p.OutlineIndex).ToList();
            if (orderedPointsWithOutline.Count <= 2)
                return;

            var bounds = GetPointsBounds();

            GetSizeOffsetPoints(orderedPointsWithOutline, visualSettings.outlineWidth, out List<Point> outlinePointsOffset);

            for (int i = 0; i < orderedPointsWithOutline.Count; i++)
            {
                outlinePoints.Add(orderedPointsWithOutline[i]);
                outlineUVs.Add(GetUVFromBounds(orderedPointsWithOutline[i].Position, bounds));

                outlinePoints.Add(outlinePointsOffset[i]);
                outlineUVs.Add(GetUVFromBounds(outlinePointsOffset[i].Position, bounds));
            }

            for (int i = 0; i < outlinePoints.Count; i += 2)
            {
                int point1Index = i;
                int point2Index = i + 1;
                int point3Index = i + 2 >= outlinePoints.Count ? 0 : i + 2;
                int point4Index = i + 3 >= outlinePoints.Count ? 1 : i + 3;

                outlineTriangles.Add(new Triangle(point1Index, point3Index, point2Index));
                outlineTriangles.Add(new Triangle(point2Index, point3Index, point4Index));
            }
        }
        void GetGlow(List<Point> originalPoints, VisualSettings visualSettings, out List<Point> outlinePoints, out List<Triangle> outlineTriangles, out List<Vector2> outlineUVs)
        {
            outlinePoints = new List<Point>();
            outlineTriangles = new List<Triangle>();
            outlineUVs = new List<Vector2>();

            List<Point> orderedPointsWithOutline = originalPoints.Where(p => p.OutlineIndex >= 0).OrderBy(p => p.OutlineIndex).ToList();
            if (orderedPointsWithOutline.Count <= 2)
                return;

            var bounds = GetPointsBounds();

            orderedPointsWithOutline.ForEach(p => p.Color = Color.white);

            GetRoundedSizeOffsetPoints(orderedPointsWithOutline, -visualSettings.outlineWidth, visualSettings.detailLevel, out List<List<Point>> outlinePointsOffset);

            outlinePointsOffset.ForEach(list => list.ForEach(p => p.Color = Color.clear));

            for (int i = 0; i < orderedPointsWithOutline.Count; i++)
            {
                outlinePoints.Add(orderedPointsWithOutline[i]);
                outlineUVs.Add(GetUVFromBounds(orderedPointsWithOutline[i].Position, bounds));

                foreach (var point in outlinePointsOffset[i])
                {
                    outlinePoints.Add(point);
                    outlineUVs.Add(GetUVFromBounds(point.Position, bounds));
                }
            }

            int currentIndex = 0;
            for (int i = 0; i < orderedPointsWithOutline.Count; i++)
            {
                int currentPointIndex = currentIndex;

                var roundedCornerPoints = outlinePointsOffset[i];

                foreach (var point in roundedCornerPoints)
                {
                    if (!(++currentIndex > currentPointIndex + 1))
                        continue;

                    outlineTriangles.Add(new Triangle(currentPointIndex, currentIndex - 1, currentIndex));
                }

                if (++currentIndex >= outlinePoints.Count)
                    currentIndex = 0;

                int nextPointIndex = currentIndex;



                int point1Index = currentPointIndex;
                int point2Index = currentPointIndex + roundedCornerPoints.Count;
                int point3Index = nextPointIndex;
                int point4Index = nextPointIndex + 1;

                outlineTriangles.Add(new Triangle(point1Index, point2Index, point3Index));
                outlineTriangles.Add(new Triangle(point2Index, point4Index, point3Index));
            }
        }

        void GetSizeOffsetPoints(List<Point> originalPoints, float offset, out List<Point> sizeOffsetPoints, bool ignoreOutlineIndex = false)
        {
            sizeOffsetPoints = new List<Point>();

            List<Point> orderedPoints = originalPoints.OrderBy(p => ignoreOutlineIndex ? 0 : p.OutlineIndex).ToList();

            for (int i = 0; i < originalPoints.Count; i++)
            {
                Point currentPoint = originalPoints[i];

                int currentPointOrderedIndex = orderedPoints.IndexOf(currentPoint);
                Point previousPoint = orderedPoints[currentPointOrderedIndex == 0 ? orderedPoints.Count - 1 : currentPointOrderedIndex - 1];
                Point nextPoint = orderedPoints[currentPointOrderedIndex == orderedPoints.Count - 1 ? 0 : currentPointOrderedIndex + 1];

                Vector2 vector1 = (previousPoint.Position - currentPoint.Position).normalized;
                Vector2 vector2 = (nextPoint.Position - currentPoint.Position).normalized;

                float modifiedOffset = offset / Mathf.Sin(Mathf.Deg2Rad * Vector2.SignedAngle(vector1, vector2));

                sizeOffsetPoints.Add(new Point(currentPoint.Position + modifiedOffset * (vector1 + vector2), currentPoint.OutlineIndex));
            }
        }
        void GetRoundedSizeOffsetPoints(List<Point> originalPoints, float offset, int detailLevel, out List<List<Point>> sizeOffsetPoints)
        {
            sizeOffsetPoints = new List<List<Point>>();

            List<Point> orderedPoints = originalPoints.OrderBy(p => p.OutlineIndex).ToList();

            for (int i = 0; i < originalPoints.Count; i++)
            {
                Point currentPoint = originalPoints[i];

                int currentPointOrderedIndex = orderedPoints.IndexOf(currentPoint);
                Point previousPoint = orderedPoints[currentPointOrderedIndex == 0 ? orderedPoints.Count - 1 : currentPointOrderedIndex - 1];
                Point nextPoint = orderedPoints[currentPointOrderedIndex == orderedPoints.Count - 1 ? 0 : currentPointOrderedIndex + 1];

                Vector2 vector1 = (previousPoint.Position - currentPoint.Position).normalized;
                Vector2 vector1Normal = new Vector2(-vector1.y, vector1.x);

                Vector2 vector2 = (nextPoint.Position - currentPoint.Position).normalized;
                Vector2 vector2Normal = new Vector2(vector2.y, -vector2.x);

                Vector2 vector3 = (vector1 + vector2);
                Vector2 vector3Normal = (vector1 + vector2).normalized;

                List<Point> pointList = new List<Point>();

                // Check the the graphic is concave with the help of cross product
                if (Mathf.Sign(vector1Normal.x * vector2Normal.y - vector1Normal.y * vector2Normal.x) > 0)
                {
                    // Use angle to check if the vectors are parallel with eachother
                    float angle = Vector2.SignedAngle(vector1, vector2);
                    if (angle == 0 || angle == 180)
                    {
                        pointList.Add(new Point(currentPoint.Position + offset * vector1Normal, currentPoint.OutlineIndex));
                    }
                    else
                    {
                        float modifiedOffset = offset / Mathf.Sin(Mathf.Deg2Rad * Vector2.SignedAngle(vector1, vector2));
                        pointList.Add(new Point(currentPoint.Position + modifiedOffset * vector3, currentPoint.OutlineIndex));
                    }
                }
                else
                {

                    float angle = Vector2.SignedAngle(vector1Normal, vector3Normal);

                    pointList.Add(new Point(currentPoint.Position + vector1Normal * offset, currentPoint.OutlineIndex));

                    for (int j = 0; j < detailLevel; j++)
                    {
                        pointList.Add(new Point(currentPoint.Position + (Vector2)(Quaternion.Euler(0, 0, angle / (detailLevel + 1) * (j + 1)) * vector1Normal) * offset, currentPoint.OutlineIndex));
                    }

                    pointList.Add(new Point(currentPoint.Position + (vector1 + vector2).normalized * offset, currentPoint.OutlineIndex));

                    for (int j = 0; j < detailLevel; j++)
                    {
                        pointList.Add(new Point(currentPoint.Position + (Vector2)(Quaternion.Euler(0, 0, angle / (detailLevel + 1) * (j + 1)) * vector3Normal.normalized) * offset, currentPoint.OutlineIndex));
                    }

                    pointList.Add(new Point(currentPoint.Position + vector2Normal * offset, currentPoint.OutlineIndex));
                }

                sizeOffsetPoints.Add(pointList);
            }
        }

        void GetPositionOffsetPoints(List<Point> originalPoints, Vector2 offset, out List<Point> positionOffsetPoints)
        {
            positionOffsetPoints = new List<Point>();

            foreach (Point point in originalPoints)
            {
                positionOffsetPoints.Add(new Point(point, offset));
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);

            if (!initiated)
                TriggerRefreshCache(false);

            vh.Clear();

            AddVertices(vh, visualPoints);
            AddTriangles(vh, visualTriangles);
        }

        void AddVertices(VertexHelper vh, List<Point> points)
        {
            foreach (var point in points)
            {
                UIVertex vert = UIVertex.simpleVert;
                vert.position = point.Position;
                vert.color = point.Color;

                vh.AddVert(vert);
            }
        }
        void AddTriangles(VertexHelper vh, List<Triangle> triangles)
        {
            foreach (var triangle in triangles)
            {
                vh.AddTriangle(triangle.PointAIndex, triangle.PointBIndex, triangle.PointCIndex);
            }
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (color.a == 0 || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 local))
                return false;

            if (!initiated)
                TriggerRefreshCache(true);

            foreach (var triangle in triangles)
            {
                if (IsPointInsideTriangle(local, triangle, points))
                    return true;
            }

            return false;
        }

        public bool IsInsideShape(Vector2 point)
        {
            foreach (var triangle in triangles)
            {
                if (IsPointInsideTriangle(point, triangle, points))
                    return true;
            }

            return false;
        }

#if UNITY_EDITOR
        // Add a menu item to create custom GameObjects.
        // Priority 1 ensures it is grouped with the other menu items of the same kind
        // and propagated to the hierarchy dropdown and hierarch context menus.
        static protected void InstantiateNewObjectFromMenu<T>(UnityEditor.MenuCommand menuCommand)
            where T : Component
        {
            // Create a custom game object
            GameObject go = new GameObject(typeof(T).Name);
            go.AddComponent<T>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            UnityEditor.GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, $"Create {go.name}");
            UnityEditor.Selection.activeObject = go;
        }
#endif

        #region Math
        static bool IsPointInsideTriangle(Vector2 point, Triangle triangle, List<Point> availablePoints)
        {
            Point a = triangle.GetPointA(availablePoints);
            Point b = triangle.GetPointB(availablePoints);
            Point c = triangle.GetPointC(availablePoints);

            // Use a hardcoded margin value instead of scaling  
            const float margin = 0.01f; // Adjust this value as needed  
            Vector2 center = (a.Position + b.Position + c.Position) / 3;

            var positionA = a.Position + (a.Position - center).normalized * margin;
            var positionB = b.Position + (b.Position - center).normalized * margin;
            var positionC = c.Position + (c.Position - center).normalized * margin;

            // Calculate vectors  
            Vector2 v0 = positionC - positionA;
            Vector2 v1 = positionB - positionA;
            Vector2 v2 = point - positionA;

            // Compute dot products  
            float dot00 = Vector2.Dot(v0, v0);
            float dot01 = Vector2.Dot(v0, v1);
            float dot02 = Vector2.Dot(v0, v2);
            float dot11 = Vector2.Dot(v1, v1);
            float dot12 = Vector2.Dot(v1, v2);

            // Compute barycentric coordinates  
            float denom = dot00 * dot11 - dot01 * dot01;
            if (Math.Abs(denom) < approachingZero) return false; // Avoid division by zero  
            float invDenom = 1 / denom;
            float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            // Check if point is in triangle  
            return (u >= 0) && (v >= 0) && (u + v <= 1);
        }

        static float Cross(Vector2 vectorA, Vector2 vectorB) => vectorA.x * vectorB.y - vectorA.y * vectorB.x;
        static bool InsideRange(float value, float start, float end) => value >= start && value <= end;

        protected Vector4 GetPointsBounds()
        {
            if (points.Count == 0)
            {
                return Vector4.zero;
            }

            return new Vector4(
                points.Min(e => e.Position.x),
                points.Min(e => e.Position.y),
                points.Max(e => e.Position.x),
                points.Max(e => e.Position.y));
        }

        protected Vector2 GetUVFromBounds(Vector2 position, Vector4 bounds)
        {
            return new Vector2(
                Mathf.InverseLerp(bounds.x, bounds.z, position.x),
                Mathf.InverseLerp(bounds.y, bounds.w, position.y));
        }
        #endregion
    }
}
