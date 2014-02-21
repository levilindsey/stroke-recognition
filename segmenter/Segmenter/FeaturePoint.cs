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
	class FeaturePoint : Drawable
	{
		private int index;
		private double x;
		private double y;

		private Ellipse ellipse;

		/// <summary>
		/// Constructor.
		/// </summary>
		public FeaturePoint(double x, double y, int index)
		{
			this.index = index;
			this.x = x;
			this.y = y;

			ellipse = CreateShape(x, y);
		}

		public double Index
		{
			get { return index; }
		}

		public double X
		{
			get { return x; }
		}

		public double Y
		{
			get { return y; }
		}

		/// <summary>
		/// Return the Shape for drawing on the Canvas.
		/// </summary>
		public Shape GetShape()
		{
			return ellipse;
		}

		/// <summary>
		/// Create this FeaturePoint's Shape for drawing on the canvas.
		/// </summary>
		private static Ellipse CreateShape(double x, double y)
		{
			Ellipse ellipse = new Ellipse();

			double marginX = x - Params.FEATURE_POINT_THICKNESS - Params.FEATURE_POINT_RADIUS;
			double marginY = y - Params.FEATURE_POINT_THICKNESS - Params.FEATURE_POINT_RADIUS;

			ellipse.Margin = new Thickness(marginX, marginY, 0, 0);
			ellipse.Width = Params.FEATURE_POINT_RADIUS * 2;
			ellipse.Height = Params.FEATURE_POINT_RADIUS * 2;
			ellipse.Stroke = Params.FEATURE_POINT_BRUSH;
			ellipse.StrokeThickness = Params.FEATURE_POINT_THICKNESS;

			return ellipse;
		}

		public String GetDataFileEntry(SaveValue saveValue,
			SegmentationAlgorithm segmentationAlgorithm)
		{
			return "" +
				index + Params.INTRA_POINT_SAVE_DELIMITER +
				x + Params.INTRA_POINT_SAVE_DELIMITER +
				y + Params.INTRA_POINT_SAVE_DELIMITER;
		}
	}
}
