/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StrokeCollector
{
	///  <summary>
	///  Interaction logic for MainWindow.xaml
	///  </summary>
	public partial class MainWindow : Window
	{

		public const bool DEBUGGING = true;

		#region FIELD_DECLARATIONS

		private List<DrawablePoint> currentStrokePoints;

		private List<DrawableStroke> strokes;
		private SortedList<double, DrawableStroke> erasedStrokes;

		private bool isStroking;
		private bool isErasingStroke;
		private bool unsavedStrokes;

		// The actual elements which are drawn on the image canvas
		private UIElementCollection graphics;

		private static Stopwatch stopWatch;
		private static double millisecPerTick;

		private int strokeAngleSmoothCount;
		private int columnSmoothCount;
		private int intersectionGaussianWidth;
		private double templateBoost;
		private static short templateColumnCount;
		private static short columnCellCount;
		private double recognitionDistanceThreshold;

		private Recognizer recognizer;
		private RecognizerWindow recognizerWindow;

		private bool mainWindowClose;
		private ShapeInstance canvasShapeInstance;

		private static MainWindow mainMainWindow;

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();

			graphics = canvas.Children;

			mainWindowClose = false;

			MainWindow.mainMainWindow = this;

			Initialize();
		}

		/// <summary>
		/// 
		/// </summary>
		private void Initialize()
		{
			graphics.Clear();

			currentStrokePoints = new List<DrawablePoint>();

			isStroking = false;
			isErasingStroke = false;
			unsavedStrokes = false;

			strokeAngleSmoothCount = Convert.ToInt32(strokeAngleSmoothTextBox.Text);
			columnSmoothCount = Convert.ToInt32(columnSmoothTextBox.Text);
			intersectionGaussianWidth = Convert.ToInt32(intersectionGaussianWidthTextBox.Text);
			templateBoost = Convert.ToDouble(templateBoostTextBox.Text);
			templateColumnCount = Convert.ToInt16(templateColumnCountTextBox.Text);
			columnCellCount = Convert.ToInt16(columnCellCountTextBox.Text);
			recognitionDistanceThreshold = Convert.ToDouble(recognitionDistanceThresholdTextBox.Text);

			strokes = new List<DrawableStroke>();
			erasedStrokes = new SortedList<double, DrawableStroke>();

			StartStopwatch();

			InitializeCurrentStroke();

			recognizer = new Recognizer(this);
			recognizerWindow = new RecognizerWindow(this);

			canvasShapeInstance = null;
		}

		private void OnWindowClose(object sender, CancelEventArgs e)
		{
			mainWindowClose = true;
			recognizerWindow.Close();
		}

		/// <summary>
		/// Set up, save parameters of, and start the stopwatch.
		/// </summary>
		private void StartStopwatch()
		{
			stopWatch = new Stopwatch();
			millisecPerTick = 1000.0 / Stopwatch.Frequency;
			stopWatch.Start();
		}

		/// <summary>
		/// Return the current ellapsed time, in milliseconds, since the stopwatch started.
		/// </summary>
		public static double GetTimestampInMillis()
		{
			return stopWatch.ElapsedTicks * millisecPerTick;
		}

		public List<DrawableStroke> Strokes
		{
			get { return strokes; }
		}

		public Recognizer Recognizer
		{
			get { return recognizer; }
		}

		public bool MainWindowClose
		{
			get { return mainWindowClose; }
		}

		public ShapeInstance CanvasShapeInstance
		{
			get { return canvasShapeInstance; }
		}

		public int ColumnSmoothCount
		{
			get
			{
				// TODO: weird hack.  remove.
				if (DEBUGGING) OnTextChange(columnSmoothTextBox, null);
				return columnSmoothCount;
			}
		}

		public int IntersectionGaussianWidth
		{
			get
			{
				// TODO: weird hack.  remove.
				if (DEBUGGING) OnTextChange(intersectionGaussianWidthTextBox, null);
				return intersectionGaussianWidth;
			}
		}

		public double TemplateBoost
		{
			get
			{
				// TODO: weird hack.  remove.
				if (DEBUGGING) OnTextChange(templateBoostTextBox, null);
				return templateBoost;
			}
		}

		public int StrokeAngleSmoothCount
		{
			get
			{
				// TODO: weird hack.  remove.
				if (DEBUGGING) OnTextChange(strokeAngleSmoothTextBox, null);
				return strokeAngleSmoothCount;
			}
		}

		public double RecognitionDistanceThreshold
		{
			get
			{
				// TODO: weird hack.  remove.
				if (DEBUGGING) OnTextChange(recognitionDistanceThresholdTextBox, null);
				return recognitionDistanceThreshold;
			}
		}

		public static short TemplateColumnCount
		{
			get
			{
				// TODO: weird hack.  remove.
				if (DEBUGGING) mainMainWindow.OnTextChange(mainMainWindow.templateColumnCountTextBox, null);
				return templateColumnCount;
			}
		}

		public static short ColumnCellCount
		{
			get
			{
				// TODO: weird hack.  remove.
				if (DEBUGGING) mainMainWindow.OnTextChange(mainMainWindow.columnCellCountTextBox, null);
				return columnCellCount;
			}
		}

		/// <summary>
		/// Load Strokes in from the given file path.
		/// </summary>
		private bool LoadDrawableStrokes(String path)
		{
			Uri uri = new Uri(path, UriKind.Absolute);
			FileStream inStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, Params.BUFFER_SIZE, false);
			StreamReader reader = new StreamReader(inStream);

			String[] tokens = null;
			DrawablePoint point;
			DrawablePoint[] points = null;
			DrawableStroke stroke;
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
							stroke = new DrawableStroke(points, StrokeAngleSmoothCount);
							strokes.Add(stroke);

							DisplayDrawables(stroke.DrawablePoints);
						}

						// Get the number of Points in the next Stroke
						pointCount = Convert.ToInt32(tokens[0]);
						points = new DrawablePoint[pointCount];
						pointIndex = 0;
					}
					else
					{
						// Save the new Point
						point = DrawablePoint.ParseStringToPoint(tokens);
						points[pointIndex++] = point;
					}
				}
			}

			// Create a Stroke object from the Points we have just read in
			if (points != null && points.Length >= Params.MIN_POINT_COUNT)
			{
				// Save the new Stroke
				stroke = new DrawableStroke(points, StrokeAngleSmoothCount);
				strokes.Add(stroke);

				DisplayDrawables(stroke.DrawablePoints);
			}

			// Clean up the StreamReader
			reader.Dispose();

			UpdateShapeInstance();

			return true;
		}

		/// <summary>
		/// Save Strokes to the given file path.
		/// </summary>
		private bool SaveStrokes(String path)
		{
			Uri uri = new Uri(path, UriKind.Absolute);
			FileStream outStream = new FileStream(path, FileMode.Create, FileAccess.Write, 
				FileShare.Read, Params.BUFFER_SIZE, false);
			StreamWriter writer = new StreamWriter(outStream);

			String dataFileEntry;

			// Write out how many strokes are in this file
			dataFileEntry = strokes.Count.ToString();

			writer.WriteLine(dataFileEntry);

			foreach (DrawableStroke stroke in strokes)
			{
				dataFileEntry = stroke.DrawablePoints.Count().ToString();
				writer.WriteLine(dataFileEntry);

				foreach (DrawablePoint point in stroke.DrawablePoints)
				{
					// Write out the data for the current point
					dataFileEntry = point.GetDataFileEntry();
					writer.WriteLine(dataFileEntry);
				}
			}

			// Clean up the StreamWriter
			writer.Flush();
			writer.Dispose();

			return true;		// TODO: handle file IO exceptions
		}

		/// <summary>
		/// Undo whichever occurred most recently: either a stroke creation or stroke erase.
		/// </summary>
		private void Undo()
		{
			IEnumerable<Drawable> displayDrawables;

			if (strokes.Count > 0)
			{
				DrawableStroke lastCreatedStroke = strokes.Last();
				bool hideStroke = false;

				if (erasedStrokes.Count > 0)
				{
					KeyValuePair<double, DrawableStroke> pair = erasedStrokes.Last();

					if (lastCreatedStroke.Timestamp > pair.Key)
					{
						strokes.RemoveAt(strokes.Count - 1);

						hideStroke = true;
					}
					else
					{
						DrawableStroke lastErasedStroke = erasedStrokes.Last().Value;

						strokes.Add(lastErasedStroke);
						erasedStrokes.RemoveAt(erasedStrokes.Count - 1);

						displayDrawables = lastErasedStroke.DrawablePoints;
						DisplayDrawables(displayDrawables);
					}
				}
				else
				{
					strokes.RemoveAt(strokes.Count - 1);

					hideStroke = true;
				}

				if (hideStroke)
				{
					HideDrawables(lastCreatedStroke.DrawablePoints);
				}
			}
			else
			{
				if (erasedStrokes.Count > 0)
				{
					DrawableStroke lastErasedStroke = erasedStrokes.Last().Value;

					strokes.Add(lastErasedStroke);
					erasedStrokes.RemoveAt(erasedStrokes.Count - 1);

					displayDrawables = lastErasedStroke.DrawablePoints;
					DisplayDrawables(displayDrawables);
				}
			}

			UpdateShapeInstance();
		}

		/// <summary>
		/// Called when the user clicks on the load button.
		/// </summary>
		private void Load()
		{
			// Display a warning dialog if the user has drawn new strokes
			if (unsavedStrokes)
			{
				MessageBoxResult messageBoxResult =
					MessageBox.Show(Params.LOAD_STROKES_WARNING_MESSAGE_STR, Params.LOAD_STROKES_WARNING_CAPTION_STR,
						MessageBoxButton.OKCancel, MessageBoxImage.Warning);

				if (messageBoxResult != MessageBoxResult.OK)
				{
					return;
				}
			}

			// Set up the open file dialog
			Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
			dialog.Title = Params.SELECT_STROKE_FILE_DIALOG_TITLE_STR;
			dialog.DefaultExt = Params.STROKE_FILE_EXTENSION_STR;
			dialog.Filter = Params.STROKE_FILE_FILTER_STR;
			dialog.Multiselect = false;
			dialog.ShowReadOnly = true;

			// Open the open file dialog
			if (dialog.ShowDialog() == true)
			{
				Initialize();

				if (LoadDrawableStrokes(dialog.FileName))
				{
					unsavedStrokes = false;
				}
			}
		}

		/// <summary>
		/// Called when the user clicks on the save button.
		/// </summary>
		private void Save()
		{
			if (unsavedStrokes)
			{
				// Set up the save file dialog
				Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
				dialog.Title = Params.SAVE_STROKE_FILE_DIALOG_TITLE_STR;
				dialog.FileName = Params.SAVE_STROKE_FILE_DEFAULT_NAME_STR;
				dialog.DefaultExt = Params.STROKE_FILE_EXTENSION_STR;
				dialog.Filter = Params.STROKE_FILE_FILTER_STR;

				// Open the save file dialog
				if (dialog.ShowDialog() == true)
				{
					if (SaveStrokes(dialog.FileName))
					{
						unsavedStrokes = false;
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void InitializeCurrentStroke()
		{
			currentStrokePoints.Clear();
		}

		/// <summary>
		/// Create a new Stroke object from the list of stroke points currently in 
		/// progress, save the stroke, and set up the program for capturing a new stroke.
		/// </summary>
		private void FinalizeCurrentStroke()
		{
			if (!isErasingStroke && currentStrokePoints.Count >= Params.MIN_POINT_COUNT)
			{
				DrawableStroke stroke = new DrawableStroke(currentStrokePoints, StrokeAngleSmoothCount);
				strokes.Add(stroke);

				IEnumerable<Drawable> displayDrawables = stroke.DrawablePoints;
				DisplayDrawables(displayDrawables);

				unsavedStrokes = true;
			}

			HideDrawables(currentStrokePoints);

			isStroking = false;
			isErasingStroke = false;

			InitializeCurrentStroke();

			UpdateShapeInstance();
		}

		/// <summary>
		/// Add the given Point to the list of Points for the current Stroke in progress 
		/// and render the new line segment formed from this new Point.
		/// </summary>
		private void AddPoint(DrawablePoint currentStrokePoint, double timestamp)
		{
			DrawablePoint previousStrokePoint = currentStrokePoints.Last();

			currentStrokePoints.Add(currentStrokePoint);

			currentStrokePoint.SetLineState(previousStrokePoint, currentStrokePoint, isErasingStroke);

			graphics.Add(previousStrokePoint.GetShape());

			if (isErasingStroke)
			{
				DrawablePoint[] points;
				List<KeyValuePair<double, DrawableStroke>> strokesToErase = new List<KeyValuePair<double, DrawableStroke>>();

				foreach (DrawableStroke stroke in strokes)
				{
					if (StrokePreProcessing.CheckForBoundingBoxIntersection(stroke.BoundingBox, 
							previousStrokePoint, currentStrokePoint))
					{
						points = stroke.DrawablePoints;

						for (int i = 0; i < points.Length - 1; i++)
						{
							if (StrokePreProcessing.GetPointOfIntersection(points[i], points[i + 1],
									previousStrokePoint, currentStrokePoint) != null)
							{
								strokesToErase.Add(new KeyValuePair<double, DrawableStroke>(timestamp, stroke));
								break;
							}
						}
					}
				}

				foreach (KeyValuePair<double, DrawableStroke> pair in strokesToErase)
				{
					EraseStroke(pair.Value, pair.Key);
				}
			}
		}

		/// <summary>
		/// Remove the given Stroke from the current list of Strokes.
		/// </summary>
		private void EraseStroke(DrawableStroke stroke, double timestamp)
		{
			strokes.Remove(stroke);
			if (!erasedStrokes.ContainsKey(timestamp))//TODO: this is a hack; fix the real problem
			{
				erasedStrokes.Add(timestamp, stroke);
			}

			IEnumerable<Drawable> hideDrawables = stroke.DrawablePoints;
			HideDrawables(hideDrawables);

			UpdateShapeInstance();
		}

		/// <summary>
		/// Show the lines from each of the given Points from the Canvas.
		/// </summary>
		private void DisplayDrawables(IEnumerable<Drawable> drawables)
		{
			foreach (Drawable drawable in drawables)
			{
				Shape shape = drawable.GetShape();
				if (shape != null)
				{
					graphics.Add(shape);
				}
			}
		}

		/// <summary>
		/// Remove the lines from each of the given Points from the Canvas.
		/// </summary>
		private void HideDrawables(IEnumerable<Drawable> drawables)
		{
			foreach (Drawable drawable in drawables)
			{
				Shape shape = drawable.GetShape();
				if (shape != null)
				{
					graphics.Remove(shape);
				}
			}
		}

		private void UpdateShapeInstance()
		{
			if (strokes.Count > 0)
			{
				canvasShapeInstance = new ShapeInstance(strokes, -1, -1, -1, TemplateColumnCount, ColumnCellCount, IntersectionGaussianWidth, true);
				if (recognizer.RecognitionEnabled)
				{
					recognizer.Recognize(canvasShapeInstance);
					if (canvasShapeInstance.RecognizedDistance < RecognitionDistanceThreshold)
					{
						canvasRecognizedAsLabel.Content = Params.ParseShapeIDToDescription(canvasShapeInstance.RecognizedShapeID);
					}
					else
					{
						canvasRecognizedAsLabel.Content = "--";
					}
				}
			}
			else
			{
				canvasShapeInstance = null;
				canvasRecognizedAsLabel.Content = "--";
			}
		}

		#region EVENT_HANDLERS

		/// <summary>
		/// Event handler for the PreviewKeyDown event.
		/// </summary>
		private void OnKeyDown(Object sender, KeyEventArgs e)
		{
			// Check whether the ctrl key is currently pressed
			if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
			{
				if (e.Key == Key.Z)
				{
					Undo();
				}
			}
		}

		/// <summary>
		/// Event handler for the Click event.
		/// </summary>
		private void OnClick(Object sender, RoutedEventArgs e)
		{
			if (sender == loadButton)
			{
				Load();
			}
			else if (sender == saveButton)
			{
				Save();
			}
			else if (sender == undoButton)
			{
				Undo();
			}
			else if (sender == selectRecognitionDirButton)
			{
				if (recognizer.GetPathToTrainingData())
				{
					recognitionDirLabel.Content = recognizer.TrainingDataPathTitle;

					String[] filePaths = Directory.GetFiles(recognizer.TrainingDataPath);

					HashSet<short> exampleNumbers = new HashSet<short>();
					short exampleNumber;
					String[] filePathDirectories;
					String filename;
					bool badFileNames = false;

					try
					{
						// Loop over each of the shape instance files
						foreach (String filePath in filePaths)
						{
							filePathDirectories = filePath.Split(Params.PATH_DIRECTORY_DELIMITER);
							filename = filePathDirectories.Last();

							// subXX-shpYY-exZZ.txt
							exampleNumber = Convert.ToInt16(filename.Substring(14, 2));
							exampleNumbers.Add(exampleNumber);
						}
					}
					catch (Exception ex)
					{
						badFileNames = true;
					}

					if (!badFileNames && exampleNumbers.Count > 0)
					{
						// Clear and fill in the list box
						holdOutListBox.Items.Clear();
						foreach (short exampleNum in exampleNumbers)
						{
							holdOutListBox.Items.Add(exampleNum);
						}

						crossValidateButton.IsEnabled = true;
					}
					else
					{
						String warningMsg =
							badFileNames ?
								"The directory \"" + recognizer.TrainingDataPath + "\" contains invalid filenames" :
								"The directory \"" + recognizer.TrainingDataPath + "\" does not contain any files";
						MessageBox.Show(warningMsg, Params.BAD_DIRECTORY_TITLE_STR, MessageBoxButton.OK, MessageBoxImage.Warning);
					}
				}
			}
			else if (sender == trainButton)
			{
				System.Collections.IList selectedListItems = holdOutListBox.SelectedItems;
				List<short> holdOut = new List<short>();
				foreach (Object selectedListItem in selectedListItems)
				{
					holdOut.Add((short)selectedListItem);
				}
				recognizer.Train(holdOut, TemplateColumnCount, ColumnCellCount, StrokeAngleSmoothCount, ColumnSmoothCount, IntersectionGaussianWidth, TemplateBoost);
				recognizer.Recognize(recognizer.HoldOuts);
				UpdateShapeInstance();

				viewTemplatesButton.IsEnabled = true;
				canvasRecognitionButton.IsEnabled = true;
				holdOutRecognitionButton.IsEnabled = true;
			}
			else if (sender == crossValidateButton)
			{
				recognizer.CrossValidate(TemplateColumnCount, ColumnCellCount, StrokeAngleSmoothCount, ColumnSmoothCount, IntersectionGaussianWidth, TemplateBoost);
				recognizerWindow.Show(RecognizerWindowType.CrossValidate);
			}
			else if (sender == viewTemplatesButton)
			{
				recognizerWindow.Show(RecognizerWindowType.Templates);
			}
			else if (sender == canvasRecognitionButton)
			{
				recognizerWindow.Show(RecognizerWindowType.Canvas);
			}
			else if (sender == holdOutRecognitionButton)
			{
				recognizerWindow.Show(RecognizerWindowType.HoldOut);
			}
		}

		/// <summary>
		/// Event handler for the TextChanged event.
		/// </summary>
		private void OnTextChange(object sender, TextChangedEventArgs e)
		{
			TextBox textBox = sender as TextBox;
			double newNumber = Double.NaN;
			bool validNumber = true;

			try
			{
				newNumber = Convert.ToDouble(textBox.Text);
			}
			catch(Exception ex)
			{
				validNumber = false;
			}

			validNumber = validNumber && newNumber >= 0;

			if (validNumber)
			{
				if (textBox == strokeAngleSmoothTextBox)
				{
					strokeAngleSmoothCount = (int)newNumber;
				}
				else if (textBox == columnSmoothTextBox)
				{
					columnSmoothCount = (int)newNumber;
				}
				else if (textBox == intersectionGaussianWidthTextBox)
				{
					intersectionGaussianWidth = (int)newNumber;
				}
				else if (textBox == templateBoostTextBox)
				{
					templateBoost = newNumber;
				}
				else if (textBox == recognitionDistanceThresholdTextBox)
				{
					recognitionDistanceThreshold = newNumber;
				}
				else if (textBox == templateColumnCountTextBox)
				{
					templateColumnCount = (short)newNumber;
				}
				else if (textBox == columnCellCountTextBox)
				{
					columnCellCount = (short)newNumber;
				}

				textBox.Style = (Style)(Resources["MenuSubTextBox"]);
			}

			if (!validNumber)
			{
				textBox.Style = (Style)(Resources["MenuSubTextBoxError"]);
			}
		}

		/// <summary>
		/// Event handler for the SelectionChanged event.
		/// </summary>
		private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender == holdOutListBox)
			{
				// Update the hold out label text
				System.Collections.IList selectedListItems = holdOutListBox.SelectedItems;
				if (selectedListItems.Count > 0)
				{
					StringBuilder strBuilder = new StringBuilder();
					foreach (Object selectedListItem in selectedListItems)
					{
						strBuilder.Append("" + (short)selectedListItem + ", ");
					}
					strBuilder.Remove(strBuilder.Length - 2, 2);
					holdOutLabel.Content = strBuilder.ToString();
					trainButton.IsEnabled = true;
				}
				else
				{
					holdOutLabel.Content = "--";
					trainButton.IsEnabled = false;
				}
			}
		}

		/// <summary>
		/// Event handler for the Checked event.
		/// </summary>
		private void OnCheck(object sender, RoutedEventArgs e)
		{
			CheckBox checkBox = sender as CheckBox;
		}

		/// <summary>
		/// Event handler for the Unchecked event.
		/// </summary>
		private void OnUncheck(object sender, RoutedEventArgs e)
		{
			CheckBox checkBox = sender as CheckBox;
		}

		/// <summary>
		/// Event handler for the MouseLeftButtonDown event.
		/// </summary>
		private void OnMouseLeftButtonDown(Object o, MouseButtonEventArgs e)
		{
			OnMouseDown(o, e, true);
		}

		/// <summary>
		/// Event handler for the MouseRightButtonDown event.
		/// </summary>
		private void OnMouseRightButtonDown(Object o, MouseButtonEventArgs e)
		{
			OnMouseDown(o, e, false);
		}

		/// <summary>
		/// Common helper method for the down event handlers of both mouse buttons.
		/// </summary>
		private void OnMouseDown(Object o, MouseButtonEventArgs e, bool isLeftButton)
		{
			double timestamp = GetTimestampInMillis();

			DrawablePoint point = new DrawablePoint(e.GetPosition(o as Canvas), timestamp);
	
			if (!isStroking)
			{
				isStroking = true;
				isErasingStroke = !isLeftButton;
				currentStrokePoints.Add(point);
			}
			else
			{
				FinalizeCurrentStroke();
			}
		}

		/// <summary>
		/// Event handler for the MouseUp event.
		/// </summary>
		private void OnMouseUp(Object o, MouseButtonEventArgs e)
		{
			if (isStroking)
			{
				FinalizeCurrentStroke();
			}
		}

		/// <summary>
		/// Event handler for the MouseMove event.
		/// </summary>
		private void OnMouseMove(Object o, MouseEventArgs e)
		{
			double timestamp = GetTimestampInMillis();

			if (isStroking)
			{
				DrawablePoint currentStrokePoint = new DrawablePoint(e.GetPosition(o as Canvas), timestamp);

				AddPoint(currentStrokePoint, timestamp);
			}
		}

		/// <summary>
		/// Event handler for the MouseMove event.
		/// </summary>
		private void OnMouseLeave(Object o, MouseEventArgs e)
		{
			if (isStroking)
			{
				FinalizeCurrentStroke();
			}
		}

		#endregion

	}
}
