/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StrokeCollector
{
	class SpeedSegSegmenter
	{
		public static FeaturePoint[] Segment(Point[] points)
		{
			// The first and last points are by definition feature points
			points[0].IsSpeedSegFeaturePoint = true;
			points[points.Length - 1].IsSpeedSegFeaturePoint = true;

			double avgPenSpeed = points[points.Length - 1].ArcLength /
				(points[points.Length - 1].Timestamp - points[0].Timestamp);

			SegmentViaSpeed(points, avgPenSpeed);
			SegmentViaCurvature(points, avgPenSpeed);

			int featurePointCount = PostProcess(points);

			// Create the FeaturePoint objects for drawing to the canvas
			FeaturePoint[] featurePoints = new FeaturePoint[featurePointCount];
			FeaturePoint featurePoint;
			for (int i = 0, j = 0; i < points.Length; i++)
			{
				if (points[i].IsSpeedSegFeaturePoint)
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
			int indexA, indexB = -1, j = 0;
			Segment[] segments = new Segment[featurePointCount - 1];

			// Find the first segmentation point (can be > 0 from post-processing)
			for (int i = 0; i < points.Length; i++)
			{
				if (points[i].IsSpeedSegFeaturePoint)
				{
					indexB = i;
					break;
				}
			}
			for (int i = indexB + 1; i < points.Length; i++)
			{
				if (points[i].IsSpeedSegFeaturePoint)
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
		/// Find segment points according to local speed minima.
		/// </summary>
		private static void SegmentViaSpeed(Point[] points, double avgPenSpeed)
		{
			double penSpeedThreshold = avgPenSpeed * Params.SPEED_SEG_P_ST;

			for (int i = 1; i < points.Length - 1; i++)
			{
				if (points[i].Speed < points[i - 1].Speed &&
					points[i].Speed < points[i + 1].Speed &&
					points[i].Speed < penSpeedThreshold)
				{
					points[i].IsSpeedSegFeaturePoint = true;
				}
			}
		}

		/// <summary>
		/// Find segment points according to local curvature maxima.
		/// </summary>
		private static void SegmentViaCurvature(Point[] points, double avgPenSpeed)
		{
			double penSpeedThreshold = avgPenSpeed * Params.SPEED_SEG_P_CST;

			for (int i = 1; i < points.Length - 1; i++)
			{
				if (Math.Abs(points[i].Curvature) > Math.Abs(points[i - 1].Curvature) &&
					Math.Abs(points[i].Curvature) > Math.Abs(points[i + 1].Curvature) &&
					points[i].Speed < penSpeedThreshold &&
					Math.Abs(points[i].Curvature) > Params.SPEED_SEG_P_CT)
				{
					points[i].IsSpeedSegFeaturePoint = true;
				}
			}
		}

		/// <summary>
		/// Remove feature points which are close to each other.
		/// </summary>
		private static int PostProcess(Point[] points)
		{
			int featurePointCount = 1;

			int indexA = 0, indexB;

			// Remove close feature points
			for (int i = indexA + 1; i < points.Length; i++)
			{
				if (points[i].IsSpeedSegFeaturePoint)
				{
					indexB = i;

					if (indexB - indexA <= Params.SPEED_SEG_P_PMR)
					{
						points[indexA].IsSpeedSegFeaturePoint = false;
					}
					else
					{
						featurePointCount++;
					}

					indexA = indexB;
				}
			}

			return featurePointCount;
		}
	}
}
