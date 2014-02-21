/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StrokeCollector
{
	class CustomSegmenter
	{
		/// <summary>
		/// Based on a hybrid from short straw and speed seg techniques.  Adds straw-based 
		/// feature points to the initial list of feature points, then uses both the speed seg and 
		/// short straw techniques for removing feature points during post processing.
		/// </summary>
		public static FeaturePoint[] Segment(Point[] points, double shortStrawThreshold)
		{
			// The first and last points are by definition feature points
			points[0].IsCustomSegFeaturePoint = true;
			points[points.Length - 1].IsCustomSegFeaturePoint = true;

			double avgPenSpeed = points[points.Length - 1].ArcLength /
				(points[points.Length - 1].Timestamp - points[0].Timestamp);

			// Add feature points
			SegmentViaShortStraw(points, shortStrawThreshold, avgPenSpeed);
			SegmentViaSpeed(points, shortStrawThreshold, avgPenSpeed);
			SegmentViaCurvature(points, avgPenSpeed);

			// Remove feature points
			int featurePointCount = PostProcess(points);

			// Create the FeaturePoint objects for drawing to the canvas
			FeaturePoint[] featurePoints = new FeaturePoint[featurePointCount];
			FeaturePoint featurePoint;
			for (int i = 0, j = 0; i < points.Length; i++)
			{
				if (points[i].IsCustomSegFeaturePoint)
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
				if (points[i].IsCustomSegFeaturePoint)
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
		/// Find segment points according to local straw value minima.
		/// 
		/// ***SAME AS SHORT STRAW*
		/// </summary>
		private static void SegmentViaShortStraw(Point[] points, double shortStrawThreshold,
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
					points[i].IsCustomSegFeaturePoint = true;
				}
			}
		}

		/// <summary>
		/// Find segment points according to local speed minima.
		/// 
		/// **SAME AS SPEED SEG**
		/// </summary>
		private static void SegmentViaSpeed(Point[] points, double shortStrawThreshold, 
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
					points[i].IsCustomSegFeaturePoint = true;
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
		private static void SegmentViaCurvature(Point[] points, double avgPenSpeed)
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
					points[i].IsCustomSegFeaturePoint = true;
				}
			}
		}

		/// <summary>
		/// Combines post processing from both short straw and speed seg for feature point removal.
		/// </summary>
		private static int PostProcess(Point[] points)
		{
			RemovePointsFromCollinearTriplets(points);
			//return RemoveCloseArcLengthPoints(points);
			return RemoveCloseIndexPoints(points);
		}

		/// <summary>
		/// Remove feature points which have indices close to each other.
		/// 
		/// **BASED ON SPEED SEG**
		/// Differs in that when a sequence of close feature points are found, only the point at 
		/// the AVERAGE of the indices of the points in the sequence will be a feature point 
		/// afterward.  The first and last feature points are an excpetion to this and are never 
		/// removed.
		/// </summary>
		private static int RemoveCloseIndexPoints(Point[] points)
		{
			int featurePointCount = 2;
			int seqStartIndex = 0;
			int seqEndIndex = points.Length - 1;

			// Remove feature points which are close to either end of the stroke, but are not 
			// actually the end points
			for (int i = 1; i < points.Length - 1; i++)
			{
				if (points[i].IsCustomSegFeaturePoint)
				{
					if (i - seqStartIndex <= Params.CUSTOM_SEG_P_INDEX_PMR)
					{
						points[i].IsCustomSegFeaturePoint = false;
						seqStartIndex = i;
					}
					else
					{
						seqStartIndex = i;
						break;
					}
				}
			}
			for (int i = points.Length - 2; i > 0; i--)
			{
				if (points[i].IsCustomSegFeaturePoint)
				{
					if (seqEndIndex - i <= Params.CUSTOM_SEG_P_INDEX_PMR)
					{
						points[i].IsCustomSegFeaturePoint = false;
						seqEndIndex = i;
					}
					else
					{
						seqEndIndex = i;
						break;
					}
				}
			}

			// Test whether the stroke has only two corner points (at the very ends)
			if (seqStartIndex == 0 || seqEndIndex == points.Length - 1)
			{
				return featurePointCount;
			}
			// Test whether the stroke has only three corner points
			else if (seqStartIndex == seqEndIndex)
			{
				points[seqStartIndex].IsCustomSegFeaturePoint = true;
				return ++featurePointCount;
			}

			double stopIndex = seqEndIndex;
			seqEndIndex = seqStartIndex;
			int seqIndexSum = seqStartIndex;
			int seqIndexCount = 1;

			// Remove close feature points from the middle parts of the stroke
			for (int i = seqStartIndex; i <= stopIndex; i++)
			{
				if (points[i].IsCustomSegFeaturePoint)
				{
					points[i].IsCustomSegFeaturePoint = false;

					if (i - seqEndIndex <= Params.CUSTOM_SEG_P_INDEX_PMR)
					{
						seqIndexSum += i;
						seqIndexCount++;
					}
					else
					{
						// Set the average point of the sequence to be the feature point
						points[seqIndexSum / seqIndexCount].IsCustomSegFeaturePoint = true;

						seqIndexSum = i;
						seqIndexCount = 1;
						seqStartIndex = i;
						featurePointCount++;
					}

					seqEndIndex = i;
				}
			}

			if (seqStartIndex == seqEndIndex)
			{
				// We removed a single feature point that was not actually close to any others, so 
				// we need to re-add it
				points[seqEndIndex].IsCustomSegFeaturePoint = true;
				featurePointCount++;
			}
			else
			{
				// We removed a sequence of feature points without actually adding the average 
				// point of the sequence back as a feature point, so we need to add it back now
				points[seqIndexSum / seqIndexCount].IsCustomSegFeaturePoint = true;
				featurePointCount++;
			}

			return featurePointCount;
		}

		/// <summary>
		/// Remove feature points which have indices close to each other.
		/// 
		/// **ORIGINAL**
		/// When a sequence of close feature points are found, only the point at the AVERAGE of 
		/// the indices of the points in the sequence will be a feature point afterward.  The 
		/// first and last feature points are an excpetion to this and are never removed.
		/// </summary>
		private static int RemoveCloseArcLengthPoints(Point[] points)
		{
			int featurePointCount = 2;
			int seqStartIndex = 0;
			int seqEndIndex = points.Length - 1;

			// Remove feature points which are close to either end of the stroke, but are not 
			// actually the end points
			for (int i = 1; i < points.Length - 1; i++)
			{
				if (points[i].IsCustomSegFeaturePoint)
				{
					if (points[i].ArcLength - points[seqStartIndex].ArcLength <= 
						Params.CUSTOM_SEG_P_ARC_LENGTH_PMR)
					{
						points[i].IsCustomSegFeaturePoint = false;
						seqStartIndex = i;
					}
					else
					{
						seqStartIndex = i;
						break;
					}
				}
			}
			for (int i = points.Length - 2; i > 0; i--)
			{
				if (points[i].IsCustomSegFeaturePoint)
				{
					if (points[seqEndIndex].ArcLength - points[i].ArcLength <= 
						Params.CUSTOM_SEG_P_ARC_LENGTH_PMR)
					{
						points[i].IsCustomSegFeaturePoint = false;
						seqEndIndex = i;
					}
					else
					{
						seqEndIndex = i;
						break;
					}
				}
			}

			// Test whether the stroke has only two corner points (at the very ends)
			if (seqStartIndex == 0 || seqEndIndex == points.Length - 1)
			{
				return featurePointCount;
			}
			// Test whether the stroke has only three corner points
			else if (seqStartIndex == seqEndIndex)
			{
				points[seqStartIndex].IsCustomSegFeaturePoint = true;
				return ++featurePointCount;
			}

			double stopIndex = seqEndIndex;
			seqEndIndex = seqStartIndex;
			int seqIndexSum = seqStartIndex;
			int seqIndexCount = 1;

			// Remove close feature points from the middle parts of the stroke
			for (int i = seqStartIndex; i <= stopIndex; i++)
			{
				if (points[i].IsCustomSegFeaturePoint)
				{
					points[i].IsCustomSegFeaturePoint = false;

					if (points[i].ArcLength - points[seqEndIndex].ArcLength <= 
						Params.CUSTOM_SEG_P_ARC_LENGTH_PMR)
					{
						seqIndexSum += i;
						seqIndexCount++;
					}
					else
					{
						// Set the average point of the sequence to be the feature point
						points[seqIndexSum / seqIndexCount].IsCustomSegFeaturePoint = true;

						seqIndexSum = i;
						seqIndexCount = 1;
						seqStartIndex = i;
						featurePointCount++;
					}

					seqEndIndex = i;
				}
			}

			if (seqStartIndex == seqEndIndex)
			{
				// We removed a single feature point that was not actually close to any others, so 
				// we need to re-add it
				points[seqEndIndex].IsCustomSegFeaturePoint = true;
				featurePointCount++;
			}
			else
			{
				// We removed a sequence of feature points without actually adding the average 
				// point of the sequence back as a feature point, so we need to add it back now
				points[seqIndexSum / seqIndexCount].IsCustomSegFeaturePoint = true;
				featurePointCount++;
			}

			return featurePointCount;
		}

		/// <summary>
		/// Remove the center point from any point triplet sequence in which the three points are 
		/// collinear.
		/// 
		/// **SAME AS SHORT STRAW**
		/// </summary>
		private static int RemovePointsFromCollinearTriplets(Point[] points)
		{
			int featurePointCount = 2;

			int indexA = 0;
			int indexB = 0;
			int indexC = -1;

			// Find the second corner
			for (int i = 1; i < points.Length; i++)
			{
				if (points[i].IsCustomSegFeaturePoint)
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
				if (points[i].IsCustomSegFeaturePoint)
				{
					indexB = indexC;
					indexC = i;

					if (IsLine(points[indexA], points[indexC]))
					{
						points[indexB].IsCustomSegFeaturePoint = false;
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
			return distance / pathDistance > Params.CUSTOM_IS_LINE_THRESHOLD;
		}
	}
}
