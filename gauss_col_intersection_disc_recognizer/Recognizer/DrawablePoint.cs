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

		private Line lineSegment;

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

			if (point.GetShape() != null)
			{
				Line otherLine = point.GetShape() as Line;
				lineSegment.X1 = otherLine.X1;
				lineSegment.Y1 = otherLine.Y1;
				lineSegment.X2 = otherLine.X2;
				lineSegment.Y2 = otherLine.Y2;
				lineSegment.Stroke = otherLine.Stroke;
				lineSegment.StrokeThickness = otherLine.StrokeThickness;
			}
			else
			{
				lineSegment = null;
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
			this.lineSegment = new Line();
			lineSegment.Stroke = Params.DEFAULT_STROKE_BRUSH;
			lineSegment.StrokeThickness = Params.DEFAULT_STROKE_THICKNESS;
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

		public String GetDataFileEntry()
		{
			return "" + 
				(int)x + Params.INTRA_POINT_SAVE_DELIMITER +
				(int)y + Params.INTRA_POINT_SAVE_DELIMITER +
				"0" + Params.INTRA_POINT_SAVE_DELIMITER +
				"0" + Params.INTRA_POINT_SAVE_DELIMITER +
				"0" + Params.INTRA_POINT_SAVE_DELIMITER +
				(long)timestamp;
		}

		/// <summary>
		/// Return the Shape for drawing on the Canvas.
		/// </summary>
		public Shape GetShape()
		{
			return lineSegment;
		}

		public void NullLine()
		{
			lineSegment = null;
		}

		public void SetColorAndWidth(bool isErasingStroke)
		{
			if (lineSegment != null)
			{
				if (!isErasingStroke)
				{
					lineSegment.Stroke = Params.DEFAULT_STROKE_BRUSH;
					lineSegment.StrokeThickness = Params.DEFAULT_STROKE_THICKNESS;
				}
				else
				{
					lineSegment.Stroke = Params.ERASE_STROKE_BRUSH;
					lineSegment.StrokeThickness = Params.ERASE_STROKE_THICKNESS;
				}
			}
		}

		public void SetLineState(DrawablePoint pointA, DrawablePoint pointB, 
			bool isErasingStroke)
		{
			lineSegment = new Line();
			lineSegment.X1 = pointA.X;
			lineSegment.Y1 = pointA.Y;
			lineSegment.X2 = pointB.X;
			lineSegment.Y2 = pointB.Y;

			if (!isErasingStroke)
			{
				lineSegment.Stroke = Params.DEFAULT_STROKE_BRUSH;
				lineSegment.StrokeThickness = Params.DEFAULT_STROKE_THICKNESS;
			}
			else
			{
				lineSegment.Stroke = Params.ERASE_STROKE_BRUSH;
				lineSegment.StrokeThickness = Params.ERASE_STROKE_THICKNESS;
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
