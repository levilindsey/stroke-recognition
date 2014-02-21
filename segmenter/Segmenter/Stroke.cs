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
	class Stroke
	{

		#region FIELD_DECLARATIONS

		private Point[] rawPoints;
		private Point[] smoothedPoints;
		private Point[] unsegmentedResampledPoints;
		private Point[] endPointsOnlySegPoints;
		private Point[] shortStrawPoints;
		private Point[] speedSegPoints;
		private Point[] customSegWOPostProcessPoints;
		private Point[] customSegPoints;

		private FeaturePoint[] endPointsOnlySegFeaturePts;
		private FeaturePoint[] shortStrawFeaturePts;
		private FeaturePoint[] speedSegFeaturePts;
		private FeaturePoint[] customSegWOPostProcessFeaturePts;
		private FeaturePoint[] customSegFeaturePts;

		private Segment[] endPointsOnlySegSegments;
		private Segment[] shortStrawSegments;
		private Segment[] speedSegSegments;
		private Segment[] customSegWOPostProcessSegments;
		private Segment[] customSegSegments;

		private double pathLength;
		private Rect boundingBox;
		private Point centroid;
		private double avgResampleError;
		private double medianStrawValue;
		private double shortStrawThreshold;
		private double customShortStrawThreshold;

		#endregion

		#region CONSTRUCTORS

		/// <summary>
		/// Constructor.
		/// </summary>
		public Stroke(IEnumerable<Point> oldPoints, int resampleCount, int speedSmoothCount, 
			int curvatureSmoothCount, bool showSpeedCurv, bool resampleWithSegAlgs)
		{
			Initialize(oldPoints, resampleCount, speedSmoothCount, curvatureSmoothCount,
				showSpeedCurv, resampleWithSegAlgs);
		}

		/// <summary>
		/// Initialize the state of this Stroke.
		/// </summary>
		private void Initialize(IEnumerable<Point> oldPoints, int resampleCount,
			int speedSmoothCount, int curvatureSmoothCount, bool showSpeedCurv, bool resampleWithSegAlgs)
		{
			rawPoints = Point.DeepCopy(oldPoints);
			smoothedPoints = null;
			unsegmentedResampledPoints = null;
			endPointsOnlySegPoints = null;
			shortStrawPoints = null;
			speedSegPoints = null;
			customSegPoints = null;
			customSegWOPostProcessPoints = null;

			endPointsOnlySegFeaturePts = null;
			shortStrawFeaturePts = null;
			speedSegFeaturePts = null;
			customSegWOPostProcessFeaturePts = null;
			customSegFeaturePts = null;

			endPointsOnlySegSegments = null;
			shortStrawSegments = null;
			speedSegSegments = null;
			customSegWOPostProcessSegments = null;
			customSegSegments = null;

			CalculatePointProperties(resampleCount, speedSmoothCount, curvatureSmoothCount,
				showSpeedCurv, resampleWithSegAlgs);
			CalculateSegmentProperties();
			CalculateStrokeProperties();
		}

		#endregion

		#region DYNAMIC_MEMBERS

		/// <summary>
		/// Calculate the length, bounding box, and centroid values for this Stroke.
		/// </summary>
		private void CalculateStrokeProperties()
		{

			// For the length
			double deltaX = 0;
			double deltaY = 0;
			pathLength = 0;

			// For the bounding box
			double minX = Double.MaxValue;
			double maxX = Double.MinValue;
			double minY = Double.MaxValue;
			double maxY = Double.MinValue;

			// For the centroid
			double centroidX = 0.0;
			double centroidY = 0.0;

			// Handle the fencepost problem
			if (smoothedPoints[0].X > maxX)
			{	// Record max X
				maxX = smoothedPoints[0].X;
			}

			if (smoothedPoints[0].X < minX)
			{	// Record min X
				minX = smoothedPoints[0].X;
			}
			if (smoothedPoints[0].Y > maxY)
			{	// Record max Y
				maxY = smoothedPoints[0].Y;
			}
			if (smoothedPoints[0].Y < minY)
			{	// Record min Y
				minY = smoothedPoints[0].Y;
			}
			centroidX += smoothedPoints[0].X;
			centroidY += smoothedPoints[0].Y;

			for (int i = 1; i < smoothedPoints.Length; i++)
			{
				// For the length
				deltaX = smoothedPoints[i].X - smoothedPoints[i - 1].X;
				deltaY = smoothedPoints[i].Y - smoothedPoints[i - 1].Y;
				pathLength += Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

				// For the bounding box
				if (smoothedPoints[i].X > maxX)
				{	// Record max X
					maxX = smoothedPoints[i].X;
				}
				else if (smoothedPoints[i].X < minX)
				{	// Record min X
					minX = smoothedPoints[i].X;
				}
				if (smoothedPoints[i].Y > maxY)
				{	// Record max Y
					maxY = smoothedPoints[i].Y;
				}
				else if (smoothedPoints[i].Y < minY)
				{	// Record min Y
					minY = smoothedPoints[i].Y;
				}

				// For the centroid
				centroidX += smoothedPoints[i].X;
				centroidY += smoothedPoints[i].Y;
			}

			this.boundingBox = new Rect(minX, minY, maxX - minX, maxY - minY);

			this.centroid = new Point(centroidX / smoothedPoints.Length, centroidY / smoothedPoints.Length, -1);
		}

		/// <summary>
		/// Calculate and save important properties for each of this Stroke's Points.
		/// </summary>
		private void CalculatePointProperties(int resampleCount, int speedSmoothCount,
			int curvatureSmoothCount, bool showSpeedCurv, bool resampleWithSegAlgs)
		{
			rawPoints = StrokePreProcessing.RemoveDuplicatePoints(rawPoints);

			smoothedPoints = Point.DeepCopy(rawPoints);

			double rawPathLength = StrokePreProcessing.GetPathLength(smoothedPoints);

			unsegmentedResampledPoints = 
				StrokePreProcessing.Resample(smoothedPoints, resampleCount);
			avgResampleError = StrokePreProcessing.GetAvgResampleError(unsegmentedResampledPoints, 
				resampleCount, rawPathLength);

			StrokePreProcessing.CalculatePointArcLengths(rawPoints);
			StrokePreProcessing.CalculatePointArcLengths(smoothedPoints);
			StrokePreProcessing.CalculatePointArcLengths(unsegmentedResampledPoints);

			StrokePreProcessing.CalculatePointSpeeds(rawPoints);
			StrokePreProcessing.CalculatePointSpeeds(smoothedPoints);
			StrokePreProcessing.CalculatePointSpeeds(unsegmentedResampledPoints);

			StrokePreProcessing.SmoothSpeeds(smoothedPoints, speedSmoothCount);
			StrokePreProcessing.SmoothSpeeds(unsegmentedResampledPoints, speedSmoothCount);

			StrokePreProcessing.CalculatePointFirstDerivatives(rawPoints);
			StrokePreProcessing.CalculatePointFirstDerivatives(smoothedPoints);
			StrokePreProcessing.CalculatePointFirstDerivatives(unsegmentedResampledPoints);

			StrokePreProcessing.CalculatePointSecondDerivatives(rawPoints);
			StrokePreProcessing.CalculatePointSecondDerivatives(smoothedPoints);
			StrokePreProcessing.CalculatePointSecondDerivatives(unsegmentedResampledPoints);

			StrokePreProcessing.CalculatePointCurvatures(rawPoints);
			StrokePreProcessing.CalculatePointCurvatures(smoothedPoints);
			StrokePreProcessing.CalculatePointCurvatures(unsegmentedResampledPoints);

			StrokePreProcessing.SmoothCurvatures(smoothedPoints, curvatureSmoothCount);
			StrokePreProcessing.SmoothCurvatures(unsegmentedResampledPoints, curvatureSmoothCount);

			medianStrawValue =
				StrokePreProcessing.CalculatePointStrawValues(unsegmentedResampledPoints);
			shortStrawThreshold = medianStrawValue * Params.SHORT_STRAW_CORNER_THRESHOLD;
			customShortStrawThreshold = medianStrawValue * Params.CUSTOM_SHORT_STRAW_CORNER_THRESHOLD;

			if (resampleWithSegAlgs)
			{
				endPointsOnlySegPoints = Point.DeepCopy(unsegmentedResampledPoints);
				shortStrawPoints = Point.DeepCopy(unsegmentedResampledPoints);
				speedSegPoints = Point.DeepCopy(unsegmentedResampledPoints);
				customSegWOPostProcessPoints = Point.DeepCopy(unsegmentedResampledPoints);
				customSegPoints = Point.DeepCopy(unsegmentedResampledPoints);
			}
			else
			{
				endPointsOnlySegPoints = Point.DeepCopy(smoothedPoints);
				shortStrawPoints = Point.DeepCopy(smoothedPoints);
				speedSegPoints = Point.DeepCopy(smoothedPoints);
				customSegWOPostProcessPoints = Point.DeepCopy(smoothedPoints);
				customSegPoints = Point.DeepCopy(smoothedPoints);
			}

			StrokePreProcessing.CalculatePointLines(rawPoints, showSpeedCurv);
			StrokePreProcessing.CalculatePointLines(smoothedPoints, showSpeedCurv);
			StrokePreProcessing.CalculatePointLines(unsegmentedResampledPoints, showSpeedCurv);
			StrokePreProcessing.CalculatePointLines(endPointsOnlySegPoints, showSpeedCurv);
			StrokePreProcessing.CalculatePointLines(shortStrawPoints, showSpeedCurv);
			StrokePreProcessing.CalculatePointLines(speedSegPoints, showSpeedCurv);
			StrokePreProcessing.CalculatePointLines(customSegPoints, showSpeedCurv);
			StrokePreProcessing.CalculatePointLines(customSegWOPostProcessPoints, showSpeedCurv);
		}

		/// <summary>
		/// Calculate and save important peroperties for each of this Stroke's Segments.
		/// </summary>
		private void CalculateSegmentProperties()
		{
			endPointsOnlySegFeaturePts = StrokeSegmentation.Segment(endPointsOnlySegPoints,
				SegmentationAlgorithm.EndPointsOnly, shortStrawThreshold);
			shortStrawFeaturePts = StrokeSegmentation.Segment(shortStrawPoints,
				SegmentationAlgorithm.ShortStraw, shortStrawThreshold);
			speedSegFeaturePts = StrokeSegmentation.Segment(speedSegPoints,
				SegmentationAlgorithm.SpeedSeg, shortStrawThreshold);
			customSegWOPostProcessFeaturePts = StrokeSegmentation.Segment(customSegWOPostProcessPoints,
				SegmentationAlgorithm.CustomSegWOPostProcess, customShortStrawThreshold);
			customSegFeaturePts = StrokeSegmentation.Segment(customSegPoints,
				SegmentationAlgorithm.CustomSeg, customShortStrawThreshold);

			endPointsOnlySegSegments = StrokeSegmentation.FitSegments(endPointsOnlySegPoints,
				SegmentationAlgorithm.EndPointsOnly, endPointsOnlySegFeaturePts.Length);
			shortStrawSegments = StrokeSegmentation.FitSegments(shortStrawPoints, 
				SegmentationAlgorithm.ShortStraw, shortStrawFeaturePts.Length);
			speedSegSegments = StrokeSegmentation.FitSegments(speedSegPoints, 
				SegmentationAlgorithm.SpeedSeg, speedSegFeaturePts.Length);
			customSegWOPostProcessSegments = StrokeSegmentation.FitSegments(customSegWOPostProcessPoints,
				SegmentationAlgorithm.CustomSegWOPostProcess, customSegWOPostProcessFeaturePts.Length);
			customSegSegments = StrokeSegmentation.FitSegments(customSegPoints,
				SegmentationAlgorithm.CustomSeg, customSegFeaturePts.Length);
		}

		public IEnumerable<Drawable> GetDrawables(PointProcessing displayType, 
			SegmentationAlgorithm segmentationAlgorithm)
		{
			List<Drawable> drawables;

			switch (displayType)
			{
				case PointProcessing.Raw:
					return rawPoints;
				case PointProcessing.Smoothed:
					return smoothedPoints;
				case PointProcessing.Resampled:
					switch (segmentationAlgorithm)
					{
						case SegmentationAlgorithm.EndPointsOnly:
							return endPointsOnlySegPoints;
						case SegmentationAlgorithm.ShortStraw:
							return shortStrawPoints;
						case SegmentationAlgorithm.SpeedSeg:
							return speedSegPoints;
						case SegmentationAlgorithm.CustomSegWOPostProcess:
							return customSegWOPostProcessPoints;
						case SegmentationAlgorithm.CustomSeg:
							return customSegPoints;
						default:
							return null;
					}
				case PointProcessing.FeaturePts:
					switch (segmentationAlgorithm)
					{
						case SegmentationAlgorithm.EndPointsOnly:
							drawables = new List<Drawable>();
							drawables.AddRange(endPointsOnlySegPoints);
							drawables.AddRange(endPointsOnlySegFeaturePts);
							return drawables;
						case SegmentationAlgorithm.ShortStraw:
							drawables = new List<Drawable>();
							drawables.AddRange(shortStrawPoints);
							drawables.AddRange(shortStrawFeaturePts);
							return drawables;
						case SegmentationAlgorithm.SpeedSeg:
							drawables = new List<Drawable>();
							drawables.AddRange(speedSegPoints);
							drawables.AddRange(speedSegFeaturePts);
							return drawables;
						case SegmentationAlgorithm.CustomSegWOPostProcess:
							drawables = new List<Drawable>();
							drawables.AddRange(customSegWOPostProcessPoints);
							drawables.AddRange(customSegWOPostProcessFeaturePts);
							return drawables;
						case SegmentationAlgorithm.CustomSeg:
							drawables = new List<Drawable>();
							drawables.AddRange(customSegPoints);
							drawables.AddRange(customSegFeaturePts);
							return drawables;
						default:
							return null;
					}
				case PointProcessing.Segments:
					switch (segmentationAlgorithm)
					{
						case SegmentationAlgorithm.EndPointsOnly:
							drawables = new List<Drawable>();
							drawables.AddRange(endPointsOnlySegSegments);
							return drawables;
						case SegmentationAlgorithm.ShortStraw:
							drawables = new List<Drawable>();
							drawables.AddRange(shortStrawSegments);
							return drawables;
						case SegmentationAlgorithm.SpeedSeg:
							drawables = new List<Drawable>();
							drawables.AddRange(speedSegSegments);
							return drawables;
						case SegmentationAlgorithm.CustomSegWOPostProcess:
							drawables = new List<Drawable>();
							drawables.AddRange(customSegWOPostProcessSegments);
							return drawables;
						case SegmentationAlgorithm.CustomSeg:
							drawables = new List<Drawable>();
							drawables.AddRange(customSegSegments);
							return drawables;
						default:
							return null;
					}
				case PointProcessing.PointsAndSegments:
					switch (segmentationAlgorithm)
					{
						case SegmentationAlgorithm.EndPointsOnly:
							drawables = new List<Drawable>();
							drawables.AddRange(endPointsOnlySegSegments);
							drawables.AddRange(endPointsOnlySegFeaturePts);
							return drawables;
						case SegmentationAlgorithm.ShortStraw:
							drawables = new List<Drawable>();
							drawables.AddRange(shortStrawSegments);
							drawables.AddRange(shortStrawFeaturePts);
							return drawables;
						case SegmentationAlgorithm.SpeedSeg:
							drawables = new List<Drawable>();
							drawables.AddRange(speedSegSegments);
							drawables.AddRange(speedSegFeaturePts);
							return drawables;
						case SegmentationAlgorithm.CustomSegWOPostProcess:
							drawables = new List<Drawable>();
							drawables.AddRange(customSegWOPostProcessSegments);
							drawables.AddRange(customSegWOPostProcessFeaturePts);
							return drawables;
						case SegmentationAlgorithm.CustomSeg:
							drawables = new List<Drawable>();
							drawables.AddRange(customSegSegments);
							drawables.AddRange(customSegFeaturePts);
							return drawables;
						default:
							return null;
					}
				case PointProcessing.InkPointsAndSegments:
					switch (segmentationAlgorithm)
					{
						case SegmentationAlgorithm.EndPointsOnly:
							drawables = new List<Drawable>();
							drawables.AddRange(endPointsOnlySegPoints);
							drawables.AddRange(endPointsOnlySegSegments);
							drawables.AddRange(endPointsOnlySegFeaturePts);
							return drawables;
						case SegmentationAlgorithm.ShortStraw:
							drawables = new List<Drawable>();
							drawables.AddRange(shortStrawPoints);
							drawables.AddRange(shortStrawSegments);
							drawables.AddRange(shortStrawFeaturePts);
							return drawables;
						case SegmentationAlgorithm.SpeedSeg:
							drawables = new List<Drawable>();
							drawables.AddRange(speedSegPoints);
							drawables.AddRange(speedSegSegments);
							drawables.AddRange(speedSegFeaturePts);
							return drawables;
						case SegmentationAlgorithm.CustomSegWOPostProcess:
							drawables = new List<Drawable>();
							drawables.AddRange(customSegWOPostProcessPoints);
							drawables.AddRange(customSegWOPostProcessSegments);
							drawables.AddRange(customSegWOPostProcessFeaturePts);
							return drawables;
						case SegmentationAlgorithm.CustomSeg:
							drawables = new List<Drawable>();
							drawables.AddRange(customSegPoints);
							drawables.AddRange(customSegSegments);
							drawables.AddRange(customSegFeaturePts);
							return drawables;
						default:
							return null;
					}
				default:
					return null;
			}
		}

		public Point[] GetPoints(PointProcessing displayType, SegmentationAlgorithm segmentationAlgorithm)
		{
			switch (displayType)
			{
				case PointProcessing.Raw:
					return rawPoints;
				case PointProcessing.Smoothed:
					return smoothedPoints;
				case PointProcessing.Resampled:
				case PointProcessing.FeaturePts:
				case PointProcessing.Segments:
				case PointProcessing.PointsAndSegments:
				case PointProcessing.InkPointsAndSegments:
					switch (segmentationAlgorithm)
					{
						case SegmentationAlgorithm.EndPointsOnly:
							return endPointsOnlySegPoints;
						case SegmentationAlgorithm.ShortStraw:
							return shortStrawPoints;
						case SegmentationAlgorithm.SpeedSeg:
							return speedSegPoints;
						case SegmentationAlgorithm.CustomSegWOPostProcess:
							return customSegWOPostProcessPoints;
						case SegmentationAlgorithm.CustomSeg:
							return customSegPoints;
						default:
							return null;
					}
				default:
					return null;
			}
		}

		public Drawable[] GetPoints(SaveValue saveValue, SegmentationAlgorithm segmentationAlgorithm)
		{
			switch (saveValue)
			{
				case SaveValue.DefaultInts:
				case SaveValue.DefaultDbls:
					return rawPoints;
				case SaveValue.ResampledInts:
				case SaveValue.ResampledDbls:
					return unsegmentedResampledPoints;
				case SaveValue.FeaturePointsInts:
				case SaveValue.FeaturePointsDbls:
					switch (segmentationAlgorithm)
					{
						case SegmentationAlgorithm.EndPointsOnly:
							return endPointsOnlySegPoints;
						case SegmentationAlgorithm.ShortStraw:
							return shortStrawPoints;
						case SegmentationAlgorithm.SpeedSeg:
							return speedSegPoints;
						case SegmentationAlgorithm.CustomSegWOPostProcess:
							return customSegWOPostProcessPoints;
						case SegmentationAlgorithm.CustomSeg:
							return customSegPoints;
						default:
							return null;
					}
				case SaveValue.ArcLength:
					return rawPoints;
				case SaveValue.SpeedRaw:
					return rawPoints;
				case SaveValue.SpeedSmoothed:
					return smoothedPoints;
				case SaveValue.CurvatureRaw:
					return rawPoints;
				case SaveValue.CurvatureSmoothed:
					return smoothedPoints;
				case SaveValue.StrawValue:
					return unsegmentedResampledPoints;
				case SaveValue.FeaturePoints:
					switch (segmentationAlgorithm)
					{
						case SegmentationAlgorithm.EndPointsOnly:
							return endPointsOnlySegFeaturePts;
						case SegmentationAlgorithm.ShortStraw:
							return shortStrawFeaturePts;
						case SegmentationAlgorithm.SpeedSeg:
							return speedSegFeaturePts;
						case SegmentationAlgorithm.CustomSegWOPostProcess:
							return customSegWOPostProcessFeaturePts;
						case SegmentationAlgorithm.CustomSeg:
							return customSegFeaturePts;
						default:
							return null;
					}
				case SaveValue.Segments:
					switch (segmentationAlgorithm)
					{
						case SegmentationAlgorithm.EndPointsOnly:
							return endPointsOnlySegSegments;
						case SegmentationAlgorithm.ShortStraw:
							return shortStrawSegments;
						case SegmentationAlgorithm.SpeedSeg:
							return speedSegSegments;
						case SegmentationAlgorithm.CustomSegWOPostProcess:
							return customSegWOPostProcessSegments;
						case SegmentationAlgorithm.CustomSeg:
							return customSegSegments;
						default:
							return null;
					}
				default:
					return null;
			}
		}

		public List<KeyValuePair<double, double>> GetYVsXData(PointProcessing displayType, 
			SegmentationAlgorithm segmentationAlgorithm, AxisValue xAxis, AxisValue yAxis)
		{
			Point[] points = GetPoints(displayType, segmentationAlgorithm);

			List<KeyValuePair<double, double>> data = new List<KeyValuePair<double, double>>();

			KeyValuePair<double, double> datum;

			double startTime = Timestamp;

			for (int i = 0; i < points.Length; i++)
			{
				datum = new KeyValuePair<double, double>(points[i].GetValue(xAxis, startTime), 
					points[i].GetValue(yAxis, startTime));
				data.Add(datum);
			}

			return data;
		}

		public void SetPointLineColors(bool showSpeedCurv)
		{
			SetPointLineColors(rawPoints, showSpeedCurv);
			SetPointLineColors(smoothedPoints, showSpeedCurv);
			SetPointLineColors(unsegmentedResampledPoints, showSpeedCurv);
			SetPointLineColors(endPointsOnlySegPoints, showSpeedCurv);
			SetPointLineColors(shortStrawPoints, showSpeedCurv);
			SetPointLineColors(speedSegPoints, showSpeedCurv);
			SetPointLineColors(customSegPoints, showSpeedCurv);
			SetPointLineColors(customSegWOPostProcessPoints, showSpeedCurv);
		}

		private static void SetPointLineColors(Point[] points, bool showSpeedCurv)
		{
			for (int i = 0; i < points.Length; i++)
			{
				points[i].SetColorAndWidth(showSpeedCurv, false);
			}
		}

		public Point[] RawPoints
		{
			get { return rawPoints; }
		}

		public Point[] SmoothedPoints
		{
			get { return smoothedPoints; }
		}

		public Point[] UnsegmentedResampledPoints
		{
			get { return unsegmentedResampledPoints; }
		}

		public Point[] ShortStrawPoints
		{
			get { return shortStrawPoints; }
		}

		public Point[] EndPointsOnlySegPoints
		{
			get { return endPointsOnlySegPoints; }
		}

		public Point[] SpeedSegPoints
		{
			get { return speedSegPoints; }
		}

		public Point[] CustomSegPoints
		{
			get { return customSegPoints; }
		}

		public Point[] CustomSegWOPostProcessPoints
		{
			get { return customSegWOPostProcessPoints; }
		}

		public FeaturePoint[] EndPointsOnlySegFeaturePts
		{
			get { return endPointsOnlySegFeaturePts; }
		}

		public FeaturePoint[] ShortStrawFeaturePts
		{
			get { return shortStrawFeaturePts; }
		}

		public FeaturePoint[] SpeedSegFeaturePts
		{
			get { return speedSegFeaturePts; }
		}

		public FeaturePoint[] CustomSegWOPostProcessFeaturePts
		{
			get { return customSegWOPostProcessFeaturePts; }
		}

		public FeaturePoint[] CustomSegFeaturePts
		{
			get { return customSegFeaturePts; }
		}

		public Segment[] EndPointsOnlySegSegments
		{
			get { return endPointsOnlySegSegments; }
		}

		public Segment[] ShortStrawSegments
		{
			get { return shortStrawSegments; }
		}

		public Segment[] SpeedSegSegments
		{
			get { return speedSegSegments; }
		}

		public Segment[] CustomSegWOPostProcessSegments
		{
			get { return customSegWOPostProcessSegments; }
		}

		public Segment[] CustomSegSegments
		{
			get { return customSegSegments; }
		}

		public double PathLength
		{
			get { return pathLength; }
		}

		public Rect BoundingBox
		{
			get { return boundingBox; }
		}

		public Point Centroid
		{
			get { return centroid; }
		}

		public double Timestamp
		{
			get { return smoothedPoints[0].Timestamp; }
		}

		public double AvgResampleError
		{
			get { return avgResampleError; }
		}

		public double MedianStrawValue
		{
			get { return medianStrawValue; }
		}

		public double ShortStrawThreshold
		{
			get { return shortStrawThreshold; }
		}

		public double CustomShortStrawThreshold
		{
			get { return customShortStrawThreshold; }
		}

		#endregion

	}
}
