/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace StrokeCollector
{
	public class ShapeInstance
	{

		#region FIELD_DECLARATIONS

		protected IEnumerable<RecognizerStroke> strokesUnnormalized;

		protected IEnumerable<RecognizerStroke> strokes0;
		protected IEnumerable<RecognizerStroke> strokes45;
		protected IEnumerable<RecognizerStroke> strokes90;
		protected IEnumerable<RecognizerStroke> strokes135;

		private Dictionary<int, double>[] columns0;
		private Dictionary<int, double>[] columns45;
		private Dictionary<int, double>[] columns90;
		private Dictionary<int, double>[] columns135;

		private double[][] columnsSmoothed0;
		private double[][] columnsSmoothed45;
		private double[][] columnsSmoothed90;
		private double[][] columnsSmoothed135;

		private double timeToRecognize;
		private double recognizedDistance;
		private short actualShapeID;
		private short subjectID;
		private short exampleNumber;
		private short recognizedShapeID;

		#endregion

		#region CONSTRUCTORS

		/// <summary>
		/// Constructor.
		/// </summary>
		public ShapeInstance() { }

		/// <summary>
		/// Constructor.
		/// </summary>
		public ShapeInstance(ShapeInstance other)
		{
			this.strokesUnnormalized = DeepCopy(other.strokesUnnormalized);
			this.strokes0 = DeepCopy(other.strokes0);
			this.strokes45 = DeepCopy(other.strokes45);
			this.strokes90 = DeepCopy(other.strokes90);
			this.strokes135 = DeepCopy(other.strokes135);
			this.columns0 = DeepCopyColumns(other.columns0);
			this.columns45 = DeepCopyColumns(other.columns45);
			this.columns90 = DeepCopyColumns(other.columns90);
			this.columns135 = DeepCopyColumns(other.columns135);
			this.columnsSmoothed0 = DeepCopyColumns(other.columnsSmoothed0);
			this.columnsSmoothed45 = DeepCopyColumns(other.columnsSmoothed45);
			this.columnsSmoothed90 = DeepCopyColumns(other.columnsSmoothed90);
			this.columnsSmoothed135 = DeepCopyColumns(other.columnsSmoothed135);
			this.timeToRecognize = other.timeToRecognize;
			this.recognizedDistance = other.recognizedDistance;
			this.actualShapeID = other.actualShapeID;
			this.subjectID = other.subjectID;
			this.exampleNumber = other.exampleNumber;
			this.recognizedShapeID = other.recognizedShapeID;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public ShapeInstance(IEnumerable<RecognizerStroke> strokes,
			short actualShapeID, short subjectID, short exampleNumber,
			short columnCount, short columnCellCount,
			int intersectionGaussianWidth, bool toRecognize)
		{
			Initialize(strokes, actualShapeID, subjectID, exampleNumber,
				columnCount, columnCellCount, intersectionGaussianWidth, 
				toRecognize);
		}

		/// <summary>
		/// Initialize the state of this shape instance.
		/// </summary>
		private void Initialize(IEnumerable<RecognizerStroke> strokes, 
			short actualShapeID, short subjectID, short exampleNumber,
			short columnCount, short columnCellCount,
			int intersectionGaussianWidth, bool toRecognize)
		{
			this.strokesUnnormalized = strokes;
			this.strokes0 = DeepCopy(this.strokesUnnormalized);
			this.strokes45 = null;
			this.strokes90 = null;
			this.strokes135 = null;
			this.actualShapeID = actualShapeID;
			this.subjectID = subjectID;
			this.exampleNumber = exampleNumber;
			this.recognizedShapeID = -1;
			this.timeToRecognize = Double.NaN;
			this.recognizedDistance = Double.NaN;
			this.columns0 = null;
			this.columns45 = null;
			this.columns90 = null;
			this.columns135 = null;
			this.columnsSmoothed0 = null;
			this.columnsSmoothed45 = null;
			this.columnsSmoothed90 = null;
			this.columnsSmoothed135 = null;

			CalculateColumnValues(columnCount, columnCellCount);

			if (toRecognize)
			{
				CreateSmoothedValues(columnCount, columnCellCount,
					intersectionGaussianWidth);
			}
		}

		public void CreateSmoothedValues(short columnCount,
			short columnCellCount, int intersectionGaussianWidth)
		{
			short columnSmoothingCount = (short)((intersectionGaussianWidth - 1) / 2);
			columnsSmoothed0 = TranslateColumns(columns0, columnCellCount);
			columnsSmoothed45 = TranslateColumns(columns45, columnCellCount);
			columnsSmoothed90 = TranslateColumns(columns90, columnCellCount);
			columnsSmoothed135 = TranslateColumns(columns135, columnCellCount);
			columnsSmoothed0 = Template.SmoothColumns(columnsSmoothed0, columnCellCount, columnSmoothingCount);
			columnsSmoothed45 = Template.SmoothColumns(columnsSmoothed45, columnCellCount, columnSmoothingCount);
			columnsSmoothed90 = Template.SmoothColumns(columnsSmoothed90, columnCellCount, columnSmoothingCount);
			columnsSmoothed135 = Template.SmoothColumns(columnsSmoothed135, columnCellCount, columnSmoothingCount);
		}

		/// <summary>
		/// Calculate the column values of this shape instance.
		/// </summary>
		protected void CalculateColumnValues(short columnCount, 
			short columnCellCount)
		{
			columns0 = new Dictionary<int, double>[columnCount];
			columns45 = new Dictionary<int, double>[columnCount];
			columns90 = new Dictionary<int, double>[columnCount];
			columns135 = new Dictionary<int, double>[columnCount];
			double time0 = MainWindow.GetTimestampInMillis();////////////////////////////////////////////////////////////////TODO: REMOVE ME
			// ---------- Normalize the 0 degree strokes ---------- //

			Rect boundingBox;
			double minX, maxX, minY, maxY;

			minX = Double.MaxValue;
			maxX = Double.MinValue;
			minY = Double.MaxValue;
			maxY = Double.MinValue;

			// Determine the min and max coordinates of this shape instance
			foreach (RecognizerStroke stroke in strokes0)
			{
				boundingBox = stroke.BoundingBox;
				minX = Math.Min(minX, boundingBox.Left);
				minY = Math.Min(minY, boundingBox.Top);
				maxX = Math.Max(maxX, boundingBox.Right);
				maxY = Math.Max(maxY, boundingBox.Bottom);
			}

			NormalizeStrokes(strokes0, minX, minY, maxX, maxY);

			// ---------- Rotate then normalize the 45 degree strokes ---------- //
			double time1 = MainWindow.GetTimestampInMillis();////////////////////////////////////////////////////////////////TODO: REMOVE ME
			double delta1 = time1 - time0;////////////////////////////////////////////////////////////////TODO: REMOVE ME
			strokes45 =
				RotateStrokes(strokes0, Params.ONE_QUARTER_PI);
			double time2 = MainWindow.GetTimestampInMillis();////////////////////////////////////////////////////////////////TODO: REMOVE ME
			double delta2 = time2 - time1;////////////////////////////////////////////////////////////////TODO: REMOVE ME
			minX = Double.MaxValue;
			maxX = Double.MinValue;
			minY = Double.MaxValue;
			maxY = Double.MinValue;

			// Determine the min and max coordinates of this shape instance
			foreach (RecognizerStroke stroke in strokes45)
			{
				boundingBox = stroke.BoundingBox;
				minX = Math.Min(minX, boundingBox.Left);
				minY = Math.Min(minY, boundingBox.Top);
				maxX = Math.Max(maxX, boundingBox.Right);
				maxY = Math.Max(maxY, boundingBox.Bottom);
			}

			NormalizeStrokes(strokes45, minX, minY, maxX, maxY);
			double time3 = MainWindow.GetTimestampInMillis();////////////////////////////////////////////////////////////////TODO: REMOVE ME
			double delta3 = time3 - time2;////////////////////////////////////////////////////////////////TODO: REMOVE ME
			// ---------- Compute the columns ---------- //

			ComputeHorizontalAndVerticalColumns(
				strokes0, columnCount, columnCellCount, columns0, columns90);
			ComputeHorizontalAndVerticalColumns(
				strokes45, columnCount, columnCellCount, columns45, columns135);
			double time4 = MainWindow.GetTimestampInMillis();////////////////////////////////////////////////////////////////TODO: REMOVE ME
			double delta4 = time4 - time3;////////////////////////////////////////////////////////////////TODO: REMOVE ME
			double total = delta1 + delta2 + delta3 + delta4;
		}

		/// <summary>
		/// Fill in the given horizontal and vertical "column" values 
		/// according to the given strokes.
		/// </summary>
		private static void ComputeHorizontalAndVerticalColumns(
			IEnumerable<RecognizerStroke> strokes, 
			short columnCount, short columnCellCount, 
			Dictionary<int, double>[] columnsHorizontal, 
			Dictionary<int, double>[] columnsVertical)
		{
			InstantiateColumnValues(columnsHorizontal);
			InstantiateColumnValues(columnsVertical);

			short columnIndexHorizontal, cellIndexHorizontal,
				columnIndexVertical, cellIndexVertical;
			double angle, spreadHorizontal, spreadVertical,
				directionalValueHorizontal, directionalValueVertical;
			RecognizerPoint point;
			int stopIndex;

			foreach (RecognizerStroke stroke in strokes)
			{
				// Get the normalized points
				RecognizerPoint[] points = stroke.RecognizerPoints;

				directionalValueHorizontal = 0;
				directionalValueVertical = 0;

				stopIndex = points.Length - Params.END_POINT_THROW_AWAY_COUNT;
				for (int i = Params.END_POINT_THROW_AWAY_COUNT; i < stopIndex; ++i)
				{
					point = points[i];

					// Calculate the pixel on which this point lies

					columnIndexHorizontal = Math.Min((short)(point.X * columnCount), (short)(columnCount - 1));
					cellIndexHorizontal = Math.Min((short)(point.Y * columnCellCount), (short)(columnCellCount - 1));

					columnIndexVertical = Math.Min((short)(columnCount - point.Y * columnCount), (short)(columnCount - 1));	// When rotating 90 deg ccw, the pos y dir becomes the neg x dir
					cellIndexVertical = Math.Min((short)(point.X * columnCellCount), (short)(columnCellCount - 1));

					// Calculate the directional values for this point

					angle = point.Angle;
					spreadHorizontal = StrokePreProcessing.GetAngleOrOppositeSpread(0.0, angle);
					spreadVertical = StrokePreProcessing.GetAngleOrOppositeSpread(Params.HALF_PI, angle);

					// ---------- DEBUGGING ---------- //
					//
					//if (spread0 > Params.HALF_PI || spread0 < 0 ||
					//    spread45 > Params.HALF_PI || spread45 < 0 ||
					//    spread90 > Params.HALF_PI || spread90 < 0 ||
					//    spread135 > Params.HALF_PI || spread135 < 0)
					//{
					//    Console.WriteLine("ShapeInstance.CalculateBitmapProperties(): DEBUG BREAK");
					//}
					//
					// ---------- DEBUGGING ---------- //

					if (spreadHorizontal < Params.ONE_QUARTER_PI)
					{
						directionalValueHorizontal = (Params.ONE_QUARTER_PI - spreadHorizontal) * Params.ONE_OVER_ONE_QUARTER_PI;
					}
					else
					{
						directionalValueHorizontal = 0;
					}

					if (spreadVertical < Params.ONE_QUARTER_PI)
					{
						directionalValueVertical = (Params.ONE_QUARTER_PI - spreadVertical) * Params.ONE_OVER_ONE_QUARTER_PI;
					}
					else
					{
						directionalValueVertical = 0;
					}

					// Save the new directional values, unless the previous 
					// directional values for these columns and cells are 
					// larger

					if (columnsHorizontal[columnIndexHorizontal].ContainsKey(cellIndexHorizontal))
					{
						columnsHorizontal[columnIndexHorizontal][cellIndexHorizontal] = Math.Max(
							columnsHorizontal[columnIndexHorizontal][cellIndexHorizontal],
							directionalValueHorizontal);
					}
					else
					{
						columnsHorizontal[columnIndexHorizontal][cellIndexHorizontal] = directionalValueHorizontal;
					}

					if (columnsVertical[columnIndexVertical].ContainsKey(cellIndexVertical))
					{
						columnsVertical[columnIndexVertical][cellIndexVertical] = Math.Max(
							columnsVertical[columnIndexVertical][cellIndexVertical],
							directionalValueVertical);
					}
					else
					{
						columnsVertical[columnIndexVertical][cellIndexVertical] = directionalValueVertical;
					}
				}
			}
		}

		private static void NormalizeStrokes(IEnumerable<RecognizerStroke> strokes, 
			double minX, double minY, double maxX, double maxY)
		{
			foreach (RecognizerStroke stroke in strokes)
			{
				stroke.Normalize(minX, minY, maxX, maxY);
			}
		}

		public void ComputeStrokes90And135()
		{
			// Precondition: the 0 and 45 degree strokes should be normalized 
			//				 and centered between 0 and 1

			strokes90 = DeepCopy(strokes0);
			strokes135 = DeepCopy(strokes45);

			RecognizerPoint[] points;
			double temp;

			foreach (RecognizerStroke stroke in strokes90)
			{
				points = stroke.RecognizerPoints;
				foreach (RecognizerPoint point in points)
				{
					temp = point.X;
					point.X = 1 - point.Y;
					point.Y = temp;
				}
			}

			foreach (RecognizerStroke stroke in strokes135)
			{
				points = stroke.RecognizerPoints;
				foreach (RecognizerPoint point in points)
				{
					temp = point.X;
					point.X = 1 - point.Y;
					point.Y = temp;
				}
			}
		}

		/// <summary>
		/// Rotate the given strokes the given amount of radians about their 
		/// cumulative centroid in the CCW direction.
		/// </summary>
		public static List<RecognizerStroke> RotateStrokes(IEnumerable<RecognizerStroke> strokes, double rotationRads)
		{
			// ---------- Compute the centroid of the shape instance ---------- //

			double centroidX = 0;
			double centroidY = 0;
			int totalPointCount = 0;

			foreach (RecognizerStroke stroke in strokes)
			{
				totalPointCount += stroke.PointCount;
			}

			foreach (RecognizerStroke stroke in strokes)
			{
				centroidX += stroke.Centroid.X * stroke.PointCount / totalPointCount;
				centroidY += stroke.Centroid.Y * stroke.PointCount / totalPointCount;
			}

			// ---------- Rotate all points ---------- //

			List<RecognizerStroke> rotatedStrokes = new List<RecognizerStroke>();
			RecognizerStroke rotatedStroke;
			RecognizerPoint[] rotatedPoints;
			double temp;
			double sinTheta = Math.Sin(rotationRads);
			double cosTheta = Math.Cos(rotationRads);

			foreach (RecognizerStroke stroke in strokes)
			{
				rotatedStroke = new RecognizerStroke(stroke);
				rotatedPoints = rotatedStroke.RecognizerPoints;
				rotatedStrokes.Add(rotatedStroke);
				foreach (RecognizerPoint point in rotatedPoints)
				{
					temp = (point.X - centroidX) * sinTheta + (point.Y - centroidY) * cosTheta + centroidY;
					point.X = (point.X - centroidX) * cosTheta + (centroidY - point.Y) * sinTheta + centroidX;
					point.Y = temp;
					point.Angle += rotationRads;
				}
				rotatedStroke.CalculateStrokeProperties();
			}

			return rotatedStrokes;
		}

		public static Dictionary<int, double>[] DeepCopyColumns(Dictionary<int, double>[] oldColumns)
		{
			if (oldColumns != null)
			{
				Dictionary<int, double>[] newColumns = new Dictionary<int, double>[oldColumns.Length];
				for (int i = 0; i < oldColumns.Length; ++i)
				{
					newColumns[i] = new Dictionary<int, double>(oldColumns[i]);
				}
				return newColumns;
			}
			return null;
		}

		public static double[][] DeepCopyColumns(double[][] oldColumns)
		{
			if (oldColumns != null)
			{
				double[][] newColumns = new double[oldColumns.Length][];
				for (int i = 0; i < oldColumns.Length; ++i)
				{
					newColumns[i] = new double[oldColumns[i].Length];
					Array.Copy(oldColumns[i], newColumns[i], oldColumns[i].Length);
				}
				return newColumns;
			}
			return null;
		}

		public static double[][] TranslateColumns(
			Dictionary<int, double>[] oldColumns, short columnCellCount)
		{
			if (oldColumns != null)
			{
				double[][] newColumns = new double[oldColumns.Length][];
				for (int i = 0; i < oldColumns.Length; ++i)
				{
					newColumns[i] = new double[columnCellCount];
					foreach (KeyValuePair<int, double> pair in oldColumns[i])
					{
						newColumns[i][pair.Key] = pair.Value;
					}
				}
				return newColumns;
			}
			return null;
		}

		public static void InstantiateColumnValues(Dictionary<int, double>[] columns)
		{
			for (int i = 0; i < columns.Length; ++i)
			{
				columns[i] = new Dictionary<int, double>();
			}
		}

		public static List<RecognizerStroke> DeepCopy(IEnumerable<RecognizerStroke> oldStrokes)
		{
			if (oldStrokes != null)
			{
				List<RecognizerStroke> newStrokes = new List<RecognizerStroke>();
				foreach (RecognizerStroke stroke in oldStrokes)
				{
					newStrokes.Add(new RecognizerStroke(stroke));
				}
				return newStrokes;
			}
			return null;
		}

		#endregion

		#region DYNAMIC_MEMBERS

		public IEnumerable<RecognizerStroke> Strokes0
		{
			get { return strokes0; }
		}

		public IEnumerable<RecognizerStroke> Strokes45
		{
			get { return strokes45; }
		}

		public IEnumerable<RecognizerStroke> Strokes90
		{
			get { return strokes90; }
		}

		public IEnumerable<RecognizerStroke> Strokes135
		{
			get { return strokes135; }
		}

		public Dictionary<int, double>[] Columns0
		{
			get { return columns0; }
		}

		public Dictionary<int, double>[] Columns45
		{
			get { return columns45; }
		}

		public Dictionary<int, double>[] Columns90
		{
			get { return columns90; }
		}

		public Dictionary<int, double>[] Columns135
		{
			get { return columns135; }
		}

		public double[][] ColumnsSmoothed0
		{
			get { return columnsSmoothed0; }
		}

		public double[][] ColumnsSmoothed45
		{
			get { return columnsSmoothed45; }
		}

		public double[][] ColumnsSmoothed90
		{
			get { return columnsSmoothed90; }
		}

		public double[][] ColumnsSmoothed135
		{
			get { return columnsSmoothed135; }
		}

		public double TimeToRecognize
		{
			get { return timeToRecognize; }
			set { timeToRecognize = value; }
		}

		public double RecognizedDistance
		{
			get { return recognizedDistance; }
			set { recognizedDistance = value; }
		}

		public short ActualShapeID
		{
			get { return actualShapeID; }
		}

		public short SubjectID
		{
			get { return subjectID; }
		}

		public short ExampleNumber
		{
			get { return exampleNumber; }
		}

		public short RecognizedShapeID
		{
			get { return recognizedShapeID; }
			set { recognizedShapeID = value; }
		}

		#endregion

	}
}
