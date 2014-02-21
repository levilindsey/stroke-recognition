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
	public class DrawablePoint : RecognizerPoint, Drawable
	{

		#region FIELD_DECLARATIONS

		private double xFirstDeriv;
		private double yFirstDeriv;
		private double xSecondDeriv;
		private double ySecondDeriv;

		private double arcLength;
		private double speed;
		private double curvature;
		private double strawValue;

		private bool isEndPointsOnlySegFeaturePoint;
		private bool isShortStrawFeaturePoint;
		private bool isSpeedSegFeaturePoint;
		private bool isCustomSegWOPostProcFeaturePoint;
		private bool isCustomSegFeaturePoint;

		// The actual line segment object which is rendered on the canvas (the LAST point in a 
		// stroke will not have a Line segment)
		private SolidColorBrush lineSpeedCurvBrush;
		private double lineSpeedCurvWidth;
		private Line lineSegment;

		#endregion

		#region CONSTRUCTORS

		/// <summary>
		/// Constructor.
		/// </summary>
		public DrawablePoint()
		{
			Initialize(Double.NaN, Double.NaN, Double.NaN);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public DrawablePoint(System.Windows.Point point, double timestamp)
		{
			Initialize(point.X, point.Y, timestamp);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public DrawablePoint(DrawablePoint point)
		{
			Initialize(point.X, point.Y, point.Timestamp);

			this.xFirstDeriv = point.XFirstDeriv;
			this.yFirstDeriv = point.YFirstDeriv;
			this.xSecondDeriv = point.XSecondDeriv;
			this.ySecondDeriv = point.YSecondDeriv;

			this.arcLength = point.ArcLength;
			this.speed = point.Speed;
			this.curvature = point.Curvature;
			this.strawValue = point.StrawValue;

			this.isEndPointsOnlySegFeaturePoint = point.IsEndPointsOnlySegFeaturePoint;
			this.isShortStrawFeaturePoint = point.IsShortStrawFeaturePoint;
			this.isSpeedSegFeaturePoint = point.IsSpeedSegFeaturePoint;
			this.isCustomSegWOPostProcFeaturePoint = point.IsCustomSegWOPostProcFeaturePoint;
			this.isCustomSegFeaturePoint = point.IsCustomSegFeaturePoint;

			if (point.GetShape() != null)
			{
				this.lineSpeedCurvBrush = new SolidColorBrush();
				this.lineSpeedCurvBrush.Color = point.LineSpeedCurvBrush.Color;
				this.lineSpeedCurvWidth = point.LineSpeedCurvWidth;

				Line otherLine = point.GetShape() as Line;
				Line thisLine = new Line();
				thisLine.X1 = otherLine.X1;
				thisLine.Y1 = otherLine.Y1;
				thisLine.X2 = otherLine.X2;
				thisLine.Y2 = otherLine.Y2;
				thisLine.Stroke = otherLine.Stroke;
				thisLine.StrokeThickness = otherLine.StrokeThickness;
			}
			else
			{
				this.lineSpeedCurvBrush = null;
				this.lineSegment = null;
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public DrawablePoint(double x, double y, double timestamp)
		{
			Initialize(x, y, timestamp);
		}

		/// <summary>
		/// Initialize the state of this Point.
		/// </summary>
		private void Initialize(double x, double y, double timestamp)
		{
			this.x = x;
			this.y = y;
			this.timestamp = timestamp;

			this.xFirstDeriv = Double.NaN;
			this.yFirstDeriv = Double.NaN;
			this.xSecondDeriv = Double.NaN;
			this.ySecondDeriv = Double.NaN;

			this.arcLength = Double.NaN;
			this.speed = Double.NaN;
			this.curvature = Double.NaN;
			this.strawValue = Double.NaN;

			this.lineSpeedCurvBrush = new SolidColorBrush();
			this.lineSpeedCurvWidth = Double.NaN;
			this.lineSegment = new Line();
			this.lineSegment.Stroke = lineSpeedCurvBrush;

			this.isEndPointsOnlySegFeaturePoint = false;
			this.isShortStrawFeaturePoint = false;
			this.isSpeedSegFeaturePoint = false;
			this.isCustomSegWOPostProcFeaturePoint = false;
			this.isCustomSegFeaturePoint = false;
		}

		#endregion

		#region DYNAMIC_MEMBERS

		/// <summary>
		/// Update the position of this stroke and all of its underlying points by the given 
		/// offset.
		/// </summary>
		public void UpdatePosition(DrawablePoint offset)
		{
			double offsetX = offset.X;
			double offsetY = offset.Y;

			this.x += offsetX;
			this.y += offsetY;
			this.lineSegment.X1 += offsetX;
			this.lineSegment.Y1 += offsetY;
			this.lineSegment.X2 += offsetX;
			this.lineSegment.Y2 += offsetY;
		}

		public double XFirstDeriv
		{
			get { return xFirstDeriv; }
			set { xFirstDeriv = value; }
		}

		public double YFirstDeriv
		{
			get { return yFirstDeriv; }
			set { yFirstDeriv = value; }
		}

		public double XSecondDeriv
		{
			get { return xSecondDeriv; }
			set { xSecondDeriv = value; }
		}

		public double YSecondDeriv
		{
			get { return ySecondDeriv; }
			set { ySecondDeriv = value; }
		}

		public double ArcLength
		{
			get { return arcLength; }
			set { arcLength = value; }
		}

		public double Speed
		{
			get { return speed; }
			set { speed = value; }
		}

		public double Curvature
		{
			get { return curvature; }
			set { curvature = value; }
		}

		public double StrawValue
		{
			get { return strawValue; }
			set { strawValue = value; }
		}

		public SolidColorBrush LineSpeedCurvBrush
		{
			get { return lineSpeedCurvBrush; }
		}

		public double LineSpeedCurvWidth
		{
			get { return lineSpeedCurvWidth; }
		}

		public void SetLineState(Color color, double width, double x1, double y1, double x2, 
			double y2, bool showSpeedCurv, bool isErasingStroke)
		{
			lineSpeedCurvBrush.Color = color;
			lineSpeedCurvWidth = width;

			lineSegment.X1 = x1;
			lineSegment.Y1 = y1;
			lineSegment.X2 = x2;
			lineSegment.Y2 = y2;

			SetColorAndWidth(showSpeedCurv, isErasingStroke);
		}

		public bool IsEndPointsOnlySegFeaturePoint
		{
			get { return isEndPointsOnlySegFeaturePoint; }
			set { isEndPointsOnlySegFeaturePoint = value; }
		}

		public bool IsShortStrawFeaturePoint
		{
			get { return isShortStrawFeaturePoint; }
			set { isShortStrawFeaturePoint = value; }
		}

		public bool IsSpeedSegFeaturePoint
		{
			get { return isSpeedSegFeaturePoint; }
			set { isSpeedSegFeaturePoint = value; }
		}

		public bool IsCustomSegWOPostProcFeaturePoint
		{
			get { return isCustomSegWOPostProcFeaturePoint; }
			set { isCustomSegWOPostProcFeaturePoint = value; }
		}

		public bool IsCustomSegFeaturePoint
		{
			get { return isCustomSegFeaturePoint; }
			set { isCustomSegFeaturePoint = value; }
		}

		public String GetDataFileEntry(SaveValue saveValue, 
			SegmentationAlgorithm segmentationAlgorithm)
		{
			switch (saveValue)
			{
				case SaveValue.ResampledInts:
				case SaveValue.DefaultInts:
					return "" + 
						(int)x + Params.INTRA_POINT_SAVE_DELIMITER +
						(int)y + Params.INTRA_POINT_SAVE_DELIMITER +
						"0" + Params.INTRA_POINT_SAVE_DELIMITER +
						"0" + Params.INTRA_POINT_SAVE_DELIMITER +
						"0" + Params.INTRA_POINT_SAVE_DELIMITER +
						(long)timestamp;
				case SaveValue.ResampledDbls:
				case SaveValue.DefaultDbls:
					return "" + 
						x + Params.INTRA_POINT_SAVE_DELIMITER +
						y + Params.INTRA_POINT_SAVE_DELIMITER +
						"0" + Params.INTRA_POINT_SAVE_DELIMITER +
						"0" + Params.INTRA_POINT_SAVE_DELIMITER +
						"0" + Params.INTRA_POINT_SAVE_DELIMITER +
						timestamp;
				case SaveValue.FeaturePointsInts:
					return "" +
						(int)x + Params.INTRA_POINT_SAVE_DELIMITER +
						(int)y + Params.INTRA_POINT_SAVE_DELIMITER +
						"0" + Params.INTRA_POINT_SAVE_DELIMITER +
						"0" + Params.INTRA_POINT_SAVE_DELIMITER +
						"0" + Params.INTRA_POINT_SAVE_DELIMITER +
						(long)timestamp + Params.INTRA_POINT_SAVE_DELIMITER +
						GetIsFeaturePointString(segmentationAlgorithm);
				case SaveValue.FeaturePointsDbls:
					return "" + 
						x + Params.INTRA_POINT_SAVE_DELIMITER +
						y + Params.INTRA_POINT_SAVE_DELIMITER +
						"0" + Params.INTRA_POINT_SAVE_DELIMITER +
						"0" + Params.INTRA_POINT_SAVE_DELIMITER +
						"0" + Params.INTRA_POINT_SAVE_DELIMITER +
						timestamp + Params.INTRA_POINT_SAVE_DELIMITER +
						GetIsFeaturePointString(segmentationAlgorithm);
				case SaveValue.ArcLength:
					return arcLength.ToString();
				case SaveValue.SpeedRaw:
				case SaveValue.SpeedSmoothed:
					return speed.ToString();
				case SaveValue.CurvatureRaw:
				case SaveValue.CurvatureSmoothed:
					return curvature.ToString();
				case SaveValue.StrawValue:
					return strawValue.ToString();
				case SaveValue.FeaturePoints:
					return null;
				case SaveValue.Segments:
					return null;
				default:
					return null;
			}
		}

		private String GetIsFeaturePointString(SegmentationAlgorithm segmentationAlgorithm)
		{
			switch (segmentationAlgorithm)
			{
				case SegmentationAlgorithm.EndPointsOnly:
					return isEndPointsOnlySegFeaturePoint ? "1" : "0";
				case SegmentationAlgorithm.ShortStraw:
					return isShortStrawFeaturePoint ? "1" : "0";
				case SegmentationAlgorithm.SpeedSeg:
					return isSpeedSegFeaturePoint ? "1" : "0";
				case SegmentationAlgorithm.CustomSegWOPostProcess:
					return isCustomSegWOPostProcFeaturePoint ? "1" : "0";
				case SegmentationAlgorithm.CustomSeg:
					return isCustomSegFeaturePoint ? "1" : "0";
				default:
					return null;
			}
		}

		/// <summary>
		/// Return the Shape for drawing on the Canvas.
		/// </summary>
		public Shape GetShape()
		{
			return lineSegment;
		}

		public double GetValue(AxisValue axisValue, double startTime)
		{
			switch (axisValue)
			{
				case AxisValue.X:
					return x;
				case AxisValue.Y:
					return y;
				case AxisValue.Timestamp:
					return timestamp - startTime;
				case AxisValue.ArcLength:
					return arcLength;
				case AxisValue.Speed:
					return speed;
				case AxisValue.Curvature:
					return curvature;
				case AxisValue.StrawValue: Console.WriteLine("strawValue=" + strawValue);
					return strawValue;
				default:
					return timestamp;
			}
		}

		public void NullLine()
		{
			lineSegment = null;
			lineSpeedCurvBrush = null;
		}

		public void SetColorAndWidth(bool showSpeedCurv, bool isErasingStroke)
		{
			if (lineSegment != null)
			{
				if (!isErasingStroke)
				{
					if (showSpeedCurv)
					{
						lineSegment.Stroke = lineSpeedCurvBrush;
						lineSegment.StrokeThickness = lineSpeedCurvWidth;
					}
					else
					{
						lineSegment.Stroke = Params.DEFAULT_STROKE_BRUSH;
						lineSegment.StrokeThickness = Params.DEFAULT_STROKE_THICKNESS;
					}
				}
				else
				{
					lineSegment.Stroke = Params.ERASE_STROKE_BRUSH;
					lineSegment.StrokeThickness = Params.ERASE_STROKE_THICKNESS;
				}
			}
		}

		#endregion

		#region STATIC_UTILITY_METHODS

		public static DrawablePoint[] DeepCopy(IEnumerable<DrawablePoint> oldPoints)
		{
			DrawablePoint[] newPoints = new DrawablePoint[oldPoints.Count()];
			int i = 0;

			foreach (DrawablePoint oldPoint in oldPoints)
			{
				newPoints[i++] = new DrawablePoint(oldPoint);
			}

			return newPoints;
		}

		#endregion

	}
}
