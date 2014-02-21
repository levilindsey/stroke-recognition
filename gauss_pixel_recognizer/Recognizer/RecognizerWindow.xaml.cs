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
		private UIElementCollection graphicsMain;
		private List<Line> bitmapGridLinesMain;
		private Rectangle[] bitmapGridSquaresMain;
		private UIElementCollection graphicsTopLeft;
		private List<Line> bitmapGridLinesTopLeft;
		private Rectangle[] bitmapGridSquaresTopLeft;
		private UIElementCollection graphicsTopRight;
		private List<Line> bitmapGridLinesTopRight;
		private Rectangle[] bitmapGridSquaresTopRight;
		private UIElementCollection graphicsBottomLeft;
		private List<Line> bitmapGridLinesBottomLeft;
		private Rectangle[] bitmapGridSquaresBottomLeft;
		private UIElementCollection graphicsBottomRight;
		private List<Line> bitmapGridLinesBottomRight;
		private Rectangle[] bitmapGridSquaresBottomRight;

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
			int gridCellCount = MainWindow.TemplateSize;

			graphicsMain = mainCanvas.Children;
			graphicsMain.Clear();
			bitmapGridLinesMain = new List<Line>();
			bitmapGridSquaresMain = new Rectangle[gridCellCount];
			graphicsTopLeft = topLeftCanvas.Children;
			graphicsTopLeft.Clear();
			bitmapGridLinesTopLeft = new List<Line>();
			bitmapGridSquaresTopLeft = new Rectangle[gridCellCount];
			graphicsTopRight = topRightCanvas.Children;
			graphicsTopRight.Clear();
			bitmapGridLinesTopRight = new List<Line>();
			bitmapGridSquaresTopRight = new Rectangle[gridCellCount];
			graphicsBottomLeft = bottomLeftCanvas.Children;
			graphicsBottomLeft.Clear();
			bitmapGridLinesBottomLeft = new List<Line>();
			bitmapGridSquaresBottomLeft = new Rectangle[gridCellCount];
			graphicsBottomRight = bottomRightCanvas.Children;
			graphicsBottomRight.Clear();
			bitmapGridLinesBottomRight = new List<Line>();
			bitmapGridSquaresBottomRight = new Rectangle[gridCellCount];

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
			RecognizerPoint[][] points, bool showStrokePoints,
			bool showStrokeLines, bool colorCodeDots, bool isNormalized, 
			Rect boundingBox)
		{
			Ellipse dot;
			int row, col;
			double canvasWidth = canvas.Width;
			double cellWidth = canvasWidth / MainWindow.TemplateSideLength;
			UIElementCollection canvasGraphics = canvas.Children;

			// Scale and translate this shape to be within the range of 0-1
			if (!isNormalized)
			{
				double minX = boundingBox.X;
				double minY = boundingBox.Y;
				double maxX = boundingBox.X + boundingBox.Width;
				double maxY = boundingBox.Y + boundingBox.Height;
				double scalingRatio = 1 / Math.Max(maxX, maxY);
				foreach (RecognizerPoint[] subPoints in points)
				{
					foreach (RecognizerPoint point in subPoints)
					{
						point.X = (point.X - minX) * scalingRatio;
						point.Y = (point.Y - minY) * scalingRatio;
					}
				}
			}

			// Scale this shape to the size of the canvas
			foreach (RecognizerPoint[] subPoints in points)
			{
				foreach (RecognizerPoint point in subPoints)
				{
					point.X = point.X * canvasWidth;
					point.Y = point.Y * canvasWidth;
				}
			}

			// Draw the line segments?
			if (showStrokeLines)
			{
				Line line;
				for (int i = 0; i < points.Length; ++i)
				{
					for (int j = 0; j < points[i].Length - 1; ++j)
					{
						line = new Line();
						line.X1 = points[i][j].X;
						line.Y1 = points[i][j].Y;
						line.X2 = points[i][j + 1].X;
						line.Y2 = points[i][j + 1].Y;
						line.Stroke = Params.BITMAP_STROKE_BRUSH;
						line.StrokeThickness = Params.BITMAP_STROKE_THICKNESS;
						canvasGraphics.Add(line);
					}
				}
			}

			// Draw the line points?
			if (showStrokePoints)
			{
				for (int i = 0; i < points.Length; ++i)
				{
					for (int j = 1; j < points[i].Length - 1; ++j)
					{
						dot = new Ellipse();
						if (colorCodeDots)
						{
							row = (int)(points[i][j].X / cellWidth);
							col = (int)(points[i][j].Y / cellWidth);
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
							points[i][j].X - Params.BITMAP_POINT_RADIUS,
							points[i][j].Y - Params.BITMAP_POINT_RADIUS,
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
						points[i][0].X - Params.BITMAP_END_POINT_RADIUS,
						points[i][0].Y - Params.BITMAP_END_POINT_RADIUS,
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
						points[i][points[i].Length - 1].X - Params.BITMAP_END_POINT_RADIUS,
						points[i][points[i].Length - 1].Y - Params.BITMAP_END_POINT_RADIUS,
						0, 0);
					dot.Width = Params.BITMAP_END_POINT_DIAMETER;
					dot.Height = Params.BITMAP_END_POINT_DIAMETER;
					canvasGraphics.Add(dot);
				}
			}
		}

		private void DrawGridCells(UIElementCollection canvas, Rectangle[] gridCells)
		{
			foreach (Rectangle gridCell in gridCells)
			{
				canvas.Add(gridCell);
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

			if (shapeInstanceValuesAloneRadioButton.IsChecked == true)
			{
				displayShapeInstanceValuesAlone = true;
			}
			else if (noPixelValuesRadioButton.IsChecked != true)
			{
				if (templateOverlayActualRadioButton.IsChecked == true)
				{
					template = recognizer.GetTemplate(shapeInstance.ActualShapeID);
					if (template != null)
					{
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresTopLeft, shapeInstance.Pixels0, template.Bitmap0);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresTopRight, shapeInstance.Pixels45, template.Bitmap45);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresBottomLeft, shapeInstance.Pixels90, template.Bitmap90);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresBottomRight, shapeInstance.Pixels135, template.Bitmap135);
					}
					else
					{
						displayShapeInstanceValuesAlone = true;
					}
				}
				else if (templateOverlayRecognizedRadioButton.IsChecked == true)
				{
					template = recognizer.GetTemplate(shapeInstance.RecognizedShapeID);
					if (template != null)
					{
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresTopLeft, shapeInstance.Pixels0, template.Bitmap0);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresTopRight, shapeInstance.Pixels45, template.Bitmap45);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresBottomLeft, shapeInstance.Pixels90, template.Bitmap90);
						UpdateDirectionalValuesTemplateOverlay(bitmapGridSquaresBottomRight, shapeInstance.Pixels135, template.Bitmap135);
					}
					else
					{
						displayShapeInstanceValuesAlone = true;
					}
				}
				else if (templateDistancesActualRadioButton.IsChecked == true)
				{
					template = recognizer.GetTemplate(shapeInstance.ActualShapeID);
					if (showDistanceNumbersCheckBox.IsChecked == true)
					{
						if (template != null)
						{
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopLeft, shapeInstance.Pixels0, template.Bitmap0, true);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopRight, shapeInstance.Pixels45, template.Bitmap45, true);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomLeft, shapeInstance.Pixels90, template.Bitmap90, true);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomRight, shapeInstance.Pixels135, template.Bitmap135, true);
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
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopLeft, shapeInstance.Pixels0, template.Bitmap0, false);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopRight, shapeInstance.Pixels45, template.Bitmap45, false);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomLeft, shapeInstance.Pixels90, template.Bitmap90, false);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomRight, shapeInstance.Pixels135, template.Bitmap135, false);
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
					if (showDistanceNumbersCheckBox.IsChecked == true)
					{
						if (template != null)
						{
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopLeft, shapeInstance.Pixels0, template.Bitmap0, true);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopRight, shapeInstance.Pixels45, template.Bitmap45, true);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomLeft, shapeInstance.Pixels90, template.Bitmap90, true);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomRight, shapeInstance.Pixels135, template.Bitmap135, true);
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
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopLeft, shapeInstance.Pixels0, template.Bitmap0, false);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresTopRight, shapeInstance.Pixels45, template.Bitmap45, false);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomLeft, shapeInstance.Pixels90, template.Bitmap90, false);
							UpdateDirectionalValuesTemplateDistances(bitmapGridSquaresBottomRight, shapeInstance.Pixels135, template.Bitmap135, false);
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
				UpdateDirectionalValuesInstanceAlone(bitmapGridSquaresTopLeft, shapeInstance.Pixels0);
				UpdateDirectionalValuesInstanceAlone(bitmapGridSquaresTopRight, shapeInstance.Pixels45);
				UpdateDirectionalValuesInstanceAlone(bitmapGridSquaresBottomLeft, shapeInstance.Pixels90);
				UpdateDirectionalValuesInstanceAlone(bitmapGridSquaresBottomRight, shapeInstance.Pixels135);
			}

			UpdateMainGridSquaresForShapeInstance(bitmapGridSquaresMain,
				shapeInstance.Pixels0, shapeInstance.Pixels45,
				shapeInstance.Pixels90, shapeInstance.Pixels135);
		}

		private void UpdateGridValuesForTemplate(Template template)
		{
			UpdateDirectionalGridSquaresForTemplate(bitmapGridSquaresTopLeft, template.Bitmap0);
			UpdateDirectionalGridSquaresForTemplate(bitmapGridSquaresTopRight, template.Bitmap45);
			UpdateDirectionalGridSquaresForTemplate(bitmapGridSquaresBottomLeft, template.Bitmap90);
			UpdateDirectionalGridSquaresForTemplate(bitmapGridSquaresBottomRight, template.Bitmap135);
			UpdateMainGridSquaresForTemplate(bitmapGridSquaresMain,
				template.Bitmap0, template.Bitmap45,
				template.Bitmap90, template.Bitmap135);
		}

		private void UpdateDirectionalValuesInstanceAlone(Rectangle[] bitmapGridSquares,
			Dictionary<int, double> instancePixels)
		{
			Color cellColor;
			byte colorIntensity;

			// Display just the shape instance probabilities
			for (int i = 0; i < bitmapGridSquares.Length; ++i)
			{
				colorIntensity = instancePixels.ContainsKey(i) ? 
					(byte)(instancePixels[i] * 255) : (byte)0;
				cellColor = Color.FromArgb(255, colorIntensity, colorIntensity, colorIntensity);
				((SolidColorBrush)bitmapGridSquares[i].Fill).Color = cellColor;
			}
		}

		private void UpdateDirectionalValuesTemplateOverlay(Rectangle[] bitmapGridSquares,
			Dictionary<int, double> instancePixels, double[] templateBitmap)
		{
			Color cellColor;
			double tempR, tempG, tempB;
			byte r, g, b;

			// Overlay both the shape instance probabilities and the template probabilities
			for (int i = 0; i < bitmapGridSquares.Length; ++i)
			{
				if (instancePixels.ContainsKey(i))
				{
					tempR = instancePixels[i] * 50 + templateBitmap[i] * 455;
					tempG = instancePixels[i] * 255 + templateBitmap[i] * 255;
					tempB = instancePixels[i] * 50 + templateBitmap[i] * 50;
				}
				else
				{
					tempR = templateBitmap[i] * 680;
					tempG = templateBitmap[i] * 380;
					tempB = templateBitmap[i] * 80;
				}
				r = (byte)(Math.Min(tempR, 255));
				g = (byte)(Math.Min(tempG, 255));
				b = (byte)(Math.Min(tempB, 255));
				cellColor = Color.FromArgb(255, r, g, b);
				((SolidColorBrush)bitmapGridSquares[i].Fill).Color = cellColor;
			}
		}

		private void UpdateDirectionalValuesTemplateDistances(Rectangle[] bitmapGridSquares,
			Dictionary<int, double> instancePixels, double[] templateBitmap, bool showNumbers)
		{
			Color cellColor;
			double tempR, tempG, tempB;
			byte r, g, b;

			for (int i = 0; i < bitmapGridSquares.Length; ++i)
			{
				if (instancePixels.ContainsKey(i))
				{
					tempR = Math.Abs(instancePixels[i] - templateBitmap[i]);
					tempG = Math.Abs(instancePixels[i] - templateBitmap[i]);
					tempB = Math.Abs(instancePixels[i] - templateBitmap[i]);
				}
				else
				{
					tempR = templateBitmap[i];
					tempG = templateBitmap[i];
					tempB = templateBitmap[i];
				}
				r = (byte)(Math.Min(tempR * 255, 255));
				g = (byte)(Math.Min(tempG * 50, 255));
				b = (byte)(Math.Min(tempB * 50, 255));
				cellColor = Color.FromArgb(255, r, g, b);
				((SolidColorBrush)bitmapGridSquares[i].Fill).Color = cellColor;
			}
		}

		private void UpdateDirectionalGridSquaresForTemplate(Rectangle[] bitmapGridSquares,
			double[] templatePixels)
		{
			Color cellColor;
			byte colorIntensity;

			for (int i = 0; i < bitmapGridSquares.Length; ++i)
			{
				colorIntensity = (byte)(templatePixels[i] * 255);
				cellColor = Color.FromArgb(255, colorIntensity, colorIntensity, colorIntensity);
				((SolidColorBrush)bitmapGridSquares[i].Fill).Color = cellColor;
			}
		}

		private void UpdateMainGridSquaresForShapeInstance(
			Rectangle[] bitmapGridSquares, Dictionary<int, double> pixels0,
			Dictionary<int, double> pixels45, Dictionary<int, double> pixels90,
			Dictionary<int, double> pixels135)
		{
			Color cellColor;
			byte colorIntensity;

			for (int i = 0; i < bitmapGridSquares.Length; ++i)
			{
				colorIntensity = pixels0.ContainsKey(i) ?
					(byte)(Math.Min((pixels0[i] + pixels45[i] + pixels90[i] + pixels135[i]), 1) * 255) :
					(byte)0;
				cellColor = Color.FromArgb(255, colorIntensity, colorIntensity, colorIntensity);
				((SolidColorBrush)bitmapGridSquares[i].Fill).Color = cellColor;
			}
		}

		private void UpdateMainGridSquaresForTemplate(
			Rectangle[] bitmapGridSquares, double[] cellProbabilities0,
			double[] cellProbabilities45, double[] cellProbabilities90,
			double[] cellProbabilities135)
		{
			Color cellColor;
			byte colorIntensity;

			for (int i = 0; i < bitmapGridSquares.Length; ++i)
			{
				colorIntensity = (byte)(Math.Min((cellProbabilities0[i] + cellProbabilities45[i] + cellProbabilities90[i] + cellProbabilities135[i]), 1) * 255);
				cellColor = Color.FromArgb(255, colorIntensity, colorIntensity, colorIntensity);
				((SolidColorBrush)bitmapGridSquares[i].Fill).Color = cellColor;
			}
		}

		private void CreateGridSquares()
		{
			double mainGridWidth, directionalGridWidth, mainPixelWidth, directionalPixelWidth;
			Rectangle gridCell;
			Thickness mainThickness, directionalThickness;
			int bitmapIndex;

			// Get the width/height of the grid
			mainGridWidth = mainCanvas.Width;
			directionalGridWidth = topLeftCanvas.Width;
			mainPixelWidth = mainGridWidth / MainWindow.TemplateSideLength;
			directionalPixelWidth = directionalGridWidth / MainWindow.TemplateSideLength;

			// Draw the bitmap cells
			for (int row = 0; row < MainWindow.TemplateSideLength; ++row)
			{
				for (int col = 0; col < MainWindow.TemplateSideLength; ++col)
				{
					bitmapIndex = row * MainWindow.TemplateSideLength + col;

					mainThickness = new Thickness(col * mainPixelWidth, row * mainPixelWidth, 0, 0);
					directionalThickness = new Thickness(col * directionalPixelWidth, row * directionalPixelWidth, 0, 0);

					// Main bitmap

					gridCell = new Rectangle();
					gridCell.Margin = mainThickness;
					gridCell.Width = mainPixelWidth;
					gridCell.Height = mainPixelWidth;
					gridCell.Fill = new SolidColorBrush();
					gridCell.Stroke = Params.BITMAP_CELL_BRUSH;
					gridCell.StrokeThickness = Params.BITMAP_CELL_THICKNESS;
					bitmapGridSquaresMain[bitmapIndex] = gridCell;

					// Directional bitmaps

					// Top-left
					gridCell = new Rectangle();
					gridCell.Margin = directionalThickness;
					gridCell.Width = directionalPixelWidth;
					gridCell.Height = directionalPixelWidth;
					gridCell.Fill = new SolidColorBrush();
					gridCell.Stroke = Params.BITMAP_CELL_BRUSH;
					gridCell.StrokeThickness = Params.BITMAP_CELL_THICKNESS;
					bitmapGridSquaresTopLeft[bitmapIndex] = gridCell;

					// Top-right
					gridCell = new Rectangle();
					gridCell.Margin = directionalThickness;
					gridCell.Width = directionalPixelWidth;
					gridCell.Height = directionalPixelWidth;
					gridCell.Fill = new SolidColorBrush();
					gridCell.Stroke = Params.BITMAP_CELL_BRUSH;
					gridCell.StrokeThickness = Params.BITMAP_CELL_THICKNESS;
					bitmapGridSquaresTopRight[bitmapIndex] = gridCell;

					// Bottom-left
					gridCell = new Rectangle();
					gridCell.Margin = directionalThickness;
					gridCell.Width = directionalPixelWidth;
					gridCell.Height = directionalPixelWidth;
					gridCell.Fill = new SolidColorBrush();
					gridCell.Stroke = Params.BITMAP_CELL_BRUSH;
					gridCell.StrokeThickness = Params.BITMAP_CELL_THICKNESS;
					bitmapGridSquaresBottomLeft[bitmapIndex] = gridCell;

					// Bottom-right
					gridCell = new Rectangle();
					gridCell.Margin = directionalThickness;
					gridCell.Width = directionalPixelWidth;
					gridCell.Height = directionalPixelWidth;
					gridCell.Fill = new SolidColorBrush();
					gridCell.Stroke = Params.BITMAP_CELL_BRUSH;
					gridCell.StrokeThickness = Params.BITMAP_CELL_THICKNESS;
					bitmapGridSquaresBottomRight[bitmapIndex] = gridCell;
				}
			}
		}

		private void CreateGridLines()
		{
			double mainGridWidth, directionalGridWidth, mainPixelWidth, directionalPixelWidth;
			Line gridLine;

			// Get the width/height of the grid
			mainGridWidth = mainCanvas.Width;
			directionalGridWidth = topLeftCanvas.Width;
			mainPixelWidth = mainGridWidth / MainWindow.TemplateSideLength;
			directionalPixelWidth = directionalGridWidth / MainWindow.TemplateSideLength;

			// Draw the grid lines
			for (double i = 1, mainLineDisplacement = mainPixelWidth,
					directionalLineDisplacement = directionalPixelWidth;
				i < MainWindow.TemplateSideLength - Params.EPSILON;
				++i, mainLineDisplacement += mainPixelWidth,
					directionalLineDisplacement += directionalPixelWidth)
			{
				// Main lines

				// Vertical line
				gridLine = new Line();
				gridLine.X1 = mainLineDisplacement;
				gridLine.Y1 = 0;
				gridLine.X2 = mainLineDisplacement;
				gridLine.Y2 = mainGridWidth;
				gridLine.Stroke = Params.BITMAP_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_GRID_THICKNESS;
				bitmapGridLinesMain.Add(gridLine);

				// Horizontal line
				gridLine = new Line();
				gridLine.X1 = 0;
				gridLine.Y1 = mainLineDisplacement;
				gridLine.X2 = mainGridWidth;
				gridLine.Y2 = mainLineDisplacement;
				gridLine.Stroke = Params.BITMAP_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_GRID_THICKNESS;
				bitmapGridLinesMain.Add(gridLine);

				// Directional lines

				// Vertical line
				gridLine = new Line();
				gridLine.X1 = directionalLineDisplacement;
				gridLine.Y1 = 0;
				gridLine.X2 = directionalLineDisplacement;
				gridLine.Y2 = directionalGridWidth;
				gridLine.Stroke = Params.BITMAP_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_GRID_THICKNESS;
				bitmapGridLinesTopLeft.Add(gridLine);

				gridLine = new Line();
				gridLine.X1 = directionalLineDisplacement;
				gridLine.Y1 = 0;
				gridLine.X2 = directionalLineDisplacement;
				gridLine.Y2 = directionalGridWidth;
				gridLine.Stroke = Params.BITMAP_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_GRID_THICKNESS;
				bitmapGridLinesTopRight.Add(gridLine);

				gridLine = new Line();
				gridLine.X1 = directionalLineDisplacement;
				gridLine.Y1 = 0;
				gridLine.X2 = directionalLineDisplacement;
				gridLine.Y2 = directionalGridWidth;
				gridLine.Stroke = Params.BITMAP_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_GRID_THICKNESS;
				bitmapGridLinesBottomLeft.Add(gridLine);

				gridLine = new Line();
				gridLine.X1 = directionalLineDisplacement;
				gridLine.Y1 = 0;
				gridLine.X2 = directionalLineDisplacement;
				gridLine.Y2 = directionalGridWidth;
				gridLine.Stroke = Params.BITMAP_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_GRID_THICKNESS;
				bitmapGridLinesBottomRight.Add(gridLine);

				// Horizontal line
				gridLine = new Line();
				gridLine.X1 = 0;
				gridLine.Y1 = directionalLineDisplacement;
				gridLine.X2 = directionalGridWidth;
				gridLine.Y2 = directionalLineDisplacement;
				gridLine.Stroke = Params.BITMAP_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_GRID_THICKNESS;
				bitmapGridLinesTopLeft.Add(gridLine);

				gridLine = new Line();
				gridLine.X1 = 0;
				gridLine.Y1 = directionalLineDisplacement;
				gridLine.X2 = directionalGridWidth;
				gridLine.Y2 = directionalLineDisplacement;
				gridLine.Stroke = Params.BITMAP_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_GRID_THICKNESS;
				bitmapGridLinesTopRight.Add(gridLine);

				gridLine = new Line();
				gridLine.X1 = 0;
				gridLine.Y1 = directionalLineDisplacement;
				gridLine.X2 = directionalGridWidth;
				gridLine.Y2 = directionalLineDisplacement;
				gridLine.Stroke = Params.BITMAP_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_GRID_THICKNESS;
				bitmapGridLinesBottomLeft.Add(gridLine);

				gridLine = new Line();
				gridLine.X1 = 0;
				gridLine.Y1 = directionalLineDisplacement;
				gridLine.X2 = directionalGridWidth;
				gridLine.Y2 = directionalLineDisplacement;
				gridLine.Stroke = Params.BITMAP_GRID_BRUSH;
				gridLine.StrokeThickness = Params.BITMAP_GRID_THICKNESS;
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
			mainCanvasStackPanel.Visibility = Visibility.Collapsed;
			splitCanvasGrid.Visibility = Visibility.Collapsed;
			infoShapeInstanceDockPanel.Visibility = Visibility.Collapsed;
			infoTemplateDockPanel.Visibility = Visibility.Collapsed;
			controlsStackPanel.Visibility = Visibility.Visible;

			statisticsCheckBox.IsEnabled = true;
			originalStrokesCheckBox.IsEnabled = true;
			directionalComponentsCheckBox.IsEnabled = true;
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
				if (originalStrokesCheckBox.IsChecked != true)
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
					directionalComponentsCheckBox.IsEnabled = false;
					gridLinesCheckBox.IsEnabled = false;
					EnableRadioButtons(false);
					colorCodeCellsCheckBox.IsEnabled = false;
					showDistanceNumbersCheckBox.IsEnabled = false;
				}
			}
			else
			{
				originalStrokesCheckBox.IsEnabled = false;
				directionalComponentsCheckBox.IsEnabled = false;
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
					originalStrokesCheckBox.IsEnabled = false;
					directionalComponentsCheckBox.IsEnabled = false;
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
					else if (singleUserHoldOutTest.ConfusionMatrix[i] >= Params.CONFUSION_MATRIX_THRESHOLD_OFF_DIAGONAL_1)
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
				infoPixelsContainingInkLabel.Content = shapeInstance.NumOfPixelsContainingInk;

				if (recognizedTemplate != null)
				{
					infoRecognizedIDLabel.Content = shapeInstance.RecognizedShapeID;
					infoRecognizedDistanceLabel.Content = shapeInstance.RecognizedDistance;
					infoRecognizedPixelsContainingInkLabel.Content = recognizedTemplate.AvgNumOfPixelsContainingInk;
				}
				else
				{
					infoRecognizedIDLabel.Content = "--";
					infoRecognizedDistanceLabel.Content = "--";
					infoRecognizedPixelsContainingInkLabel.Content = "--";
				}

				infoActualLabel.Content = shapeInstance.ActualShapeID;
				infoActualDistanceLabel.Content = recognizer.GetDistance(shapeInstance, actualTemplate);
				infoActualPixelsContainingInkLabel.Content = actualTemplate.AvgNumOfPixelsContainingInk;
			}
			else
			{
				// Show image information

				if (originalStrokesCheckBox.IsChecked == true)
				{
					// Show the original strokes

					RecognizerPoint[][] points = GetPoints(shapeInstance, false);

					mainCanvasStackPanel.Visibility = Visibility.Visible;

					graphicsMain.Clear();

					DrawStrokes(mainCanvas,
						points,
						strokePointsCheckBox.IsChecked == true,
						strokeLinesCheckBox.IsChecked == true,
						colorCodeCellsCheckBox.IsChecked == true,
						false,
						shapeInstance.BoundingBox);
				}
				else
				{
					// Normalize the strokes

					if (directionalComponentsCheckBox.IsChecked == true)
					{
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
							GetPoints(shapeInstance, true),
							strokePointsCheckBox.IsChecked == true,
							strokeLinesCheckBox.IsChecked == true,
							colorCodeCellsCheckBox.IsChecked == true,
							true,
							shapeInstance.BoundingBox);
						DrawStrokes(topRightCanvas,
							GetPoints(shapeInstance, true), 
							strokePointsCheckBox.IsChecked == true, 
							strokeLinesCheckBox.IsChecked == true,
							colorCodeCellsCheckBox.IsChecked == true,
							true,
							shapeInstance.BoundingBox);
						DrawStrokes(bottomLeftCanvas,
							GetPoints(shapeInstance, true), 
							strokePointsCheckBox.IsChecked == true, 
							strokeLinesCheckBox.IsChecked == true,
							colorCodeCellsCheckBox.IsChecked == true,
							true,
							shapeInstance.BoundingBox);
						DrawStrokes(bottomRightCanvas,
							GetPoints(shapeInstance, true), 
							strokePointsCheckBox.IsChecked == true, 
							strokeLinesCheckBox.IsChecked == true,
							colorCodeCellsCheckBox.IsChecked == true,
							true,
							shapeInstance.BoundingBox);
					}
					else
					{
						// Show the single main bitmap

						mainCanvasStackPanel.Visibility = Visibility.Visible;

						graphicsMain.Clear();

						if (noPixelValuesRadioButton.IsChecked != true)
						{
							DrawGridCells(graphicsMain, bitmapGridSquaresMain);
						}

						if (gridLinesCheckBox.IsChecked == true)
						{
							DrawGridLines(graphicsMain, bitmapGridLinesMain);
						}

						DrawStrokes(mainCanvas,
							GetPoints(shapeInstance, true), 
							strokePointsCheckBox.IsChecked == true, 
							strokeLinesCheckBox.IsChecked == true,
							colorCodeCellsCheckBox.IsChecked == true,
							true,
							shapeInstance.BoundingBox);
					}
				}
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
				infoAvgPixelsContainingInkLabel.Content = template.AvgNumOfPixelsContainingInk;
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

		private static RecognizerPoint[][] GetPoints(ShapeInstance shapeInstance, bool normalize)
		{
			IEnumerable<RecognizerStroke> strokes = shapeInstance.Strokes;
			Rect boundingBox = shapeInstance.BoundingBox;
			double minX = boundingBox.X;
			double minY = boundingBox.Y;
			double maxX = boundingBox.X + boundingBox.Width;
			double maxY = boundingBox.Y + boundingBox.Height;
			if (normalize)
			{
				RecognizerPoint[][] points = new RecognizerPoint[strokes.Count()][];
				for (int i = 0; i < strokes.Count(); ++i)
				{
					points[i] = strokes.ElementAt(i).GetNormalizedRecognizerPoints(minX, minY, maxX, maxY);
				}
				return points;
			}
			else
			{
				RecognizerPoint[][] points = new RecognizerPoint[strokes.Count()][];
				for (int i = 0; i < strokes.Count(); ++i)
				{
					points[i] = strokes.ElementAt(i).RecognizerPoints;
				}
				return points;
			}
		}

		private void EnableRadioButtons(bool isEnabled)
		{
			shapeInstanceValuesAloneRadioButton.IsEnabled = isEnabled;
			templateOverlayActualRadioButton.IsEnabled = isEnabled;
			templateOverlayRecognizedRadioButton.IsEnabled = isEnabled;
			noPixelValuesRadioButton.IsEnabled = isEnabled;
			templateDistancesActualRadioButton.IsEnabled = isEnabled;
			templateDistancesRecognizedRadioButton.IsEnabled = isEnabled;
		}
	}
}
