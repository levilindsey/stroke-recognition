/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StrokeCollector
{
	class ShortStrawSegmenter
	{
		public static FeaturePoint[] Segment(Point[] points,
			double shortStrawThreshold)
		{
			BottomUp(points, shortStrawThreshold);
			int featurePointCount = TopDown(points);

			// Create the FeaturePoint objects for drawing to the canvas
			FeaturePoint[] featurePoints = new FeaturePoint[featurePointCount];
			FeaturePoint featurePoint;
			for (int i = 0, j = 0; i < points.Length; i++)
			{
				if (points[i].IsShortStrawFeaturePoint)
				{
					featurePoint = new FeaturePoint(points[i].X, points[i].Y, i);
					featurePoints[j++] = featurePoint;
				}
			}

			return featurePoints;
		}

		/// <summary>
		/// Fit Segments to the given array of points according to the feature points.
		/// </summary>
		public static Segment[] FitSegments(Point[] points, int featurePointCount)
		{
			int indexA, indexB = 0, j = 0;
			Segment[] segments = new Segment[featurePointCount - 1];

			for (int i = indexB + 1; i < points.Length; i++)
			{
				if (points[i].IsShortStrawFeaturePoint)
				{
					indexA = indexB;
					indexB = i;
					segments[j++] = FitASegment(points, indexA, indexB);
				}
			}

			return segments;
		}

		/// <summary>
		/// Fit a segment to the given sub-array of points.
		/// </summary>
		private static Segment FitASegment(Point[] points, int startIndex, int endIndex)
		{
			ArcSegment arcSegment = StrokeSegmentation.FitACircle(points, startIndex, endIndex);
			LineSegment lineSegment = StrokeSegmentation.FitALine(points, startIndex, endIndex);

			if (arcSegment.ErrorOfFit < lineSegment.ErrorOfFit &&
				arcSegment.AngleSpread >= Params.SPEED_SEG_CIRCLE_FIT_ANGLE_THRES)
				return arcSegment;
			else
				return lineSegment;
		}

		/// <summary>
		/// Add feature points according to straw values.
		/// </summary>
		private static void BottomUp(Point[] points, double shortStrawThreshold)
		{
			// The first and last points are by definition feature points
			points[0].IsShortStrawFeaturePoint = true;
			points[points.Length - 1].IsShortStrawFeaturePoint = true;

			// Determine which points are feature points
			for (int i = 1; i < points.Length - 1; i++)
			{
				if (points[i].StrawValue < points[i - 1].StrawValue &&
					points[i].StrawValue < points[i + 1].StrawValue &&
					points[i].StrawValue < shortStrawThreshold)
				{
					points[i].IsShortStrawFeaturePoint = true;
				}
			}
		}

		/// <summary>
		/// Add feature points to non-line segments and remove them from collinear segments.
		/// </summary>
		private static int TopDown(Point[] points)
		{
			AddPointsToFormLines(points, 0, points.Length - 1);
			return RemovePointsFromCollinearTriplets(points);
		}

		/// <summary>
		/// Add corner points to the given segment until all sub-segments are lines.
		/// </summary>
		private static void AddPointsToFormLines(Point[] points, int startIndex, int endIndex)
		{
			if (endIndex - startIndex >= Params.SHORT_STRAW_ADD_LINE_STOP_DISTANCE)
			{
				int indexA = startIndex;
				int indexB = indexA;
				int halfWayCornerIndex;

				for (int i = startIndex + 1; i <= endIndex; i++)
				{
					if (points[i].IsShortStrawFeaturePoint)
					{
						indexB = i;

						if (!IsLine(points[indexA], points[indexB]))
						{
							halfWayCornerIndex = HalfWayCorner(points, indexA, indexB);
							AddPointsToFormLines(points, indexA, halfWayCornerIndex);
							AddPointsToFormLines(points, halfWayCornerIndex, indexB);
						}

						indexA = indexB;
					}
				}
			}
		}

		/// <summary>
		/// Remove the center point from any point triplet sequence in which the three points are 
		/// collinear.
		/// </summary>
		private static int RemovePointsFromCollinearTriplets(Point[] points)
		{
			int featurePointCount = 2;

			int indexA = 0;
			int indexB = 0;
			int indexC = -1;

			// Find the second corner
			for (int i = 1; i < points.Length - 1; i++)
			{
				if (points[i].IsShortStrawFeaturePoint)
				{
					indexC = i;
					break;
				}
			}

			// Test whether the stroke has corner points only at the very ends
			if (indexC < 0)
				return featurePointCount;

			// Loop through and remove corners
			for (int i = indexC + 1; i < points.Length; i++)
			{
				if (points[i].IsShortStrawFeaturePoint)
				{
					indexB = indexC;
					indexC = i;

					if (IsLine(points[indexA], points[indexC]))
					{
						points[indexB].IsShortStrawFeaturePoint = false;
					}
					else
					{
						indexA = indexB;
						featurePointCount++;
					}
				}
			}

			return featurePointCount;
		}

		/// <summary>
		/// Return true if the segment between the two given points is a line.
		/// </summary>
		private static bool IsLine(Point a, Point b)
		{
			double distance = StrokePreProcessing.GetDistance(a, b);
			double pathDistance = Math.Abs(a.ArcLength - b.ArcLength);
			return distance / pathDistance > Params.SHORT_STRAW_IS_LINE_THRESHOLD;
		}

		/// <summary>
		/// Find the "half-way corner" to use for the given segment, mark that point as being a 
		/// short straw feature point, and return its index.
		/// </summary>
		private static int HalfWayCorner(Point[] points, int startIndex, int endIndex)
		{
			int quarter = (endIndex - startIndex) / 4;

			int minIndex = startIndex + quarter;
			double minValue = points[startIndex + quarter].StrawValue;

			for (int i = startIndex + quarter + 1; i <= endIndex - quarter; i++)
			{
				if (points[i].StrawValue < minValue)
				{
					minValue = points[i].StrawValue;
					minIndex = i;
				}
			}

			points[minIndex].IsShortStrawFeaturePoint = true;
			return minIndex;
		}
	}
}
