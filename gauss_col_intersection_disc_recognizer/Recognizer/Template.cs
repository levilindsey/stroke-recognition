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

		private double[][] columns0;
		private double[][] columns45;
		private double[][] columns90;
		private double[][] columns135;

		/// <summary>
		/// Constructor.
		/// </summary>
		public Template(Template other)
		{
			this.id = other.id;
			this.columns0 = ShapeInstance.DeepCopyColumns(other.columns0);
			this.columns45 = ShapeInstance.DeepCopyColumns(other.columns45);
			this.columns90 = ShapeInstance.DeepCopyColumns(other.columns90);
			this.columns135 = ShapeInstance.DeepCopyColumns(other.columns135);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public Template(short id, List<ShapeInstance> trainingInstances,
			short columnCount, short columnCellCount, 
			int bitmapSmoothingCount, double templateBoost)
		{
			Initialize(id, trainingInstances, columnCount, columnCellCount,
				bitmapSmoothingCount, templateBoost);
		}

		private void Initialize(short id,
			List<ShapeInstance> trainingInstances, short columnCount,
			short columnCellCount, int columnSmoothingCount, 
			double templateBoost)
		{
			this.id = id;
			CalculateColumnCellValues(trainingInstances, columnCount, 
				columnCellCount, templateBoost);
			columns0 = SmoothColumns(columns0, columnCellCount, columnSmoothingCount);
			columns45 = SmoothColumns(columns45, columnCellCount, columnSmoothingCount);
			columns90 = SmoothColumns(columns90, columnCellCount, columnSmoothingCount);
			columns135 = SmoothColumns(columns135, columnCellCount, columnSmoothingCount);
		}

		private void CalculateColumnCellValues(
			List<ShapeInstance> trainingInstances, short columnCount,
			short columnCellCount, double templateBoost)
		{
			this.columns0 = new double[columnCount][];
			this.columns45 = new double[columnCount][];
			this.columns90 = new double[columnCount][];
			this.columns135 = new double[columnCount][];
			InstantiateColumnValues(this.columns0, columnCellCount);
			InstantiateColumnValues(this.columns45, columnCellCount);
			InstantiateColumnValues(this.columns90, columnCellCount);
			InstantiateColumnValues(this.columns135, columnCellCount);

			Dictionary<int, double> column0, column45, column90, column135;

			foreach (ShapeInstance trainingInstance in trainingInstances)
			{
				for (int i = 0; i < columnCount; i++)
				{
					column0 = trainingInstance.Columns0[i];
					column45 = trainingInstance.Columns45[i];
					column90 = trainingInstance.Columns90[i];
					column135 = trainingInstance.Columns135[i];

					foreach (KeyValuePair<int, double> pair in column0)
					{
						this.columns0[i][pair.Key] += pair.Value;
					}
					foreach (KeyValuePair<int, double> pair in column45)
					{
						this.columns45[i][pair.Key] += pair.Value;
					}
					foreach (KeyValuePair<int, double> pair in column90)
					{
						this.columns90[i][pair.Key] += pair.Value;
					}
					foreach (KeyValuePair<int, double> pair in column135)
					{
						this.columns135[i][pair.Key] += pair.Value;
					}
				}
			}

			double normalizeAndBoost = templateBoost / trainingInstances.Count;

			for (int i = 0; i < columnCount; ++i)
			{
				for (int j = 0; j < columnCellCount; ++j)
				{
					this.columns0[i][j] *= normalizeAndBoost;
					this.columns45[i][j] *= normalizeAndBoost;
					this.columns90[i][j] *= normalizeAndBoost;
					this.columns135[i][j] *= normalizeAndBoost;
				}
			}
		}

		public static double[][] SmoothColumns(double[][] oldColumns, 
			short columnCellCount, int smoothingIterationCount)
		{
			double[][] smoothedColumns = oldColumns;

			for (int i = 0; i < smoothingIterationCount; ++i)
			{
				smoothedColumns = CalculateSmoothedColumnValues(
					smoothedColumns, columnCellCount);
			}

			return smoothedColumns;
		}

		private static double[][] CalculateSmoothedColumnValues(
			double[][] oldColumns, short columnCellCount)
		{
			double[][] smoothedColumns = new double[oldColumns.Length][];
			InstantiateColumnValues(smoothedColumns, columnCellCount);

			for (int i = 0; i < smoothedColumns.Length; ++i)
			{
				smoothedColumns[i][0] = StrokePreProcessing.GetSmoothedValue(oldColumns[i][1], oldColumns[i][0]);

				for (int j = 1; j < columnCellCount - 1; ++j)
				{
					smoothedColumns[i][j] = StrokePreProcessing.GetSmoothedValue(oldColumns[i][j - 1], oldColumns[i][j], oldColumns[i][j + 1]);
				}

				smoothedColumns[i][columnCellCount - 1] = StrokePreProcessing.GetSmoothedValue(oldColumns[i][columnCellCount - 2], oldColumns[i][columnCellCount - 1]);
			}

			return smoothedColumns;
		}

		private static void InstantiateColumnValues(double[][] columns, 
			short columnCellCount)
		{
			for (int i = 0; i < columns.Length; ++i)
			{
				columns[i] = new double[columnCellCount];
			}
		}

		public short ID
		{
			get { return id; }
		}

		public double[][] Columns0
		{
			get { return columns0; }
		}

		public double[][] Columns45
		{
			get { return columns45; }
		}

		public double[][] Columns90
		{
			get { return columns90; }
		}

		public double[][] Columns135
		{
			get { return columns135; }
		}
	}
}
