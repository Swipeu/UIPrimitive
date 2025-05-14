using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
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

            points.Clear();
            triangles.Clear();

            if(smoothness <= 1 || radius <= 0)
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

            AddSingleCorner(new Vector2(rect.x, rect.y), Quaternion.identity);
            triangles.Add(new Triangle(0, points.Count - 1, points.Count));
            AddSingleCorner(new Vector2(rect.x, rect.y + rect.height), Quaternion.Euler(0,0, -90));
            triangles.Add(new Triangle(0, points.Count - 1, points.Count));
            AddSingleCorner(new Vector2(rect.x + rect.width, rect.y + rect.height), Quaternion.Euler(0,0, -180));
            triangles.Add(new Triangle(0, points.Count - 1, points.Count));
            AddSingleCorner(new Vector2(rect.x + rect.width, rect.y), Quaternion.Euler(0,0, -270));
            triangles.Add(new Triangle(0, points.Count - 1, 1));
        }

        void AddSingleCorner(Vector2 point, Quaternion rotation)
        {
            Vector2 offsetPoint = point + (Vector2)(rotation *  Vector2.one * radius);
            int currentCount = points.Count;

            for (int i = 0; i < smoothness; i++)
            {
                Vector2 direction = rotation * Quaternion.Euler(0, 0, -90 * (i / ((float)smoothness - 1))) * Vector2.down;
                points.Add(new Point(offsetPoint + direction * radius, points.Count - 1));

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
