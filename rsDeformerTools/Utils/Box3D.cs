using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Deformer
{

    public class PointRay
    {
        public Vector3 orig = Vector3.zero;
        public Vector3 dir = Vector3.zero;

        public PointRay(Vector3 orig, Vector3 dir)
        {
            this.orig = orig;
            this.dir = dir;
        }
    }

    public class Line3D
    {
        public Vector3 pointA = Vector3.zero;
        public Vector3 pointB = Vector3.zero;

        public Line3D(Vector3 pointA, Vector3 pointB)
        {
            this.pointA = pointA;
            this.pointB = pointB;
        }
    }

    public class Box3D
    {
        public static readonly int Stride = 8 * sizeof(float);

        public Vector3 Min;
        public float Padding0;
        public Vector3 Max;
        public float Padding1;

        public static Box3D Union(Box3D a, Box3D b)
        {
            return
              new Box3D
              (
                new Vector3
                (
                  Mathf.Min(a.Min.x, b.Min.x),
                  Mathf.Min(a.Min.y, b.Min.y),
                  Mathf.Min(a.Min.z, b.Min.z)
                ),
                new Vector3
                (
                  Mathf.Max(a.Max.x, b.Max.x),
                  Mathf.Max(a.Max.y, b.Max.y),
                  Mathf.Max(a.Max.z, b.Max.z)
                )
              );
        }

        public static bool Intersects(Box3D a, Box3D b)
        {
            return
                 a.Min.x <= b.Max.x && a.Max.x >= b.Min.x
              && a.Min.y <= b.Max.y && a.Max.y >= b.Min.y
              && a.Min.z <= b.Max.z && a.Max.z >= b.Min.z;
        }

        private static Box3D s_empty = new Box3D(float.MaxValue * Vector3.one, float.MinValue * Vector3.one);
        public static Box3D Empty { get { return s_empty; } }

        public float HalfArea { get { Vector3 e = Max - Min; return e.x * e.y + e.y * e.z + e.z * e.x; } }
        public float Area { get { Vector3 e = Max - Min; return 2.0f * (e.x * e.y + e.y * e.z + e.z * e.x); } }


        public Vector3 Center { get { return 0.5f * (Min + Max); } }
        public Vector3 Extents { get { return Max - Min; } }
        public Vector3 HalfExtents { get { return 0.5f * (Max - Min); } }

        public Box3D()
        {
            Min = float.MaxValue * Vector3.one;
            Max = float.MinValue * Vector3.one;
            Padding0 = Padding1 = 0.0f;
        }

        public Box3D(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
            Padding0 = Padding1 = 0.0f;
        }

        public void Include(Vector3 p)
        {
            Min.x = Mathf.Min(Min.x, p.x);
            Min.y = Mathf.Min(Min.y, p.y);
            Min.z = Mathf.Min(Min.z, p.z);

            Max.x = Mathf.Max(Max.x, p.x);
            Max.y = Mathf.Max(Max.y, p.y);
            Max.z = Mathf.Max(Max.z, p.z);
        }

        public void Expand(float r)
        {
            Min.x -= r;
            Min.y -= r;
            Min.z -= r;

            Max.x += r;
            Max.y += r;
            Max.z += r;
        }

        public void Expand(Vector3 r)
        {
            Min.x -= r.x;
            Min.y -= r.y;
            Min.z -= r.z;

            Max.x += r.x;
            Max.y += r.y;
            Max.z += r.z;
        }

        public bool Contains(Box3D rhs)
        {
            return
                 Min.x <= rhs.Min.x
              && Min.y <= rhs.Min.y
              && Min.z <= rhs.Min.z
              && Max.x >= rhs.Max.x
              && Max.y >= rhs.Max.y
              && Max.z >= rhs.Max.z;
        }

        public int MaxDimension()
        {
            int result = 0;
            if (Extents.y > Extents.x)
            {
                result = 1;
                if (Extents.z > Extents.y)
                {
                    result = 2;
                }
            }
            else
            {
                if (Extents.z > Extents.x)
                {
                    result = 2;
                }
            }
            return result;
        }


        public void Draw()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Center,Extents);
        }


        // Real-time Collision Detection, p179.
        public float RayCast(Vector3 from, Vector3 to, float maxFraction = 1.0f)
        {
            float tMin = float.MinValue;
            float tMax = float.MaxValue;

            Vector3 d = to - from;
            Vector3 absD = VectorUtil.Abs(d);

            for (int axis = 0; axis < 3; ++axis)
            {
                float dComp = d[axis];
                float absDComp = absD[axis];
                float fromComp = from[axis];
                float minComp = Min[axis];
                float maxComp = Max[axis];

                if (absDComp < float.Epsilon)
                {
                    // parallel?
                    if (fromComp < minComp || maxComp < fromComp)
                        return float.MinValue;
                }
                else
                {
                    float invD = 1.0f / dComp;
                    float t1 = (minComp - fromComp) * invD;
                    float t2 = (maxComp - fromComp) * invD;

                    if (t1 > t2)
                    {
                        float temp = t1;
                        t1 = t2;
                        t2 = temp;
                    }

                    tMin = Mathf.Max(tMin, t1);
                    tMax = Mathf.Min(tMax, t2);

                    if (tMin > tMax)
                        return float.MinValue;
                }
            }

            // does the ray start inside the box?
            // does the ray intersect beyond the max fraction?
            if (tMin < 0.0f || maxFraction < tMin)
                return float.MinValue;

            // intersection detected
            return tMin;
        }
    }

    struct PointBox3D
    {
        Vector3 pt;
        Box3D box;
    };

}