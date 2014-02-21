/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StrokeCollector
{
	class ArcSegment : Segment
	{
		private const double TWO_PI				= Math.PI * 2;

		private double centerX;
		private double centerY;
		private double radius;
		private double startAngle;
		private double endAngle;
		private bool isCW;

		private Polyline polyline;

		/// <summary>
		/// Constructor.
		/// </summary>
		public ArcSegment(int strokeStartPointIndex, int strokeEndPointIndex,
			double errorOfFit, double centerX, double centerY, double radius, 
			double startAngle, double endAngle, bool isCW)
		{
			this.strokeStartPointIndex = strokeStartPointIndex;
			this.strokeEndPointIndex = strokeEndPointIndex;
			this.errorOfFit = errorOfFit;

			this.centerX = centerX;
			this.centerY = centerY;
			this.radius = radius;
			this.startAngle = startAngle;
			this.endAngle = endAngle;
			this.isCW = isCW;

			CreateShape();
		}

		/// <summary>
		/// Create this Segment's shape to draw on the canvas.
		/// </summary>
		protected override void CreateShape()
		{
			PointCollection pointCollection = new PointCollection();
			System.Windows.Point point;
			double x, y, theta, deltaTheta, startAngle, endAngle;

			// Sanitize the angles against the possibility of zero-angle wrap-around
			startAngle = this.startAngle + TWO_PI;
			endAngle = this.endAngle + TWO_PI;

			// Determine where to start and end for creating the circle segments
			if (isCW)
			{
				if (endAngle > startAngle)
				{
					startAngle += TWO_PI;

					double tempAngle = startAngle;
					startAngle = endAngle;
					endAngle = tempAngle;
				}
				else	//endAngle < startAngle
				{
					double tempAngle = startAngle;
					startAngle = endAngle;
					endAngle = tempAngle;
				}
			}
			else	// !isCW
			{
				if (endAngle > startAngle)
				{
					// Do nothing
				}
				else	// endAngle < startAngle
				{
					endAngle += TWO_PI;
				}
			}

			// Compute the delta to use for the given circumference point spread
			double angleSpread = endAngle - startAngle;
			double numberOfLineSegments = TWO_PI * radius / Params.ARC_SEGMENT_POINT_SPACING;
			deltaTheta = angleSpread / numberOfLineSegments;

			// Create the points of the arc
			for (theta = startAngle; theta <= endAngle + Params.EPSILON; theta += deltaTheta)
			{
				x = centerX + radius * Math.Cos(theta);
				y = centerY + radius * Math.Sin(theta);
				point = new System.Windows.Point(x, y);
				pointCollection.Add(point);
			}

			// Create the Shape object for drawing
			polyline = new Polyline();
			polyline.Points = pointCollection;
			polyline.Stroke = Params.ARC_SEGMENT_BRUSH;
			polyline.StrokeThickness = Params.ARC_SEGMENT_THICKNESS;
		}

		public double CenterX
		{
			get { return centerX; }
		}

		public double CenterY
		{
			get { return centerY; }
		}

		public double Radius
		{
			get { return radius; }
		}

		public double StartAngle
		{
			get { return startAngle; }
		}

		public double EndAngle
		{
			get { return endAngle; }
		}

		public double AngleSpread
		{
			get {
				// Sanitize the angles against the possibility of zero-angle wrap-around
				double startAngle = (this.startAngle + TWO_PI);
				double endAngle = (this.endAngle + TWO_PI);
				if (isCW)
				{
					if (endAngle > startAngle)
					{
						startAngle += TWO_PI;
						double tempAngle = startAngle;
						startAngle = endAngle;
						endAngle = tempAngle;
					}
					else	//endAngle < startAngle
					{
						double tempAngle = startAngle;
						startAngle = endAngle;
						endAngle = tempAngle;
					}
				}
				else	// !isCW
				{
					if (endAngle > startAngle)
					{
						// Do nothing
					}
					else	// endAngle < startAngle
					{
						endAngle += TWO_PI;
					}
				}
				return endAngle - startAngle;
			}
		}

		public bool IsCW
		{
			get { return isCW; }
		}

		/// <summary>
		/// Return the Shape for drawing on the Canvas.
		/// </summary>
		public override Shape GetShape()
		{
			return polyline;
		}

		public override String GetDataFileEntry(SaveValue saveValue,
		   SegmentationAlgorithm segmentationAlgorithm)
		{
			return "" + 
				centerX + Params.INTRA_POINT_SAVE_DELIMITER +
				centerY + Params.INTRA_POINT_SAVE_DELIMITER +
				radius + Params.INTRA_POINT_SAVE_DELIMITER +
				startAngle + Params.INTRA_POINT_SAVE_DELIMITER +
				endAngle + Params.INTRA_POINT_SAVE_DELIMITER + 
				isCW + Params.INTRA_POINT_SAVE_DELIMITER;
		}
	}
}
