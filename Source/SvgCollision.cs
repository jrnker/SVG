using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;


// Code exerps from CodeProject http://www.codeproject.com/Articles/15573/2D-Polygon-Collision-Detection, licensed under MIT license


namespace Svg
{
    public class SvgCollision
    {
        private static List<PolygonCollisionResult> _collisionResultList;
        private static SvgElementCollection _elCol;
        private static Vector _velocity;
        private static collisionCheckType _colCheckType;
        private static float _minimumArea;

        /// <summary>
        /// Holds the result from the tests, sometimes only with partial results depending on the test.
        /// </summary>
        public struct PolygonCollisionResult
        {
            public bool WillIntersect; // Are the polygons going to intersect forward in time?
            public bool IsIntersecting; // Are the polygons currently intersecting
            public bool OnPath; // Are the polygons paths currently intersecting
            public Vector MinimumTranslationVector; // The translation to apply to polygon A to push the polygons appart. 
            public string collidor; // The object that is moving
            public string collidee; // The object that got collided with
            public List<PointF> LineIntersectingPoints;
            public boundCrossingType rayCastingResult; // Will be odd if inside a polygon
        }

        /// <summary>
        /// Type of test to perform
        /// </summary>
        public enum collisionCheckType
        {
            LineCollision,
            SeparatingAxisTheorem,
            Mixed
        }
        
        /// <summary>
        /// Declares if the object tested is inside or outside (or mixed) the object tested against, by using raycasting. 
        /// </summary>
        public enum boundCrossingType
        { 
            inside,
            outside,
            mixed
        }
        /// <summary>
        /// Test all visualelements in a SvgElementCollection for collisions and return the result as a list of PolygonCollisionResult
        /// </summary>
        /// <param name="elCol"></param>
        /// <param name="colCheckType">Optional</param>
        /// <param name="minimumArea">Optional, should only be used if a majority of objects should be excluded from collision detection as it otherwise can cause a large performance penalty</param>
        /// <param name="velocity">Optional to simulate movement of all objects in certain direction. Result stored in PolygonCollisionResult.WillIntersect.</param>
        /// <returns></returns>
        public static List<PolygonCollisionResult> checkForCollision(SvgElementCollection elCol, collisionCheckType colCheckType = collisionCheckType.Mixed, float minimumArea = 0, Vector velocity = default(Vector))
        {
            _collisionResultList = new List<PolygonCollisionResult>(); 
            _elCol = elCol;
            _velocity = velocity;
            _colCheckType = colCheckType;
            _minimumArea = minimumArea;
            iterate(elCol, elCol);
            return _collisionResultList;
        }

        private static void iterate(SvgElementCollection elementCollection1, SvgElementCollection elementCollection2)
        { 
            foreach (SvgElement outerElement in elementCollection1)
            {
                if (outerElement.Children.Count != 0) iterate(outerElement.Children, elementCollection2);
                foreach (SvgElement innerElement in elementCollection2)
                {
                    if (innerElement.Children.Count != 0) iterate(outerElement.Children, innerElement.Children);
                    {
                        if (innerElement is SvgVisualElement && outerElement is SvgVisualElement && outerElement != innerElement && !isParent(innerElement, outerElement))
                        {
                            PolygonCollisionResult r = new PolygonCollisionResult(); 
                            try
                            {
                                if (_minimumArea != 0)
                                {
                                    if (((SvgVisualElement)outerElement).PathOuterArea.Value < _minimumArea) break;
                                    if (((SvgVisualElement)innerElement).PathOuterArea.Value < _minimumArea) break;
                                }
                                if (outerElement is SvgCircle) ((SvgCircle)outerElement).Path.Flatten();
                                if (outerElement is SvgEllipse) ((SvgEllipse)outerElement).Path.Flatten();
                                if (innerElement is SvgCircle) ((SvgCircle)innerElement).Path.Flatten();
                                if (innerElement is SvgEllipse) ((SvgEllipse)innerElement).Path.Flatten();
                                PointF[] ptsA = ((SvgVisualElement)outerElement).Path.PathPoints;
                                PointF[] ptsB = ((SvgVisualElement)innerElement).Path.PathPoints;

                                switch (_colCheckType)
                                {
                                    case collisionCheckType.LineCollision:
                                        r = LineIntersection.PolygonCollision(ptsA, ptsB);
                                        break;
                                    case collisionCheckType.SeparatingAxisTheorem:
                                        r = SeparatingAxisTheorem.PolygonCollision(ptsA, ptsB, _velocity);
                                        break;
                                    case collisionCheckType.Mixed:
                                        r = SeparatingAxisTheorem.PolygonCollision(ptsA, ptsB, _velocity);
                                        if (r.IsIntersecting)
                                        {
                                            PolygonCollisionResult t = LineIntersection.PolygonCollision( ptsA, ptsB);
                                            r.rayCastingResult = LineIntersection.RayCasting(ptsA, ptsB);
                                            r.OnPath = t.OnPath;
                                            r.LineIntersectingPoints = t.LineIntersectingPoints; 
                                        }
                                        break;
                                }
                            }
                            catch { } 

                            if (r.IsIntersecting == true || r.WillIntersect == true)
                            {
                                r.collidor = outerElement.ID ?? "-";
                                r.collidee = innerElement.ID ?? "-";
                                _collisionResultList.Add(r);
                            }
                        }
                    }
                }
            }
        }

