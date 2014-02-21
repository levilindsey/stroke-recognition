/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Double.Factorization;
using MathNet.Numerics.LinearAlgebra.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StrokeCollector
{
	class StrokeSegmentation
	{
		/// <summary>
		/// Determine which of the given points are feature points.
		/// </summary>
		public static FeaturePoint[] Segment(DrawablePoint[] points,
			SegmentationAlgorithm segmentationAlgorithm, double shortStrawThreshold)
		{
			switch (segmentationAlgorithm)
			{
				case SegmentationAlgorithm.EndPointsOnly:
					return EndPointsOnlySegmenter.Segment(points, shortStrawThreshold);
				case SegmentationAlgorithm.ShortStraw:
					return ShortStrawSegmenter.Segment(points, shortStrawThreshold);
				case SegmentationAlgorithm.SpeedSeg:
					return SpeedSegSegmenter.Segment(points);
				case SegmentationAlgorithm.CustomSegWOPostProcess:
					return CustomSegmenterWOPostProcess.Segment(points, shortStrawThreshold);
				case SegmentationAlgorithm.CustomSeg:
					return CustomSegmenter.Segment(points, shortStrawThreshold);
				default:
					return null;
			}
		}

		/// <summary>
		/// Fit Segments to the given array of points according to the feature points.
		/// </summary>
		public static Segment[] FitSegments(DrawablePoint[] points,
			SegmentationAlgorithm segmentationAlgorithm, int featurePointCount)
		{
			switch (segmentationAlgorithm)
			{
				case SegmentationAlgorithm.EndPointsOnly:
					return EndPointsOnlySegmenter.FitSegments(points, featurePointCount);
				case SegmentationAlgorithm.ShortStraw:
					return ShortStrawSegmenter.FitSegments(points, featurePointCount);
				case SegmentationAlgorithm.SpeedSeg:
					return SpeedSegSegmenter.FitSegments(points, featurePointCount);
				case SegmentationAlgorithm.CustomSegWOPostProcess:
					return CustomSegmenterWOPostProcess.FitSegments(points, featurePointCount);
				case SegmentationAlgorithm.CustomSeg:
					return CustomSegmenter.FitSegments(points, featurePointCount);
				default:
					return null;
			}
		}

		/// <summary>
		/// Return the segment representing the least fit circular arc between the given sub array 
		/// of points.
		/// </summary>
		public static LineSegment FitALine(DrawablePoint[] points, int startIndex, int endIndex)
		{
			bool isHorizontal = StrokePreProcessing.IsHorizontal(points, startIndex, endIndex);

			Matrix<double> A = GetLineMatrixA(points, startIndex, endIndex, isHorizontal);
			Vector<double> b = GetLineVectorB(points, startIndex, endIndex, isHorizontal);

			Svd svd = A.Svd(true);
			Matrix<double> U = svd.U();
			Matrix<double> V = svd.VT().Transpose();
			Vector<double> w = svd.S();

			Vector<double> a = GetVectorA(U, V, w, b);

			double errorOfFit = ComputeLineErrorOfFit(points, startIndex, endIndex, a[0], a[1], 
				isHorizontal);
			DrawablePoint startPoint = FindLineSegmentEnd(points[startIndex], a[0], a[1], isHorizontal);
			DrawablePoint endPoint = FindLineSegmentEnd(points[endIndex], a[0], a[1], isHorizontal);

			return new LineSegment(startIndex, endIndex, errorOfFit, startPoint.X, startPoint.Y, 
				endPoint.X, endPoint.Y);
		}

		/// <summary>
		/// 
		/// </summary>
		public static DrawablePoint FindLineSegmentEnd(DrawablePoint point, double A, double B, bool isHorizontal)
		{
			// The fit line
			double A1, B1, C1;

			// The line perpendicular and running through the end point
			double A2, B2, C2;

			if (isHorizontal)
			{
				A1 = -B;
				if (B != 0)
					A2 = 1 / B;
				else
					A2 = Double.PositiveInfinity;

				B1 = 1;
				B2 = 1;
			}
			else	// !isHorizontal
			{
				A1 = 1;
				A2 = 1;

				B1 = -B;
				if (B != 0)
					B2 = 1 / B;
				else
					B2 = Double.PositiveInfinity;
			}

			C1 = A;
			C2 = B2*point.Y + A2*point.X;

			return StrokePreProcessing.FindLineIntersection(A1, B1, A2, B2, C1, C2);
		}

		/// <summary>
		/// (From eqn 8 in Speed Seg paper)
		/// </summary>
		public static Matrix<double> GetLineMatrixA(DrawablePoint[] points, int startIndex, int endIndex, 
			bool isHorizontal)
		{
			double
				M_11 = 0,
				M_12 = 0,
				M_21 = 0,
				M_22 = 0;

			double x, y;

			if (isHorizontal)
			{
				for (int i = startIndex; i <= endIndex; i++)
				{
					x = points[i].X;

					M_12 += x;
					M_22 += x * x;
				}
			}
			else
			{
				for (int i = startIndex; i <= endIndex; i++)
				{
					y = points[i].Y;

					M_12 += y;
					M_22 += y * y;
				}
			}

			M_11 = endIndex - startIndex + 1;
			M_21 = M_12;

			return new DenseMatrix(new double[,] {
				{ M_11, M_12 },
				{ M_21, M_22 }
			});
		}

		/// <summary>
		/// (From eqn 8 in Speed Seg paper)
		/// </summary>
		public static Vector<double> GetLineVectorB(DrawablePoint[] points, int startIndex, int endIndex, 
			bool isHorizontal)
		{
			double
				b_1 = 0,
				b_2 = 0;

			double x, y;

			if (isHorizontal)
			{
				for (int i = startIndex; i <= endIndex; i++)
				{
					x = points[i].X;
					y = points[i].Y;

					b_1 += y;
					b_2 += x * y;
				}
			}
			else
			{
				for (int i = startIndex; i <= endIndex; i++)
				{
					x = points[i].X;
					y = points[i].Y;

					b_1 += x;
					b_2 += x * y;
				}
			}

			return new DenseVector(new double[] { b_1, b_2 });
		}

		/// <summary>
		/// Return the segment representing the least fit circular arc between the given sub array 
		/// of points.
		/// </summary>
		public static ArcSegment FitACircle(DrawablePoint[] points, int startIndex, int endIndex)
		{
			Matrix<double> A = GetCircleMatrixA(points, startIndex, endIndex);
			Vector<double> b = GetCircleVectorB(points, startIndex, endIndex);

			Svd svd = A.Svd(true);
			Matrix<double> U = svd.U();
			Matrix<double> V = svd.VT().Transpose();
			Vector<double> w = svd.S();

			Vector<double> a = GetVectorA(U, V, w, b);

			double x = a[0];
			double y = a[1];
			double c = a[2];
			double radius = Math.Sqrt(x * x + y * y - c);
			DrawablePoint center = new DrawablePoint(-x, -y, -1);
			double errorOfFit = ComputeCircleErrorOfFit(points, startIndex,
				endIndex, center, radius);
			double startAngle = StrokePreProcessing.GetAngle(center, points[startIndex], false);
			double endAngle = StrokePreProcessing.GetAngle(center, points[endIndex], false);
			bool isCW = StrokePreProcessing.IsCW(points, startIndex, endIndex, center);

			return new ArcSegment(startIndex, endIndex, errorOfFit, -x, -y, radius, startAngle, 
				endAngle, isCW);
		}

		/// <summary>
		/// (From eqn 8 in Speed Seg paper)
		/// </summary>
		public static Matrix<double> GetCircleMatrixA(DrawablePoint[] points, int startIndex, int endIndex)
		{
			double
				M_11 = 0,
				M_12 = 0,
				M_13 = 0,
				M_21 = 0,
				M_22 = 0,
				M_23 = 0,
				M_31 = 0,
				M_32 = 0,
				M_33 = 0;

			double x, y;

			for (int i = startIndex; i <= endIndex; i++)
			{
				x = points[i].X;
				y = points[i].Y;

				M_11 += x * x;
				M_12 += x * y;
				M_13 += x;
				M_22 += y * y;
				M_23 += y;
			}

			M_21 = M_12;
			M_31 = M_13;
			M_32 = M_23;

			M_11 *= 2;
			M_12 *= 2;
			M_21 *= 2;
			M_22 *= 2;
			M_31 *= 2;
			M_32 *= 2;
			M_33 = endIndex - startIndex + 1;

			return new DenseMatrix(new double[,] {
				{ M_11, M_12, M_13 },
				{ M_21, M_22, M_23 },
				{ M_31, M_32, M_33 }
			});
		}

		/// <summary>
		/// (From eqn 8 in Speed Seg paper)
		/// </summary>
		public static Vector<double> GetCircleVectorB(DrawablePoint[] points, int startIndex, int endIndex)
		{
			double
				b_1 = 0,
				b_2 = 0,
				b_3 = 0;

			double x, y, sumOfSquares;

			for (int i = startIndex; i <= endIndex; i++)
			{
				x = points[i].X;
				y = points[i].Y;
				sumOfSquares = (x * x + y * y);

				b_1 += sumOfSquares * x;
				b_2 += sumOfSquares * y;
				b_3 += sumOfSquares;
			}

			b_1 *= -1;
			b_2 *= -1;
			b_3 *= -1;

			return new DenseVector(new double[] { b_1, b_2, b_3 });
		}

		/// <summary>
		/// (From eqn 14.3.17 in "Numerical Recipes in C: The Art of Scientific Computing")
		/// </summary>
		public static Vector<double> GetVectorA(Matrix<double> U, Matrix<double> V,
			Vector<double> w, Vector<double> b)
		{
			int ColumnCount = U.ColumnCount;

			Vector<double> a = new DenseVector(new double[ColumnCount]);

			for (int i = 0; i < ColumnCount; i++)
			{
				a += (U.Column(i).DotProduct(b) / w[i]) * V.Column(i);
			}

			return a;
		}

		private static double ComputeCircleErrorOfFit(DrawablePoint[] points, int startIndex,
				int endIndex, DrawablePoint center, double radius)
		{
			double distance = 0;

			for (int i = startIndex; i <= endIndex; i++)
			{
				distance += Math.Abs(StrokePreProcessing.GetDistance(center, points[i]) - radius);
			}

			return distance / (endIndex - startIndex + 1);
		}

		private static double ComputeLineErrorOfFit(DrawablePoint[] points, int startIndex,
				int endIndex, double A, double B, bool isHorizontal)
		{
			double distance = 0;

			if (isHorizontal)
				for (int i = startIndex; i <= endIndex; i++)
					distance += Math.Abs(B * points[i].X + A - points[i].Y);
			else
				for (int i = startIndex; i <= endIndex; i++)
					distance += Math.Abs(B * points[i].Y + A - points[i].X);

			return distance / (endIndex - startIndex + 1);
		}
	}
}
