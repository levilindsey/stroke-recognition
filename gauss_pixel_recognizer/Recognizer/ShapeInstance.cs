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

		protected IEnumerable<RecognizerStroke> strokes;

		protected short numOfPixelsContainingInk;

		private Dictionary<int, double> pixels0;
		private Dictionary<int, double> pixels45;
		private Dictionary<int, double> pixels90;
		private Dictionary<int, double> pixels135;

		private double timeToRecognize;
		private double recognizedDistance;
		private short actualShapeID;
		private short subjectID;
		private short exampleNumber;
		private short recognizedShapeID;

		private Rect boundingBox;

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
			this.strokes = new List<RecognizerStroke>(other.strokes);
			this.numOfPixelsContainingInk = other.numOfPixelsContainingInk;
			this.pixels0 = new Dictionary<int, double>(other.pixels0);
			this.pixels45 = new Dictionary<int, double>(other.pixels45);
			this.pixels90 = new Dictionary<int, double>(other.pixels90);
			this.pixels135 = new Dictionary<int, double>(other.pixels135);
			this.timeToRecognize = other.timeToRecognize;
			this.recognizedDistance = other.recognizedDistance;
			this.actualShapeID = other.actualShapeID;
			this.subjectID = other.subjectID;
			this.exampleNumber = other.exampleNumber;
			this.recognizedShapeID = other.recognizedShapeID;
			this.boundingBox = other.boundingBox;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public ShapeInstance(IEnumerable<RecognizerStroke> strokes, 
			short actualShapeID, short subjectID, short exampleNumber)
		{
			Initialize(strokes, actualShapeID, subjectID, exampleNumber);
		}

		/// <summary>
		/// Initialize the state of this Stroke.
		/// </summary>
		private void Initialize(IEnumerable<RecognizerStroke> strokes, 
			short actualShapeID, short subjectID, short exampleNumber)
		{
			this.strokes = strokes;
			this.actualShapeID = actualShapeID;
			this.subjectID = subjectID;
			this.exampleNumber = exampleNumber;
			this.recognizedShapeID = -1;
			this.timeToRecognize = Double.NaN;
			this.recognizedDistance = Double.NaN;

			CalculateBitmapProperties();
		}

		/// <summary>
		/// Calculate the bitmap properties of this stroke.
		/// </summary>
		protected void CalculateBitmapProperties()
		{
			numOfPixelsContainingInk = 0;

			pixels0 = new Dictionary<int, double>();
			pixels45 = new Dictionary<int, double>();
			pixels90 = new Dictionary<int, double>();
			pixels135 = new Dictionary<int, double>();

			short pixelX, pixelY, bitmapIndex;
			double minX, maxX, minY, maxY, width, height, angle, spread0, 
				spread45, spread90, spread135, directionalValue0, 
				directionalValue45, directionalValue90, directionalValue135;

			minX = Double.MaxValue;
			maxX = Double.MinValue;
			minY = Double.MaxValue;
			maxY = Double.MinValue;

			// Determine the min and max coordinates of this shape instance
			foreach (RecognizerStroke stroke in strokes)
			{
				boundingBox = stroke.BoundingBox;
				minX = Math.Min(minX, boundingBox.Left);
				minY = Math.Min(minY, boundingBox.Top);
				maxX = Math.Max(maxX, boundingBox.Right);
				maxY = Math.Max(maxY, boundingBox.Bottom);
			}

			width = maxX - minX;
			height = maxY - minY;

			boundingBox = new Rect(minX, minY, width, height);

			RecognizerPoint point;
			int stopIndex;

			foreach (RecognizerStroke stroke in strokes)
			{
				// Get the normalized points
				RecognizerPoint[] points = stroke.GetNormalizedRecognizerPoints(minX, minY, maxX, maxY);

				bitmapIndex = -1;
				directionalValue0 = 0;
				directionalValue45 = 0;
				directionalValue90 = 0;
				directionalValue135 = 0;

				stopIndex = points.Length - Params.END_POINT_THROW_AWAY_COUNT;
				for (int i = Params.END_POINT_THROW_AWAY_COUNT; i < stopIndex; ++i)
				{
					point = points[i];

					// Calculate the pixel on which this point lies
					pixelX = Math.Min((short)(point.X * MainWindow.TemplateSideLength), (short)(MainWindow.TemplateSideLength - 1));
					pixelY = Math.Min((short)(point.Y * MainWindow.TemplateSideLength), (short)(MainWindow.TemplateSideLength - 1));
					bitmapIndex = (short)(pixelY * MainWindow.TemplateSideLength + pixelX);

					// Calculate each of the four directional values for this point
					angle = point.Angle;
					spread0 = StrokePreProcessing.GetAngleOrOppositeSpread(0.0, angle);
					spread45 = StrokePreProcessing.GetAngleOrOppositeSpread(Params.ONE_QUARTER_PI, angle);
					spread90 = StrokePreProcessing.GetAngleOrOppositeSpread(Params.HALF_PI, angle);
					spread135 = StrokePreProcessing.GetAngleOrOppositeSpread(Params.THREE_QUARTERS_PI, angle);

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

					if (spread0 < Params.ONE_QUARTER_PI)
					{
						directionalValue0 = (Params.ONE_QUARTER_PI - spread0) * Params.ONE_OVER_ONE_QUARTER_PI;
					}
					else
					{
						directionalValue0 = 0;
					}

					if (spread45 < Params.ONE_QUARTER_PI)
					{
						directionalValue45 = (Params.ONE_QUARTER_PI - spread45) * Params.ONE_OVER_ONE_QUARTER_PI;
					}
					else
					{
						directionalValue45 = 0;
					}

					if (spread90 < Params.ONE_QUARTER_PI)
					{
						directionalValue90 = (Params.ONE_QUARTER_PI - spread90) * Params.ONE_OVER_ONE_QUARTER_PI;
					}
					else
					{
						directionalValue90 = 0;
					}

					if (spread135 < Params.ONE_QUARTER_PI)
					{
						directionalValue135 = (Params.ONE_QUARTER_PI - spread135) * Params.ONE_OVER_ONE_QUARTER_PI;
					}
					else
					{
						directionalValue135 = 0;
					}

					// Save the new directional values, unless the previous 
					// directional value for this pixel is larger
					if (pixels0.ContainsKey(bitmapIndex))
					{
						pixels0[bitmapIndex] = Math.Max(
							pixels0[bitmapIndex],
							directionalValue0);
					}
					else
					{
						pixels0[bitmapIndex] = directionalValue0;
					}
					if (pixels45.ContainsKey(bitmapIndex))
					{
						pixels45[bitmapIndex] = Math.Max(
							pixels45[bitmapIndex],
							directionalValue45);
					}
					else
					{
						pixels45[bitmapIndex] = directionalValue45;
					}
					if (pixels90.ContainsKey(bitmapIndex))
					{
						pixels90[bitmapIndex] = Math.Max(
							pixels90[bitmapIndex],
							directionalValue90);
					}
					else
					{
						pixels90[bitmapIndex] = directionalValue90;
					}
					if (pixels135.ContainsKey(bitmapIndex))
					{
						pixels135[bitmapIndex] = Math.Max(
							pixels135[bitmapIndex],
							directionalValue135);
					}
					else
					{
						pixels135[bitmapIndex] = directionalValue135;
					}
				}
			}

			// Count how many pixels contain ink
			numOfPixelsContainingInk = (short)pixels0.Count();
		}

		#endregion

		#region DYNAMIC_MEMBERS

		public IEnumerable<RecognizerStroke> Strokes
		{
			get { return strokes; }
		}

		public short NumOfPixelsContainingInk
		{
			get { return numOfPixelsContainingInk; }
		}

		public Dictionary<int, double> Pixels0
		{
			get { return pixels0; }
		}

		public Dictionary<int, double> Pixels45
		{
			get { return pixels45; }
		}

		public Dictionary<int, double> Pixels90
		{
			get { return pixels90; }
		}

		public Dictionary<int, double> Pixels135
		{
			get { return pixels135; }
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

		public Rect BoundingBox
		{
			get { return boundingBox; }
		}

		#endregion

	}
}