        private static Boolean isParent(SvgElement parent, SvgElement child)
        {
            SvgElement testIfParent = child.Parent;
            while (true)
            {
                if (testIfParent == null)
                { return false; }
                if (parent == testIfParent)
                { return true; }
                testIfParent = testIfParent.Parent;
            }
        }
    }

    public class LineIntersection
    {
        /// <summary>
        /// Test if two polygons collide by testing each line segment in polygon 1 against each line segment in polygon 2.
        /// Returns a parially filled SvgCollision.PolygonCollisionResult.
        /// </summary>
        /// <param name="ptsA">Polygon 1 point array</param>
        /// <param name="ptsB">Polygon 2 point array</param> 
        /// <returns></returns>
        public static SvgCollision.PolygonCollisionResult PolygonCollision(PointF[] ptsA, PointF[] ptsB)
        {
            SvgCollision.PolygonCollisionResult result = new SvgCollision.PolygonCollisionResult(); 
            result.LineIntersectingPoints = new List<PointF>();
            PointF previousA = new PointF(); 
            PointF previousB = new PointF();

           foreach (PointF ptsa in ptsA)
           {
               if (!(previousA.IsEmpty))
               {
                   foreach (PointF ptsb in ptsB)
                   {
                       if (!(previousB.IsEmpty))
                       {
                           if (ptsa != ptsb && previousA != previousB)
                           {
                               bool blurp = DoLinesIntersect(previousA, ptsa, previousB, ptsb);
                               if (blurp)
                               {
                                   result.IsIntersecting = true;
                                   result.OnPath = true; 
                                   PointF intersection;
                                   LineIntersectionPoint(ptsa, previousA, ptsb, previousB, out intersection);
                                   result.LineIntersectingPoints.Add(intersection);
                               }
                           }
                       }
                       previousB = ptsb;
                   }
                   previousB = new PointF();
               }
               previousA = ptsa;
           } 
            return result;
        }

