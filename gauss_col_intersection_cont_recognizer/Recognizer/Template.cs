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

		// (mean, standard deviation, amplitude)
		private List<Tuple<float, float, float>>[] columns0;
		private List<Tuple<float, float, float>>[] columns45;
		private List<Tuple<float, float, float>>[] columns90;
		private List<Tuple<float, float, float>>[] columns135;

		/// <summary>
		/// Constructor.
		/// </summary>
		public Template(Template other)
		{
			this.id = other.id;
			this.columns0 = DeepCopyColumns(other.columns0);
			this.columns45 = DeepCopyColumns(other.columns45);
			this.columns90 = DeepCopyColumns(other.columns90);
			this.columns135 = DeepCopyColumns(other.columns135);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public Template(short id, List<ShapeInstance> trainingInstances,
			short columnCount)
		{
			Initialize(id, trainingInstances, columnCount);
		}

		private void Initialize(short id,
			List<ShapeInstance> trainingInstances, short columnCount)
		{
			this.id = id;
			CalculateColumnCellValues(trainingInstances, columnCount);
		}

		private void CalculateColumnCellValues(
			List<ShapeInstance> trainingInstances, short columnCount)
		{
			this.columns0 = new List<Tuple<float, float, float>>[columnCount];
			this.columns45 = new List<Tuple<float, float, float>>[columnCount];
			this.columns90 = new List<Tuple<float, float, float>>[columnCount];
			this.columns135 = new List<Tuple<float, float, float>>[columnCount];
			InstantiateColumnValues(this.columns0);
			InstantiateColumnValues(this.columns45);
			InstantiateColumnValues(this.columns90);
			InstantiateColumnValues(this.columns135);

			ComputeIntersectionCounts();
			ComputeMeans();
			ComputeStandardDevs();
			ComputeAmpltitudes();

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

		private static void InstantiateColumnValues(List<Tuple<float, float, float>>[] columns)
		{
			for (int i = 0; i < columns.Length; ++i)
			{
				columns[i] = new List<Tuple<float, float, float>>();
			}
		}

		private static List<Tuple<float, float, float>>[] DeepCopyColumns(List<Tuple<float, float, float>>[] oldColumns)
		{
			if (oldColumns != null)
			{
				List<Tuple<float, float, float>>[] newColumns = new List<Tuple<float, float, float>>[oldColumns.Length];
				for (int i = 0; i < oldColumns.Length; ++i)
				{
					newColumns[i] = new List<Tuple<float, float, float>>(oldColumns[i]);
				}
				return newColumns;
			}
			return null;
		}

		public short ID
		{
			get { return id; }
		}

		public List<Tuple<float, float, float>>[] Columns0
		{
			get { return columns0; }
		}

		public List<Tuple<float, float, float>>[] Columns45
		{
			get { return columns45; }
		}

		public List<Tuple<float, float, float>>[] Columns90
		{
			get { return columns90; }
		}

		public List<Tuple<float, float, float>>[] Columns135
		{
			get { return columns135; }
		}
	}
}
