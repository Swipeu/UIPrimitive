using UnityEngine;

namespace Swipeu.UIPrimitive
{
    public class PrimitiveRoundedRectangle : PrimitiveBase
    {
        [SerializeField] int smoothness = 1;
        [SerializeField] float radius;
        protected override void RefreshCache()
        {
            Rect rect = GetPixelAdjustedRect();

            // Clamp the radius to ensure it doesn't exceed half the width or height of the rectangle
            float clampedRadius = Mathf.Min(radius, rect.width / 2, rect.height / 2);

            points.Clear();
            triangles.Clear();

            if (smoothness <= 1 || clampedRadius <= 0)
            {
                points.Add(new Point(new Vector2(rect.x, rect.y), 0));
                points.Add(new Point(new Vector2(rect.x, rect.y + rect.height), 1));
                points.Add(new Point(new Vector2(rect.x + rect.width, rect.y + rect.height), 2));
                points.Add(new Point(new Vector2(rect.x + rect.width, rect.y), 3));
                triangles.Add(new Triangle(0, 1, 2));
                triangles.Add(new Triangle(0, 2, 3));
                return;
            }

            Vector2 center = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);
            points.Add(new Point(center));

            AddSingleCorner(new Vector2(rect.x, rect.y), Quaternion.identity, clampedRadius, rect.height / 2);
            triangles.Add(new Triangle(0, points.Count - 1, points.Count));
            AddSingleCorner(new Vector2(rect.x, rect.y + rect.height), Quaternion.Euler(0, 0, -90), clampedRadius, rect.width / 2);
            triangles.Add(new Triangle(0, points.Count - 1, points.Count));
            AddSingleCorner(new Vector2(rect.x + rect.width, rect.y + rect.height), Quaternion.Euler(0, 0, -180), clampedRadius, rect.height / 2);
            triangles.Add(new Triangle(0, points.Count - 1, points.Count));
            AddSingleCorner(new Vector2(rect.x + rect.width, rect.y), Quaternion.Euler(0, 0, -270), clampedRadius, rect.width / 2);
            triangles.Add(new Triangle(0, points.Count - 1, 1));
        }

        void AddSingleCorner(Vector2 point, Quaternion rotation, float clampedRadius, float valueToCompare)
        {
            Vector2 offsetPoint = point + (Vector2)(rotation * Vector2.one * clampedRadius);
            int currentCount = points.Count;

            for (int i = 0; i < smoothness; i++)
            {
                // Skip the last point if the radius is exactly half the width or height
                if (i == smoothness - 1 && Mathf.Approximately(clampedRadius, valueToCompare))
                    continue;

                Vector2 direction = rotation * Quaternion.Euler(0, 0, -90 * (i / ((float)smoothness - 1))) * Vector2.down;
                points.Add(new Point(offsetPoint + direction * clampedRadius, points.Count - 1));

                if (i < 1)
                    continue;

                triangles.Add(new Triangle(0, currentCount + i - 1, currentCount + i));
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/UI/Primitive/Rounded Rectangle", false, 10)]
        static void CreateCustomGameObject(UnityEditor.MenuCommand menuCommand) => InstantiateNewObjectFromMenu<PrimitiveRoundedRectangle>(menuCommand);
#endif
    }
}