        private static bool LineIntersectionPoint(PointF ps1, PointF pe1, PointF ps2, PointF pe2)
        {
            PointF dummy;
            return LineIntersectionPoint(ps1, pe1, ps2, pe2, out dummy);
        }
        private static bool LineIntersectionPoint(PointF ps1, PointF pe1, PointF ps2, PointF pe2, out PointF intersection)
        {
            bool result = true;
            // Get A,B,C of first line - points : ps1 to pe1
            float A1 = pe1.Y - ps1.Y;
            float B1 = ps1.X - pe1.X;
            float C1 = A1 * ps1.X + B1 * ps1.Y;

            // Get A,B,C of second line - points : ps2 to pe2
            float A2 = pe2.Y - ps2.Y;
            float B2 = ps2.X - pe2.X;
            float C2 = A2 * ps2.X + B2 * ps2.Y;

            // Get delta and check if the lines are parallel
            float delta = A1 * B2 - A2 * B1;
            if (delta == 0) result = false;

            // now return the Vector2 intersection point
            intersection = new PointF(
                (B2 * C1 - B1 * C2) / delta,
                (A1 * C2 - A2 * C1) / delta
            ); 
            return result;
        }
        /// <summary>
        /// Test if two lines intersect by providing start and end points for the two lines.
        /// </summary>
        /// <param name="ps1">Start point line 1</param>
        /// <param name="pe1">End point line 1</param>
        /// <param name="ps2">Start point line 2</param>
        /// <param name="pe2">End point line 2</param>
        /// <returns></returns>
        public static bool DoLinesIntersect(PointF ps1, PointF pe1, PointF ps2, PointF pe2)
        {
            PointF CmP = new PointF(ps2.X - ps1.X, ps2.Y - ps1.Y);
            PointF r = new PointF(pe1.X - ps1.X, pe1.Y - ps1.Y);
            PointF s = new PointF(pe2.X - ps2.X, pe2.Y - ps2.Y);

            float CmPxr = CmP.X * r.Y - CmP.Y * r.X;
            float CmPxs = CmP.X * s.Y - CmP.Y * s.X;
            float rxs = r.X * s.Y - r.Y * s.X;

            if (CmPxr == 0f)
            {
                // Lines are collinear, and so intersect if they have any overlap

                return ((ps2.X - ps1.X < 0f) != (ps2.X - pe1.X < 0f))
                    || ((ps2.Y - ps1.Y < 0f) != (ps2.Y - pe1.Y < 0f));
            }

            if (rxs == 0f)
                return false; // Lines are parallel.

            float rxsr = 1f / rxs;
            float t = CmPxs * rxsr;
            float u = CmPxr * rxsr;

            return (t >= 0f) && (t <= 1f) && (u >= 0f) && (u <= 1f);
        }


        /// <summary>
        /// Returns the state polygon 2 crossing the bounds of a polygon 1.
        /// If minCrossings and maxCrossings are the same then the result is either inside or outside.
        /// If they aren't the same then polygon 2 is crossing the bounds of polygon 1. 
        /// </summary>
        /// <param name="ptsA"></param>
        /// <param name="ptsB"></param>
        /// <param name="minCrossings">Returns the minimum value the edges of polygon 2 crossed the bounds of polygon 1</param>
        /// <param name="maxCrossings">Returns the maximum value the edges of polygon 2 crossed the bounds of polygon 1</param>
        /// <returns></returns>
        public static SvgCollision.boundCrossingType RayCasting(PointF[] ptsA, PointF[] ptsB)
        { 
            int maxCrossings;
            return RayCasting(ptsA, ptsB,  out maxCrossings);
        }
        public static SvgCollision.boundCrossingType RayCasting(PointF[] ptsA, PointF[] ptsB, out int maxCrossings )  
        {
            maxCrossings = 0;
            int intersections = 0; 
            bool intersect, hasOddIntersects = false, hasEvenInterSects=false;
            foreach (PointF pts in ptsA)
            {
                intersect = IsPointInPolygon(ptsB, pts, out intersections);
                if ((intersections & 1) == 0) hasEvenInterSects = true;
                if ((intersections & 1) == 1) hasOddIntersects = true;
                if (intersections > maxCrossings) maxCrossings = intersections;
            }
            if (hasEvenInterSects & !hasOddIntersects) return SvgCollision.boundCrossingType.outside;  //All results are even
            else if (!hasEvenInterSects & hasOddIntersects) return SvgCollision.boundCrossingType.inside;  //All results are odd
            else return SvgCollision.boundCrossingType.mixed;  //Results are different
        }
        private static bool IsPointInPolygon(PointF[] poly, PointF point, out int intersections)
        {
            int i, j;
            intersections = 0;
            bool c = false;
            for (i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((((poly[i].Y <= point.Y) && (point.Y < poly[j].Y)) |
                    ((poly[j].Y <= point.Y) && (point.Y < poly[i].Y))) &&
                    (point.X < (poly[j].X - poly[i].X) * (point.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X))
                {
                    c = !c;
                    intersections++;
                }
            }
            return c;
        }
    }

    public class SeparatingAxisTheorem
    {

