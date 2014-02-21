/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace StrokeCollector
{
	public class RecognizerStroke
	{

		#region FIELD_DECLARATIONS

		protected RecognizerPoint[] recognizerPoints;

		protected double pathLength;
		protected Rect boundingBox;
		protected RecognizerPoint centroid;

		#endregion

		#region CONSTRUCTORS

		/// <summary>
		/// Constructor.
		/// </summary>
		public RecognizerStroke() { }

		/// <summary>
		/// Constructor.
		/// </summary>
		public RecognizerStroke(RecognizerStroke other)
		{
			this.recognizerPoints = RecognizerPoint.DeepCopy(other.recognizerPoints);
			this.pathLength = other.pathLength;
			this.boundingBox = other.boundingBox;
			this.centroid = other.centroid;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public RecognizerStroke(IEnumerable<RecognizerPoint> oldPoints, 
			int angleSmoothCount)
		{
			Initialize(oldPoints, angleSmoothCount);
		}

		/// <summary>
		/// Initialize the state of this Stroke.
		/// </summary>
		protected void Initialize(IEnumerable<RecognizerPoint> oldPoints, 
			int angleSmoothCount)
		{
			recognizerPoints = StrokePreProcessing.RemoveDuplicatePoints(
				RecognizerPoint.DeepCopy(oldPoints));
			CalculateStrokeProperties(recognizerPoints);

			// Calculate the angle values (in radians) for the actual point objects
			StrokePreProcessing.CalculatePointAngles(recognizerPoints);

			// Smooth the angles
			StrokePreProcessing.SmoothAngles(recognizerPoints, angleSmoothCount);
		}

		#endregion

		#region DYNAMIC_MEMBERS

		public void CalculateStrokeProperties()
		{
			CalculateStrokeProperties(recognizerPoints);
		}

		/// <summary>
		/// Calculate the length, bounding box, and centroid values for this Stroke.
		/// </summary>
		private void CalculateStrokeProperties(RecognizerPoint[] points)
		{
			// For the length
			double deltaX = 0;
			double deltaY = 0;
			pathLength = 0;

			// For the bounding box
			double minX = Double.MaxValue;
			double maxX = Double.MinValue;
			double minY = Double.MaxValue;
			double maxY = Double.MinValue;

			// For the centroid
			double centroidX = 0.0;
			double centroidY = 0.0;

			// Handle the fencepost problem
			if (points[0].X > maxX)
			{	// Record max X
				maxX = points[0].X;
			}

			if (points[0].X < minX)
			{	// Record min X
				minX = points[0].X;
			}
			if (points[0].Y > maxY)
			{	// Record max Y
				maxY = points[0].Y;
			}
			if (points[0].Y < minY)
			{	// Record min Y
				minY = points[0].Y;
			}
			centroidX += points[0].X;
			centroidY += points[0].Y;

			for (int i = 1; i < points.Length; i++)
			{
				// For the length
				deltaX = points[i].X - points[i - 1].X;
				deltaY = points[i].Y - points[i - 1].Y;
				pathLength += Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

				// For the bounding box
				if (points[i].X > maxX)
				{	// Record max X
					maxX = points[i].X;
				}
				else if (points[i].X < minX)
				{	// Record min X
					minX = points[i].X;
				}
				if (points[i].Y > maxY)
				{	// Record max Y
					maxY = points[i].Y;
				}
				else if (points[i].Y < minY)
				{	// Record min Y
					minY = points[i].Y;
				}

				// For the centroid
				centroidX += points[i].X;
				centroidY += points[i].Y;
			}

			this.boundingBox = new Rect(minX, minY, maxX - minX, maxY - minY);

			this.centroid = new RecognizerPoint(centroidX / points.Length, centroidY / points.Length, -1, Double.NaN);
		}

		public RecognizerPoint[] GetNormalizedRecognizerPoints(double minX, 
			double minY, double maxX, double maxY)
		{
			RecognizerPoint[] normalizedPoints = RecognizerPoint.DeepCopy(recognizerPoints);

			NormalizePoints(normalizedPoints, minX, minY, maxX, maxY);

			return normalizedPoints;
		}

		public void Normalize(double minX, double minY, double maxX, double maxY)
		{
			NormalizePoints(recognizerPoints, minX, minY, maxX, maxY);
		}

		/// <summary>
		/// Uniformly normalize and translate the coordinates to range from 0 
		/// to 1 in one axis and be centered between 0 and 1 in the other
		/// </summary>
		private void NormalizePoints(RecognizerPoint[] points, double minX, double minY, double maxX, double maxY)
		{
			double width = maxX - minX;
			double height = maxY - minY;

			double scale, xOffset, yOffset;

			if (width > height)
			{
				scale = 1 / width;
				xOffset = 0;
				yOffset = 0.5 * (width - height);
			}
			else
			{
				scale = 1 / height;
				xOffset = 0.5 * (height - width);
				yOffset = 0;
			}

			for (int i = 0; i < points.Length; ++i)
			{
				points[i].X = (recognizerPoints[i].X - minX + xOffset) * scale;
				points[i].Y = (recognizerPoints[i].Y - minY + yOffset) * scale;
			}
		}

		public double PathLength
		{
			get { return pathLength; }
		}

		public Rect BoundingBox
		{
			get { return boundingBox; }
		}

		public RecognizerPoint Centroid
		{
			get { return centroid; }
		}

		public RecognizerPoint[] RecognizerPoints
		{
			get { return recognizerPoints; }
		}

		public int PointCount
		{
			get { return recognizerPoints.Length; }
		}

		#endregion

	}
}
