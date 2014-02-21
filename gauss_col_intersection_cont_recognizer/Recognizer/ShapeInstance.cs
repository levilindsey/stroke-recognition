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

		// (mean, amplitude)
		private List<Tuple<float, float>>[] columns0;
		private List<Tuple<float, float>>[] columns45;
		private List<Tuple<float, float>>[] columns90;
		private List<Tuple<float, float>>[] columns135;

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
			short columnCount, short columnCellCount)
		{
			Initialize(strokes, actualShapeID, subjectID, exampleNumber,
				columnCount, columnCellCount);
		}

		/// <summary>
		/// Initialize the state of this shape instance.
		/// </summary>
		private void Initialize(IEnumerable<RecognizerStroke> strokes, 
			short actualShapeID, short subjectID, short exampleNumber,
			short columnCount, short columnCellCount)
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

			CalculateColumnValues(columnCount, columnCellCount);
		}

		/// <summary>
		/// Calculate the column values of this shape instance.
		/// </summary>
		protected void CalculateColumnValues(short columnCount, 
			short columnCellCount)
		{
			columns0 = new List<Tuple<float, float>>[columnCount];
			columns45 = new List<Tuple<float, float>>[columnCount];
			columns90 = new List<Tuple<float, float>>[columnCount];
			columns135 = new List<Tuple<float, float>>[columnCount];

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

			strokes45 =
				RotateStrokes(strokes0, Params.ONE_QUARTER_PI);

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

			// ---------- Compute the columns ---------- //

			ComputeHorizontalAndVerticalColumns(
				strokes0, columnCount, columnCellCount, columns0, columns90);
			ComputeHorizontalAndVerticalColumns(
				strokes45, columnCount, columnCellCount, columns45, columns135);
		}

		/// <summary>
		/// Fill in the given horizontal and vertical "column" values 
		/// according to the given strokes.
		/// </summary>
		private static void ComputeHorizontalAndVerticalColumns(
			IEnumerable<RecognizerStroke> strokes, short columnCount, 
			List<Tuple<float, float>>[] columnsHorizontal,
			List<Tuple<float, float>>[] columnsVertical)
		{
			InstantiateColumnValues(columnsHorizontal);
			InstantiateColumnValues(columnsVertical);

			double angle, angleSpread, dx, dy, cx, slope;
			float currIntersectionX, currIntersectionY, prevIntersectionY, directionalIntensity;
			int prevColumnIndex, currColumnIndex, lowerColumnIndex, upperColumnIndex, prevIntersectionPrePointIndex, prevIntersectionPreColumnIndex, prevPointIndex;
			RecognizerPoint prevPoint, currPoint, lowerXPoint, upperXPoint, prevBorderIntersection, currBorderIntersection;
			RecognizerPoint[] points;

			double colWidth = 1.0 / columnCount;

			foreach (RecognizerStroke stroke in strokes)
			{
				points = stroke.RecognizerPoints;

				// Get location of first column border intersection (the fencepost problem)

				currColumnIndex = (int)(points[0].X / colWidth);
				int i = 1;

				do
				{
					prevColumnIndex = currColumnIndex;
					currColumnIndex = (int)(points[i].X / colWidth);
					i++;
				}
				while (currColumnIndex == prevColumnIndex && i < points.Length);
				
				prevIntersectionPrePointIndex = i - 1;
				prevIntersectionPreColumnIndex = prevColumnIndex;
				prevPointIndex = prevIntersectionPrePointIndex;
				prevPoint = points[prevPointIndex];
				ee;
				// Find each of the next column border intersections and compute the appropriate intersection values
				for (; i < points.Length; ++i)
				{
					currPoint = points[i];

					// Determine whether these two adjacent points cross a column border line
					currColumnIndex = (int)(currPoint.X / colWidth);
					if (prevColumnIndex != currColumnIndex)
					{
						// Determine whether this new column border intersection is not over the same border as the previous intersection
						if (prevIntersectionPreColumnIndex != currColumnIndex)
						{
							// Determine which point lies further to the left
							if (prevPoint.X < currPoint.X)
							{
								lowerXPoint = prevPoint;
								upperXPoint = currPoint;
							}
							else
							{
								lowerXPoint = currPoint;
								upperXPoint = prevPoint;
							}

							dx = upperXPoint.X - lowerXPoint.X;
							dy = upperXPoint.Y - lowerXPoint.Y;
							slope = dy / dx;
							ee;GetColumnBorderIntersection(, , );
							// We only care about column intersections with |angle| < 45 degrees
							if (slope < 1 && slope > -1)
							{
								// Determine which column lies further to the left
								if (prevColumnIndex < currColumnIndex)
								{
									lowerColumnIndex = prevColumnIndex;
									upperColumnIndex = currColumnIndex;
								}
								else
								{
									lowerColumnIndex = currColumnIndex;
									upperColumnIndex = prevColumnIndex;
								}
								ee;
								// It is possible for these two adjacent points to cross more than one column border
								while (lowerColumnIndex < upperColumnIndex)
								{
									// Compute the location of the intersection (across the center of the column)
									currIntersectionX = (float)((lowerColumnIndex + 0.5) * colWidth);
									cx = currIntersectionX - lowerXPoint.X;
									currIntersectionY = (float)(lowerXPoint.Y + cx * slope);

									// Compute the angle of the intersection
									angle = StrokePreProcessing.GetAngle(lowerXPoint, upperXPoint, true);
									angleSpread = StrokePreProcessing.GetAngleOrOppositeSpread(0.0, angle);

									if (angleSpread < Params.ONE_QUARTER_PI)
									{
										directionalIntensity = (float)((Params.ONE_QUARTER_PI - angleSpread) * Params.ONE_OVER_ONE_QUARTER_PI);
									}
									else
									{
										directionalIntensity = 0.0f;
									}

									columnsHorizontal[lowerColumnIndex].Add(new Tuple<float, float>(currIntersectionY, directionalIntensity));

									++lowerColumnIndex;
								}
							}
						}

						prevIntersectionPrePointIndex = prevPointIndex;
						prevIntersectionPreColumnIndex = prevColumnIndex;
						prevColumnIndex = currColumnIndex;
					}

					prevPointIndex = i;
					prevPoint = currPoint;
				}
			}
		}

		private double GetColumnBorderIntersection(RecognizerPoint point1, RecognizerPoint point2, double columnBorderX)
		{
			RecognizerPoint lowerXPoint, upperXPoint;

			// Determine which point lies further to the left
			if (point1.X < point2.X)
			{
				lowerXPoint = point1;
				upperXPoint = point2;
			}
			else
			{
				lowerXPoint = point2;
				upperXPoint = point1;
			}

			double dx = upperXPoint.X - lowerXPoint.X;
			double dy = upperXPoint.Y - lowerXPoint.Y;
			double slope = dy / dx;

			// Compute the location of the intersection
			double cx = columnBorderX - lowerXPoint.X;
			double intersectionY = lowerXPoint.Y + cx * slope;

			return intersectionY;
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

		private static List<Tuple<float, float>>[] DeepCopyColumns(List<Tuple<float, float>>[] oldColumns)
		{
			if (oldColumns != null)
			{
				List<Tuple<float, float>>[] newColumns = new List<Tuple<float, float>>[oldColumns.Length];
				for (int i = 0; i < oldColumns.Length; ++i)
				{
					newColumns[i] = new List<Tuple<float, float>>(oldColumns[i]);
				}
				return newColumns;
			}
			return null;
		}

		public static void InstantiateColumnValues(List<Tuple<float, float>>[] columns)
		{
			for (int i = 0; i < columns.Length; ++i)
			{
				columns[i] = new List<Tuple<float, float>>();
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

		public List<Tuple<float, float>>[] Columns0
		{
			get { return columns0; }
		}

		public List<Tuple<float, float>>[] Columns45
		{
			get { return columns45; }
		}

		public List<Tuple<float, float>>[] Columns90
		{
			get { return columns90; }
		}

		public List<Tuple<float, float>>[] Columns135
		{
			get { return columns135; }
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