        public static SvgCollision.PolygonCollisionResult PolygonCollision(PointF[] ptsA, PointF[] ptsB, Vector velocity)
        {
            Polygon polygonA, polygonB;
            polygonA = new Polygon();
            polygonB = new Polygon();
            foreach (PointF pts in ptsA)
            {
                polygonA.Points.Add(new Vector(pts.X, pts.Y));
            }
            foreach (PointF pts in ptsB)
            {
                polygonB.Points.Add(new Vector(pts.X, pts.Y));
            }

            polygonA.BuildEdges();
            polygonB.BuildEdges();

            SeparatingAxisTheorem svgCollision = new SeparatingAxisTheorem();
            return svgCollision.PolygonCollision(polygonA, polygonB, velocity);
        }

		// Check if polygon A is going to collide with polygon B for the given velocity
        public SvgCollision.PolygonCollisionResult PolygonCollision(Polygon polygonA, Polygon polygonB, Vector velocity)
        {
            SvgCollision.PolygonCollisionResult result = new SvgCollision.PolygonCollisionResult();
			result.IsIntersecting = true;
			result.WillIntersect = true;

			int edgeCountA = polygonA.Edges.Count;
			int edgeCountB = polygonB.Edges.Count;
			float minIntervalDistance = float.PositiveInfinity;
			Vector translationAxis = new Vector();
			Vector edge;

			// Loop through all the edges of both polygons
			for (int edgeIndex = 0; edgeIndex < edgeCountA + edgeCountB; edgeIndex++) {
				if (edgeIndex < edgeCountA) {
					edge = polygonA.Edges[edgeIndex];
				} else {
					edge = polygonB.Edges[edgeIndex - edgeCountA];
				}

				// ===== 1. Find if the polygons are currently intersecting =====

				// Find the axis perpendicular to the current edge
				Vector axis = new Vector(-edge.Y, edge.X);
				axis.Normalize();

				// Find the projection of the polygon on the current axis
				float minA = 0; float minB = 0; float maxA = 0; float maxB = 0;
				ProjectPolygon(axis, polygonA, ref minA, ref maxA);
				ProjectPolygon(axis, polygonB, ref minB, ref maxB);

				// Check if the polygon projections are currentlty intersecting
                float intervalDistance = IntervalDistance(minA, maxA, minB, maxB);
                if (intervalDistance > 0) result.IsIntersecting = false;

				// ===== 2. Now find if the polygons *will* intersect ===== 
				// Project the velocity on the current axis
				float velocityProjection = axis.DotProduct(velocity);

				// Get the projection of polygon A during the movement
				if (velocityProjection < 0) {
					minA += velocityProjection;
				} else {
					maxA += velocityProjection;
				}

				// Do the same test as above for the new projection
				intervalDistance = IntervalDistance(minA, maxA, minB, maxB);
				if (intervalDistance > 0) result.WillIntersect = false;

				// If the polygons are not intersecting and won't intersect, exit the loop
				if (!result.IsIntersecting && !result.WillIntersect) break;

				// Check if the current interval distance is the minimum one. If so store
				// the interval distance and the current distance.
				// This will be used to calculate the minimum translation vector
				intervalDistance = Math.Abs(intervalDistance);
				if (intervalDistance < minIntervalDistance) {
					minIntervalDistance = intervalDistance;
					translationAxis = axis;

					Vector d = polygonA.Center - polygonB.Center;
					if (d.DotProduct(translationAxis) < 0) translationAxis = -translationAxis;
				} 
			}

			// The minimum translation vector can be used to push the polygons appart.
			// First moves the polygons by their velocity
			// then move polygonA by MinimumTranslationVector.
			if (result.WillIntersect) result.MinimumTranslationVector = translationAxis * minIntervalDistance;
			
			return result;
		}

		// Calculate the distance between [minA, maxA] and [minB, maxB]
		// The distance will be negative if the intervals overlap
		public float IntervalDistance(float minA, float maxA, float minB, float maxB) {
			if (minA < minB) {
				return minB - maxA;
			} else {
				return minA - maxB;
			}
		}

