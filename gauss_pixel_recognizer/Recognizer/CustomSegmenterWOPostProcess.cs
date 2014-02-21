/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StrokeCollector
{
	class CustomSegmenterWOPostProcess
	{
		/// <summary>
		/// Based on a hybrid from short straw and speed seg techniques.  Adds straw-based 
		/// feature points to the initial list of feature points, then uses both the speed seg and 
		/// short straw techniques for removing feature points during post processing.
		/// </summary>
		public static FeaturePoint[] Segment(DrawablePoint[] points, double shortStrawThreshold)
		{
			// The first and last points are by definition feature points
			points[0].IsCustomSegWOPostProcFeaturePoint = true;
			points[points.Length - 1].IsCustomSegWOPostProcFeaturePoint = true;

			double avgPenSpeed = points[points.Length - 1].ArcLength /
				(points[points.Length - 1].Timestamp - points[0].Timestamp);

			// Add feature points
			SegmentViaShortStraw(points, shortStrawThreshold, avgPenSpeed);
			SegmentViaSpeed(points, shortStrawThreshold, avgPenSpeed);
			SegmentViaCurvature(points, avgPenSpeed);

			int featurePointCount = CountFeaturePoints(points);

			// Create the FeaturePoint objects for drawing to the canvas
			FeaturePoint[] featurePoints = new FeaturePoint[featurePointCount];
			FeaturePoint featurePoint;
			for (int i = 0, j = 0; i < points.Length; i++)
			{
				if (points[i].IsCustomSegWOPostProcFeaturePoint)
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
		public static Segment[] FitSegments(DrawablePoint[] points, int featurePointCount)
		{
			int indexA, indexB = 0, j = 0;
			Segment[] segments = new Segment[featurePointCount - 1];

			for (int i = indexB + 1; i < points.Length; i++)
			{
				if (points[i].IsCustomSegWOPostProcFeaturePoint)
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
		private static Segment FitASegment(DrawablePoint[] points, int startIndex, int endIndex)
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
		/// Find segment points according to local straw value minima.
		/// 
		/// ***SAME AS SHORT STRAW*
		/// </summary>
		private static void SegmentViaShortStraw(DrawablePoint[] points, double shortStrawThreshold,
			double avgPenSpeed)
		{
			double penSpeedThreshold = avgPenSpeed * Params.CUSTOM_SEG_P_SSST;

			// Determine which points are feature points
			for (int i = 1; i < points.Length - 1; i++)
			{
				if (points[i].StrawValue < points[i - 1].StrawValue &&
					points[i].StrawValue < points[i + 1].StrawValue &&
					points[i].StrawValue < shortStrawThreshold &&
					points[i].Speed < penSpeedThreshold)
				{
					points[i].IsCustomSegWOPostProcFeaturePoint = true;
				}
			}
		}

		/// <summary>
		/// Find segment points according to local speed minima.
		/// 
		/// **SAME AS SPEED SEG**
		/// </summary>
		private static void SegmentViaSpeed(DrawablePoint[] points, double shortStrawThreshold,
			double avgPenSpeed)
		{
			double penSpeedThreshold = avgPenSpeed * Params.CUSTOM_SEG_P_ST;

			for (int i = 1; i < points.Length - 1; i++)
			{
				if (points[i].Speed < points[i - 1].Speed &&
					points[i].Speed < points[i + 1].Speed &&
					points[i].Speed < penSpeedThreshold &&
					Math.Abs(points[i].Curvature) > Params.CUSTOM_SEG_P_SCT)
				{
					points[i].IsCustomSegWOPostProcFeaturePoint = true;
				}
			}
		}

		/// <summary>
		/// Find segment points according to local curvature maxima.
		/// 
		/// **BASED ON SPEED SEG**
		/// Differs in that this accounts for the sign of the curvature; i.e., if a given point's 
		/// curvature is negative, then it checks if it is at a local minima of curvature rather 
		/// than at a local maxima.  On the same note, the absolute value of curvature is checked 
		/// against the curvature threshold P_CT.
		/// </summary>
		private static void SegmentViaCurvature(DrawablePoint[] points, double avgPenSpeed)
		{
			double penSpeedThreshold = avgPenSpeed * Params.CUSTOM_SEG_P_CST;

			for (int i = 1; i < points.Length - 1; i++)
			{
				if (((points[i].Curvature > 0 &&
							(points[i].Curvature > points[i - 1].Curvature &&
							points[i].Curvature > points[i + 1].Curvature)) ||
						(points[i].Curvature < 0 &&
							(points[i].Curvature < points[i - 1].Curvature &&
							points[i].Curvature < points[i + 1].Curvature))) &&
					((points[i].Speed < penSpeedThreshold &&
							Math.Abs(points[i].Curvature) > Params.CUSTOM_SEG_P_CT) ||
						Math.Abs(points[i].Curvature) > Params.CUSTOM_SEG_P_VETO_CT))
				{
					points[i].IsCustomSegWOPostProcFeaturePoint = true;
				}
			}
		}

		private static int CountFeaturePoints(DrawablePoint[] points)
		{
			int featurePointCount = 0;

			for (int i = 0; i < points.Length; i++)
			{
				if (points[i].IsCustomSegWOPostProcFeaturePoint)
				{
					featurePointCount++;
				}
			}

			return featurePointCount;
		}
	}
}
