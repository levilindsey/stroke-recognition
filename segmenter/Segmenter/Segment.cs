/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Shapes;

namespace StrokeCollector
{
	public abstract class Segment : Drawable
	{
		protected int strokeStartPointIndex;
		protected int strokeEndPointIndex;

		protected double errorOfFit;

		/// <summary>
		/// Create this Segment's shape to draw on the canvas.
		/// </summary>
		protected abstract void CreateShape();

		/// <summary>
		/// Return the Shape for drawing on the Canvas.
		/// </summary>
		public abstract Shape GetShape();

		public abstract String GetDataFileEntry(SaveValue saveValue, 
			SegmentationAlgorithm segmentationAlgorithm);

		public int StrokeStartPointIndex
		{
			get { return strokeStartPointIndex; }
		}

		public int StrokeEndPointIndex
		{
			get { return strokeEndPointIndex; }
		}

		public double ErrorOfFit
		{
			get { return errorOfFit; }
		}
	}
}
