/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StrokeCollector
{
	public class SingleUserHoldOutTest
	{
		private static Recognizer recognizer = null;

		private List<Template> templates;
		private List<ShapeInstance> holdOuts;

		private short userID;

		private double timeToTrain;
		private double avgTimeToRecognize;

		private double accuracy;
		private short[] shapeIDs;
		private double[] confusionMatrix;
		private double[] truePositives;
		private double[] falsePositives;
		private double[] precisions;
		private double[] recalls;
		private double[] fMeasures;

		public SingleUserHoldOutTest(short userID, double timeToTrain,
			List<Template> templates, List<ShapeInstance> holdOuts)
		{
			Initialize(userID, timeToTrain, templates, holdOuts);
		}

		private void Initialize(short userID, double timeToTrain,
			List<Template> templates, List<ShapeInstance> holdOuts)
		{
			this.userID = userID;
			this.timeToTrain = timeToTrain;
			this.avgTimeToRecognize = Double.NaN;
			this.templates = templates;
			this.holdOuts = holdOuts;

			this.accuracy = Double.NaN;
			this.shapeIDs = null;
			this.confusionMatrix = null;
			this.truePositives = null;
			this.falsePositives = null;
			this.precisions = null;
			this.recalls = null;
			this.fMeasures = null;
		}

		public void TestThisHoldOut()
		{
			int numberOfShapes = templates.Count;
			int totalCorrectlyClassifiedCount = 0;
			int totalInstancesCount = 0;
			int[] actualInstancesCountPerShape = new int[numberOfShapes];
			shapeIDs = new short[numberOfShapes];
			confusionMatrix = new double[numberOfShapes * numberOfShapes];
			truePositives = new double[numberOfShapes];
			falsePositives = new double[numberOfShapes];
			precisions = new double[numberOfShapes];
			recalls = new double[numberOfShapes];
			fMeasures = new double[numberOfShapes];
			int actualShapeIndex, recognizedShapeIndex;
			double denominator;
			avgTimeToRecognize = 0;

			// Get the shape IDs
			int i = 0;
			foreach (Template template in templates)
			{
				shapeIDs[i++] = template.ID;
			}
			Array.Sort(shapeIDs);

			// Loop over the shape instances for this user
			foreach (ShapeInstance holdOut in holdOuts)
			{
				recognizer.Recognize(holdOut, templates);

				avgTimeToRecognize += holdOut.TimeToRecognize;

				actualShapeIndex = Array.BinarySearch(shapeIDs, holdOut.ActualShapeID);
				recognizedShapeIndex = Array.BinarySearch(shapeIDs, holdOut.RecognizedShapeID);

				++actualInstancesCountPerShape[actualShapeIndex];
				++totalInstancesCount;

				if (actualShapeIndex == recognizedShapeIndex)
				{
					++totalCorrectlyClassifiedCount;
					++truePositives[recognizedShapeIndex];
				}
				else
				{
					++falsePositives[recognizedShapeIndex];
				}

				++confusionMatrix[actualShapeIndex * numberOfShapes + recognizedShapeIndex];
			}

			avgTimeToRecognize /= holdOuts.Count;

			// Loop over the shape recognition results for this user
			for (i = 0; i < numberOfShapes; ++i)
			{
				denominator = truePositives[i] + falsePositives[i];
				precisions[i] = denominator != 0 ? truePositives[i] / denominator : 0;
				recalls[i] = truePositives[i] / actualInstancesCountPerShape[i];
				denominator = precisions[i] + recalls[i];
				fMeasures[i] = denominator != 0 ? 2 * (precisions[i] * recalls[i]) / denominator : 0;
			}

			accuracy = (double)totalCorrectlyClassifiedCount / totalInstancesCount;
		}

		public static void SetRecognizer(Recognizer recognizer)
		{
			SingleUserHoldOutTest.recognizer = recognizer;
		}

		public short UserID
		{
			get { return userID; }
		}

		public double TimeToTrain
		{
			get { return timeToTrain; }
			set { timeToTrain = value; }
		}

		public double AvgTimeToRecognize
		{
			get { return avgTimeToRecognize; }
			set { avgTimeToRecognize = value; }
		}

		public double Accuracy
		{
			get { return accuracy; }
			set { accuracy = value; }
		}

		// Row-major order (and a row represents all of the recognition counts for the actual shape)
		public double[] ConfusionMatrix
		{
			get { return confusionMatrix; }
			set { confusionMatrix = value; }
		}

		public double[] FMeasures
		{
			get { return fMeasures; }
			set { fMeasures = value; }
		}

		public short[] ShapeIDs
		{
			get { return shapeIDs; }
			set { shapeIDs = value; }
		}

		public double[] TruePositives
		{
			get { return truePositives; }
			set { truePositives = value; }
		}

		public double[] FalsePositives
		{
			get { return falsePositives; }
			set { falsePositives = value; }
		}

		public double[] Precisions
		{
			get { return precisions; }
			set { precisions = value; }
		}

		public double[] Recalls
		{
			get { return recalls; }
			set { recalls = value; }
		}
	}
}
