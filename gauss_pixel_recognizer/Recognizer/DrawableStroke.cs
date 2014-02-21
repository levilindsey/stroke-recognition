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
	public class DrawableStroke : RecognizerStroke
	{

		#region FIELD_DECLARATIONS

		private DrawablePoint[] rawPoints;
		private DrawablePoint[] smoothedPoints;
		private DrawablePoint[] unsegmentedResampledPoints;
		private DrawablePoint[] endPointsOnlySegPoints;
		private DrawablePoint[] shortStrawPoints;
		private DrawablePoint[] speedSegPoints;
		private DrawablePoint[] customSegWOPostProcessPoints;
		private DrawablePoint[] customSegPoints;

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

		private double avgResampleError;
		private double medianStrawValue;
		private double shortStrawThreshold;
		private double customShortStrawThreshold;

		#endregion

		#region CONSTRUCTORS

		/// <summary>
		/// Constructor.
		/// </summary>
		public DrawableStroke()
		{
			Initialize(null, -1, -1, -1, -1, false, false);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public DrawableStroke(IEnumerable<DrawablePoint> oldPoints, int angleSmoothCount, 
			int resampleCount, int speedSmoothCount, int curvatureSmoothCount, bool showSpeedCurv, 
			bool resampleWithSegAlgs)
		{
			Initialize(oldPoints, resampleCount, angleSmoothCount, speedSmoothCount, 
				curvatureSmoothCount, showSpeedCurv, resampleWithSegAlgs);
		}

		/// <summary>
		/// Initialize the state of this Stroke.
		/// </summary>
		private void Initialize(IEnumerable<DrawablePoint> oldPoints, int angleSmoothCount, 
			int resampleCount,int speedSmoothCount, int curvatureSmoothCount, bool showSpeedCurv, 
			bool resampleWithSegAlgs)
		{
			rawPoints = DrawablePoint.DeepCopy(oldPoints);
			recognizerPoints = StrokePreProcessing.RemoveDuplicatePoints(
				RecognizerPoint.DeepCopy(oldPoints));
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

			CalculatePointProperties(resampleCount, angleSmoothCount, speedSmoothCount, 
				curvatureSmoothCount, showSpeedCurv, resampleWithSegAlgs);
			CalculateSegmentProperties();
			CalculateStrokeProperties(smoothedPoints);
		}

		#endregion

		#region DYNAMIC_MEMBERS

		/// <summary>
		/// Calculate and save important properties for each of this Stroke's Points.
		/// </summary>
		private void CalculatePointProperties(int resampleCount, int angleSmoothCount, 
			int speedSmoothCount,int curvatureSmoothCount, bool showSpeedCurv, 
			bool resampleWithSegAlgs)
		{
			rawPoints = StrokePreProcessing.RemoveDuplicatePoints(rawPoints);

			smoothedPoints = DrawablePoint.DeepCopy(rawPoints);

			double rawPathLength = StrokePreProcessing.GetPathLength(smoothedPoints);

			unsegmentedResampledPoints = 
				StrokePreProcessing.Resample(smoothedPoints, resampleCount);
			avgResampleError = StrokePreProcessing.GetAvgResampleError(unsegmentedResampledPoints,
				resampleCount, rawPathLength);

			StrokePreProcessing.CalculatePointAngles(recognizerPoints);

			StrokePreProcessing.SmoothAngles(recognizerPoints, angleSmoothCount);

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
				endPointsOnlySegPoints = DrawablePoint.DeepCopy(unsegmentedResampledPoints);
				shortStrawPoints = DrawablePoint.DeepCopy(unsegmentedResampledPoints);
				speedSegPoints = DrawablePoint.DeepCopy(unsegmentedResampledPoints);
				customSegWOPostProcessPoints = DrawablePoint.DeepCopy(unsegmentedResampledPoints);
				customSegPoints = DrawablePoint.DeepCopy(unsegmentedResampledPoints);
			}
			else
			{
				endPointsOnlySegPoints = DrawablePoint.DeepCopy(smoothedPoints);
				shortStrawPoints = DrawablePoint.DeepCopy(smoothedPoints);
				speedSegPoints = DrawablePoint.DeepCopy(smoothedPoints);
				customSegWOPostProcessPoints = DrawablePoint.DeepCopy(smoothedPoints);
				customSegPoints = DrawablePoint.DeepCopy(smoothedPoints);
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

		public DrawablePoint[] GetPoints(PointProcessing displayType, SegmentationAlgorithm segmentationAlgorithm)
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
			DrawablePoint[] points = GetPoints(displayType, segmentationAlgorithm);

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

		private static void SetPointLineColors(DrawablePoint[] points, bool showSpeedCurv)
		{
			for (int i = 0; i < points.Length; i++)
			{
				points[i].SetColorAndWidth(showSpeedCurv, false);
			}
		}

		public DrawablePoint[] RawPoints
		{
			get { return rawPoints; }
		}

		public DrawablePoint[] SmoothedPoints
		{
			get { return smoothedPoints; }
		}

		public DrawablePoint[] UnsegmentedResampledPoints
		{
			get { return unsegmentedResampledPoints; }
		}

		public DrawablePoint[] ShortStrawPoints
		{
			get { return shortStrawPoints; }
		}

		public DrawablePoint[] EndPointsOnlySegPoints
		{
			get { return endPointsOnlySegPoints; }
		}

		public DrawablePoint[] SpeedSegPoints
		{
			get { return speedSegPoints; }
		}

		public DrawablePoint[] CustomSegPoints
		{
			get { return customSegPoints; }
		}

		public DrawablePoint[] CustomSegWOPostProcessPoints
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

		public double Timestamp
		{
			get { return rawPoints[0].Timestamp; }
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
