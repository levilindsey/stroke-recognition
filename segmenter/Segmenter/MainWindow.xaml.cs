/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
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

		#region FIELD_DECLARATIONS

		private List<Point> currentStrokePoints;

		private List<Stroke> strokes;
		private SortedList<double, Stroke> erasedStrokes;

		private bool isStroking;
		private bool isErasingStroke;
		private bool unsavedStrokes;
		private bool graphIsOpen;
		private bool showSpeedCurv;
		private bool resampleWithSegAlgs;

		// The actual elements which are drawn on the image canvas
		private UIElementCollection graphics;

		private Stopwatch stopWatch;
		private double millisecPerTick;

		private PointProcessing pointProcessing;
		private SegmentationAlgorithm segmentationAlgorithm;
		private AxisValue xAxisValue;
		private AxisValue yAxisValue;
		private SaveValue saveValue;

		private int resampleCount;
		private int speedSmoothCount;
		private int curvatureSmoothCount;

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();

			graphics = canvas.Children;

			Initialize();
		}

		/// <summary>
		/// 
		/// </summary>
		private void Initialize()
		{
			graphics.Clear();

			currentStrokePoints = new List<Point>();

			isStroking = false;
			isErasingStroke = false;
			unsavedStrokes = false;
			graphIsOpen = false;

			showSpeedCurv = (bool)showSpeedCurvCheckBox.IsChecked;
			resampleWithSegAlgs = (bool)resampleCheckBox.IsChecked;

			pointProcessing = ComboBoxItemToPointProcessing(pointProcessingComboBox.SelectedItem as ComboBoxItem);
			segmentationAlgorithm = ComboBoxItemToSegmentationAlgorithm(segAlgComboBox.SelectedItem as ComboBoxItem);
			xAxisValue = ComboBoxItemToAxisValue(xAxisComboBox.SelectedItem as ComboBoxItem);
			yAxisValue = ComboBoxItemToAxisValue(yAxisComboBox.SelectedItem as ComboBoxItem);
			saveValue = ComboBoxItemToSaveValue(saveComboBox.SelectedItem as ComboBoxItem);

			resampleCount = Convert.ToInt32(resampleTextBox.Text);
			speedSmoothCount = Convert.ToInt32(speedSmoothTextBox.Text);
			curvatureSmoothCount = Convert.ToInt32(curvatureSmoothTextBox.Text);

			strokes = new List<Stroke>();
			erasedStrokes = new SortedList<double, Stroke>();

			StartStopwatch();

			InitializeCurrentStroke();
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
		private double GetTimestampInMillis()
		{
			return stopWatch.ElapsedTicks * millisecPerTick;
		}

		/// <summary>
		/// Load Strokes in from the given file path.
		/// </summary>
		private bool LoadStrokes(String path)
		{
			Uri uri = new Uri(path, UriKind.Absolute);
			FileStream inStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, Params.BUFFER_SIZE, false);
			StreamReader reader = new StreamReader(inStream);

			String[] tokens = null;
			Point point;
			Point[] points = null;
			Stroke stroke;
			int pointCount;
			int pointIndex = 0;

			IEnumerable<Drawable> displayDrawables;

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
							stroke = new Stroke(points, resampleCount, speedSmoothCount, 
								curvatureSmoothCount, showSpeedCurv, resampleWithSegAlgs);
							strokes.Add(stroke);

							displayDrawables = stroke.GetDrawables(pointProcessing, segmentationAlgorithm);

							DisplayDrawables(displayDrawables);
						}

						// Get the number of Points in the next Stroke
						pointCount = Convert.ToInt32(tokens[0]);
						points = new Point[pointCount];
						pointIndex = 0;
					}
					else
					{
						// Save the new Point
						point = Point.ParseStringToPoint(tokens);
						points[pointIndex++] = point;
					}
				}
			}

			// Create a Stroke object from the Points we have just read in
			if (points != null && points.Length >= Params.MIN_POINT_COUNT)
			{
				// Save the new Stroke
				stroke = new Stroke(points, resampleCount, speedSmoothCount, curvatureSmoothCount, 
					showSpeedCurv, resampleWithSegAlgs);
				strokes.Add(stroke);

				displayDrawables = stroke.GetDrawables(pointProcessing, segmentationAlgorithm);

				DisplayDrawables(displayDrawables);
			}

			// Clean up the StreamReader
			reader.Dispose();

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
			Drawable[] drawables;

			// Write out some sort of information about all of the strokes
			switch(saveValue)
			{
				case SaveValue.DefaultInts:
				case SaveValue.DefaultDbls:
				case SaveValue.ResampledInts:
				case SaveValue.ResampledDbls:
				case SaveValue.FeaturePointsInts:
				case SaveValue.FeaturePointsDbls:
					// Write out how many strokes are in this file
					dataFileEntry = strokes.Count.ToString();
					break;
				case SaveValue.ArcLength:
				case SaveValue.SpeedRaw:
				case SaveValue.SpeedSmoothed:
				case SaveValue.CurvatureRaw:
				case SaveValue.CurvatureSmoothed:
				case SaveValue.StrawValue:
					// Write out how many strokes are in this file AND a delimiter
					dataFileEntry = Params.BIG_LINE_DELIMITER + Params.NEWLINE_STR + 
						"Stroke count: " + strokes.Count.ToString() + 
						Params.NEWLINE_STR + Params.BIG_LINE_DELIMITER;
					break;
				case SaveValue.Segments:
				case SaveValue.FeaturePoints:
					// Write out how many strokes are in this file AND a delimiter
					dataFileEntry = Params.BIG_LINE_DELIMITER + Params.NEWLINE_STR +
						"Stroke count: " + strokes.Count.ToString() + 
						Params.NEWLINE_STR + Params.BIG_LINE_DELIMITER;
					break;
				default:
					dataFileEntry = null;
					break;
			}
			writer.WriteLine(dataFileEntry);

			foreach (Stroke stroke in strokes)
			{
				drawables = stroke.GetPoints(saveValue, segmentationAlgorithm);

				// Write out some sort of information about the overall contents of this stroke
				switch (saveValue)
				{
					case SaveValue.DefaultInts:
					case SaveValue.DefaultDbls:
					case SaveValue.ResampledInts:
					case SaveValue.ResampledDbls:
					case SaveValue.FeaturePointsInts:
					case SaveValue.FeaturePointsDbls:
						// Write out how many points are in this stroke
						dataFileEntry = drawables.Count().ToString();
						break;
					case SaveValue.ArcLength:
					case SaveValue.SpeedRaw:
					case SaveValue.SpeedSmoothed:
					case SaveValue.CurvatureRaw:
					case SaveValue.CurvatureSmoothed:
					case SaveValue.StrawValue:
						// Write out how many points are in this stroke AND a delimiter
						dataFileEntry = Params.BIG_LINE_DELIMITER + Params.NEWLINE_STR +
							"Point count: " + drawables.Count().ToString() +
							Params.NEWLINE_STR + Params.BIG_LINE_DELIMITER;
						break;
					case SaveValue.Segments:
						// Write out how many feature points are in this stroke AND a delimiter
						dataFileEntry = Params.BIG_LINE_DELIMITER + Params.NEWLINE_STR +
							"Segment count: " + drawables.Count().ToString() + Params.NEWLINE_STR +
							"(x1\ty1\tx2\ty2\tslope\ty-intercept) OR (centerX\tcenterY\tradius\tstartAngle\tendAngle\tisCW)" +
							Params.NEWLINE_STR + Params.BIG_LINE_DELIMITER;
						break;
					case SaveValue.FeaturePoints:
						// Write out how many segments are in this stroke AND a delimiter
						dataFileEntry = Params.BIG_LINE_DELIMITER + Params.NEWLINE_STR +
							"Feature point count: " + drawables.Count().ToString() + Params.NEWLINE_STR +
							"(index\tx\ty)" +
							Params.NEWLINE_STR + Params.BIG_LINE_DELIMITER;
						break;
					default:
						dataFileEntry = null;
						break;
				}
				writer.WriteLine(dataFileEntry);

				foreach (Drawable drawable in drawables)
				{
					// Write out the data for the current point
					dataFileEntry = drawable.GetDataFileEntry(saveValue, segmentationAlgorithm);
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
				Stroke lastCreatedStroke = strokes.Last();
				bool hideStroke = false;

				if (erasedStrokes.Count > 0)
				{
					KeyValuePair<double, Stroke> pair = erasedStrokes.Last();

					if (lastCreatedStroke.Timestamp > pair.Key)
					{
						strokes.RemoveAt(strokes.Count - 1);

						hideStroke = true;
					}
					else
					{
						Stroke lastErasedStroke = erasedStrokes.Last().Value;

						strokes.Add(lastErasedStroke);
						erasedStrokes.RemoveAt(erasedStrokes.Count - 1);

						displayDrawables = lastErasedStroke.GetDrawables(pointProcessing, segmentationAlgorithm);
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
					HideDrawables(lastCreatedStroke.RawPoints);
					HideDrawables(lastCreatedStroke.SmoothedPoints);
					HideDrawables(lastCreatedStroke.UnsegmentedResampledPoints);
					HideDrawables(lastCreatedStroke.ShortStrawPoints);
					HideDrawables(lastCreatedStroke.SpeedSegPoints);
					HideDrawables(lastCreatedStroke.CustomSegWOPostProcessPoints);
					HideDrawables(lastCreatedStroke.CustomSegPoints);
					HideDrawables(lastCreatedStroke.ShortStrawFeaturePts);
					HideDrawables(lastCreatedStroke.SpeedSegFeaturePts);
					HideDrawables(lastCreatedStroke.CustomSegWOPostProcessFeaturePts);
					HideDrawables(lastCreatedStroke.CustomSegFeaturePts);
					HideDrawables(lastCreatedStroke.ShortStrawSegments);
					HideDrawables(lastCreatedStroke.SpeedSegSegments);
					HideDrawables(lastCreatedStroke.CustomSegWOPostProcessSegments);
					HideDrawables(lastCreatedStroke.CustomSegSegments);
				}
			}
			else
			{
				if (erasedStrokes.Count > 0)
				{
					Stroke lastErasedStroke = erasedStrokes.Last().Value;

					strokes.Add(lastErasedStroke);
					erasedStrokes.RemoveAt(erasedStrokes.Count - 1);

					displayDrawables = lastErasedStroke.GetDrawables(pointProcessing, segmentationAlgorithm);
					DisplayDrawables(displayDrawables);
				}
			}
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

				if (LoadStrokes(dialog.FileName))
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
				Stroke stroke = new Stroke(currentStrokePoints, resampleCount, speedSmoothCount, 
					curvatureSmoothCount, showSpeedCurv, resampleWithSegAlgs);
				strokes.Add(stroke);

				IEnumerable<Drawable> displayDrawables = stroke.GetDrawables(pointProcessing, segmentationAlgorithm);
				DisplayDrawables(displayDrawables);

				unsavedStrokes = true;
			}

			HideDrawables(currentStrokePoints);

			isStroking = false;
			isErasingStroke = false;

			InitializeCurrentStroke();
		}

		/// <summary>
		/// Add the given Point to the list of Points for the current Stroke in progress 
		/// and render the new line segment formed from this new Point.
		/// </summary>
		private void AddPoint(Point currentStrokePoint, double timestamp)
		{
			Point previousStrokePoint = currentStrokePoints.Last();

			currentStrokePoints.Add(currentStrokePoint);

			StrokePreProcessing.SetLineState(previousStrokePoint, currentStrokePoint, showSpeedCurv, isErasingStroke);

			graphics.Add(currentStrokePoint.GetShape());

			if (isErasingStroke)
			{
				Point[] points;
				List<KeyValuePair<double, Stroke>> strokesToErase = new List<KeyValuePair<double, Stroke>>();

				foreach (Stroke stroke in strokes)
				{
					if (StrokePreProcessing.CheckForBoundingBoxIntersection(stroke.BoundingBox, 
							previousStrokePoint, currentStrokePoint))
					{
						points = stroke.GetPoints(pointProcessing, segmentationAlgorithm);

						for (int i = 0; i < points.Length - 1; i++)
						{
							if (StrokePreProcessing.GetPointOfIntersection(points[i], points[i + 1],
									previousStrokePoint, currentStrokePoint) != null)
							{
								strokesToErase.Add(new KeyValuePair<double, Stroke>(timestamp, stroke));
								break;
							}
						}
					}
				}

				foreach (KeyValuePair<double, Stroke> pair in strokesToErase)
				{
					EraseStroke(pair.Value, pair.Key);
				}
			}
		}

		/// <summary>
		/// Remove the given Stroke from the current list of Strokes.
		/// </summary>
		private void EraseStroke(Stroke stroke, double timestamp)
		{
			strokes.Remove(stroke);
			if (!erasedStrokes.ContainsKey(timestamp))//TODO: this is a hack; fix the real problem
			{
				erasedStrokes.Add(timestamp, stroke);
			}

			IEnumerable<Drawable> hideDrawables = 
				stroke.GetDrawables(pointProcessing, segmentationAlgorithm);
			HideDrawables(hideDrawables);
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

		/// <summary>
		/// Called when the user makes a change to a PointProcessing property.  Updates the 
		/// Canvas/graph accordingly.
		/// </summary>
		private void ChangePointProcessing(PointProcessing pointProcessing)
		{
			if (this.pointProcessing != pointProcessing)
			{
				RedrawCanvas(this.pointProcessing, pointProcessing, segmentationAlgorithm,
					segmentationAlgorithm, false);

				if (graphIsOpen && 
					(pointProcessing == PointProcessing.Raw ||
						pointProcessing == PointProcessing.Smoothed ||
						pointProcessing == PointProcessing.Resampled))
				{
					Graph(false);
				}
			}
		}

		/// <summary>
		/// Called when the user makes a change to a SegmentationAlgorithm property.  Updates the 
		/// Canvas accordingly.
		/// </summary>
		private void ChangeSegmentationAlgorithm(SegmentationAlgorithm segmentationAlgorithm)
		{
			RedrawCanvas(pointProcessing, pointProcessing, this.segmentationAlgorithm,
				segmentationAlgorithm, false);

			if (graphIsOpen)
				Graph(false);

			unsavedStrokes = true;
		}

		/// <summary>
		/// Update the Canvas to reflect the current system state.
		/// </summary>
		private void RedrawCanvas(PointProcessing oldPointProcessing, 
			PointProcessing newPointProcessing, SegmentationAlgorithm oldSegmentationAlgorithm,
			SegmentationAlgorithm newSegmentationAlgorithm, bool fromShowSpeedCurvChange)
		{
			if (fromShowSpeedCurvChange)
			{
				if (strokes != null)
				{
					foreach (Stroke stroke in strokes)
					{
						stroke.SetPointLineColors(showSpeedCurv);
					}
				}
			}
			else if (oldPointProcessing != newPointProcessing || 
				oldSegmentationAlgorithm != newSegmentationAlgorithm)
			{
				this.segmentationAlgorithm = newSegmentationAlgorithm;
				this.pointProcessing = newPointProcessing;

				IEnumerable<Drawable> hideDrawables, displayDrawables;

				if (strokes != null)
				{
					foreach (Stroke stroke in strokes)
					{
						hideDrawables = 
							stroke.GetDrawables(oldPointProcessing, oldSegmentationAlgorithm);
						displayDrawables = 
							stroke.GetDrawables(newPointProcessing, newSegmentationAlgorithm);

						HideDrawables(hideDrawables);
						DisplayDrawables(displayDrawables);
					}
				}
			}
		}

		/// <summary>
		/// Called when the user makes a change to an AxisValue property.  Updates the Graph 
		/// accordingly if it is currently displayed.
		/// </summary>
		private void ChangeAxisValue(AxisValue axisValue, bool isXAxis)
		{
			bool axisValueChanged = false;

			if (isXAxis && xAxisValue != axisValue)
			{
				xAxisValue = axisValue;
				axisValueChanged = true;
			}
			else if (!isXAxis && yAxisValue != axisValue)
			{
				yAxisValue = axisValue;
				axisValueChanged = true;
			}

			if (axisValueChanged && graphIsOpen)
			{
				Graph(false);
			}
		}

		/// <summary>
		/// Called when the user makes a change to a SaveValue property.
		/// </summary>
		private void ChangeSaveValue(SaveValue saveValue)
		{
			this.saveValue = saveValue;

			unsavedStrokes = true;
		}

		/// <summary>
		/// Update the state of the graph to match the current system state.
		/// </summary>
		private void Graph(bool forceGraphVisibilityChange)
		{
			if (forceGraphVisibilityChange)
			{
				graphIsOpen = !graphIsOpen;
			}

			if (graphIsOpen)
			{
				// Update the GUI state
				DisplayGraph(true);

				// Update the graph title
				chart.Title = AxisValueToAxisLabelString(yAxisValue) + Params.VS_STR + 
					AxisValueToAxisLabelString(xAxisValue);

				List<KeyValuePair<double, double>> valueList;
				LineSeries lineSeries;
				int i = 1;

				// Update the graph data series
				foreach (Stroke stroke in strokes)
				{
					// Create the values of the line series
					valueList = stroke.GetYVsXData(pointProcessing, segmentationAlgorithm, 
						xAxisValue, yAxisValue);

					// Create the line series
					lineSeries = new LineSeries();
					lineSeries.Title = Params.STROKE_STR + i++;
					lineSeries.DependentValueBinding = new Binding("Value");
					lineSeries.IndependentValueBinding = new Binding("Key");
					lineSeries.ItemsSource = valueList;

					// Add the line series to the chart
					chart.Series.Add(lineSeries);
				}
			}
			else
			{
				// Update the GUI state
				DisplayGraph(false);
			}
		}

		/// <summary>
		/// Update some important GUI state for displaying or collapsing the graph and canvas.
		/// </summary>
		private void DisplayGraph(bool idDisplayed)
		{
			if (idDisplayed)
			{
				chart.Width = mainWindow.ActualWidth - Params.MENU_WIDTH;
				chart.Visibility = Visibility.Visible;
				canvas.Visibility = Visibility.Collapsed;
				chart.Series.Clear();
				graphButton.Style = (Style)(Resources["MenuButtonPressed"]);
			}
			else
			{
				canvas.Visibility = Visibility.Visible;
				chart.Visibility = Visibility.Collapsed;
				chart.Series.Clear();
				graphButton.Style = (Style)(Resources["MenuButtonUnpressed"]);
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

			if (e.Key == Key.Escape && graphIsOpen)
			{
				Graph(true);
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
			else if (sender == graphButton)
			{
				Graph(true);
			}
		}

		/// <summary>
		/// Event handler for the TextChanged event.
		/// </summary>
		private void OnTextChange(object sender, RoutedEventArgs e)
		{
			TextBox textBox = sender as TextBox;
			int newNumber = Int32.MinValue;
			bool validNumber = true;

			try
			{
				newNumber = Convert.ToInt32(textBox.Text);
			}
			catch(Exception ex)
			{
				validNumber = false;
			}

			validNumber = validNumber && newNumber >= 0;

			if (validNumber)
			{
				if (textBox == resampleTextBox)
				{
					validNumber = newNumber >= Params.MIN_POINT_COUNT;

					if (validNumber)
					{
						resampleCount = newNumber;
					}
				}
				else if (textBox == speedSmoothTextBox)
				{
					speedSmoothCount = newNumber;
				}
				else if (textBox == curvatureSmoothTextBox)
				{
					curvatureSmoothCount = newNumber;
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
		private void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox comboBox = sender as ComboBox;

			ComboBoxItem comboBoxItem = comboBox.SelectedItem as ComboBoxItem;

			if (comboBox == saveComboBox)
			{
				ChangeSaveValue(ComboBoxItemToSaveValue(comboBoxItem));
			}
			else if (comboBox == pointProcessingComboBox)
			{
				ChangePointProcessing(ComboBoxItemToPointProcessing(comboBoxItem));
			}
			else if (comboBox == segAlgComboBox)
			{
				ChangeSegmentationAlgorithm(ComboBoxItemToSegmentationAlgorithm(comboBoxItem));
			}
			else if (comboBox == xAxisComboBox)
			{
				ChangeAxisValue(ComboBoxItemToAxisValue(comboBoxItem), true);
			}
			else if (comboBox == yAxisComboBox)
			{
				ChangeAxisValue(ComboBoxItemToAxisValue(comboBoxItem), false);
			}
		}

		/// <summary>
		/// Event handler for the Checked event.
		/// </summary>
		private void OnCheck(object sender, RoutedEventArgs e)
		{
			CheckBox checkBox = sender as CheckBox;

			if (checkBox == showSpeedCurvCheckBox)
			{
				bool oldShowSpeedCurv = showSpeedCurv;
				showSpeedCurv = true;
				if (oldShowSpeedCurv != showSpeedCurv)
				{
					RedrawCanvas(pointProcessing, pointProcessing, segmentationAlgorithm,
						segmentationAlgorithm, true);
				}
			}
			else if (checkBox == resampleCheckBox)
			{
				resampleWithSegAlgs = true;
			}
		}

		/// <summary>
		/// Event handler for the Unchecked event.
		/// </summary>
		private void OnUncheck(object sender, RoutedEventArgs e)
		{
			CheckBox checkBox = sender as CheckBox;

			if (checkBox == showSpeedCurvCheckBox)
			{
				bool oldShowSpeedCurv = showSpeedCurv;
				showSpeedCurv = false;
				if (oldShowSpeedCurv != showSpeedCurv)
				{
					RedrawCanvas(pointProcessing, pointProcessing, segmentationAlgorithm, 
						segmentationAlgorithm, true);
				}
			}
			else if (checkBox == resampleCheckBox)
			{
				resampleWithSegAlgs = false;
			}
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

			Point point = new Point(e.GetPosition(o as Canvas), timestamp);
	
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
				Point currentStrokePoint = new Point(e.GetPosition(o as Canvas), timestamp);

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

		#region ENUM_PARSING

		private PointProcessing ComboBoxItemToPointProcessing(ComboBoxItem comboBoxItem)
		{
			if (comboBoxItem == toRenderRawComboBoxItem)
			{
				return PointProcessing.Raw;
			}
			else if (comboBoxItem == toRenderSmoothedComboBoxItem)
			{
				return PointProcessing.Smoothed;
			}
			else if (comboBoxItem == toRenderResampledComboBoxItem)
			{
				return PointProcessing.Resampled;
			}
			else if (comboBoxItem == toRenderFeaturePtsComboBoxItem)
			{
				return PointProcessing.FeaturePts;
			}
			else if (comboBoxItem == toRenderSegmentsComboBoxItem)
			{
				return PointProcessing.Segments;
			}
			else if (comboBoxItem == toRenderPointsAndSegmentsComboBoxItem)
			{
				return PointProcessing.PointsAndSegments;
			}
			else if (comboBoxItem == toRenderInkPointsAndSegmentsComboBoxItem)
			{
				return PointProcessing.InkPointsAndSegments;
			}
			else
			{
				return PointProcessing.Raw;
			}
		}

		private SegmentationAlgorithm ComboBoxItemToSegmentationAlgorithm(ComboBoxItem comboBoxItem)
		{
			if (comboBoxItem == segAlgEndPointsOnlyComboBoxItem)
			{
				return SegmentationAlgorithm.EndPointsOnly;
			}
			else if (comboBoxItem == segAlgShortStrawComboBoxItem)
			{
				return SegmentationAlgorithm.ShortStraw;
			}
			else if (comboBoxItem == segAlgSpeedSegComboBoxItem)
			{
				return SegmentationAlgorithm.SpeedSeg;
			}
			else if (comboBoxItem == segAlgCustomSegWOPostProcComboBoxItem)
			{
				return SegmentationAlgorithm.CustomSegWOPostProcess;
			}
			else if (comboBoxItem == segAlgCustomSegComboBoxItem)
			{
				return SegmentationAlgorithm.CustomSeg;
			}
			else
			{
				return SegmentationAlgorithm.SpeedSeg;
			}
		}

		private AxisValue ComboBoxItemToAxisValue(ComboBoxItem comboBoxItem)
		{
			if (comboBoxItem == xAxisXComboBoxItem || comboBoxItem == yAxisXComboBoxItem)
			{
				return AxisValue.X;
			}
			else if (comboBoxItem == xAxisYComboBoxItem || comboBoxItem == yAxisYComboBoxItem)
			{
				return AxisValue.Y;
			}
			else if (comboBoxItem == xAxisTimestampComboBoxItem || comboBoxItem == yAxisTimestampComboBoxItem)
			{
				return AxisValue.Timestamp;
			}
			else if (comboBoxItem == xAxisArcLengthComboBoxItem || comboBoxItem == yAxisArcLengthComboBoxItem)
			{
				return AxisValue.ArcLength;
			}
			else if (comboBoxItem == xAxisSpeedComboBoxItem || comboBoxItem == yAxisSpeedComboBoxItem)
			{
				return AxisValue.Speed;
			}
			else if (comboBoxItem == xAxisCurvatureComboBoxItem || comboBoxItem == yAxisCurvatureComboBoxItem)
			{
				return AxisValue.Curvature;
			}
			else if (comboBoxItem == xAxisStrawValueComboBoxItem || comboBoxItem == yAxisStrawValueComboBoxItem)
			{
				return AxisValue.StrawValue;
			}
			else
			{
				return AxisValue.Timestamp;
			}
		}

		private SaveValue ComboBoxItemToSaveValue(ComboBoxItem comboBoxItem)
		{
			if (comboBoxItem == saveDefaultIntsComboBoxItem)
			{
				return SaveValue.DefaultInts;
			}
			if (comboBoxItem == saveDefaultDblsComboBoxItem)
			{
				return SaveValue.DefaultDbls;
			}
			else if (comboBoxItem == saveResampledIntsComboBoxItem)
			{
				return SaveValue.ResampledInts;
			}
			else if (comboBoxItem == saveResampledDblsComboBoxItem)
			{
				return SaveValue.ResampledDbls;
			}
			else if (comboBoxItem == saveFeaturePointsIntsComboBoxItem)
			{
				return SaveValue.FeaturePointsInts;
			}
			else if (comboBoxItem == saveFeaturePointsDblsComboBoxItem)
			{
				return SaveValue.FeaturePointsDbls;
			}
			else if (comboBoxItem == saveArcLengthComboBoxItem)
			{
				return SaveValue.ArcLength;
			}
			else if (comboBoxItem == saveSpeedRawComboBoxItem)
			{
				return SaveValue.SpeedRaw;
			}
			else if (comboBoxItem == saveSpeedSmoothedComboBoxItem)
			{
				return SaveValue.SpeedSmoothed;
			}
			else if (comboBoxItem == saveCurvatureRawComboBoxItem)
			{
				return SaveValue.CurvatureRaw;
			}
			else if (comboBoxItem == saveCurvatureSmoothedComboBoxItem)
			{
				return SaveValue.CurvatureSmoothed;
			}
			else if (comboBoxItem == saveStrawValueComboBoxItem)
			{
				return SaveValue.StrawValue;
			}
			else if (comboBoxItem == saveFeaturePointsComboBoxItem)
			{
				return SaveValue.FeaturePoints;
			}
			else if (comboBoxItem == saveSegmentsComboBoxItem)
			{
				return SaveValue.Segments;
			}
			else
			{
				return SaveValue.DefaultInts;
			}
		}

		private static String AxisValueToAxisLabelString(AxisValue axisValue)
		{
			switch (axisValue)
			{
				case AxisValue.X:
					return Params.X_AXIS_LABEL_STR;
				case AxisValue.Y:
					return Params.Y_AXIS_LABEL_STR;
				case AxisValue.Timestamp:
					return Params.TIMESTAMP_AXIS_LABEL_STR;
				case AxisValue.ArcLength:
					return Params.ARC_LENGTH_AXIS_LABEL_STR;
				case AxisValue.Speed:
					return Params.SPEED_AXIS_LABEL_STR;
				case AxisValue.Curvature:
					return Params.CURVATURE_AXIS_LABEL_STR;
				case AxisValue.StrawValue:
					return Params.STRAW_VALUE_AXIS_LABEL_STR;
				default:
					return Params.TIMESTAMP_AXIS_LABEL_STR;
			}
		}

		#endregion

	}
}
