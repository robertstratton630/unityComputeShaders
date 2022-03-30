using System;
using UnityEngine;


namespace Deformer
{
    /// <summary>
    /// Hyper-Rectangle class supporting KDTree class
    /// </summary>
    public class HRect
    {
        public HPoint min;
        public HPoint max;

        protected HRect()
        {
            min = new HPoint();
            max = new HPoint();
        }

        protected HRect(HPoint vmin, HPoint vmax)
        {

            min = (HPoint)vmin.clone();
            max = (HPoint)vmax.clone();
        }

        public HRect clone()
        {
            return new HRect(min, max);
        }

        // from Moore's eqn. 6.6
        public HPoint closest(HPoint t)
        {

            HPoint p = new HPoint();

            for (int i = 0; i < 3; ++i)
            {
                if (t.position[i] <= min.position[i])
                {
                    p.position[i] = min.position[i];
                }
                else if (t.position[i] >= max.position[i])
                {
                    p.position[i] = max.position[i];
                }
                else
                {
                    p.position[i] = t.position[i];
                }
            }
            
            return p;
        }

        // used in initial conditions of KDTree.nearest()
        public static HRect infiniteHRect()
        {

            HPoint vmin = new HPoint();
            HPoint vmax = new HPoint();

            vmin.position = Vector3.negativeInfinity;
            vmax.position = Vector3.positiveInfinity;
            
            return new HRect(vmin, vmax);
        }

    }
}
