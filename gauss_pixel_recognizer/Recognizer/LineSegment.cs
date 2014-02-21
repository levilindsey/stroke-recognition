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
	class LineSegment : Segment
	{
		private double startX;
		private double startY;
		private double endX;
		private double endY;

		private Line line;

		/// <summary>
		/// Constructor.
		/// </summary>
		public LineSegment(int strokeStartPointIndex, int strokeEndPointIndex,
			double errorOfFit, double startX, double startY, double endX, double endY)
		{
			this.strokeStartPointIndex = strokeStartPointIndex;
			this.strokeEndPointIndex = strokeEndPointIndex;
			this.errorOfFit = errorOfFit;

			this.startX = startX;
			this.startY = startY;
			this.endX = endX;
			this.endY = endY;

			CreateShape();
		}

		/// <summary>
		/// Create this Segment's shape to draw on the canvas.
		/// </summary>
		protected override void CreateShape()
		{
			line = new Line();
			line.X1 = startX;
			line.Y1 = startY;
			line.X2 = endX;
			line.Y2 = endY;
			line.Stroke = Params.LINE_SEGMENT_BRUSH;
			line.StrokeThickness = Params.LINE_SEGMENT_THICKNESS;
		}

		public double StartX
		{
			get { return startX; }
		}

		public double StartY
		{
			get { return startY; }
		}

		public double EndX
		{
			get { return endX; }
		}

		public double EndY
		{
			get { return endY; }
		}

		/// <summary>
		/// Return the Shape for drawing on the Canvas.
		/// </summary>
		public override Shape GetShape()
		{
			return line;
		}

		public override String GetDataFileEntry(SaveValue saveValue,
		   SegmentationAlgorithm segmentationAlgorithm)
		{
			return "" + 
				startX + Params.INTRA_POINT_SAVE_DELIMITER + 
				startY + Params.INTRA_POINT_SAVE_DELIMITER + 
				endX + Params.INTRA_POINT_SAVE_DELIMITER + 
				endY + Params.INTRA_POINT_SAVE_DELIMITER +
				StrokePreProcessing.GetSlope(startX, startY, endX, endY) + 
					Params.INTRA_POINT_SAVE_DELIMITER +
				StrokePreProcessing.GetYIntercept(startX, startY, endX, endY);
		}
	}
}