		// Calculate the projection of a polygon on an axis and returns it as a [min, max] interval
		public void ProjectPolygon(Vector axis, Polygon polygon, ref float min, ref float max) {
			// To project a point on an axis use the dot product
			float d = axis.DotProduct(polygon.Points[0]);
			min = d;
			max = d;
			for (int i = 0; i < polygon.Points.Count; i++) {
				d = polygon.Points[i].DotProduct(axis);
				if (d < min) {
					min = d;
				} else {
					if (d > max) {
						max = d;
					}
				}
			}
		}
	}

    public class Polygon
    {

        private List<Vector> points = new List<Vector>();
        private List<Vector> edges = new List<Vector>();

        public void BuildEdges()
        {
            Vector p1;
            Vector p2;
            edges.Clear();
            for (int i = 0; i < points.Count; i++)
            {
                p1 = points[i];
                if (i + 1 >= points.Count)
                {
                    p2 = points[0];
                }
                else
                {
                    p2 = points[i + 1];
                }
                edges.Add(p2 - p1);
            }
        }

        public List<Vector> Edges
        {
            get { return edges; }
        }

        public List<Vector> Points
        {
            get { return points; }
        }

        public Vector Center
        {
            get
            {
                float totalX = 0;
                float totalY = 0;
                for (int i = 0; i < points.Count; i++)
                {
                    totalX += points[i].X;
                    totalY += points[i].Y;
                }

                return new Vector(totalX / (float)points.Count, totalY / (float)points.Count);
            }
        }

        public void Offset(Vector v)
        {
            Offset(v.X, v.Y);
        }

        public void Offset(float x, float y)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Vector p = points[i];
                points[i] = new Vector(p.X + x, p.Y + y);
            }
        }

        public override string ToString()
        {
            string result = "";

            for (int i = 0; i < points.Count; i++)
            {
                if (result != "") result += " ";
                result += "{" + points[i].ToString(true) + "}";
            }

            return result;
        }

    }
    public struct Vector
    {

        public float X;
        public float Y;

        static public Vector FromPoint(Point p)
        {
            return Vector.FromPoint(p.X, p.Y);
        }

        static public Vector FromPoint(int x, int y)
        {
            return new Vector((float)x, (float)y);
        }

        public Vector(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public float Magnitude
        {
            get { return (float)Math.Sqrt(X * X + Y * Y); }
        }

        public void Normalize()
        {
            float magnitude = Magnitude;
            X = X / magnitude;
            Y = Y / magnitude;
        }

        public Vector GetNormalized()
        {
            float magnitude = Magnitude;

            return new Vector(X / magnitude, Y / magnitude);
        }

        public float DotProduct(Vector vector)
        {
            return this.X * vector.X + this.Y * vector.Y;
        }

        public float DistanceTo(Vector vector)
        {
            return (float)Math.Sqrt(Math.Pow(vector.X - this.X, 2) + Math.Pow(vector.Y - this.Y, 2));
        }

        public static implicit operator Point(Vector p)
        {
            return new Point((int)p.X, (int)p.Y);
        }

        public static implicit operator PointF(Vector p)
        {
            return new PointF(p.X, p.Y);
        }

        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.X + b.X, a.Y + b.Y);
        }

        public static Vector operator -(Vector a)
        {
            return new Vector(-a.X, -a.Y);
        }

        public static Vector operator -(Vector a, Vector b)
        {
            return new Vector(a.X - b.X, a.Y - b.Y);
        }

        public static Vector operator *(Vector a, float b)
        {
            return new Vector(a.X * b, a.Y * b);
        }

        public static Vector operator *(Vector a, int b)
        {
            return new Vector(a.X * b, a.Y * b);
        }

        public static Vector operator *(Vector a, double b)
        {
            return new Vector((float)(a.X * b), (float)(a.Y * b));
        }

        public override bool Equals(object obj)
        {
            Vector v = (Vector)obj;

            return X == v.X && Y == v.Y;
        }

        public bool Equals(Vector v)
        {
            return X == v.X && Y == v.Y;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public static bool operator ==(Vector a, Vector b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(Vector a, Vector b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        public override string ToString()
        {
            return X + ", " + Y;
        }

        public string ToString(bool rounded)
        {
            if (rounded)
            {
                return (int)Math.Round(X) + ", " + (int)Math.Round(Y);
            }
            else
            {
                return ToString();
            }
        }


    }

}