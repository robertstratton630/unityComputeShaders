using System;
using UnityEngine;

namespace Deformer
{
    /// <summary>
    /// Hyper-Point class supporting KDTree class
    /// </summary>
    public class HPoint
    {
        public Vector3 position;
        public int id = 1;


        public HPoint()
        {
        }

        public HPoint(Vector3 position)
        {
            this.position = position;
        }

        public HPoint(Vector3 position,int id)
        {
            this.position = position;
            this.id = id;
        }

        public HPoint clone()
        {

            return new HPoint(position,id);
        }

        public static double sqrdist(HPoint x, HPoint y)
        {
            Vector3 tmp = x.position - y.position;
            double dist = tmp.sqrMagnitude;
            return dist;
        }

        public static double eucdist(HPoint x, HPoint y)
        {
            return System.Math.Sqrt(sqrdist(x, y));
        }
    }
}
