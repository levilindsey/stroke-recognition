/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StrokeCollector
{
	public class RecognizerPoint
	{

		#region FIELD_DECLARATIONS

		protected double x;
		protected double y;
		protected double timestamp;
		protected double angle;

		#endregion

		#region CONSTRUCTORS

		/// <summary>
		/// Constructor.
		/// </summary>
		public RecognizerPoint(){}

		/// <summary>
		/// Constructor.
		/// </summary>
		public RecognizerPoint(RecognizerPoint point)
		{
			Initialize(point.X, point.Y, point.Timestamp, point.Angle);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public RecognizerPoint(double x, double y, double timestamp, double angle)
		{
			Initialize(x, y, timestamp, angle);
		}

		/// <summary>
		/// Initialize the state of this Point.
		/// </summary>
		private void Initialize(double x, double y, double timestamp, double angle)
		{
			this.x = x;
			this.y = y;
			this.timestamp = timestamp;
			this.angle = angle;
		}

		#endregion

		#region DYNAMIC_MEMBERS

		public double X
		{
			get { return x; }
			set { x = value; }
		}

		public double Y
		{
			get { return y; }
			set { y = value; }
		}

		public double Timestamp
		{
			get { return timestamp; }
		}

		// NOTE: this angle is in radians
		public double Angle
		{
			get { return angle; }
			set { angle = value; }
		}

		#endregion

		#region STATIC_UTILITY_METHODS

		public static DrawablePoint ParseStringToPoint(String[] tokens)
		{
			if (tokens != null && tokens.Length == 6)
			{
				double x = Convert.ToDouble(tokens[0]);
				double y = Convert.ToDouble(tokens[1]);
				int timestamp = Convert.ToInt32(tokens[5]);

				return new DrawablePoint(x, y, timestamp);
			}

			throw new FormatException("Incorrectly formatted Point String: " + tokens.ToString());
		}

		public static RecognizerPoint[] DeepCopy(IEnumerable<RecognizerPoint> oldPoints)
		{
			RecognizerPoint[] newPoints = new RecognizerPoint[oldPoints.Count()];
			int i = 0;

			foreach (RecognizerPoint oldPoint in oldPoints)
			{
				newPoints[i++] = new RecognizerPoint(oldPoint);
			}

			return newPoints;
		}

		#endregion

	}
}
