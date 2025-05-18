using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Swipeu.UIPrimitive
{
    [ExecuteAlways]
    [RequireComponent(typeof(PrimitiveBase))]
    public class AdditionalGraphic : BehaviourCopyBase<PrimitiveBase, PrimitiveCopy>
    {
        [SerializeField] VisualSettings visualSettings;
        [SerializeField] TransformSettings transformSettings;

        public VisualSettings VisualSettings => visualSettings;
        public TransformSettings TransformSettings => transformSettings;

        protected override void OnCopy()
        {
            var points = OriginalComponent.CopyPoints();
            points.ForEach(p =>
            {
                if (p == null || visualSettings == null)
                    return;

                p.Color = visualSettings.color;
            });

            InstanceComponent.VisualSettings.Copy(visualSettings);
            InstanceComponent.TransformSettings.Copy(transformSettings);

            // Handle inner shadow
            if (visualSettings != null && visualSettings.innerShadow)
            {
                AddInnerShadow(points);
            }
            else
            {
                InstanceComponent.Points = points;
                InstanceComponent.Triangles = OriginalComponent.CopyTriangles();
            }

            InstanceComponent.TriggerRefreshCache(true);
        }

        protected override void OnInstantiate()
        {
            InstanceComponent.raycastTarget = false;

            if (transform.parent == InstanceComponent.transform.parent)
            {
                InstanceComponent.gameObject.name = $"{name} Shadow Copy";
                InstanceComponent.transform.SetSiblingIndex(transform.GetSiblingIndex());
            }
            else
            {
                InstanceComponent.gameObject.name = $"{name} Overlay Copy";
            }
        }

        public void SetVisuals(VisualSettings settings)
        {
            visualSettings = settings;
            //InstanceComponent.VisualSettings.Copy(settings);
            InstanceComponent.SetAllDirty();
        }

        private void AddInnerShadow(List<Point> originalPoints)
        {
            Vector2 positionOffset = transformSettings != null ? transformSettings.positionOffset : Vector2.zero;

            // Only use outline points, ordered by OutlineIndex
            var outlinePoints = originalPoints
                .Where(p => p.OutlineIndex >= 0)
                .OrderBy(p => p.OutlineIndex)
                .ToList();

            int count = outlinePoints.Count;
            if (count < 2)
                return;

            // Build shadow points
            List<Point> combinedPoints = new List<Point>();
            Queue<Point> shadowPoints = new Queue<Point>();
            Queue<Point> remainingOriginalPoints = new Queue<Point>();

            for (int i = 0; i < count; i++)
            {
                Vector2 shadowPosition = outlinePoints[i].Position + positionOffset;
                if (OriginalComponent.IsInsideShape(shadowPosition))
                {
                    var shadowPoint = new Point(shadowPosition, outlinePoints[i].OutlineIndex)
                    {
                        Color = Color.black,
                    };
                    combinedPoints.Add(outlinePoints[i]);
                    combinedPoints.Add(shadowPoint);

                    shadowPoints.Enqueue(shadowPoint);
                    remainingOriginalPoints.Enqueue(outlinePoints[i]);
                }
            }

            // Build triangles for the band, only if OutlineIndex is chained or wrapped
            int startIndex = -1;
            List<Triangle> shadowTriangles = new List<Triangle>();
            for (int i = 0; i < combinedPoints.Count; i += 2)
            {
                int currOutlineIdx = combinedPoints[i].OutlineIndex;
                int next = (i + 2) % combinedPoints.Count;
                int nextOutlineIdx = combinedPoints[next].OutlineIndex;

                // Check if OutlineIndex is chained or wrapped
                if (nextOutlineIdx == currOutlineIdx + 1 || nextOutlineIdx == 0)
                {
                    int origA = i;
                    int shadowA = i + 1;
                    int origB = next;
                    int shadowB = next + 1;

                    // Triangle 1: origA, origB, shadowA
                    shadowTriangles.Add(new Triangle(origA, origB, shadowA));
                    // Triangle 2: shadowA, origB, shadowB
                    shadowTriangles.Add(new Triangle(shadowA, origB, shadowB));
                }
                else if (startIndex < 0)
                {
                    startIndex = i + 1;
                }
            }

            if (startIndex < 0)
            {
                startIndex = 0;
            }

            combinedPoints.ForEach(p => p.OutlineIndex = -1);

            for (int i = 0; i < startIndex; i += 2)
            {
                var point = remainingOriginalPoints.Dequeue();
                remainingOriginalPoints.Enqueue(point);
            }

            var orderedShadowPoints = new List<Point>();
            for (int i = 0; i < startIndex; i += 2)
            {
                orderedShadowPoints.Add(shadowPoints.Dequeue());
            }
            orderedShadowPoints.Reverse();
            orderedShadowPoints.AddRange(shadowPoints.Reverse());

            for (int i = 0; i < orderedShadowPoints.Count; i++)
            {
                orderedShadowPoints.ElementAt(i).OutlineIndex = i;
            }

            for (int i = 0; i < remainingOriginalPoints.Count; i++)
            {
                remainingOriginalPoints.ElementAt(i).OutlineIndex = i + orderedShadowPoints.Count;
            }

            InstanceComponent.Points = combinedPoints;
            InstanceComponent.Triangles = shadowTriangles;
        }
    }
}
