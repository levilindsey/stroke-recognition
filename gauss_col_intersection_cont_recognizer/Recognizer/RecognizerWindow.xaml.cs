/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StrokeCollector
{
	/// <summary>
	/// Interaction logic for CanvasRecognitionWindow.xaml
	/// </summary>
	public partial class RecognizerWindow : Window
	{
		private MainWindow mainWindow;
		private Recognizer recognizer;

		// The actual elements which are drawn on the canvas
		private UIElementCollection graphicsTopLeft;
		private List<Line> bitmapGridLinesTopLeft;
		private Rectangle[][] bitmapGridSquaresTopLeft;
		private UIElementCollection graphicsTopRight;
		private List<Line> bitmapGridLinesTopRight;
		private Rectangle[][] bitmapGridSquaresTopRight;
		private UIElementCollection graphicsBottomLeft;
		private List<Line> bitmapGridLinesBottomLeft;
		private Rectangle[][] bitmapGridSquaresBottomLeft;
		private UIElementCollection graphicsBottomRight;
		private List<Line> bitmapGridLinesBottomRight;
		private Rectangle[][] bitmapGridSquaresBottomRight;

		private RecognizerWindowType recognizerWindowType;

		private int currentIndex;

		public RecognizerWindow(MainWindow mainWindow)
		{
			InitializeComponent();

			this.mainWindow = mainWindow;
			this.recognizer = this.mainWindow.Recognizer;

			Initialize();
		}

		private void Initialize()
		{
			graphicsTopLeft = topLeftCanvas.Children;
			graphicsTopLeft.Clear();
			bitmapGridLinesTopLeft = new List<Line>();
			bitmapGridSquaresTopLeft = new Rectangle[MainWindow.TemplateColumnCount][];
			InstantiateRectangleValues(bitmapGridSquaresTopLeft, MainWindow.ColumnCellCount);
			graphicsTopRight = topRightCanvas.Children;
			graphicsTopRight.Clear();
			bitmapGridLinesTopRight = new List<Line>();
			bitmapGridSquaresTopRight = new Rectangle[MainWindow.TemplateColumnCount][];
			InstantiateRectangleValues(bitmapGridSquaresTopRight, MainWindow.ColumnCellCount);
			graphicsBottomLeft = bottomLeftCanvas.Children;
			graphicsBottomLeft.Clear();
			bitmapGridLinesBottomLeft = new List<Line>();
			bitmapGridSquaresBottomLeft = new Rectangle[MainWindow.TemplateColumnCount][];
			InstantiateRectangleValues(bitmapGridSquaresBottomLeft, MainWindow.ColumnCellCount);
			graphicsBottomRight = bottomRightCanvas.Children;
			graphicsBottomRight.Clear();
			bitmapGridLinesBottomRight = new List<Line>();
			bitmapGridSquaresBottomRight = new Rectangle[MainWindow.TemplateColumnCount][];
			InstantiateRectangleValues(bitmapGridSquaresBottomRight, MainWindow.ColumnCellCount);

			recognizerWindowType = RecognizerWindowType.Canvas;

			currentIndex = 0;

			CreateGridSquares();
			CreateGridLines();
		}

		public void Show(RecognizerWindowType recognizerWindowType)
		{
			this.recognizerWindowType = recognizerWindowType;
			currentIndex = 0;
			Refresh();
			this.Show();
		}

		private void OnWindowClose(object sender, CancelEventArgs e)
		{
			if (!mainWindow.MainWindowClose)
			{
				e.Cancel = true;
				Hide();
			}
		}

		private void DrawStrokes(Canvas canvas,
			IEnumerable<RecognizerStroke> strokes, bool showStrokePoints,
			bool showStrokeLines, bool colorCodeDots, bool isNormalized)
		{
			RecognizerPoint[] points;
			Ellipse dot;
			int row, col;
			double canvasWidth = canvas.Width;
			double cellWidth = canvasWidth / MainWindow.TemplateColumnCount;
			double cellHeight = canvasWidth / MainWindow.ColumnCellCount;
			UIElementCollection canvasGraphics = canvas.Children;

			// Draw the line segments?
			if (showStrokeLines)
			{
				Line line;
				foreach (RecognizerStroke stroke in strokes)
				{
					points = stroke.RecognizerPoints;
					for (int j = 0; j < points.Length - 1; ++j)
					{
						line = new Line();
						line.X1 = points[j].X * canvasWidth;
						line.Y1 = points[j].Y * canvasWidth;
						line.X2 = points[j + 1].X * canvasWidth;
						line.Y2 = points[j + 1].Y * canvasWidth;
						line.Stroke = Params.BITMAP_STROKE_BRUSH;
						line.StrokeThickness = Params.BITMAP_STROKE_THICKNESS;
						canvasGraphics.Add(line);
					}
				}
			}

			// Draw the line points?
			if (showStrokePoints)
			{
				foreach (RecognizerStroke stroke in strokes)
				{
					points = stroke.RecognizerPoints;
					for (int j = 1; j < points.Length - 1; ++j)
					{
						dot = new Ellipse();
						if (colorCodeDots)
						{
							row = (int)(points[j].X * canvasWidth / cellWidth);
							col = (int)(points[j].Y * canvasWidth / cellWidth);
							if (row % 2 == 0)
							{
								if (col % 2 == 0)
								{
									dot.Fill = Params.BITMAP_PIXEL_DOT_1_BRUSH;
								}
								else
								{
									dot.Fill = Params.BITMAP_PIXEL_DOT_2_BRUSH;
								}
							}
							else
							{
								if (col % 2 == 0)
								{
									dot.Fill = Params.BITMAP_PIXEL_DOT_2_BRUSH;
								}
								else
								{
									dot.Fill = Params.BITMAP_PIXEL_DOT_1_BRUSH;
								}
							}
						}
						else
						{
							dot.Fill = Params.BITMAP_PIXEL_DOT_DEFAULT_BRUSH;
						}
						dot.Stroke = Params.BITMAP_PIXEL_DOT_DEFAULT_BRUSH;
						dot.StrokeThickness = Params.BITMAP_DOT_THICKNESS;
						dot.Margin = new Thickness(
							points[j].X * canvasWidth - Params.BITMAP_POINT_RADIUS,
							points[j].Y * canvasWidth - Params.BITMAP_POINT_RADIUS,
							0, 0);
						dot.Width = Params.BITMAP_POINT_DIAMETER;
						dot.Height = Params.BITMAP_POINT_DIAMETER;
						canvasGraphics.Add(dot);
					}

					// Draw special dots for the first and last points

					// First point
					dot = new Ellipse();
					if (colorCodeDots)
					{
						dot.Fill = Params.BITMAP_PIXEL_DOT_FIRST_BRUSH;
					}
					else
					{
						dot.Fill = Params.BITMAP_PIXEL_DOT_DEFAULT_BRUSH;
					}
					dot.Stroke = Params.BITMAP_PIXEL_DOT_DEFAULT_BRUSH;
					dot.StrokeThickness = Params.BITMAP_DOT_THICKNESS;
					dot.Margin = new Thickness(
						points[0].X * canvasWidth - Params.BITMAP_END_POINT_RADIUS,
						points[0].Y * canvasWidth - Params.BITMAP_END_POINT_RADIUS,
						0, 0);
					dot.Width = Params.BITMAP_END_POINT_DIAMETER;
					dot.Height = Params.BITMAP_END_POINT_DIAMETER;
					canvasGraphics.Add(dot);

					// Last point
					dot = new Ellipse();
					if (colorCodeDots)
					{
						dot.Fill = Params.BITMAP_PIXEL_DOT_LAST_BRUSH;
					}
					else
					{
						dot.Fill = Params.BITMAP_PIXEL_DOT_DEFAULT_BRUSH;
					}
					dot.Stroke = Params.BITMAP_PIXEL_DOT_DEFAULT_BRUSH;
					dot.StrokeThickness = Params.BITMAP_DOT_THICKNESS;
					dot.Margin = new Thickness(
						points[points.Length - 1].X * canvasWidth - Params.BITMAP_END_POINT_RADIUS,
						points[points.Length - 1].Y * canvasWidth - Params.BITMAP_END_POINT_RADIUS,
						0, 0);
					dot.Width = Params.BITMAP_END_POINT_DIAMETER;
					dot.Height = Params.BITMAP_END_POINT_DIAMETER;
					canvasGraphics.Add(dot);
				}
			}
		}

		private void DrawGridCells(UIElementCollection canvas, Rectangle[][] gridCells)
		{
			foreach (Rectangle[] subGridCells in gridCells)
			{
				foreach (Rectangle gridCell in subGridCells)
				{
					canvas.Add(gridCell);
				}
			}
		}

		private void DrawGridLines(UIElementCollection canvas, List<Line> gridLines)
		{
			foreach (Line gridLine in gridLines)
			{
				canvas.Add(gridLine);
			}
		}

		private void UpdateGridValuesForShapeInstance(ShapeInstance shapeInstance)
		{
			Template template;
			bool displayShapeInstanceValuesAlone = false;
			bool displaySmoothedValues = true;

			if (unsmoothedShapeInstanceValuesAloneRadioButton.IsChecked == true)
			{
				displayShapeInstanceValuesAlone = true;
				displaySmoothedValues = false;
			}
			else if (smoothedShapeInstanceValuesAloneRadioButton.IsChecked == true)
			{
				displayShapeInstanceValuesAlone = true;
				displaySmoothedValues = true;
			}
			else if (noPixelValuesRadioButton.IsChecked != true)
			{
				if (smoothedTemplateOverlayActualRadioButton.IsChecked == true)
				{
					template = recognizer.GetTemplate(shapeInstance.ActualShapeID);
					displaySmoothedValues = true;
					if (template != null)
					{
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresTopLeft, shapeInstance.ColumnsSmoothed0, template.Columns0);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresTopRight, shapeInstance.ColumnsSmoothed45, template.Columns45);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresBottomLeft, shapeInstance.ColumnsSmoothed90, template.Columns90);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresBottomRight, shapeInstance.ColumnsSmoothed135, template.Columns135);
					}
					else
					{
						displayShapeInstanceValuesAlone = true;
					}
				}
				else if (smoothedTemplateOverlayRecognizedRadioButton.IsChecked == true)
				{
					template = recognizer.GetTemplate(shapeInstance.RecognizedShapeID);
					displaySmoothedValues = true;
					if (template != null)
					{
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresTopLeft, shapeInstance.ColumnsSmoothed0, template.Columns0);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresTopRight, shapeInstance.ColumnsSmoothed45, template.Columns45);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresBottomLeft, shapeInstance.ColumnsSmoothed90, template.Columns90);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresBottomRight, shapeInstance.ColumnsSmoothed135, template.Columns135);
					}
					else
					{
						displayShapeInstanceValuesAlone = true;
					}
				}
				else if (unsmoothedTemplateOverlayActualRadioButton.IsChecked == true)
				{
					template = recognizer.GetTemplate(shapeInstance.ActualShapeID);
					displaySmoothedValues = false;
					if (template != null)
					{
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresTopLeft, shapeInstance.Columns0, template.Columns0);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresTopRight, shapeInstance.Columns45, template.Columns45);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresBottomLeft, shapeInstance.Columns90, template.Columns90);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresBottomRight, shapeInstance.Columns135, template.Columns135);
					}
					else
					{
						displayShapeInstanceValuesAlone = true;
					}
				}
				else if (unsmoothedTemplateOverlayRecognizedRadioButton.IsChecked == true)
				{
					template = recognizer.GetTemplate(shapeInstance.RecognizedShapeID);
					displaySmoothedValues = false;
					if (template != null)
					{
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresTopLeft, shapeInstance.Columns0, template.Columns0);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresTopRight, shapeInstance.Columns45, template.Columns45);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresBottomLeft, shapeInstance.Columns90, template.Columns90);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresBottomRight, shapeInstance.Columns135, template.Columns135);
					}
					else
					{
						displayShapeInstanceValuesAlone = true;
					}
				}
				else if (templateDistancesActualRadioButton.IsChecked == true)
				{
					template = recognizer.GetTemplate(shapeInstance.ActualShapeID);
					displaySmoothedValues = true;
					if (showDistanceNumbersCheckBox.IsChecked == true)
					{
						if (template != null)
						{
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopLeft, shapeInstance.ColumnsSmoothed0, template.Columns0, true);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopRight, shapeInstance.ColumnsSmoothed45, template.Columns45, true);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomLeft, shapeInstance.ColumnsSmoothed90, template.Columns90, true);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomRight, shapeInstance.ColumnsSmoothed135, template.Columns135, true);
						}
						else
						{
							displayShapeInstanceValuesAlone = true;
						}
					}
					else
					{
						if (template != null)
						{
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopLeft, shapeInstance.ColumnsSmoothed0, template.Columns0, false);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopRight, shapeInstance.ColumnsSmoothed45, template.Columns45, false);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomLeft, shapeInstance.ColumnsSmoothed90, template.Columns90, false);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomRight, shapeInstance.ColumnsSmoothed135, template.Columns135, false);
						}
						else
						{
							displayShapeInstanceValuesAlone = true;
						}
					}
				}
				else if (templateDistancesRecognizedRadioButton.IsChecked == true)
				{
					template = recognizer.GetTemplate(shapeInstance.RecognizedShapeID);
					displaySmoothedValues = true;
					if (showDistanceNumbersCheckBox.IsChecked == true)
					{
						if (template != null)
						{
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopLeft, shapeInstance.ColumnsSmoothed0, template.Columns0, true);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopRight, shapeInstance.ColumnsSmoothed45, template.Columns45, true);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomLeft, shapeInstance.ColumnsSmoothed90, template.Columns90, true);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomRight, shapeInstance.ColumnsSmoothed135, template.Columns135, true);
						}
						else
						{
							displayShapeInstanceValuesAlone = true;
						}
					}
					else
					{
						if (template != null)
						{
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopLeft, shapeInstance.ColumnsSmoothed0, template.Columns0, false);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopRight, shapeInstance.ColumnsSmoothed45, template.Columns45, false);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomLeft, shapeInstance.ColumnsSmoothed90, template.Columns90, false);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomRight, shapeInstance.ColumnsSmoothed135, template.Columns135, false);
						}
						else
						{
							displayShapeInstanceValuesAlone = true;
						}
					}
				}
			}

			if (displayShapeInstanceValuesAlone)
			{
				if (displaySmoothedValues)
				{
					UpdateDirectionalValuesInstanceAlone(bitmapGridSquaresTopLeft, shapeInstance.ColumnsSmoothed0);
					UpdateDirectionalValuesInstanceAlone(bitmapGridSquaresTopRight, shapeInstance.ColumnsSmoothed45);
					UpdateDirectionalValuesInstanceAlone(bitmapGridSquaresBottomLeft, shapeInstance.ColumnsSmoothed90);
					UpdateDirectionalValuesInstanceAlone(bitmapGridSquaresBottomRight, shapeInstance.ColumnsSmoothed135);
				}
				else
				{
					UpdateDirectionalValuesInstanceAlone(bitmapGridSquaresTopLeft, shapeInstance.Columns0);
					UpdateDirectionalValuesInstanceAlone(bitmapGridSquaresTopRight, shapeInstance.Columns45);
					UpdateDirectionalValuesInstanceAlone(bitmapGridSquaresBottomLeft, shapeInstance.Columns90);
					UpdateDirectionalValuesInstanceAlone(bitmapGridSquaresBottomRight, shapeInstance.Columns135);
				}
			}
		}

		private void UpdateGridValuesForTemplate(Template template)
		{
			UpdateDirectionalGridSquaresForTemplate(bitmapGridSquaresTopLeft, template.Columns0);
			UpdateDirectionalGridSquaresForTemplate(bitmapGridSquaresTopRight, template.Columns45);
			UpdateDirectionalGridSquaresForTemplate(bitmapGridSquaresBottomLeft, template.Columns90);
			UpdateDirectionalGridSquaresForTemplate(bitmapGridSquaresBottomRight, template.Columns135);
		}

		private void UpdateDirectionalValuesInstanceAlone(Rectangle[][] bitmapGridSquares,
			Dictionary<int, double>[] instanceColumns)
		{
			Color cellColor;
			byte colorIntensity;

			// Display just the shape instance probabilities
			for (int col = 0; col < bitmapGridSquares.Length; ++col)
			{
				for (int row = 0; row < bitmapGridSquares[col].Length; ++row)
				{
					colorIntensity = instanceColumns[col].ContainsKey(row) ?
						(byte)(instanceColumns[col][row] * 255) : (byte)0;
					cellColor = Color.FromArgb(255, colorIntensity, colorIntensity, colorIntensity);
					((SolidColorBrush)bitmapGridSquares[col][row].Fill).Color = cellColor;
				}
			}
		}

		private void UpdateDirectionalValuesInstanceAlone(Rectangle[][] bitmapGridSquares,
			double[][] instanceColumns)
		{
			Color cellColor;
			byte colorIntensity;

			// Display just the shape instance probabilities
			for (int col = 0; col < bitmapGridSquares.Length; ++col)
			{
				for (int row = 0; row < bitmapGridSquares[col].Length; ++row)
				{
					colorIntensity = (byte)(instanceColumns[col][row] * 255);
					cellColor = Color.FromArgb(255, colorIntensity, colorIntensity, colorIntensity);
					((SolidColorBrush)bitmapGridSquares[col][row].Fill).Color = cellColor;
				}
			}
		}

		private void UpdateDirectionalValuesTemplateOverlay(Rectangle[][] bitmapGridSquares,
			Dictionary<int, double>[] instanceColumns, double[][] templateColumns)
		{
			Color cellColor;
			double tempR, tempG, tempB;
			byte r, g, b;

			// Overlay both the shape instance probabilities and the template probabilities
			for (int col = 0; col < bitmapGridSquares.Length; ++col)
			{
				for (int row = 0; row < bitmapGridSquares[col].Length; ++row)
				{
					if (instanceColumns[col].ContainsKey(row))
					{
						tempR = instanceColumns[col][row] * 50 + templateColumns[col][row] * 455;
						tempG = instanceColumns[col][row] * 255 + templateColumns[col][row] * 255;
						tempB = instanceColumns[col][row] * 50 + templateColumns[col][row] * 50;
					}
					else
					{
						tempR = templateColumns[col][row] * 680;
						tempG = templateColumns[col][row] * 380;
						tempB = templateColumns[col][row] * 80;
					}
					r = (byte)(Math.Min(tempR, 255));
					g = (byte)(Math.Min(tempG, 255));
					b = (byte)(Math.Min(tempB, 255));
					cellColor = Color.FromArgb(255, r, g, b);
					((SolidColorBrush)bitmapGridSquares[col][row].Fill).Color = cellColor;
				}
			}
		}

		private void UpdateDirectionalValuesTemplateOverlay(Rectangle[][] bitmapGridSquares,
			double[][] instanceColumns, double[][] templateColumns)
		{
			Color cellColor;
			double tempR, tempG, tempB;
			byte r, g, b;

			// Overlay both the shape instance probabilities and the template probabilities
			for (int col = 0; col < bitmapGridSquares.Length; ++col)
			{
				for (int row = 0; row < bitmapGridSquares[col].Length; ++row)
				{
					tempR = instanceColumns[col][row] * 50 + templateColumns[col][row] * 455;
					tempG = instanceColumns[col][row] * 255 + templateColumns[col][row] * 255;
					tempB = instanceColumns[col][row] * 50 + templateColumns[col][row] * 50;
					r = (byte)(Math.Min(tempR, 255));
					g = (byte)(Math.Min(tempG, 255));
					b = (byte)(Math.Min(tempB, 255));
					cellColor = Color.FromArgb(255, r, g, b);
					((SolidColorBrush)bitmapGridSquares[col][row].Fill).Color = cellColor;
				}
			}
		}

		private void UpdateDirectionalValuesTemplateDistances(Rectangle[][] bitmapGridSquares,
			double[][] instanceColumns, double[][] templateColumns, bool showNumbers)
		{
			Color cellColor;
			double tempR, tempG, tempB;
			byte r, g, b;

			for (int col = 0; col < bitmapGridSquares.Length; ++col)
			{
				for (int row = 0; row < bitmapGridSquares[col].Length; ++row)
				{
					tempR = Math.Abs(instanceColumns[col][row] - templateColumns[col][row]);
					tempG = Math.Abs(instanceColumns[col][row] - templateColumns[col][row]);
					tempB = Math.Abs(instanceColumns[col][row] - templateColumns[col][row]);
					r = (byte)(Math.Min(tempR * 255, 255));
					g = (byte)(Math.Min(tempG * 50, 255));
					b = (byte)(Math.Min(tempB * 50, 255));
					cellColor = Color.FromArgb(255, r, g, b);
					((SolidColorBrush)bitmapGridSquares[col][row].Fill).Color = cellColor;
				}
			}
		}

		private void UpdateDirectionalGridSquaresForTemplate(Rectangle[][] bitmapGridSquares,
			double[][] templateColumns)
		{
			Color cellColor;
			byte colorIntensity;

			for (int col = 0; col < bitmapGridSquares.Length; ++col)
			{
				for (int row = 0; row < bitmapGridSquares[col].Length; ++row)
				{
					colorIntensity = (byte)(templateColumns[col][row] * 255);
					cellColor = Color.FromArgb(255, colorIntensity, colorIntensity, colorIntensity);
					((SolidColorBrush)bitmapGridSquares[col][row].Fill).Color = cellColor;
				}
			}
		}

		private void CreateGridSquares()
		{
			double directionalGridWidth, directionalPixelWidth;
			Rectangle gridCell;
			Thickness directionalThickness;

			short columnCount = MainWindow.TemplateColumnCount;
			short columnCellCount = MainWindow.ColumnCellCount;

			// Get the width/height of the grid
			directionalGridWidth = topLeftCanvas.Width;
			directionalPixelWidth = directionalGridWidth / columnCount;

			// Draw the bitmap cells
			for (int row = 0; row < columnCellCount; ++row)
			{
				for (int col = 0; col < columnCount; ++col)
				{
					directionalThickness = new Thickness(col * directionalPixelWidth, row * directionalPixelWidth, 0, 0);

					// Top-left
					gridCell = new Rectangle();
					gridCell.Margin = directionalThickness;
					gridCell.Width = directionalPixelWidth;
					gridCell.Height = directionalPixelWidth;
					gridCell.Fill = new SolidColorBrush();
					gridCell.Stroke = Params.BITMAP_CELL_BRUSH;
					gridCell.StrokeThickness = Params.BITMAP_CELL_THICKNESS;
					bitmapGridSquaresTopLeft[col][row] = gridCell;

					// Top-right
					gridCell = new Rectangle();
					gridCell.Margin = directionalThickness;
					gridCell.Width = directionalPixelWidth;
					gridCell.Height = directionalPixelWidth;
					gridCell.Fill = new SolidColorBrush();
					gridCell.Stroke = Params.BITMAP_CELL_BRUSH;
					gridCell.StrokeThickness = Params.BITMAP_CELL_THICKNESS;
					bitmapGridSquaresTopRight[col][row] = gridCell;

					// Bottom-left
					gridCell = new Rectangle();
					gridCell.Margin = directionalThickness;
					gridCell.Width = directionalPixelWidth;
					gridCell.Height = directionalPixelWidth;
					gridCell.Fill = new SolidColorBrush();
					gridCell.Stroke = Params.BITMAP_CELL_BRUSH;
					gridCell.StrokeThickness = Params.BITMAP_CELL_THICKNESS;
					bitmapGridSquaresBottomLeft[col][row] = gridCell;

					// Bottom-right
					gridCell = new Rectangle();
					gridCell.Margin = directionalThickness;
					gridCell.Width = directionalPixelWidth;
					gridCell.Height = directionalPixelWidth;
					gridCell.Fill = new SolidColorBrush();
					gridCell.Stroke = Params.BITMAP_CELL_BRUSH;
					gridCell.StrokeThickness = Params.BITMAP_CELL_THICKNESS;
					bitmapGridSquaresBottomRight[col][row] = gridCell;
				}
			}
		}

		private void CreateGridLines()
		{
			double directionalGridWidth, directionalPixelWidth, directionalPixelHeight;
			Line gridLine;

			short columnCount = MainWindow.TemplateColumnCount;
			short columnCellCount = MainWindow.ColumnCellCount;

			// Get the width/height of the grid
			directionalGridWidth = topLeftCanvas.Width;
			directionalPixelWidth = directionalGridWidth / columnCount;
			directionalPixelHeight = directionalGridWidth / columnCellCount;

			// Draw the horizontal lines
			for (double i = 1, directionalLineDisplacement = directionalPixelHeight;
				i < columnCellCount - Params.EPSILON;
				++i, directionalLineDisplacement += directionalPixelHeight)
			{
				gridLine = new Line();
				gridLine.X1 = 0;
				gridLine.Y1 = directionalLineDisplacement;
				gridLine.X2 = directionalGridWidth;
				gridLine.Y2 = directionalLineDisplacement;
				gridLine.Stroke = Params.BITMAP_HORIZONTAL_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_HORIZONTAL_GRID_THICKNESS;
				bitmapGridLinesTopLeft.Add(gridLine);

				gridLine = new Line();
				gridLine.X1 = 0;
				gridLine.Y1 = directionalLineDisplacement;
				gridLine.X2 = directionalGridWidth;
				gridLine.Y2 = directionalLineDisplacement;
				gridLine.Stroke = Params.BITMAP_HORIZONTAL_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_HORIZONTAL_GRID_THICKNESS;
				bitmapGridLinesTopRight.Add(gridLine);

				gridLine = new Line();
				gridLine.X1 = 0;
				gridLine.Y1 = directionalLineDisplacement;
				gridLine.X2 = directionalGridWidth;
				gridLine.Y2 = directionalLineDisplacement;
				gridLine.Stroke = Params.BITMAP_HORIZONTAL_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_HORIZONTAL_GRID_THICKNESS;
				bitmapGridLinesBottomLeft.Add(gridLine);

				gridLine = new Line();
				gridLine.X1 = 0;
				gridLine.Y1 = directionalLineDisplacement;
				gridLine.X2 = directionalGridWidth;
				gridLine.Y2 = directionalLineDisplacement;
				gridLine.Stroke = Params.BITMAP_HORIZONTAL_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_HORIZONTAL_GRID_THICKNESS;
				bitmapGridLinesBottomRight.Add(gridLine);
			}

			// Draw the vertical lines
			for (double i = 1, directionalLineDisplacement = directionalPixelWidth;
				i < columnCount - Params.EPSILON;
				++i, directionalLineDisplacement += directionalPixelWidth)
			{
				gridLine = new Line();
				gridLine.X1 = directionalLineDisplacement;
				gridLine.Y1 = 0;
				gridLine.X2 = directionalLineDisplacement;
				gridLine.Y2 = directionalGridWidth;
				gridLine.Stroke = Params.BITMAP_VERTICAL_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_VERTICAL_GRID_THICKNESS;
				bitmapGridLinesTopLeft.Add(gridLine);

				gridLine = new Line();
				gridLine.X1 = directionalLineDisplacement;
				gridLine.Y1 = 0;
				gridLine.X2 = directionalLineDisplacement;
				gridLine.Y2 = directionalGridWidth;
				gridLine.Stroke = Params.BITMAP_VERTICAL_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_VERTICAL_GRID_THICKNESS;
				bitmapGridLinesTopRight.Add(gridLine);

				gridLine = new Line();
				gridLine.X1 = directionalLineDisplacement;
				gridLine.Y1 = 0;
				gridLine.X2 = directionalLineDisplacement;
				gridLine.Y2 = directionalGridWidth;
				gridLine.Stroke = Params.BITMAP_VERTICAL_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_VERTICAL_GRID_THICKNESS;
				bitmapGridLinesBottomLeft.Add(gridLine);

				gridLine = new Line();
				gridLine.X1 = directionalLineDisplacement;
				gridLine.Y1 = 0;
				gridLine.X2 = directionalLineDisplacement;
				gridLine.Y2 = directionalGridWidth;
				gridLine.Stroke = Params.BITMAP_VERTICAL_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_VERTICAL_GRID_THICKNESS;
				bitmapGridLinesBottomRight.Add(gridLine);
			}
		}

		private void OnClick(Object sender, RoutedEventArgs e)
		{
			if (sender == prevButton)
			{
				--currentIndex;
				Refresh();
			}
			else if (sender == nextButton)
			{
				++currentIndex;
				Refresh();
			}
		}

		private void OnCheck(object sender, RoutedEventArgs e)
		{
			Refresh();
		}

		private void OnUncheck(object sender, RoutedEventArgs e)
		{
			Refresh();
		}

		private void Refresh()
		{
			crossValidationStackPanel.Visibility = Visibility.Collapsed;
			splitCanvasGrid.Visibility = Visibility.Collapsed;
			infoShapeInstanceDockPanel.Visibility = Visibility.Collapsed;
			infoTemplateDockPanel.Visibility = Visibility.Collapsed;
			controlsStackPanel.Visibility = Visibility.Visible;

			statisticsCheckBox.IsEnabled = true;
			gridLinesCheckBox.IsEnabled = true;
			EnableRadioButtons(true);
			strokePointsCheckBox.IsEnabled = true;
			strokeLinesCheckBox.IsEnabled = true;
			colorCodeCellsCheckBox.IsEnabled = true;
			showDistanceNumbersCheckBox.IsEnabled = true;

			// The relevance of some checkboxes are is dependent upon other 
			// checkboxes
			if (statisticsCheckBox.IsChecked != true)
			{
				if (strokePointsCheckBox.IsChecked != true)
				{
					colorCodeCellsCheckBox.IsEnabled = false;
				}

				if (templateDistancesActualRadioButton.IsChecked != true &&
						templateDistancesRecognizedRadioButton.IsChecked != true)
				{
					showDistanceNumbersCheckBox.IsEnabled = false;
				}
			}
			else
			{
				gridLinesCheckBox.IsEnabled = false;
				EnableRadioButtons(false);
				strokePointsCheckBox.IsEnabled = false;
				strokeLinesCheckBox.IsEnabled = false;
				colorCodeCellsCheckBox.IsEnabled = false;
				showDistanceNumbersCheckBox.IsEnabled = false;
			}

			ShapeInstance shapeInstance;
			Template template;

			switch (recognizerWindowType)
			{
				case RecognizerWindowType.CrossValidate:
					List<SingleUserHoldOutTest> crossValidationResults = recognizer.CrossValidationResults;
					SingleUserHoldOutTest singleUserHoldOutTest = crossValidationResults[currentIndex];

					prevButton.IsEnabled = currentIndex > 0;
					nextButton.IsEnabled = currentIndex < crossValidationResults.Count - 1;
					pageTitle.Content = singleUserHoldOutTest.UserID < 0 ?
						"Average Results" :
						"User " + singleUserHoldOutTest.UserID;

					ShowCrossValidationResults(singleUserHoldOutTest);
					break;
				case RecognizerWindowType.Canvas:
					shapeInstance = mainWindow.CanvasShapeInstance;

					UpdateGridValuesForShapeInstance(shapeInstance);

					prevButton.IsEnabled = false;
					nextButton.IsEnabled = false;
					pageTitle.Content = "Canvas Strokes";

					ShowShapeData(shapeInstance);
					break;
				case RecognizerWindowType.HoldOut:
					shapeInstance = recognizer.HoldOuts[currentIndex];

					UpdateGridValuesForShapeInstance(shapeInstance);

					prevButton.IsEnabled = currentIndex > 0;
					nextButton.IsEnabled = currentIndex < recognizer.HoldOuts.Count - 1;
					pageTitle.Content = 
						"sub" + shapeInstance.SubjectID + 
						", shp" + shapeInstance.ActualShapeID + 
						", ex" + shapeInstance.ExampleNumber + 
						" --> " + Params.ParseShapeIDToDescription(shapeInstance.ActualShapeID);
					pageTitle.Foreground =
						(shapeInstance.ActualShapeID == shapeInstance.RecognizedShapeID &&
								shapeInstance.RecognizedDistance < mainWindow.RecognitionDistanceThreshold) ?
							Params.RECOGNITION_PASS_BRUSH :
							Params.RECOGNITION_FAIL_BRUSH;

					ShowShapeData(shapeInstance);
					break;
				case RecognizerWindowType.Templates:
					EnableRadioButtons(false);
					strokePointsCheckBox.IsEnabled = false;
					strokeLinesCheckBox.IsEnabled = false;
					colorCodeCellsCheckBox.IsEnabled = false;
					noPixelValuesRadioButton.IsEnabled = false;

					template = recognizer.Templates[currentIndex];

					UpdateGridValuesForTemplate(template);

					prevButton.IsEnabled = currentIndex > 0;
					nextButton.IsEnabled = currentIndex < recognizer.Templates.Count - 1;
					pageTitle.Content = "Shape " + Params.ParseShapeIDToDescription(template.ID);

					ShowTemplateData(template);
					break;
				default:
					return;
			}
		}

		private void ShowCrossValidationResults(SingleUserHoldOutTest singleUserHoldOutTest)
		{
			crossValidationStackPanel.Visibility = Visibility.Visible;
			controlsStackPanel.Visibility = Visibility.Collapsed;

			int shapeCount = singleUserHoldOutTest.ShapeIDs.Length;
			int shapeCountSquared = shapeCount * shapeCount;
			RowDefinition rowDefinition;
			ColumnDefinition colDefinition;
			TextBlock textBlock;
			int row, col;

			accuracyLabel.Content = "Accuracy = " + singleUserHoldOutTest.Accuracy.ToString("0.000");

			fMeasuresGrid.RowDefinitions.Clear();
			confusionMatrixGrid.RowDefinitions.Clear();
			confusionMatrixGrid.ColumnDefinitions.Clear();
			fMeasuresGrid.Children.Clear();
			confusionMatrixGrid.Children.Clear();

			for (int i = 0; i < shapeCount; ++i)
			{
				rowDefinition = new RowDefinition();
				fMeasuresGrid.RowDefinitions.Add(rowDefinition);
				rowDefinition = new RowDefinition();
				confusionMatrixGrid.RowDefinitions.Add(rowDefinition);
				colDefinition = new ColumnDefinition();
				confusionMatrixGrid.ColumnDefinitions.Add(colDefinition);
			}
			rowDefinition = new RowDefinition();
			confusionMatrixGrid.RowDefinitions.Add(rowDefinition);
			colDefinition = new ColumnDefinition();
			confusionMatrixGrid.ColumnDefinitions.Add(colDefinition);
			rowDefinition = new RowDefinition();
			confusionMatrixGrid.RowDefinitions.Add(rowDefinition);
			colDefinition = new ColumnDefinition();
			confusionMatrixGrid.ColumnDefinitions.Add(colDefinition);

			// Confusion matrix row and column headers
			for (int i = 0; i < shapeCount; ++i)
			{
				textBlock = new TextBlock();
				textBlock.Foreground = Params.F_MEASURES_BRUSH;
				textBlock.FontSize = 18;
				textBlock.FontWeight = FontWeights.Bold;
				textBlock.Text = "" + singleUserHoldOutTest.ShapeIDs[i];
				Grid.SetRow(textBlock, i + 2);
				Grid.SetColumn(textBlock, 1);
				confusionMatrixGrid.Children.Add(textBlock);

				textBlock = new TextBlock();
				textBlock.Foreground = Params.F_MEASURES_BRUSH;
				textBlock.FontSize = 18;
				textBlock.FontWeight = FontWeights.Bold;
				textBlock.Text = "" + singleUserHoldOutTest.ShapeIDs[i];
				Grid.SetRow(textBlock, 1);
				Grid.SetColumn(textBlock, i + 2);
				confusionMatrixGrid.Children.Add(textBlock);
			}
			textBlock = new TextBlock();
			textBlock.Foreground = Params.F_MEASURES_BRUSH;
			textBlock.FontSize = 18;
			textBlock.FontWeight = FontWeights.Bold;
			textBlock.HorizontalAlignment = HorizontalAlignment.Center;
			textBlock.VerticalAlignment = VerticalAlignment.Center;
			textBlock.Text = "A\nc\nt\nu\na\nl\n \nS\nh\na\np\ne";
			Grid.SetRowSpan(textBlock, shapeCount + 2);
			Grid.SetColumn(textBlock, 0);
			confusionMatrixGrid.Children.Add(textBlock);

			textBlock = new TextBlock();
			textBlock.Foreground = Params.F_MEASURES_BRUSH;
			textBlock.FontSize = 18;
			textBlock.FontWeight = FontWeights.Bold;
			textBlock.HorizontalAlignment = HorizontalAlignment.Center;
			textBlock.Text = "Recognized Shape";
			Grid.SetRow(textBlock, 0);
			Grid.SetColumnSpan(textBlock, shapeCount + 2);
			confusionMatrixGrid.Children.Add(textBlock);

			for (int i = 0; i < shapeCountSquared; ++i)
			{
				row = i / shapeCount;
				col = i % shapeCount;

				textBlock = new TextBlock();
				if (row == col)
				{
					// Diagonal cell

					if (singleUserHoldOutTest.ConfusionMatrix[i] < Params.CONFUSION_MATRIX_THRESHOLD_DIAGONAL_2)
					{
						textBlock.Background = Params.CONFUSION_MATRIX_BACKGROUND_3_BRUSH;
						textBlock.Foreground = Params.CONFUSION_MATRIX_FOREGROUND_3_BRUSH;
					}
					else if (singleUserHoldOutTest.ConfusionMatrix[i] < Params.CONFUSION_MATRIX_THRESHOLD_DIAGONAL_1)
					{
						textBlock.Background = Params.CONFUSION_MATRIX_BACKGROUND_2_BRUSH;
						textBlock.Foreground = Params.CONFUSION_MATRIX_FOREGROUND_2_BRUSH;
					}
					else
					{
						textBlock.Background = Params.CONFUSION_MATRIX_BACKGROUND_1_BRUSH;
						textBlock.Foreground = Params.CONFUSION_MATRIX_FOREGROUND_1_BRUSH;
					}
				}
				else
				{
					// Off-diagonal cell

					if (singleUserHoldOutTest.ConfusionMatrix[i] >= Params.CONFUSION_MATRIX_THRESHOLD_OFF_DIAGONAL_2)
					{
						textBlock.Background = Params.CONFUSION_MATRIX_BACKGROUND_3_BRUSH;
						textBlock.Foreground = Params.CONFUSION_MATRIX_FOREGROUND_3_BRUSH;
					}
					else if (singleUserHoldOutTest.ConfusionMatrix[i] > Params.CONFUSION_MATRIX_THRESHOLD_OFF_DIAGONAL_1)
					{
						textBlock.Background = Params.CONFUSION_MATRIX_BACKGROUND_2_BRUSH;
						textBlock.Foreground = Params.CONFUSION_MATRIX_FOREGROUND_2_BRUSH;
					}
					else
					{
						textBlock.Background = Params.CONFUSION_MATRIX_BACKGROUND_0_BRUSH;
						textBlock.Foreground = Params.CONFUSION_MATRIX_FOREGROUND_0_BRUSH;
					}
				}
				textBlock.FontSize = 14;
				textBlock.FontWeight = FontWeights.Bold;
				textBlock.Text = singleUserHoldOutTest.ConfusionMatrix[i].ToString("0.00");
				Grid.SetRow(textBlock, row + 2);
				Grid.SetColumn(textBlock, col + 2);
				confusionMatrixGrid.Children.Add(textBlock);
			}

			for (int i = 0; i < shapeCount; ++i)
			{
				// Add the f-measure label
				textBlock = new TextBlock();
				textBlock.Foreground = Params.F_MEASURES_BRUSH;
				textBlock.FontSize = 14;
				textBlock.FontWeight = FontWeights.Bold;
				textBlock.Text = "Shape " + Params.ParseShapeIDToDescription(singleUserHoldOutTest.ShapeIDs[i]);
				Grid.SetRow(textBlock, i);
				Grid.SetColumn(textBlock, 0);
				fMeasuresGrid.Children.Add(textBlock);

				// Add the f-measure value
				textBlock = new TextBlock();
				textBlock.Foreground = Params.F_MEASURES_BRUSH;
				textBlock.FontSize = 14;
				textBlock.FontWeight = FontWeights.Bold;
				textBlock.Text = singleUserHoldOutTest.FMeasures[i].ToString("0.000");
				Grid.SetRow(textBlock, i);
				Grid.SetColumn(textBlock, 1);
				fMeasuresGrid.Children.Add(textBlock);
			}

			fifteenFoldCrossValidationTimeLabel.Content = recognizer.TimeToCrossValidate.ToString("0.00000") + " seconds";
			avgTimeForTrainingASingleUserHoldOutLabel.Content = singleUserHoldOutTest.TimeToTrain.ToString("0.00000") + " seconds";
			avgTimeToRecognizeAShapeLabel.Content = singleUserHoldOutTest.AvgTimeToRecognize.ToString("0.00000") + " seconds";
		}

		private void ShowShapeData(ShapeInstance shapeInstance)
		{
			controlsStackPanel.Visibility = Visibility.Visible;

			if (statisticsCheckBox.IsChecked == true)
			{
				// Show statistics

				infoShapeInstanceDockPanel.Visibility = Visibility.Visible;

				Template actualTemplate = recognizer.GetTemplate(shapeInstance.ActualShapeID);
				Template recognizedTemplate = recognizer.GetTemplate(shapeInstance.RecognizedShapeID);

				infoSubjectLabel.Content = shapeInstance.SubjectID;
				infoExampleLabel.Content = shapeInstance.ExampleNumber;
				infoTimeToRecognizeLabel.Content = shapeInstance.TimeToRecognize;

				if (recognizedTemplate != null)
				{
					infoRecognizedIDLabel.Content = shapeInstance.RecognizedShapeID;
					infoRecognizedDistanceLabel.Content = shapeInstance.RecognizedDistance;
				}
				else
				{
					infoRecognizedIDLabel.Content = "--";
					infoRecognizedDistanceLabel.Content = "--";
				}

				infoActualLabel.Content = shapeInstance.ActualShapeID;
				infoActualDistanceLabel.Content = recognizer.GetDistance(shapeInstance, actualTemplate);
			}
			else
			{
				// Show image information

				// Normalize the strokes

				// Show the four directional bitmaps

				splitCanvasGrid.Visibility = Visibility.Visible;

				graphicsTopLeft.Clear();
				graphicsTopRight.Clear();
				graphicsBottomLeft.Clear();
				graphicsBottomRight.Clear();

				if (noPixelValuesRadioButton.IsChecked != true)
				{
					DrawGridCells(graphicsTopLeft, bitmapGridSquaresTopLeft);
					DrawGridCells(graphicsTopRight, bitmapGridSquaresTopRight);
					DrawGridCells(graphicsBottomLeft, bitmapGridSquaresBottomLeft);
					DrawGridCells(graphicsBottomRight, bitmapGridSquaresBottomRight);
				}

				if (gridLinesCheckBox.IsChecked == true)
				{
					DrawGridLines(graphicsTopLeft, bitmapGridLinesTopLeft);
					DrawGridLines(graphicsTopRight, bitmapGridLinesTopRight);
					DrawGridLines(graphicsBottomLeft, bitmapGridLinesBottomLeft);
					DrawGridLines(graphicsBottomRight, bitmapGridLinesBottomRight);
				}

				DrawStrokes(topLeftCanvas,
					shapeInstance.Strokes0,
					strokePointsCheckBox.IsChecked == true,
					strokeLinesCheckBox.IsChecked == true,
					colorCodeCellsCheckBox.IsChecked == true,
					true);
				DrawStrokes(topRightCanvas,
					shapeInstance.Strokes45,
					strokePointsCheckBox.IsChecked == true, 
					strokeLinesCheckBox.IsChecked == true,
					colorCodeCellsCheckBox.IsChecked == true,
					true);
				DrawStrokes(bottomLeftCanvas,
					shapeInstance.Strokes90,
					strokePointsCheckBox.IsChecked == true, 
					strokeLinesCheckBox.IsChecked == true,
					colorCodeCellsCheckBox.IsChecked == true,
					true);
				DrawStrokes(bottomRightCanvas,
					shapeInstance.Strokes135,
					strokePointsCheckBox.IsChecked == true, 
					strokeLinesCheckBox.IsChecked == true,
					colorCodeCellsCheckBox.IsChecked == true,
					true);
			}
		}

		private void ShowTemplateData(Template template)
		{
			controlsStackPanel.Visibility = Visibility.Visible;

			if (statisticsCheckBox.IsChecked == true)
			{
				// Show statistics

				infoTemplateDockPanel.Visibility = Visibility.Visible;

				infoShapeIDLabel.Content = template.ID;
				infoTimeToTrainLabel.Content = recognizer.TimeToTrain;
			}
			else
			{
				// Show the four directional bitmaps

				splitCanvasGrid.Visibility = Visibility.Visible;

				graphicsTopLeft.Clear();
				graphicsTopRight.Clear();
				graphicsBottomLeft.Clear();
				graphicsBottomRight.Clear();

				DrawGridCells(graphicsTopLeft, bitmapGridSquaresTopLeft);
				DrawGridCells(graphicsTopRight, bitmapGridSquaresTopRight);
				DrawGridCells(graphicsBottomLeft, bitmapGridSquaresBottomLeft);
				DrawGridCells(graphicsBottomRight, bitmapGridSquaresBottomRight);

				if (gridLinesCheckBox.IsChecked == true)
				{
					DrawGridLines(graphicsTopLeft, bitmapGridLinesTopLeft);
					DrawGridLines(graphicsTopRight, bitmapGridLinesTopRight);
					DrawGridLines(graphicsBottomLeft, bitmapGridLinesBottomLeft);
					DrawGridLines(graphicsBottomRight, bitmapGridLinesBottomRight);
				}
			}
		}

		private void EnableRadioButtons(bool isEnabled)
		{
			unsmoothedShapeInstanceValuesAloneRadioButton.IsEnabled = isEnabled;
			unsmoothedTemplateOverlayActualRadioButton.IsEnabled = isEnabled;
			unsmoothedTemplateOverlayRecognizedRadioButton.IsEnabled = isEnabled;
			smoothedShapeInstanceValuesAloneRadioButton.IsEnabled = isEnabled;
			smoothedTemplateOverlayActualRadioButton.IsEnabled = isEnabled;
			smoothedTemplateOverlayRecognizedRadioButton.IsEnabled = isEnabled;
			noPixelValuesRadioButton.IsEnabled = isEnabled;
			templateDistancesActualRadioButton.IsEnabled = isEnabled;
			templateDistancesRecognizedRadioButton.IsEnabled = isEnabled;
		}

		private static void InstantiateRectangleValues(Rectangle[][] rectangles,
			short columnCellCount)
		{
			for (int i = 0; i < rectangles.Length; ++i)
			{
				rectangles[i] = new Rectangle[columnCellCount];
			}
		}
	}
}
