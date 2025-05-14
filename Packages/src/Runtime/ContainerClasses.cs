using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swipeu.UIPrimitive
{

    [Serializable]
    public class VisualSettings
    {
        [SerializeField] public Color color = Color.white;
        [SerializeField] public bool outline;
        [SerializeField] public bool glow;
        [SerializeField] [Range(-200, 200)] public float outlineWidth;
        [SerializeField] [Range(0, 10)] public int detailLevel;
        [SerializeField] public bool antialiasing;
        [SerializeField] public bool innerShadow;

        public void Copy(VisualSettings visualSettings)
        {
            this.color = visualSettings.color;
            this.outline = visualSettings.outline;
            this.glow = visualSettings.glow;
            this.outlineWidth = visualSettings.outlineWidth;
            this.detailLevel = visualSettings.detailLevel;
            this.antialiasing = visualSettings.antialiasing;
            this.innerShadow = visualSettings.innerShadow;
        }
    }

    [Serializable]
    public class TransformSettings
    {
        [SerializeField] [Range(-200, 200)] public float sizeOffset;
        [SerializeField] public Vector2 positionOffset;

        public void Copy(TransformSettings transformSettings)
        {
            this.sizeOffset = transformSettings.sizeOffset;
            this.positionOffset = transformSettings.positionOffset;
        }
    }
    public class Point
    {
        public Vector2 Position { get; set; }
        public int OutlineIndex { get; set; }
        public Color Color { get; set; }

        Point() { }

        public Point(Vector2 position, int outlineIndex = -1)
        {
            Position = position;
            OutlineIndex = outlineIndex;
            Color = Color.white;
        }
        public Point(Point currentPoint, Vector2 offsetVector)
        {
            Position = currentPoint.Position + offsetVector;
            OutlineIndex = currentPoint.OutlineIndex;
            Color = currentPoint.Color;
        }
        public Point Copy()
        {
            return new Point() { Position = Position, OutlineIndex = OutlineIndex, Color = Color };
        }
    }

    public class Triangle
    {
        public int PointAIndex { get; set; }
        public int PointBIndex { get; set; }
        public int PointCIndex { get; set; }


        public List<Point> GetPoints(List<Point> points)
        {
            return new List<Point>() { points[PointAIndex], points[PointBIndex], points[PointCIndex] };
        }

        public Point GetPointA(List<Point> points)
        {
            return points[PointAIndex];
        }
        public Point GetPointB(List<Point> points)
        {
            return points[PointBIndex];
        }
        public Point GetPointC(List<Point> points)
        {
            return points[PointCIndex];
        }

        public Triangle(int pointAIndex, int pointBIndex, int pointCIndex)
        {
            PointAIndex = pointAIndex;
            PointBIndex = pointBIndex;
            PointCIndex = pointCIndex;
        }

        public Triangle Copy()
        {
            return new Triangle(PointAIndex, PointBIndex, PointCIndex);
        }
    }
}
