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

		private DrawablePoint[] drawablePoints;

		public DrawableStroke()
		{
			Initialize(null, -1);
		}

		public DrawableStroke(IEnumerable<DrawablePoint> drawablePoints,
			int angleSmoothCount)
		{
			Initialize(drawablePoints, angleSmoothCount);
		}

		private void Initialize(IEnumerable<DrawablePoint> drawablePoints, 
			int angleSmoothCount)
		{
			this.drawablePoints = StrokePreProcessing.RemoveDuplicatePoints(DrawablePoint.DeepCopy(drawablePoints));
			base.Initialize(drawablePoints, angleSmoothCount);
		}

		public void SetPointLineColors()
		{
			for (int i = 0; i < drawablePoints.Length; i++)
			{
				drawablePoints[i].SetColorAndWidth(false);
			}
		}

		public DrawablePoint[] DrawablePoints
		{
			get { return drawablePoints; }
		}

		public double Timestamp
		{
			get { return drawablePoints[0].Timestamp; }
		}

	}
}
