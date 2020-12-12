using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Habrador_Computational_Geometry
{
    //Generate a convex hull in 3d space with an iterative algorithm (also known as beneath-beyond)
    //Based on "Computational Geometry in C" by Joseph O'Rourke 
    public static class IterativeHullAlgorithm3D
    {
        public static HalfEdgeData3 GenerateConvexHull(HashSet<MyVector3> points)
        {
            HalfEdgeData3 convexHull = new HalfEdgeData3();

            //Step 1. Init by making a tetrahedron (triangular pyramid) and remove all points within the tetrahedron
            BuildFirstTetrahedron(points, convexHull);

            //To easier add and remove triangles, we have to connect the edges with an opposite edge
            convexHull.ConnectAllEdges();

            //Debug.Log(convexHull.faces.Count);

            //return convexHull;

            //Step 2. For each other point: 
            // -If the point is within the hull constrcuted so far, remove it
            // - Otherwise, see which triangles are visible to the point and remove them
            //   Then build new triangles from the edges that have no neighbor to the point

            List<MyVector3> pointsToAdd = new List<MyVector3>(points);

            int removedPointsCounter = 0;

            int debugCounter = 0;

            foreach (MyVector3 p in pointsToAdd)
            {
                //Is this point within the tetrahedron
                bool isWithinHull = _Intersections.PointWithinConvexHull(p, convexHull);

                if (isWithinHull)
                {
                    points.Remove(p);

                    removedPointsCounter += 1;

                    continue;
                }


                //The point is outside the tetrahedron so find a triangle which is visible from the point
                HalfEdgeFace3 visibleTriangle = null;

                HashSet<HalfEdgeFace3> triangles = convexHull.faces;

                foreach (HalfEdgeFace3 triangle in triangles)
                {
                    //A triangle is visible from a point the point is outside of a plane formed with the triangles position and normal 
                    Plane3 plane = new Plane3(triangle.edge.v.position, triangle.edge.v.normal);

                    bool isPointOutsidePlane = _Geometry.IsPointOutsidePlane(p, plane);

                    //We have found a triangle which is visible from the point and should be removed
                    if (isPointOutsidePlane)
                    {
                        visibleTriangle = triangle;

                        break;
                    }
                }

                //If we didn't find a visible triangle, we have some kind of edge case and should move on for now
                if (visibleTriangle == null)
                {
                    Debug.LogWarning("Couldn't find a visible triangle so will ignore the point");

                    continue;
                }


                //Flood-fill from the visible triangle to find all other visible triangles
                //WHen you cross an edge from a visible triangle to an invisible triangle, 
                //save the edge because thhose edge should be used to build triangles with the point
                //These edges should belong to the triangle which is not visible
                HashSet<HalfEdge3> borderEdges = new HashSet<HalfEdge3>();

                //Store all visible triangles here so we can't visit triangles multiple times
                HashSet<HalfEdgeFace3> visibleTriangles = new HashSet<HalfEdgeFace3>();

                //The queue which we will use when flood-filling
                Queue<HalfEdgeFace3> trianglesToFloodFrom = new Queue<HalfEdgeFace3>();

                //Add the first triangle to init the flood-fill 
                trianglesToFloodFrom.Enqueue(visibleTriangle);

                List<HalfEdge3> edgesToCross = new List<HalfEdge3>();

                int safety = 0;

                while (true)
                {
                    //We have visited all visible triangles
                    if (trianglesToFloodFrom.Count == 0)
                    {
                        break;
                    }

                    HalfEdgeFace3 triangleToFloodFrom = trianglesToFloodFrom.Dequeue();

                    //This triangle is always visible and should be deleted
                    visibleTriangles.Add(triangleToFloodFrom);

                    //Investigate bordering triangles
                    edgesToCross.Clear();

                    edgesToCross.Add(triangleToFloodFrom.edge);
                    edgesToCross.Add(triangleToFloodFrom.edge.nextEdge);
                    edgesToCross.Add(triangleToFloodFrom.edge.nextEdge.nextEdge);

                    //Jump from this triangle to a bordering triangle
                    foreach (HalfEdge3 edgeToCross in edgesToCross)
                    {
                        HalfEdge3 oppositeEdge = edgeToCross.oppositeEdge;

                        if (oppositeEdge == null)
                        {
                            Debug.LogWarning("Found an opposite edge which is null");

                            break;
                        }

                        HalfEdgeFace3 oppositeTriangle = oppositeEdge.face;

                        //Have we visited this triangle before (only test visible triangles)?
                        if (trianglesToFloodFrom.Contains(oppositeTriangle) || visibleTriangles.Contains(oppositeTriangle))
                        {
                            continue;
                        }

                        //Check if this triangle is visible
                        //A triangle is visible from a point the point is outside of a plane formed with the triangles position and normal 
                        Plane3 plane = new Plane3(oppositeTriangle.edge.v.position, oppositeTriangle.edge.v.normal);

                        bool isPointOutsidePlane = _Geometry.IsPointOutsidePlane(p, plane);

                        //This triangle is visible so save it so we can flood from it
                        if (isPointOutsidePlane)
                        {
                            trianglesToFloodFrom.Enqueue(oppositeTriangle);
                        }
                        //This triangle is invisible. Since we only flood from visible triangles, 
                        //it means we crossed from a visible triangle to an invisible triangle, so save the crossing edge
                        else
                        {
                            borderEdges.Add(oppositeEdge);
                        }
                    }


                    safety += 1;

                    if (safety > 50000)
                    {
                        Debug.Log("Stuck in infinite loop when flood-filling visible triangles");

                        break;
                    }
                }


                //Remove all visible triangles
                foreach (HalfEdgeFace3 triangle in visibleTriangles)
                {
                    convexHull.DeleteTriangleFace(triangle);
                }


                //Make new triangle by connecting all edges on the border with the point 
                //Debug.Log($"Number of border edges: {borderEdges.Count}");

                foreach(HalfEdge3 borderEdge in borderEdges)
                {
                    //Each edge is point TO a vertex
                    MyVector3 p1 = borderEdge.prevEdge.v.position;
                    MyVector3 p2 = borderEdge.v.position;
                    

                    //Debug.DrawLine(p1.ToVector3(), p2.ToVector3(), Color.white, 2f);

                    //Debug.DrawLine(p1.ToVector3(), p.ToVector3(), Color.white, 2f);
                    //Debug.DrawLine(p2.ToVector3(), p.ToVector3(), Color.white, 2f);

                    //Debug.Log(borderEdge.face);

                    //The border edge belongs to a triangle which is invisible
                    //Because triangles are oriented clockwise, we have to add the vertices in the other direction
                    //to build a new triangle with the point
                    convexHull.AddTriangle(p2, p1, p);
                }


                //Connect all new triangles and the triangles on the border, 
                //so each edge has an opposite edge or flood filling will be impossible 
                convexHull.ConnectAllEdges();


                //debugCounter += 1;

                //if (debugCounter > 0)
                //{
                //    break;
                //}
            }


            Debug.Log($"Removed {removedPointsCounter} points during the construction of the hull because they were inside the hull");

            return convexHull;
        }



        //Initialize by making 2 triangles by using three points, so its a flat triangle with a face on each side
        //We could use the ideas from Quickhull to make the start triangle as big as possible
        //Then find a point which is the furthest away as possible from these triangles
        //Add that point and you have a tetrahedron (triangular pyramid)
        private static void BuildFirstTetrahedron(HashSet<MyVector3> points, HalfEdgeData3 convexHull)
        {
            //Of all points, find the two points that are furthes away from each other
            Edge3 eFurthestApart = FindEdgeFurthestApart(points);

            //Remove the two points we found         
            points.Remove(eFurthestApart.p1);
            points.Remove(eFurthestApart.p2);


            //Find a point which is the furthest away from this edge
            MyVector3 pointFurthestAway = FindPointFurthestFromEdge(eFurthestApart, points);

            //Remove the point
            points.Remove(pointFurthestAway);


            //Display the triangle
            //Debug.DrawLine(eFurthestApart.p1.ToVector3(), eFurthestApart.p2.ToVector3(), Color.white, 1f);
            //Debug.DrawLine(eFurthestApart.p1.ToVector3(), pointFurthestAway.ToVector3(), Color.blue, 1f);
            //Debug.DrawLine(eFurthestApart.p2.ToVector3(), pointFurthestAway.ToVector3(), Color.blue, 1f);


            //Now we can build two triangles
            //It doesnt matter how we build these triangles as long as they are opposite
            //But the normal matters, so make sure it is calculated so the triangles are ordered clock-wise while the normal is pointing out
            MyVector3 p1 = eFurthestApart.p1;
            MyVector3 p2 = eFurthestApart.p2;
            MyVector3 p3 = pointFurthestAway;

            convexHull.AddTriangle(p1, p2, p3);
            convexHull.AddTriangle(p1, p3, p2);

            //Debug.Log(convexHull.faces.Count);
            /*
            foreach (HalfEdgeFace3 f in convexHull.faces)
            {
                TestAlgorithmsHelpMethods.DebugDrawTriangle(f, Color.white, Color.red);
            }
            */

            //Find the point which is furthest away from the triangle (this point cant be co-planar)
            List<HalfEdgeFace3> triangles = new List<HalfEdgeFace3>(convexHull.faces);

            //Just pick one of the triangles
            HalfEdgeFace3 triangle = triangles[0];

            //Build a plane
            Plane3 plane = new Plane3(triangle.edge.v.position, triangle.edge.v.normal);

            //Find the point furthest away from the plane
            MyVector3 p4 = FindPointFurthestAwayFromPlane(points, plane);

            //Remove the point
            points.Remove(p4);

            //Debug.DrawLine(p1.ToVector3(), p4.ToVector3(), Color.green, 1f);
            //Debug.DrawLine(p2.ToVector3(), p4.ToVector3(), Color.green, 1f);
            //Debug.DrawLine(p3.ToVector3(), p4.ToVector3(), Color.green, 1f);

            //Now we have to remove one of the triangles == the triangle the point is outside of
            HalfEdgeFace3 triangleToRemove = triangles[0];
            HalfEdgeFace3 triangleToKeep = triangles[1];

            //This means the point is inside the triangle-plane, so we have to switch
            //We used triangle #0 to generate the plane
            if (_Geometry.GetSignedDistanceFromPointToPlane(p4, plane) < 0f)
            {
                triangleToRemove = triangles[1];
                triangleToKeep = triangles[0];
            }

            //Delete the triangle 
            convexHull.DeleteTriangleFace(triangleToRemove);

            //Build three new triangles

            //The triangle we keep is ordered clock-wise:
            MyVector3 p1_opposite = triangleToKeep.edge.v.position;
            MyVector3 p2_opposite = triangleToKeep.edge.nextEdge.v.position;
            MyVector3 p3_opposite = triangleToKeep.edge.nextEdge.nextEdge.v.position;

            //But we are looking at it from the back-side, 
            //so we add those vertices counter-clock-wise to make the new triangles clock-wise
            convexHull.AddTriangle(p1_opposite, p3_opposite, p4);
            convexHull.AddTriangle(p3_opposite, p2_opposite, p4);
            convexHull.AddTriangle(p2_opposite, p1_opposite, p4);

            //Debug.Log(convexHull.faces.Count);

            //Display what weve got so far
            //foreach (HalfEdgeFace3 f in convexHull.faces)
            //{
            //    TestAlgorithmsHelpMethods.DebugDrawTriangle(f, Color.white, Color.red);
            //}

            
            //Now we might as well remove all the points that are within the tetrahedron because they are not on the hull
            HashSet<MyVector3> pointsToRemove = new HashSet<MyVector3>();

            foreach (MyVector3 p in points)
            {
                bool isWithinConvexHull = _Intersections.PointWithinConvexHull(p, convexHull);

                if (isWithinConvexHull)
                {
                    pointsToRemove.Add(p);
                }
            }

            Debug.Log($"Found {pointsToRemove.Count} points within the tetrahedron that should be removed");

            foreach (MyVector3 p in pointsToRemove)
            {
                points.Remove(p);
            }
            
        }


       
        //Given points and a plane, find the point furthest away from the plane
        private static MyVector3 FindPointFurthestAwayFromPlane(HashSet<MyVector3> points, Plane3 plane)
        {
            //Cant init by picking the first point in a list because it might be co-planar
            MyVector3 bestPoint = default;

            float bestDistance = -Mathf.Infinity;

            foreach (MyVector3 p in points)
            {
                float distance = _Geometry.GetSignedDistanceFromPointToPlane(p, plane);

                //Make sure the point is not co-planar
                float epsilon = MathUtility.EPSILON;

                //If distance is around 0
                if (distance > -epsilon && distance < epsilon)
                {
                    continue;
                }

                //Make sure distance is positive
                if (distance < 0f) distance *= -1f;

                if (distance > bestDistance)
                {
                    bestDistance = distance;

                    bestPoint = p;
                }
            }

            return bestPoint;
        }



        //From a list of points, find the two points that are furthest away from each other
        private static Edge3 FindEdgeFurthestApart(HashSet<MyVector3> pointsHashSet)
        {
            List<MyVector3> points = new List<MyVector3>(pointsHashSet);
        

            //Find all possible combinations of edges between all points
            //TODO: Better to first find the points on the hull???
            List<Edge3> pointCombinations = new List<Edge3>();

            for (int i = 0; i < points.Count; i++)
            {
                MyVector3 p1 = points[i];

                for (int j = i + 1; j < points.Count; j++)
                {
                    MyVector3 p2 = points[j];

                    Edge3 e = new Edge3(p1, p2);

                    pointCombinations.Add(e);
                }
            }


            //Find the edge that is the furthest apart

            //Init by picking the first edge
            Edge3 eFurthestApart = pointCombinations[0];

            float maxDistanceBetween = MyVector3.SqrDistance(eFurthestApart.p1, eFurthestApart.p2);

            //Try to find a better edge
            for (int i = 1; i < pointCombinations.Count; i++)
            {
                Edge3 e = pointCombinations[i];

                float distanceBetween = MyVector3.SqrDistance(e.p1, e.p2);

                if (distanceBetween > maxDistanceBetween)
                {
                    maxDistanceBetween = distanceBetween;

                    eFurthestApart = e;
                }
            }

            return eFurthestApart;
        }



        //Given an edge and a list of points, find the point furthest away from the edge
        private static MyVector3 FindPointFurthestFromEdge(Edge3 edge, HashSet<MyVector3> pointsHashSet)
        {
            List<MyVector3> points = new List<MyVector3>(pointsHashSet);

            //Init with the first point
            MyVector3 pointFurthestAway = points[0];

            MyVector3 closestPointOnLine = _Geometry.GetClosestPointOnLine(edge, pointFurthestAway, withinSegment: false);

            float maxDistSqr = MyVector3.SqrDistance(pointFurthestAway, closestPointOnLine);

            //Try to find a better point
            for (int i = 1; i < points.Count; i++)
            {
                MyVector3 thisPoint = points[i];

                //TODO make sure that thisPoint is NOT colinear with the edge because then we wont be able to build a triangle

                closestPointOnLine = _Geometry.GetClosestPointOnLine(edge, thisPoint, withinSegment: false);

                float distSqr = MyVector3.SqrDistance(thisPoint, closestPointOnLine);

                if (distSqr > maxDistSqr)
                {
                    maxDistSqr = distSqr;

                    pointFurthestAway = thisPoint;
                }
            }


            return pointFurthestAway;
        }
    }
}