/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StrokeCollector
{
	class EndPointsOnlySegmenter
	{
		public static FeaturePoint[] Segment(Point[] points, double shortStrawThreshold)
		{
			points[0].IsEndPointsOnlySegFeaturePoint = true;
			points[points.Length - 1].IsEndPointsOnlySegFeaturePoint = true;

			FeaturePoint[] featurePoints = new FeaturePoint[]
			{
				new FeaturePoint(points[0].X, points[0].Y, 0),
				new FeaturePoint(points[points.Length - 1].X, points[points.Length - 1].Y, 
					points.Length - 1)
			};

			return featurePoints;
		}

		/// <summary>
		/// Fit Segments to the given array of points according to the feature points.
		/// </summary>
		public static Segment[] FitSegments(Point[] points, int featurePointCount)
		{
			return new Segment[] { FitASegment(points, 0, points.Length - 1) };
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
	}
}
