/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StrokeCollector
{
	public class Recognizer
	{
		private static MainWindow mainWindow;

		private List<Template> templates;
		private List<ShapeInstance> holdOuts;

		private String trainingDataPath;

		private double timeToTrain;
		private double timeToCrossValidate;

		private List<SingleUserHoldOutTest> crossValidationResults;

		/// <summary>
		/// Constructor.
		/// </summary>
		public Recognizer(MainWindow mainWindow)
		{
			templates = new List<Template>();
			holdOuts = new List<ShapeInstance>();
			trainingDataPath = null;
			timeToTrain = Double.NaN;
			timeToCrossValidate = Double.NaN;
			crossValidationResults = null;

			Recognizer.mainWindow = mainWindow;
			SingleUserHoldOutTest.SetRecognizer(this);
		}

		public void Train(List<short> holdOut, short columnCount, 
			short columnCellCount, int strokeAngleSmoothCount,
			int columnSmoothCount, int intersectionGaussianWidth, 
			double templateBoost)
		{
			double startTime = MainWindow.GetTimestampInMillis();

			Dictionary<short, List<ShapeInstance>> classIDToTrainingInstancesMap = 
				LoadTrainingData(holdOut, columnCount, columnCellCount, 
					strokeAngleSmoothCount, intersectionGaussianWidth);

			templates = new List<Template>();

			foreach (KeyValuePair<short, List<ShapeInstance>> pair in classIDToTrainingInstancesMap)
			{
				templates.Add(new Template(pair.Key, pair.Value, columnCount, 
					columnCellCount, columnSmoothCount, templateBoost));
			}

			double endTime = MainWindow.GetTimestampInMillis();

			timeToTrain = (endTime - startTime) / 1000.0;

			holdOuts = LoadHoldOuts(holdOut, columnCount, columnCellCount,
					strokeAngleSmoothCount, intersectionGaussianWidth);

			ComputeStrokes90And135(holdOuts);
		}

		private static List<Template> Train(
			Dictionary<short, List<ShapeInstance>> trainingSetShapeIDToInstancesMap,
			short columnCount, short columnCellCount, 
			int columnSmoothCount, double templateBoost)
		{
			List<Template> templates = new List<Template>();

			foreach (KeyValuePair<short, List<ShapeInstance>> pair in trainingSetShapeIDToInstancesMap)
			{
				templates.Add(new Template(pair.Key, pair.Value, columnCount, 
					columnCellCount, columnSmoothCount, templateBoost));
			}

			return templates;
		}

		public void Recognize(List<ShapeInstance> shapeInstances)
		{
			foreach (ShapeInstance shapeInstance in shapeInstances)
			{
				Recognize(shapeInstance);
			}
		}

		public void Recognize(ShapeInstance shapeInstance)
		{
			Recognize(shapeInstance, templates);
		}

		public void Recognize(ShapeInstance shapeInstance, List<Template> templates)
		{
			double distance;
			double closestDistance = Double.MaxValue;
			short closestTemplateID = -1;

			double startTime = MainWindow.GetTimestampInMillis();

			foreach (Template template in templates)
			{
				distance = GetDistance(shapeInstance, template);
				if (distance < closestDistance)
				{
					closestDistance = distance;
					closestTemplateID = template.ID;
				}
			}

			double endTime = MainWindow.GetTimestampInMillis();

			// Record the ellapsed time of this recognition
			shapeInstance.TimeToRecognize = (endTime - startTime) / 1000.0;
			shapeInstance.RecognizedDistance = closestDistance;
			shapeInstance.RecognizedShapeID = closestTemplateID;
		}

		public void CrossValidate(short columnCount, short columnCellCount,
			int strokeAngleSmoothCount, int columnSmoothCount,
			int intersectionGaussianWidth, double templateBoost)
		{
			Dictionary<short, List<ShapeInstance>> trainingSetShapeIDToInstancesMap;
			List<ShapeInstance> holdOutSet;
			double timeToTrain;
			List<Template> templates;
			SingleUserHoldOutTest singleUserHoldOutTest;
			double startTime, endTime, avgAvgTimeToRecognize;

			int numberOfUsers, numberOfShapes, numberOfShapesSquared;
			double avgTimeToTrain, avgAccuracy;
			double[] avgConfusionMatrix, avgFMeasures, avgTruePositives, avgFalsePositives, avgPrecisions, avgRecalls;
			short[] shapeIDs;

			crossValidationResults = new List<SingleUserHoldOutTest>();

			Dictionary<short, List<ShapeInstance>> userIDToInstancesMap = 
				LoadAllDataByUser(columnCount, columnCellCount,
					strokeAngleSmoothCount, intersectionGaussianWidth);
			numberOfUsers = userIDToInstancesMap.Count;

			avgAvgTimeToRecognize = 0;

			// ---------- Compute the per-user results ---------- //

			foreach (short userID in userIDToInstancesMap.Keys)
			{
				trainingSetShapeIDToInstancesMap = new Dictionary<short, List<ShapeInstance>>();
				holdOutSet = new List<ShapeInstance>();
				CopyIntoTrainingAndHoldOutSets(userIDToInstancesMap, userID, trainingSetShapeIDToInstancesMap, holdOutSet);

				startTime = MainWindow.GetTimestampInMillis();

				templates = Train(trainingSetShapeIDToInstancesMap, 
					columnCount, columnCellCount, columnSmoothCount, 
					templateBoost);

				endTime = MainWindow.GetTimestampInMillis();

				timeToTrain = (endTime - startTime) / 1000.0;
				singleUserHoldOutTest = new SingleUserHoldOutTest(userID, timeToTrain, templates, holdOutSet);
				singleUserHoldOutTest.TestThisHoldOut();
				avgAvgTimeToRecognize += singleUserHoldOutTest.AvgTimeToRecognize;
				crossValidationResults.Add(singleUserHoldOutTest);
			}

			// ---------- Compute the average results across all users ---------- //

			shapeIDs = crossValidationResults.First().ShapeIDs;

			numberOfShapes = shapeIDs.Length;
			numberOfShapesSquared = numberOfShapes * numberOfShapes;

			timeToCrossValidate = 0;
			avgAccuracy = 0;
			avgConfusionMatrix = new double[numberOfShapesSquared];
			avgFMeasures = new double[numberOfShapes];
			avgTruePositives = new double[numberOfShapes];
			avgFalsePositives = new double[numberOfShapes];
			avgPrecisions = new double[numberOfShapes];
			avgRecalls = new double[numberOfShapes];

			singleUserHoldOutTest = new SingleUserHoldOutTest(-1, -1, null, null);

			foreach (SingleUserHoldOutTest tempSingleUserHoldOutTest in crossValidationResults)
			{
				timeToCrossValidate += tempSingleUserHoldOutTest.TimeToTrain;
				avgAccuracy += tempSingleUserHoldOutTest.Accuracy;

				for (int i = 0; i < numberOfShapesSquared; ++i)
				{
					avgConfusionMatrix[i] += tempSingleUserHoldOutTest.ConfusionMatrix[i];
				}

				for (int i = 0; i < numberOfShapes; ++i)
				{
					avgFMeasures[i] += tempSingleUserHoldOutTest.FMeasures[i];
					avgTruePositives[i] += tempSingleUserHoldOutTest.TruePositives[i];
					avgFalsePositives[i] += tempSingleUserHoldOutTest.FalsePositives[i];
					avgPrecisions[i] += tempSingleUserHoldOutTest.Precisions[i];
					avgRecalls[i] += tempSingleUserHoldOutTest.Recalls[i];
				}
			}

			avgAvgTimeToRecognize /= numberOfUsers;
			avgTimeToTrain = timeToCrossValidate / numberOfUsers;
			avgAccuracy /= numberOfUsers;

			for (int i = 0; i < numberOfShapesSquared; ++i)
			{
				avgConfusionMatrix[i] /= numberOfUsers;
			}

			for (int i = 0; i < numberOfShapes; ++i)
			{
				avgFMeasures[i] /= numberOfUsers;
				avgTruePositives[i] /= numberOfUsers;
				avgFalsePositives[i] /= numberOfUsers;
				avgPrecisions[i] /= numberOfUsers;
				avgRecalls[i] /= numberOfUsers;
			}

			singleUserHoldOutTest.AvgTimeToRecognize = avgAvgTimeToRecognize;
			singleUserHoldOutTest.TimeToTrain = avgTimeToTrain;
			singleUserHoldOutTest.Accuracy = avgAccuracy;
			singleUserHoldOutTest.ConfusionMatrix = avgConfusionMatrix;
			singleUserHoldOutTest.FMeasures = avgFMeasures;
			singleUserHoldOutTest.TruePositives = avgTruePositives;
			singleUserHoldOutTest.FalsePositives = avgFalsePositives;
			singleUserHoldOutTest.Precisions = avgPrecisions;
			singleUserHoldOutTest.Recalls = avgRecalls;
			singleUserHoldOutTest.ShapeIDs = shapeIDs;

			crossValidationResults.Insert(0, singleUserHoldOutTest);
		}

		private static void CopyIntoTrainingAndHoldOutSets(
			Dictionary<short, List<ShapeInstance>> userIDToInstancesMap,
			short holdOutUserID,
			Dictionary<short, List<ShapeInstance>> trainingSetShapeIDToInstancesMap,
			List<ShapeInstance> holdOutSet)
		{
			short userID;
			short shapeID;
			foreach (KeyValuePair<short, List<ShapeInstance>> pair in userIDToInstancesMap)
			{
				userID = pair.Key;
				foreach (ShapeInstance shapeInstance in pair.Value)
				{
					if (userID != holdOutUserID)
					{
						shapeID = shapeInstance.ActualShapeID;
						if (!trainingSetShapeIDToInstancesMap.ContainsKey(shapeID))
						{
							trainingSetShapeIDToInstancesMap[shapeID] = new List<ShapeInstance>();
						}
						trainingSetShapeIDToInstancesMap[shapeID].Add(new ShapeInstance(shapeInstance));
					}
					else
					{
						holdOutSet.Add(new ShapeInstance(shapeInstance));
					}
				}
			}
		}

		public double GetDistance(ShapeInstance shapeInstance, Template template)
		{
			double distance = 0;

			Template templateCopy = new Template(template);

			distance += GetDirectionalDistance(shapeInstance.Columns0, templateCopy.Columns0);
			distance += GetDirectionalDistance(shapeInstance.Columns45, templateCopy.Columns45);
			distance += GetDirectionalDistance(shapeInstance.Columns90, templateCopy.Columns90);
			distance += GetDirectionalDistance(shapeInstance.Columns135, templateCopy.Columns135);

			distance = Math.Sqrt(distance);

			return distance;
		}

		public double GetDirectionalDistance(
			Dictionary<int, double>[] instanceColumns, 
			double[][] templateColumns)
		{
			Dictionary<int, double> instanceColumn;
			double[] templateColumn;
			double instanceCellValue, tempDistance;
			double distance = 0;

			for (int col = 0; col < templateColumns.Length; ++col)
			{
				templateColumn = templateColumns[col];
				instanceColumn = instanceColumns[col];
				for (int row = 0; row < templateColumn.Length; ++row)
				{
					instanceCellValue = instanceColumn.ContainsKey(row) ? instanceColumn[row] : 0;
					tempDistance = templateColumn[row] - instanceCellValue;
					distance += tempDistance * tempDistance;
				}
			}

			return distance;
		}

		public Dictionary<short, List<ShapeInstance>> LoadTrainingData(
			List<short> holdOut, short columnCount, short columnCellCount,
			int angleSmoothCount, int intersectionGaussianWidth)
		{
			if (trainingDataPath != null)
			{
				String[] filePaths = Directory.GetFiles(trainingDataPath);

				Dictionary<short, List<ShapeInstance>> classIDToTrainingInstancesMap = new Dictionary<short, List<ShapeInstance>>();

				short subjectID, shapeClassID, exampleNumber;
				String[] filePathDirectories;
				String filename;
				List<RecognizerStroke> strokes;

				// Loop over each of the shape instance files
				foreach (String filePath in filePaths)
				{
					filePathDirectories = filePath.Split(Params.PATH_DIRECTORY_DELIMITER);
					filename = filePathDirectories.Last();

					// subXX-shpYY-exZZ.txt
					subjectID = Convert.ToInt16(filename.Substring(3, 2));
					shapeClassID = Convert.ToInt16(filename.Substring(9, 2));
					exampleNumber = Convert.ToInt16(filename.Substring(14, 2));

					// Only load as training data shapes which are not in the hold-out set
					if (!holdOut.Contains(exampleNumber))
					{
						// Read in the stroke data
						strokes = LoadRecognizerStrokes(filePath, angleSmoothCount);

						// Don't create empty shape instances (my pre-processing may have made this stroke collection be empty)
						if (strokes.Count > 0)
						{
							// Store the shape instance
							if (!classIDToTrainingInstancesMap.ContainsKey(shapeClassID))
							{
								classIDToTrainingInstancesMap[shapeClassID] = new List<ShapeInstance>();
							}
							classIDToTrainingInstancesMap[shapeClassID].Add(
								new ShapeInstance(strokes, shapeClassID, subjectID, exampleNumber, columnCount, columnCellCount, intersectionGaussianWidth, false));
						}
					}
				}

				return classIDToTrainingInstancesMap;
			}
			else
			{
				return null;
			}
		}

		public Dictionary<short, List<ShapeInstance>> LoadAllDataByUser(
			short columnCount, short columnCellCount, int angleSmoothCount, 
			int intersectionGaussianWidth)
		{
			if (trainingDataPath != null)
			{
				String[] filePaths = Directory.GetFiles(trainingDataPath);

				Dictionary<short, List<ShapeInstance>> userIDToInstancesMap = new Dictionary<short, List<ShapeInstance>>();

				short subjectID, shapeClassID, exampleNumber;
				String[] filePathDirectories;
				String filename;
				List<RecognizerStroke> strokes;

				// Loop over each of the shape instance files
				foreach (String filePath in filePaths)
				{
					filePathDirectories = filePath.Split(Params.PATH_DIRECTORY_DELIMITER);
					filename = filePathDirectories.Last();

					// subXX-shpYY-exZZ.txt
					subjectID = Convert.ToInt16(filename.Substring(3, 2));
					shapeClassID = Convert.ToInt16(filename.Substring(9, 2));
					exampleNumber = Convert.ToInt16(filename.Substring(14, 2));

					// Read in the stroke data
					strokes = LoadRecognizerStrokes(filePath, angleSmoothCount);

					// Don't create empty shape instances (my pre-processing may have made this stroke collection be empty)
					if (strokes.Count > 0)
					{
						// Store the shape instance
						if (!userIDToInstancesMap.ContainsKey(subjectID))
						{
							userIDToInstancesMap[subjectID] = new List<ShapeInstance>();
						}
						userIDToInstancesMap[subjectID].Add(
							new ShapeInstance(strokes, shapeClassID, subjectID, exampleNumber, columnCount, columnCellCount, intersectionGaussianWidth, true));
					}
				}

				return userIDToInstancesMap;
			}
			else
			{
				return null;
			}
		}

		public List<ShapeInstance> LoadHoldOuts(List<short> holdOut, 
			short columnCount, short columnCellCount, int angleSmoothCount, 
			int intersectionGaussianWidth)
		{
			if (trainingDataPath != null)
			{
				String[] filePaths = Directory.GetFiles(trainingDataPath);

				List<ShapeInstance> holdOuts = new List<ShapeInstance>();

				short subjectID, shapeClassID, exampleNumber;
				String[] filePathDirectories;
				String filename;
				List<RecognizerStroke> strokes;

				// Loop over each of the shape instance files
				foreach (String filePath in filePaths)
				{
					filePathDirectories = filePath.Split(Params.PATH_DIRECTORY_DELIMITER);
					filename = filePathDirectories.Last();

					// subXX-shpYY-exZZ.txt
					subjectID = Convert.ToInt16(filename.Substring(3, 2));
					shapeClassID = Convert.ToInt16(filename.Substring(9, 2));
					exampleNumber = Convert.ToInt16(filename.Substring(14, 2));

					// Only load shapes which are in the hold-out set
					if (holdOut.Contains(exampleNumber))
					{
						// Read in the stroke data
						strokes = LoadRecognizerStrokes(filePath, angleSmoothCount);

						// Don't create empty shape instances (my pre-processing may have made this stroke collection be empty)
						if (strokes.Count > 0)
						{
							// Store the shape instance
							holdOuts.Add(
							new ShapeInstance(strokes, shapeClassID, subjectID, exampleNumber, columnCount, columnCellCount, intersectionGaussianWidth, true));
						}
					}
				}

				return holdOuts;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public bool GetPathToTrainingData()
		{
			// Set up the open directory dialog
			System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
			dialog.Description = Params.SELECT_SHAPES_DIRECTORY_DIALOG_TITLE_STR;

			// Open the open directory dialog
			System.Windows.Forms.DialogResult result = dialog.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK)
			{
				trainingDataPath = dialog.SelectedPath;
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Load Strokes in from the given file path.
		/// </summary>
		private List<RecognizerStroke> LoadRecognizerStrokes(String path, int angleSmoothCount)
		{
			Uri uri = new Uri(path, UriKind.Absolute);
			FileStream inStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, Params.BUFFER_SIZE, false);
			StreamReader reader = new StreamReader(inStream);

			String[] tokens = null;
			RecognizerPoint point;
			RecognizerPoint[] points = null;
			RecognizerStroke stroke;
			List<RecognizerStroke> strokes = new List<RecognizerStroke>();
			int pointCount;
			int pointIndex = 0;

			// Determine whether the file has any content
			if (reader.Peek() >= 0)
			{
				// Read in how many Strokes are in the file
				tokens = reader.ReadLine().Split(Params.INTRA_POINT_LOAD_DELIMITERS);

				// Determine whether the file has more content
				while (reader.Peek() >= 0)
				{
					// Read in the next line
					tokens = reader.ReadLine().Split(Params.INTRA_POINT_LOAD_DELIMITERS);

					// Determine whether this next line denotes a new Stroke or a new Point
					if (tokens.Length == 1)
					{
						// Create a Stroke object from the Points we have just read in
						if (points != null && points.Length >= Params.MIN_POINT_COUNT &&
							pointIndex == points.Length)
						{
							// Save the new Stroke
							stroke = new RecognizerStroke(points, angleSmoothCount);
							strokes.Add(stroke);
						}

						// Get the number of Points in the next Stroke
						pointCount = Convert.ToInt32(tokens[0]);
						points = new RecognizerPoint[pointCount];
						pointIndex = 0;
					}
					else
					{
						// Save the new Point
						point = RecognizerPoint.ParseStringToPoint(tokens);
						points[pointIndex++] = point;
					}
				}
			}

			// Create a Stroke object from the Points we have just read in
			if (points != null && points.Length >= Params.MIN_POINT_COUNT)
			{
				// Save the new Stroke
				stroke = new RecognizerStroke(points, angleSmoothCount);
				strokes.Add(stroke);
			}

			// Clean up the StreamReader
			reader.Dispose();

			return strokes;
		}

		private static void ComputeStrokes90And135(List<ShapeInstance> shapeInstances)
		{
			foreach (ShapeInstance shapeInstance in shapeInstances)
			{
				shapeInstance.ComputeStrokes90And135();
			}
		}

		public Template GetTemplate(short shapeID)
		{
			if (shapeID >= 0)
			{
				foreach (Template template in templates)
				{
					if (template.ID == shapeID)
					{
						return template;
					}
				}
			}
			return null;
		}

		public String TrainingDataPathTitle
		{
			get
			{
				return
					(trainingDataPath != null ?
						(trainingDataPath.Length > Params.TRAINING_DATA_PATH_TITLE_LENGTH ?
							"..." + trainingDataPath.Substring(trainingDataPath.Length - Params.TRAINING_DATA_PATH_TITLE_LENGTH) :
							trainingDataPath) :
						"--");
			}
		}

		public String TrainingDataPath
		{
			get { return trainingDataPath; }
		}

		public bool RecognitionEnabled
		{
			get { return templates.Count > 0; }
		}

		public double TimeToTrain
		{
			get { return timeToTrain; }
		}

		public double TimeToCrossValidate
		{
			get { return timeToCrossValidate; }
		}

		public List<Template> Templates
		{
			get { return templates; }
		}

		public List<ShapeInstance> HoldOuts
		{
			get { return holdOuts; }
		}

		public List<SingleUserHoldOutTest> CrossValidationResults
		{
			get { return crossValidationResults; }
		}
	}
}
