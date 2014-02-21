/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StrokeCollector
{
	public class Template
	{
		private short id;
		private short avgNumOfPixelsContainingInk;

		private double[] bitmap0;
		private double[] bitmap45;
		private double[] bitmap90;
		private double[] bitmap135;

		/// <summary>
		/// Constructor.
		/// </summary>
		public Template(short id, List<ShapeInstance> trainingInstances, 
			int bitmapSmoothingCount)
		{
			Initialize(id, trainingInstances, bitmapSmoothingCount);
		}

		private void Initialize(short id, 
			List<ShapeInstance> trainingInstances, int bitmapSmoothingCount)
		{
			this.id = id;
			CalculateBitmapValues(trainingInstances);
			bitmap0 = StrokePreProcessing.SmoothBitmap(bitmap0, bitmapSmoothingCount);
			bitmap45 = StrokePreProcessing.SmoothBitmap(bitmap45, bitmapSmoothingCount);
			bitmap90 = StrokePreProcessing.SmoothBitmap(bitmap90, bitmapSmoothingCount);
			bitmap135 = StrokePreProcessing.SmoothBitmap(bitmap135, bitmapSmoothingCount);
		}

		private void CalculateBitmapValues(List<ShapeInstance> trainingInstances)
		{
			this.avgNumOfPixelsContainingInk = 0;

			this.bitmap0 = new double[MainWindow.TemplateSideLength * MainWindow.TemplateSideLength];
			this.bitmap45 = new double[MainWindow.TemplateSideLength * MainWindow.TemplateSideLength];
			this.bitmap90 = new double[MainWindow.TemplateSideLength * MainWindow.TemplateSideLength];
			this.bitmap135 = new double[MainWindow.TemplateSideLength * MainWindow.TemplateSideLength];

			foreach (ShapeInstance trainingInstance in trainingInstances)
			{
				foreach (KeyValuePair<int, double> pair in trainingInstance.Pixels0)
				{
					this.bitmap0[pair.Key] += pair.Value;
					this.bitmap45[pair.Key] += trainingInstance.Pixels45[pair.Key];
					this.bitmap90[pair.Key] += trainingInstance.Pixels90[pair.Key];
					this.bitmap135[pair.Key] += trainingInstance.Pixels135[pair.Key];
				}

				avgNumOfPixelsContainingInk += trainingInstance.NumOfPixelsContainingInk;
			}

			short trainingInstanceCount = (short)trainingInstances.Count;

			for (int i = 0; i < this.bitmap0.Length; i++)
			{
				this.bitmap0[i] /= trainingInstanceCount;
				this.bitmap45[i] /= trainingInstanceCount;
				this.bitmap90[i] /= trainingInstanceCount;
				this.bitmap135[i] /= trainingInstanceCount;
			}

			avgNumOfPixelsContainingInk /= trainingInstanceCount;
		}

		public short ID
		{
			get { return id; }
		}

		public short AvgNumOfPixelsContainingInk
		{
			get { return avgNumOfPixelsContainingInk; }
		}

		public double[] Bitmap0
		{
			get { return bitmap0; }
		}

		public double[] Bitmap45
		{
			get { return bitmap45; }
		}

		public double[] Bitmap90
		{
			get { return bitmap90; }
		}

		public double[] Bitmap135
		{
			get { return bitmap135; }
		}
	}
}
