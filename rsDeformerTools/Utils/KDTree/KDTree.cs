using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deformer
{

    public class KDTree
    {
        private int dim;
        private KDNode m_root;
        public int m_count;

        public KDNode root { get { return m_root; } }
        public int dimentions { get { return dim; } }
        public int nodeCount { get { return m_count; } }


        // constructor
        public KDTree(int dim)
        {
            this.dim = dim;
            m_root = null;
        }

        // insert new point into tree ( this is using tri centroid and tri id )
        public void insert(Vector3 vert, int id)
        {
            m_root = KDNode.ins(new HPoint(vert, id), m_root, 0, dim);
            m_count++;
        }

        /// <summary>
        /// K-D Tree node class
        /// </summary>
        public class KDNode
        {
            // these are seen by KDTree
            public HPoint point;
            public KDNode left, right;



            // Method ins translated from 352.ins.c of Gonnet & Baeza-Yates
            public static KDNode ins(HPoint point, KDNode t, int axis, int dim)
            {
                if (t == null)
                {
                    t = new KDNode(point);
                }

                else if (point.position[axis] > t.point.position[axis])
                {
                    t.right = ins(point, t.right, (axis + 1) % dim, dim);
                }
                else
                {
                    t.left = ins(point, t.left, (axis + 1) % dim, dim);
                }

                return t;
            }

            // Method Nearest Neighbor from Andrew Moore's thesis. Numbered
            // comments are direct quotes from there. Step "SDL" is added to
            // make the algorithm work correctly.  NearestNeighborList solution
            // courtesy of Bjoern Heckel.
            public static void nnbr(KDNode kd, HPoint target, HRect hr,
                                  double max_dist_sqd, int axis, int dim,
                                  NearestNeighborList nnl)
            {

                // 1. if kd is empty then set dist-sqd to infinity and exit.
                if (kd == null) return;

                // 2. s := split field of kd
                int curAxis = axis % dim;

                // 3. pivot := dom-elt field of kd
                HPoint pivot = kd.point;
                double pivot_to_target = HPoint.sqrdist(pivot, target);

                // 4. Cut hr into to sub-hyperrectangles left-hr and right-hr.
                //    The cut plane is through pivot and perpendicular to the s
                //    dimension.
                HRect left_hr = hr; // optimize by not cloning
                HRect right_hr = (HRect)hr.clone();
                left_hr.max.position[curAxis] = pivot.position[curAxis];
                right_hr.min.position[curAxis] = pivot.position[curAxis];

                // 5. target-in-left := target_s <= pivot_s
                bool target_in_left = target.position[curAxis] < pivot.position[curAxis];

                KDNode nearer_kd;
                HRect nearer_hr;
                KDNode further_kd;
                HRect further_hr;

                // 6. if target-in-left then
                //    6.1. nearer-kd := left field of kd and nearer-hr := left-hr
                //    6.2. further-kd := right field of kd and further-hr := right-hr
                if (target_in_left)
                {
                    nearer_kd = kd.left;
                    nearer_hr = left_hr;
                    further_kd = kd.right;
                    further_hr = right_hr;
                }
                //
                // 7. if not target-in-left then
                //    7.1. nearer-kd := right field of kd and nearer-hr := right-hr
                //    7.2. further-kd := left field of kd and further-hr := left-hr
                else
                {
                    nearer_kd = kd.right;
                    nearer_hr = right_hr;
                    further_kd = kd.left;
                    further_hr = left_hr;
                }

                // 8. Recursively call Nearest Neighbor with paramters
                //    (nearer-kd, target, nearer-hr, max-dist-sqd), storing the
                //    results in nearest and dist-sqd
                nnbr(nearer_kd, target, nearer_hr, max_dist_sqd, axis + 1, dim, nnl);

                KDNode nearest = (KDNode)nnl.getHighest();
                double dist_sqd;

                if (!nnl.isCapacityReached())
                {
                    dist_sqd = Double.MaxValue;
                }
                else
                {
                    dist_sqd = nnl.getMaxPriority();
                }

                // 9. max-dist-sqd := minimum of max-dist-sqd and dist-sqd
                max_dist_sqd = System.Math.Min(max_dist_sqd, dist_sqd);

                // 10. A nearer point could only lie in further-kd if there were some
                //     part of further-hr within distance sqrt(max-dist-sqd) of
                //     target.  If this is the case then
                HPoint closest = further_hr.closest(target);
                if (HPoint.eucdist(closest, target) <= System.Math.Sqrt(max_dist_sqd))
                {

                    // 10.1 if (pivot-target)^2 < dist-sqd then
                    if (pivot_to_target < dist_sqd)
                    {

                        // 10.1.1 nearest := (pivot, range-elt field of kd)
                        nearest = kd;

                        // 10.1.2 dist-sqd = (pivot-target)^2
                        dist_sqd = pivot_to_target;

                        // add to nnl
                        nnl.insert(kd, dist_sqd);


                        // 10.1.3 max-dist-sqd = dist-sqd
                        // max_dist_sqd = dist_sqd;
                        if (nnl.isCapacityReached())
                        {
                            max_dist_sqd = nnl.getMaxPriority();
                        }
                        else
                        {
                            max_dist_sqd = Double.MaxValue;
                        }
                    }

                    // 10.2 Recursively call Nearest Neighbor with parameters
                    //      (further-kd, target, further-hr, max-dist_sqd),
                    //      storing results in temp-nearest and temp-dist-sqd
                    nnbr(further_kd, target, further_hr, max_dist_sqd, axis + 1, dim, nnl);
                    KDNode temp_nearest = (KDNode)nnl.getHighest();
                    double temp_dist_sqd = nnl.getMaxPriority();

                    // 10.3 If tmp-dist-sqd < dist-sqd then
                    if (temp_dist_sqd < dist_sqd)
                    {

                        // 10.3.1 nearest := temp_nearest and dist_sqd := temp_dist_sqd
                        nearest = temp_nearest;
                        dist_sqd = temp_dist_sqd;
                    }
                }

                // SDL: otherwise, current point is nearest
                else if (pivot_to_target < max_dist_sqd)
                {
                    nearest = kd;
                    dist_sqd = pivot_to_target;
                }
            }


            // constructor is used only by class; other methods are static
            private KDNode(HPoint key)
            {
                point = key;
                left = null;
                right = null;
            }

        }
        public void nearestAddNbrs(Vector3 query, int numResults, ref List<int> nbrs)
        {
            NearestNeighborList nnl = new NearestNeighborList(numResults);

            // initial call is with infinite hyper-rectangle and max distance
            HRect hr = HRect.infiniteHRect();
            double max_dist_sqd = Double.MaxValue;
            HPoint keyp = new HPoint(query);

            KDNode.nnbr(m_root, keyp, hr, max_dist_sqd, 0, 3, nnl);

            for (int i = 0; i < numResults; ++i)
            {
                KDNode kd = (KDNode)nnl.removeHighest();
                nbrs.Add((int)kd.point.id);
            }
        }
    }
}