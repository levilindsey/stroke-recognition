/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StrokeCollector
{
	class StrokePreProcessing
	{

		#region COLLECTIONS_STATIC_UTILITY_METHODS

		/// <summary>
		/// Return the average resampling error in the given collection of resampled Points.
		/// </summary>
		public static double GetAvgResampleError(Point[] resampledPoints, int n, double pathLength)
		{
			double I = pathLength / (n - 1);
			double cumulativeDifference = 0;

			for (int i = 1; i < resampledPoints.Length; i++)
			{
				cumulativeDifference += Math.Abs(I - GetDistance(resampledPoints[i - 1], resampledPoints[i]));
			}

			return cumulativeDifference / (n - 1);
		}

		/// <summary>
		/// Resample the given array of Points into a new array of n relatively evenly-spaced 
		/// Points.
		/// </summary>
		public static Point[] Resample(Point[] oldPoints, int n)
		{
			double I = GetPathLength(oldPoints) / (n - 1);
			double D = 0.0;

			List<Point> newPoints = new List<Point>();

			Point oldPoint, newPoint;
			double d, ratio, qX, qY, qT;

			int i = 0;

			// Handle the fencepost problem (in regards to the old Points indexing)
			oldPoint = oldPoints[i++];
			newPoint = new Point(oldPoint);
			newPoints.Add(newPoint);

			while (i < oldPoints.Length)
			{
				oldPoint = newPoint;
				newPoint = oldPoints[i];

				d = GetDistance(oldPoint, newPoint);

				if (D + d >= I)
				{
					ratio = (I - D) / d;
					qX = oldPoint.X + ratio * (newPoint.X - oldPoint.X);
					qY = oldPoint.Y + ratio * (newPoint.Y - oldPoint.Y);
					qT = oldPoint.Timestamp + ratio * (newPoint.Timestamp - oldPoint.Timestamp);
					newPoint = new Point(qX, qY, qT);

					newPoints.Add(newPoint);

					D = 0.0;
				}
				else
				{
					D += d;
					i++;
				}
			}

			// Handle the fencepost problem (in regards to the overall Stroke length)
			if (newPoints.Count == n - 1)
			{
				newPoint = new Point(oldPoints[oldPoints.Length - 1]);
				newPoints.Add(newPoint);
			}

			return newPoints.ToArray();
		}

		/// <summary>
		/// Return the overall path length of the given array of Points.
		/// </summary>
		public static double GetPathLength(Point[] points)
		{
			double pathLength = 0;

			for (int i = 1; i < points.Length; i++)
			{
				pathLength += GetDistance(points[i - 1], points[i]);
			}

			return pathLength;
		}

		/// <summary>
		/// Remove any consecutive Points with identical X/Y coordinates from the given Points and 
		/// return the resulting collection.
		/// </summary>
		public static Point[] RemoveDuplicatePoints(Point[] points)
		{
			List<Point> cleanPoints = new List<Point>();

			Point previousPoint = points[0];
			cleanPoints.Add(previousPoint);

			for (int i = 1; i < points.Length; i++)
			{
				if (previousPoint.X != points[i].X || previousPoint.Y != points[i].Y)
				{
					previousPoint = points[i];
					cleanPoints.Add(previousPoint);
				}
			}

			return cleanPoints.ToArray();
		}

		/// <summary>
		/// Calculate and save smoothed speed values for each of the given Points.
		/// </summary>
		public static void SmoothSpeeds(Point[] points, int smoothingIterationCount)
		{
			double[] smoothedSpeeds;

			for (int j = 0; j < smoothingIterationCount; j++)
			{
				smoothedSpeeds = CalculateSmoothedSpeeds(points);
				ApplySmoothedSpeeds(smoothedSpeeds, points);
			}
		}

		private static double[] CalculateSmoothedSpeeds(Point[] points)
		{
			double[] smoothedSpeeds = new double[points.Length];

			smoothedSpeeds[0] = GetSmoothedSpeed(points[0], points[1]);

			for (int i = 1; i < points.Length - 1; i++)
			{
				smoothedSpeeds[i] = GetSmoothedSpeed(points[i - 1], points[i], points[i + 1]);
			}

			smoothedSpeeds[points.Length - 1] = GetSmoothedSpeed(points[points.Length - 1], points[points.Length - 2]);

			return smoothedSpeeds;
		}

		private static void ApplySmoothedSpeeds(double[] smoothedSpeeds, Point[] points)
		{
			for (int i = 0; i < points.Length; i++)
			{
				points[i].Speed = smoothedSpeeds[i];
			}
		}

		/// <summary>
		/// Calculate and save smoothed curvature values for each of the given Points.
		/// </summary>
		public static void SmoothCurvatures(Point[] points, int smoothingIterationCount)
		{
			double[] smoothedCurvatures;

			for (int j = 0; j < smoothingIterationCount; j++)
			{
				smoothedCurvatures = CalculateSmoothedCurvatures(points);
				ApplySmoothedCurvatures(smoothedCurvatures, points);
			}
		}

		private static double[] CalculateSmoothedCurvatures(Point[] points)
		{
			double[] smoothedCurvatures = new double[points.Length];

			smoothedCurvatures[0] = GetSmoothedCurvature(points[0], points[1]);

			for (int i = 1; i < points.Length - 1; i++)
			{
				smoothedCurvatures[i] = GetSmoothedCurvature(points[i - 1], points[i], points[i + 1]);
			}

			smoothedCurvatures[points.Length - 1] = GetSmoothedCurvature(points[points.Length - 1], points[points.Length - 2]);

			return smoothedCurvatures;
		}

		private static void ApplySmoothedCurvatures(double[] smoothedCurvatures, Point[] points)
		{
			for (int i = 0; i < points.Length; i++)
			{
				points[i].Curvature = smoothedCurvatures[i];
			}
		}

		/// <summary>
		/// Calculate and save the position derivatives for each of the given Points.
		/// </summary>
		public static void CalculatePointFirstDerivatives(Point[] points)
		{
			points[0].XFirstDeriv = GetXDerivative(points[0], points[1]);
			points[0].YFirstDeriv = GetYDerivative(points[0], points[1]);

			for (int i = 1; i < points.Length - 1; i++)
			{
				points[i].XFirstDeriv = GetXDerivative(points[i - 1], points[i + 1]);
				points[i].YFirstDeriv = GetYDerivative(points[i - 1], points[i + 1]);
			}

			points[points.Length - 1].XFirstDeriv = GetXDerivative(points[points.Length - 2], points[points.Length - 1]);
			points[points.Length - 1].YFirstDeriv = GetYDerivative(points[points.Length - 2], points[points.Length - 1]);
		}

		/// <summary>
		/// Calculate and save the position double derivatives for each of the given Points.
		/// </summary>
		public static void CalculatePointSecondDerivatives(Point[] points)
		{
			points[0].XSecondDeriv = GetXDoubleDerivative(points[0], points[1]);
			points[0].YSecondDeriv = GetYDoubleDerivative(points[0], points[1]);

			for (int i = 1; i < points.Length - 1; i++)
			{
				points[i].XSecondDeriv = GetXDoubleDerivative(points[i - 1], points[i + 1]);
				points[i].YSecondDeriv = GetYDoubleDerivative(points[i - 1], points[i + 1]);
			}

			points[points.Length - 1].XSecondDeriv = GetXDoubleDerivative(points[points.Length - 2], points[points.Length - 1]);
			points[points.Length - 1].YSecondDeriv = GetYDoubleDerivative(points[points.Length - 2], points[points.Length - 1]);
		}

		/// <summary>
		/// Calculate and save the curvature for each of the given Points.
		/// </summary>
		public static void CalculatePointCurvatures(Point[] points)
		{
			for (int i = 0; i < points.Length; i++)
			{
				points[i].Curvature = GetCurvature(points[i]);
			}
		}

		/// <summary>
		/// Calculate and save the speed for each of the given Points.
		/// </summary>
		public static void CalculatePointSpeeds(Point[] points)
		{
			points[0].Speed = GetSpeed(points[0], points[1]);

			for (int i = 1; i < points.Length - 1; i++)
			{
				points[i].Speed = GetSpeed(points[i - 1], points[i + 1]);
			}

			points[points.Length - 1].Speed = GetSpeed(points[points.Length - 2], points[points.Length - 1]);
		}

		/// <summary>
		/// Calculate and save the arc length values for each of the given Points.
		/// </summary>
		public static void CalculatePointArcLengths(Point[] points)
		{
			points[0].ArcLength = 0;

			for (int i = 1; i < points.Length; i++)
			{
				points[i].ArcLength =
					points[i - 1].ArcLength + GetDistance(points[i - 1], points[i]);
			}
		}

		/// <summary>
		/// Calculate and save the line segment for each of the given Points.
		/// </summary>
		public static void CalculatePointLines(Point[] points, bool showSpeedCurv)
		{
			for (int i = 0; i < points.Length - 1; i++)
			{
				SetLineState(points[i], points[i + 1], showSpeedCurv, false);
			}

			points[points.Length - 1].NullLine();
		}

		/// <summary>
		/// Calculate and save the straw values for each of the given resampled Points.  Also return the median straw value.
		/// </summary>
		public static double CalculatePointStrawValues(Point[] points)
		{
			List<double> strawValues = new List<double>();
			double strawValue;

			// Calculate and save the straw values
			for (int i = Params.STRAW_VALUE_W; i < points.Length - Params.STRAW_VALUE_W; i++)
			{
				strawValue = GetDistance(points[i - Params.STRAW_VALUE_W], points[Params.STRAW_VALUE_W]);
				points[i].StrawValue = strawValue;
				strawValues.Add(strawValue);
			}

			// Obtain the straw median and threshold values
			strawValues.Sort();
			double medianStrawValue = strawValues.ElementAt(strawValues.Count() / 2);

			// Default the edge points to a median straw value
			for (int i = 0; i < Params.STRAW_VALUE_W; i++)
			{
				points[i].StrawValue = medianStrawValue;
			}
			for (int i = points.Length - Params.STRAW_VALUE_W; i < points.Length; i++)
			{
				points[i].StrawValue = medianStrawValue;
			}

			return medianStrawValue;
		}

		#endregion

		#region INDIVIDUALS_STATIC_UTILITY_METHODS

		/// <summary>
		/// Return the angle (in radians) between the two given points.
		/// </summary>
		public static double GetAngle(Point pointA, Point pointB)
		{
			return Math.Atan2(pointB.Y - pointA.Y, pointB.X - pointA.X);
		}

		/// <summary>
		/// Return true if the given sub-array of points is mostly horizontal.
		/// </summary>
		public static bool IsHorizontal(Point[] points, int startIndex, int endIndex)
		{
			double deltaX = Math.Abs(points[endIndex].X - points[startIndex].X);
			double deltaY = Math.Abs(points[endIndex].Y - points[startIndex].Y);

			return deltaX > deltaY;
		}

		/// <summary>
		/// The given arguments should represent two lines of the form Ax + By = C.
		/// </summary>
		public static Point FindLineIntersection(double A1, double B1, double A2, double B2,
			double C1, double C2)
		{
			double temp = A1 * B2 - A2 * B1;

			// Lines are parallel
			if (temp == 0)
			{
				return null;
			}

			double x = (B2 * C1 - B1 * C2) / temp;
			double y = (A1 * C2 - A2 * C1) / temp;

			return new Point(x, y, -1);
		}

		/// <summary>
		/// Retrun true if the given sub-array of points is clock-wise.
		/// </summary>
		public static bool IsCW(Point[] points, int startIndex, int endIndex, Point center)
		{
			int deltaIndex = (int)((endIndex - startIndex + 1) * Params.CW_TEST_PT_2_INDEX_RATIO);
			Point vector1 = Difference(points[startIndex], center);
			Point vector2 = Difference(points[startIndex + deltaIndex], center);
			return CrossProduct(vector1, vector2) < 0;
		}

		/// <summary>
		/// Return the difference (the vector) between the two given points.
		/// </summary>
		/// <returns></returns>
		public static Point Difference(Point point2, Point point1)
		{
			return new Point(point2.X - point1.X, point2.Y - point1.Y, -1);
		}

		/// <summary>
		/// Return the z-component of the cross product of the two given points representing vectors.
		/// </summary>
		/// <returns></returns>
		public static double CrossProduct(Point point1, Point point2)
		{
			return point1.X*point2.Y - point2.X*point1.Y;
		}

		/// <summary>
		/// Return the smoothed speed value for the center Point (p2).
		/// </summary>
		public static double GetSmoothedSpeed(Point p1, Point p2, Point p3)
		{
			return GetSmoothedValue(p1.Speed, p2.Speed, p3.Speed);
		}

		/// <summary>
		/// Return the smoothed speed value for the center Point.
		/// </summary>
		public static double GetSmoothedSpeed(Point centerPoint, Point sidePoint)
		{
			return GetSmoothedValue(centerPoint.Speed, sidePoint.Speed);
		}

		/// <summary>
		/// Return the smoothed curvature value for the center Point (p2).
		/// </summary>
		public static double GetSmoothedCurvature(Point p1, Point p2, Point p3)
		{
			return GetSmoothedValue(p1.Curvature, p2.Curvature, p3.Curvature);
		}

		/// <summary>
		/// Return the smoothed curvature value for the center Point.
		/// </summary>
		public static double GetSmoothedCurvature(Point centerPoint, Point sidePoint)
		{
			return GetSmoothedValue(centerPoint.Curvature, sidePoint.Curvature);
		}

		/// <summary>
		/// Return the smoothed value for the given three values.
		/// </summary>
		private static double GetSmoothedValue(double value1, double value2, double value3)
		{
			if (Double.IsNaN(value1))
				value1 = 0.0;
			if (Double.IsNaN(value2))
				value2 = 0.0;
			if (Double.IsNaN(value3))
				value3 = 0.0;

			return
				value1 * Params.GAUSSIAN_SIDE_RATIO +
				value2 * Params.GAUSSIAN_CENTER_RATIO +
				value3 * Params.GAUSSIAN_SIDE_RATIO;
		}

		/// <summary>
		/// Return the smoothed value for the given two values.
		/// </summary>
		private static double GetSmoothedValue(double centerValue, double sideValue)
		{
			if (Double.IsNaN(centerValue))
				centerValue = 0.0;
			if (Double.IsNaN(sideValue))
				sideValue = 0.0;

			return
				centerValue * (Params.GAUSSIAN_CENTER_RATIO + Params.GAUSSIAN_SIDE_RATIO) +
				sideValue * Params.GAUSSIAN_SIDE_RATIO;
		}

		/// <summary>
		/// Return the x derivative between the two given points.
		/// </summary>
		public static double GetXDerivative(Point previousPoint, Point nextPoint)
		{
			double deltaX = nextPoint.X - previousPoint.X;
			double deltaS = nextPoint.ArcLength - previousPoint.ArcLength;
			return deltaX / deltaS;
		}

		/// <summary>
		/// Return the y derivative between the two given points.
		/// </summary>
		public static double GetYDerivative(Point previousPoint, Point nextPoint)
		{
			double deltaY = nextPoint.Y - previousPoint.Y;
			double deltaS = nextPoint.ArcLength - previousPoint.ArcLength;
			return deltaY / deltaS;
		}

		/// <summary>
		/// Return the x double derivative between the two given points.
		/// </summary>
		public static double GetXDoubleDerivative(Point previousPoint, Point nextPoint)
		{
			double deltaXDeriv = nextPoint.XFirstDeriv - previousPoint.XFirstDeriv;
			double deltaS = nextPoint.ArcLength - previousPoint.ArcLength;
			return deltaXDeriv / deltaS;
		}

		/// <summary>
		/// Return the y double derivative between the two given points.
		/// </summary>
		public static double GetYDoubleDerivative(Point previousPoint, Point nextPoint)
		{
			double deltaYDeriv = nextPoint.YFirstDeriv - previousPoint.YFirstDeriv;
			double deltaS = nextPoint.ArcLength - previousPoint.ArcLength;
			return deltaYDeriv / deltaS;
		}

		/// <summary>
		/// Return the curvature of the given point.
		/// </summary>
		public static double GetCurvature(Point p)
		{
			double curvature = (p.XFirstDeriv * p.YSecondDeriv - p.YFirstDeriv * p.XSecondDeriv) / 
				Math.Pow((p.XFirstDeriv * p.XFirstDeriv + p.YFirstDeriv * p.YFirstDeriv), 1.5);

			// It is possible for the preceeding and proceeding points of a given point to both 
			// have the same X/Y coordinates
			return Double.IsNaN(curvature) ? 0 : curvature;
		}

		/// <summary>
		/// Return the speed between the two given points.
		/// </summary>
		public static double GetSpeed(Point previousPoint, Point nextPoint)
		{
			double deltaS = nextPoint.ArcLength - previousPoint.ArcLength;
			double deltaT = nextPoint.Timestamp - previousPoint.Timestamp;
			return deltaS / deltaT;
		}

		/// <summary>
		/// Return the line between the two given points.
		/// </summary>
		public static void SetLineState(Point pointA, Point pointB, bool showSpeedCurv, 
			bool isErasingStroke)
		{
			Color lineColor;
			double lineThickness;

			if (!isErasingStroke)
			{
				if (!Double.IsNaN(pointA.Curvature))
				{
					lineColor = CalculateColor(pointA.Curvature);
					lineThickness = CalculateThickness(pointA.Speed);
				}
				else
				{
					lineColor = Params.DEFAULT_STROKE_COLOR;
					lineThickness = Params.DEFAULT_STROKE_THICKNESS;
				}
			}
			else
			{
				lineColor = Params.ERASE_STROKE_COLOR;
				lineThickness = Params.ERASE_STROKE_THICKNESS;
			}

			pointA.SetLineState(lineColor, lineThickness, pointA.X, pointA.Y, pointB.X, pointB.Y,
				showSpeedCurv, isErasingStroke);
		}

		/// <summary>
		/// Return the color which corresponds to the given curvature.  The given curvature should 
		/// be between -PI and PI;
		/// </summary>
		private static Color CalculateColor(double curvature)
		{
			byte R, G, B;

			if (curvature > 0)
			{
				R = Math.Min((byte)(curvature * Params.CURVATURE_COLOR_STRENGTH), (byte)255);
				G = (byte)(R / 2);
				B = (byte)(G / 3);
			}
			else
			{
				G = Math.Min((byte)(-curvature * Params.CURVATURE_COLOR_STRENGTH), (byte)255);
				B = (byte)(G / 2);
				R = (byte)(B / 3);
			}

			return Color.FromArgb(255, R, G, B);
		}

		/// <summary>
		/// Return the thickness which corresponds to the given speed.
		/// </summary>
		private static double CalculateThickness(double speed)
		{
			double ratioOfSpeedScale = 1 - (speed - Params.MAX_WIDTH_SPEED) / 
				(Params.MIN_WIDTH_SPEED - Params.MAX_WIDTH_SPEED);
			double widthFromRatio = Params.MIN_WIDTH + (Params.MAX_WIDTH - Params.MIN_WIDTH) * 
				ratioOfSpeedScale;

			if (widthFromRatio > Params.MAX_WIDTH)
			{
				widthFromRatio = Params.MAX_WIDTH;
			}
			else if (widthFromRatio < Params.MIN_WIDTH)
			{
				widthFromRatio = Params.MIN_WIDTH;
			}

			return widthFromRatio;
		}

		///<summary>
		///Return the distance between two points.
		///</summary>
		public static double GetDistance(Point pointA, Point pointB)
		{
			double deltaX = pointA.X - pointB.X;
			double deltaY = pointA.Y - pointB.Y;
			return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
		}

		/// <summary>
		/// Return the point of intersection between the line SEGMENT from point A to point B and 
		/// the line SEGMENT from point C to point D if the two segments do indeed intersect, else 
		/// return null.  It IS considered an intersection if the end point of one of the segments 
		/// lies exactly along the other segment; unless the two lines are colinear, which is 
		/// considered NOT an intersection.
		/// </summary>
		public static Point GetPointOfIntersection(Point A, Point B, Point C, Point D)
		{
			Point pointOfIntersection = new Point(Double.NaN, Double.NaN, Double.NaN);

			GetPointOfIntersectionAndHandleCollinearCase(A, B, C, D, pointOfIntersection);

			if (!Double.IsNaN(pointOfIntersection.X))
			{
				return pointOfIntersection;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Assign the fields of pointOfIntersection as the point of intersection between the line 
		/// SEGMENT from point A to point B and the line SEGMENT from point C to point D if the 
		/// two segments do indeed intersect, else assign null.  It IS considered an intersection 
		/// if the end point of one of the segments lies exactly along the other segment.
		/// 
		/// Return true if the two line segments represented by the given points are collinear and 
		/// point in the same direction, else return false.
		/// </summary>
		private static bool GetPointOfIntersectionAndHandleCollinearCase(Point A, Point B, Point C,
			Point D, Point pointOfIntersection)
		{
			// First, check whether the bounding boxes intersect
			if (CheckForBoundingBoxIntersection(A, B, C, D))
			{
				// Then, calculate where the actual segments intersect

				double denominator = (D.Y - C.Y) *  (B.X - A.X) - (D.X - C.X) *  (B.Y - A.Y);

				// Check whether the two line segments are not parallel
				if (denominator != 0)
				{
					// Calculate how far up the first line segment the point of intersection is
					double u = ((D.X - C.X) *  (A.Y - C.Y) - (D.Y - C.Y) *  (A.X - C.X)) /
								denominator;

					// Check whether the point of "intersection" is actually within the bounds of 
					// the line segment
					if (u >= 0 && u <= 1)
					{
						// Calculate the components of the point of intersection
						double x = A.X + u * (B.X - A.X);
						double y = A.Y + u * (B.Y - A.Y);

						// Assign the Point object's fields
						pointOfIntersection.X = x;
						pointOfIntersection.Y = y;
					}
				}
				else
				{
					double parallelity2 = (C.Y - A.Y) *  (D.X - B.X) - (C.X - A.X) *  (D.Y - B.Y);
					double parallelity3 = (D.Y - A.Y) *  (C.X - B.X) - (D.X - A.X) *  (C.Y - B.Y);

					// Test for collinearity
					if (parallelity2 == 0 && parallelity3 == 0)
					{
						// Test for the same direction (slope)
						if ((A.Y - B.Y) / (A.X - B.X) == (C.Y - D.Y) / (C.X - D.X))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Return true if the bounding box for the line segment from point A to point B 
		/// intersects the bounding box for the line segment from point C to point D.
		/// </summary>
		public static bool CheckForBoundingBoxIntersection(Point A, Point B, Point C, Point D)
		{
			double
				segment1MinX, segment1MaxX, segment1MinY, segment1MaxY,
				segment2MinX, segment2MaxX, segment2MinY, segment2MaxY;

			// Get the bounding box dimensions
			segment1MinX = Math.Min(A.X, B.X);
			segment1MaxX = Math.Max(A.X, B.X);
			segment1MinY = Math.Min(A.Y, B.Y);
			segment1MaxY = Math.Max(A.Y, B.Y);
			segment2MinX = Math.Min(C.X, D.X);
			segment2MaxX = Math.Max(C.X, D.X);
			segment2MinY = Math.Min(C.Y, D.Y);
			segment2MaxY = Math.Max(C.Y, D.Y);

			// Check for bounding box intersection
			return CheckForBoundingBoxIntersection(
				segment1MinX, segment1MaxX, segment1MinY, segment1MaxY,
				segment2MinX, segment2MaxX, segment2MinY, segment2MaxY);
		}

		/// <summary>
		/// Return true if the bounding box for the line segment from point A to point B 
		/// intersects the given bounding box.
		/// </summary>
		public static bool CheckForBoundingBoxIntersection(Rect box1, Point A, Point B)
		{
			double segmentMinX, segmentMaxX, segmentMinY, segmentMaxY;

			// Get the line segment's bounding box dimensions
			segmentMinX = Math.Min(A.X, B.X);
			segmentMaxX = Math.Max(A.X, B.X);
			segmentMinY = Math.Min(A.Y, B.Y);
			segmentMaxY = Math.Max(A.Y, B.Y);

			// Check for bounding box intersection
			return CheckForBoundingBoxIntersection(
				box1.X, box1.X + box1.Width, box1.Y, box1.Y + box1.Height,
				segmentMinX, segmentMaxX, segmentMinY, segmentMaxY);
		}

		/// <summary>
		/// Return true if the bounding boxes specified by the given min and max components 
		/// intersect.  It IS considered an intersection if one side of one of the rectangles lies 
		/// exactly along the side of the other.
		/// </summary>
		public static bool CheckForBoundingBoxIntersection(
			double box1MinX, double box1MaxX, double box1MinY, double box1MaxY,
			double box2MinX, double box2MaxX, double box2MinY, double box2MaxY)
		{
			// Check for bounding box intersection
			if (box1MaxX >= box2MinX && box1MaxY >= box2MinY &&
				box2MaxX >= box1MinX && box2MaxY >= box1MinY)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public static double GetSlope(double x1, double y1, double x2, double y2)
		{
			return (y2 - y1) / (x2 - x1);
		}

		public static double GetYIntercept(double x1, double y1, double x2, double y2)
		{
			double slope = GetSlope(x1, y1, x2, y2);
			return y1 - slope * x1;
		}

		#endregion

	}
}
