﻿/**
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
		public static double GetAvgResampleError(DrawablePoint[] resampledPoints, int n, double pathLength)
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
		public static DrawablePoint[] Resample(DrawablePoint[] oldPoints, int n)
		{
			double I = GetPathLength(oldPoints) / (n - 1);
			double D = 0.0;

			List<DrawablePoint> newPoints = new List<DrawablePoint>();

			DrawablePoint oldPoint, newPoint;
			double d, ratio, qX, qY, qT;

			int i = 0;

			// Handle the fencepost problem (in regards to the old Points indexing)
			oldPoint = oldPoints[i++];
			newPoint = new DrawablePoint(oldPoint);
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
					newPoint = new DrawablePoint(qX, qY, qT);

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
				newPoint = new DrawablePoint(oldPoints[oldPoints.Length - 1]);
				newPoints.Add(newPoint);
			}

			return newPoints.ToArray();
		}

		/// <summary>
		/// Return the overall path length of the given array of Points.
		/// </summary>
		public static double GetPathLength(DrawablePoint[] points)
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
		public static RecognizerPoint[] RemoveDuplicatePoints(RecognizerPoint[] points)
		{
			List<RecognizerPoint> cleanPoints = new List<RecognizerPoint>();

			RecognizerPoint previousPoint = points[0];
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
		/// Remove any consecutive Points with identical X/Y coordinates from the given Points and 
		/// return the resulting collection.
		/// </summary>
		public static DrawablePoint[] RemoveDuplicatePoints(DrawablePoint[] points)
		{
			List<DrawablePoint> cleanPoints = new List<DrawablePoint>();

			DrawablePoint previousPoint = points[0];
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

		public static double GetSmoothedValue(double value1, double value2, double value3)
		{
			if (Double.IsNaN(value1))
				value1 = 0.0;
			if (Double.IsNaN(value2))
				value2 = 0.0;
			if (Double.IsNaN(value3))
				value3 = 0.0;

			return
				value1 * Params.GAUSSIAN_3_1D_1_RATIO +
				value2 * Params.GAUSSIAN_3_1D_0_RATIO +
				value3 * Params.GAUSSIAN_3_1D_1_RATIO;
		}

		public static double GetSmoothedValue(double centerValue, double sideValue)
		{
			if (Double.IsNaN(centerValue))
				centerValue = 0.0;
			if (Double.IsNaN(sideValue))
				sideValue = 0.0;

			return
				centerValue * Params.GAUSSIAN_3_1D_1_RATIO +
				sideValue * Params.GAUSSIAN_3_1D_SIDE_PLUS_CENTER_RATIO;
		}

		/// <summary>
		/// Calculate and save smoothed angle values for each of the given Points.
		/// </summary>
		public static void SmoothAngles(RecognizerPoint[] points, int smoothingIterationCount)
		{
			double[] smoothedAngles;

			for (int j = 0; j < smoothingIterationCount; j++)
			{
				smoothedAngles = CalculateSmoothedAngles(points);
				ApplySmoothedAngles(smoothedAngles, points);
			}
		}

		private static double[] CalculateSmoothedAngles(RecognizerPoint[] points)
		{
			double[] smoothedAngles = new double[points.Length];

			smoothedAngles[0] = GetSmoothedAngle(points[1], points[0]);

			for (int i = 1; i < points.Length - 1; i++)
			{
				smoothedAngles[i] = GetSmoothedAngle(points[i - 1], points[i], points[i + 1]);
			}

			smoothedAngles[points.Length - 1] = GetSmoothedAngle(points[points.Length - 2], points[points.Length - 1]);

			return smoothedAngles;
		}

		private static void ApplySmoothedAngles(double[] smoothedAngles, RecognizerPoint[] points)
		{
			for (int i = 0; i < points.Length; i++)
			{
				points[i].Angle = smoothedAngles[i];
			}
		}

		/// <summary>
		/// Calculate and save the angles (in radians) for each of the given Points.
		/// </summary>
		public static void CalculatePointAngles(RecognizerPoint[] points)
		{
			points[0].Angle = GetAngle(points[0], points[1], true);

			for (int i = 1; i < points.Length - 1; i++)
			{
				points[i].Angle = GetAngle(points[i - 1], points[i + 1], true);
			}

			points[points.Length - 1].Angle = GetAngle(points[points.Length - 2], points[points.Length - 1], true);
		}

		/// <summary>
		/// Calculate and save the line segment for each of the given Points.
		/// </summary>
		public static void CalculatePointLines(DrawablePoint[] points)
		{
			for (int i = 0; i < points.Length - 1; i++)
			{
				points[i].SetLineState(points[i], points[i + 1], false);
			}

			points[points.Length - 1].NullLine();
		}

		#endregion

		#region INDIVIDUALS_STATIC_UTILITY_METHODS

		/// <summary>
		/// Return true if the given sub-array of points is mostly horizontal.
		/// </summary>
		public static bool IsHorizontal(DrawablePoint[] points, int startIndex, int endIndex)
		{
			double deltaX = Math.Abs(points[endIndex].X - points[startIndex].X);
			double deltaY = Math.Abs(points[endIndex].Y - points[startIndex].Y);

			return deltaX > deltaY;
		}

		/// <summary>
		/// The given arguments should represent two lines of the form Ax + By = C.
		/// </summary>
		public static DrawablePoint FindLineIntersection(double A1, double B1, double A2, double B2,
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

			return new DrawablePoint(x, y, -1);
		}

		/// <summary>
		/// Retrun true if the given sub-array of points is clock-wise.
		/// </summary>
		public static bool IsCW(DrawablePoint[] points, int startIndex, int endIndex, DrawablePoint center)
		{
			int deltaIndex = (int)((endIndex - startIndex + 1) * Params.CW_TEST_PT_2_INDEX_RATIO);
			DrawablePoint vector1 = Difference(points[startIndex], center);
			DrawablePoint vector2 = Difference(points[startIndex + deltaIndex], center);
			return CrossProduct(vector1, vector2) < 0;
		}

		/// <summary>
		/// Return the difference (the vector) between the two given points.
		/// </summary>
		/// <returns></returns>
		public static DrawablePoint Difference(DrawablePoint point2, DrawablePoint point1)
		{
			return new DrawablePoint(point2.X - point1.X, point2.Y - point1.Y, -1);
		}

		/// <summary>
		/// Return the z-component of the cross product of the two given points representing vectors.
		/// </summary>
		/// <returns></returns>
		public static double CrossProduct(DrawablePoint point1, DrawablePoint point2)
		{
			return point1.X*point2.Y - point2.X*point1.Y;
		}

		/// <summary>
		/// Return the smoothed speed value for the center Point (p2).
		/// </summary>
		public static double GetSmoothedAngle(RecognizerPoint p1, RecognizerPoint p2, RecognizerPoint p3)
		{
			double angle1 = p1.Angle;
			double angle2 = p2.Angle;
			double angle3 = p3.Angle;

			if (Double.IsNaN(angle1))
				angle1 = 0.0;
			if (Double.IsNaN(angle2))
				angle2 = 0.0;
			if (Double.IsNaN(angle3))
				angle3 = 0.0;

			double angleAvg12 = GetAngleAverage(angle1, angle2, Params.GAUSSIAN_3_1D_1_RATIO);

			return GetAngleAverage(angle3, angleAvg12, Params.GAUSSIAN_3_1D_1_RATIO);
		}

		/// <summary>
		/// Return the smoothed speed value for the center Point.
		/// </summary>
		public static double GetSmoothedAngle(RecognizerPoint centerPoint, RecognizerPoint sidePoint)
		{
			double centerAngle = centerPoint.Angle;
			double sideAngle = sidePoint.Angle;

			if (Double.IsNaN(centerAngle))
				centerAngle = 0.0;
			if (Double.IsNaN(sideAngle))
				sideAngle = 0.0;

			return GetAngleAverage(centerAngle, sideAngle, Params.GAUSSIAN_3_1D_1_RATIO);
		}

		/// <summary>
		/// Return the angle (in radians) between the two given points.
		/// </summary>
		public static double GetAngle(RecognizerPoint previousPoint, 
			RecognizerPoint nextPoint, bool enforcePositivity)
		{
			double deltaX = nextPoint.X - previousPoint.X;
			double deltaY = nextPoint.Y - previousPoint.Y;
			double angle = Math.Atan2(deltaY, deltaX);
			if (enforcePositivity && angle < 0)
			{
				angle += Math.PI;
			}
			return angle;
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
		public static double GetDistance(DrawablePoint pointA, DrawablePoint pointB)
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
		public static DrawablePoint GetPointOfIntersection(DrawablePoint A, DrawablePoint B, DrawablePoint C, DrawablePoint D)
		{
			DrawablePoint pointOfIntersection = new DrawablePoint(Double.NaN, Double.NaN, Double.NaN);

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
		private static bool GetPointOfIntersectionAndHandleCollinearCase(DrawablePoint A, DrawablePoint B, DrawablePoint C,
			DrawablePoint D, DrawablePoint pointOfIntersection)
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
		public static bool CheckForBoundingBoxIntersection(DrawablePoint A, DrawablePoint B, DrawablePoint C, DrawablePoint D)
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
		public static bool CheckForBoundingBoxIntersection(Rect box1, DrawablePoint A, DrawablePoint B)
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

		public static double GetAngleSpread(double startAngle, double endAngle, bool isCW)
		{
			// Sanitize the angles against the possibility of zero-angle wrap-around
			startAngle = startAngle + Params.TWO_PI;
			endAngle = endAngle + Params.TWO_PI;
			if (isCW)
			{
				if (endAngle > startAngle)
				{
					startAngle += Params.TWO_PI;
					double tempAngle = startAngle;
					startAngle = endAngle;
					endAngle = tempAngle;
				}
				else	//endAngle < startAngle
				{
					double tempAngle = startAngle;
					startAngle = endAngle;
					endAngle = tempAngle;
				}
			}
			else	// !isCW
			{
				if (endAngle > startAngle)
				{
					// Do nothing
				}
				else	// endAngle < startAngle
				{
					endAngle += Params.TWO_PI;
				}
			}
			return endAngle - startAngle;
		}

		public static double GetAngleSpread(double angle1, double angle2)
		{
			while (angle1 < 0)
			{
				angle1 += Params.TWO_PI;
			}
			while (angle2 < 0)
			{
				angle2 += Params.TWO_PI;
			}
			while (angle1 >= Params.TWO_PI)
			{
				angle1 -= Params.TWO_PI;
			}
			while (angle2 >= Params.TWO_PI)
			{
				angle2 -= Params.TWO_PI;
			}

			return
				Math.Min(
					Math.Min(
						Math.Abs(angle1 - angle2),
						Math.Abs(angle1 - (angle2 - Params.TWO_PI))),
					Math.Abs(angle2 - (angle1 - Params.TWO_PI)));
		}

		public static double GetAngleOrOppositeSpread(double angle1, double angle2)
		{
			return Math.Min(
				GetAngleSpread(angle1, angle2),
				GetAngleSpread(angle1 + Math.PI, angle2));
		}

		/// <summary>
		/// Return the weighted average of the two given angles with the single
		/// given weight; this weight should be less than one and determines 
		/// how much closer the resulting weighted average will lie to the 
		/// first angle.
		/// </summary>
		public static double GetAngleAverage(double angle1, double angle2, 
			double weight1)
		{
			double weight2 = 1 - weight1;

			// Ensure angles are between 0 and 2PI
			while (angle1 < 0)
			{
				angle1 += Params.TWO_PI;
			}
			while (angle2 < 0)
			{
				angle2 += Params.TWO_PI;
			}
			while (angle1 >= Params.TWO_PI)
			{
				angle1 -= Params.TWO_PI;
			}
			while (angle2 >= Params.TWO_PI)
			{
				angle2 -= Params.TWO_PI;
			}

			double spread, weightedAvg;

			// Find the weighted average of the angles
			if (angle1 > angle2)
			{
				if (angle1 > Params.THREE_HALVES_PI &&
					angle2 < Params.HALF_PI)
				{
					spread = angle2 + Params.TWO_PI - angle1;
					weightedAvg = angle1 + spread * weight2;
				}
				else
				{
					spread = angle1 - angle2;
					weightedAvg = angle2 + spread * weight1;
				}
			}
			else
			{
				if (angle2 > Params.THREE_HALVES_PI &&
					angle1 < Params.HALF_PI)
				{
					spread = angle1 + Params.TWO_PI - angle2;
					weightedAvg = angle2 + spread * weight1;
				}
				else
				{
					spread = angle2 - angle1;
					weightedAvg = angle1 + spread * weight2;
				}
			}

			// Ensure the angle is between 0 and 2PI
			if (weightedAvg > Params.TWO_PI)
			{
				weightedAvg -= Params.TWO_PI;
			}

			return weightedAvg;
		}

		#endregion

	}
}
