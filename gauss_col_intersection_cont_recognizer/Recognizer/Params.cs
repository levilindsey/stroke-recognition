/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StrokeCollector
{
	public enum RecognizerWindowType
	{
		CrossValidate, Canvas, HoldOut, Templates
	}

	class Params
	{

		#region CONST_VALUES

		public const char
			INTRA_POINT_SAVE_DELIMITER			= '\t'
			;

		public static char[]
			INTRA_POINT_LOAD_DELIMITERS			= new char[] { '\t', ' ', ',' },
			PATH_DIRECTORY_DELIMITER			= {'\\', '/'}
			;

		public const short
			SPEED_SEG_P_PMR						= 7,

			CUSTOM_SEG_P_INDEX_PMR				= 3,

			SHORT_STRAW_ADD_LINE_STOP_DISTANCE	= 4,
			STRAW_VALUE_W						= 3,

			MIN_POINT_COUNT						= 4,

			MENU_WIDTH							= 140,

			BUFFER_SIZE							= 512,

			TRAINING_DATA_PATH_TITLE_LENGTH		= 24,

			CONFUSION_MATRIX_THRESHOLD_DIAGONAL_1		= 10,
			CONFUSION_MATRIX_THRESHOLD_DIAGONAL_2		= 7,
			CONFUSION_MATRIX_THRESHOLD_OFF_DIAGONAL_1	= 0,
			CONFUSION_MATRIX_THRESHOLD_OFF_DIAGONAL_2	= 3
			;

		public const double
			SPEED_SEG_CIRCLE_FIT_ANGLE_THRES	= 36 * Math.PI / 180,	// rad

			CUSTOM_SEG_P_SSST					= 0.4,
			CUSTOM_SEG_P_SSCT					= 0.5 * Math.PI / 180,	// rad/px
			CUSTOM_SEG_P_SCT					= 0.005 * Math.PI / 180,	// rad/px
			CUSTOM_SEG_P_VETO_CT				= 4.0 * Math.PI / 180,	// rad/px

			CUSTOM_SEG_P_ST						= 0.25,
			CUSTOM_SEG_P_CT						= 0.75 * Math.PI / 180,	// rad/px
			CUSTOM_SEG_P_CST					= 0.4,
			CUSTOM_SHORT_STRAW_CORNER_THRESHOLD = 0.8,
			CUSTOM_IS_LINE_THRESHOLD			= 0.92,
			CUSTOM_SEG_P_ARC_LENGTH_PMR			= 13.0,		// in pixels

			//CUSTOM_SEG_P_SSST					= 0.8,
			//CUSTOM_SEG_P_SSCT					= 0.5 * Math.PI / 180,	// rad/px
			//CUSTOM_SEG_P_ST						= 0.25,
			//CUSTOM_SEG_P_CT						= 0.75 * Math.PI / 180,	// rad/px
			//CUSTOM_SEG_P_CST					= 0.8,
			//CUSTOM_SHORT_STRAW_CORNER_THRESHOLD = 0.8,

			SPEED_SEG_P_ST						= 0.25,
			SPEED_SEG_P_CT						= 0.75 * Math.PI / 180,	// rad/px
			SPEED_SEG_P_CST						= 0.8,

			SHORT_STRAW_CORNER_THRESHOLD		= 0.98,
			SHORT_STRAW_IS_LINE_THRESHOLD		= 0.95,

			GAUSSIAN_3_1D_0_RATIO				= 0.5504,
			GAUSSIAN_3_1D_1_RATIO				= 0.2248,
			GAUSSIAN_3_1D_SIDE_PLUS_CENTER_RATIO	= GAUSSIAN_3_1D_1_RATIO + GAUSSIAN_3_1D_0_RATIO,

			GAUSSIAN_2D_BIG_RATIO				= GAUSSIAN_3_1D_0_RATIO * GAUSSIAN_3_1D_0_RATIO,
			GAUSSIAN_2D_MEDIUM_RATIO			= GAUSSIAN_3_1D_0_RATIO * GAUSSIAN_3_1D_1_RATIO,
			GAUSSIAN_2D_SMALL_RATIO				= GAUSSIAN_3_1D_1_RATIO * GAUSSIAN_3_1D_1_RATIO,

			GAUSSIAN_2D_BIG_PLUS_2_MEDIUM_PLUS_SMALL_RATIO	= GAUSSIAN_2D_BIG_RATIO + 2 * GAUSSIAN_2D_MEDIUM_RATIO + GAUSSIAN_2D_SMALL_RATIO,
			GAUSSIAN_2D_BIG_PLUS_MEDIUM_RATIO	= GAUSSIAN_2D_BIG_RATIO + GAUSSIAN_2D_MEDIUM_RATIO,
			GAUSSIAN_2D_MEDIUM_PLUS_SMALL_RATIO = GAUSSIAN_2D_MEDIUM_RATIO + GAUSSIAN_2D_SMALL_RATIO,

			GAUSSIAN_5_1D_0_RATIO				= 0.40401,
			GAUSSIAN_5_1D_1_RATIO				= 0.24746,
			GAUSSIAN_5_1D_2_RATIO				= 0.050535,

			GAUSSIAN_7_1D_0_RATIO				= 0.333625,
			GAUSSIAN_7_1D_1_RATIO				= 0.238384,
			GAUSSIAN_7_1D_2_RATIO				= 0.083443,
			GAUSSIAN_7_1D_3_RATIO				= 0.01136,

			GAUSSIAN_9_1D_0_RATIO				= 0.290805,
			GAUSSIAN_9_1D_1_RATIO				= 0.224963,
			GAUSSIAN_9_1D_2_RATIO				= 0.10207,
			GAUSSIAN_9_1D_3_RATIO				= 0.025011,
			GAUSSIAN_9_1D_4_RATIO				= 0.002554,

			CURVATURE_COLOR_STRENGTH			= 10000,
			TWO_PI								= 2 * Math.PI,
			ONE_QUARTER_PI						= 0.25 * Math.PI,
			HALF_PI								= 0.5 * Math.PI,
			THREE_QUARTERS_PI					= 0.75 * Math.PI,
			THREE_HALVES_PI						= 1.5 * Math.PI,
			ONE_OVER_ONE_QUARTER_PI				= 1 / ONE_QUARTER_PI,

			MIN_WIDTH							= 0.5,		// in pixels
			MAX_WIDTH							= 4.0,		// in pixels

			MIN_WIDTH_SPEED						= 1.88,		// in pixels per millisecond
			MAX_WIDTH_SPEED						= 0.05,		// in pixels per millisecond

			ERASE_STROKE_THICKNESS				= 2.4,		// in pixels
			DEFAULT_STROKE_THICKNESS			= 1.2,		// in pixels
			LINE_SEGMENT_THICKNESS				= 3.0,		// in pixels
			ARC_SEGMENT_THICKNESS				= 3.0,		// in pixels
			FEATURE_POINT_THICKNESS				= 3.0,		// in pixels
			BITMAP_HORIZONTAL_GRID_THICKNESS	= 1.0,		// in pixels
			BITMAP_VERTICAL_GRID_THICKNESS		= 2.0,		// in pixels
			BITMAP_STROKE_THICKNESS				= 1.5,		// in pixels
			BITMAP_CELL_THICKNESS				= 0.0,		// in pixels
			BITMAP_DOT_THICKNESS				= 0.0,		// in pixels

			FEATURE_POINT_RADIUS				= 7.0,		// in pixels
			BITMAP_POINT_RADIUS					= 2.0,		// in pixels
			BITMAP_POINT_DIAMETER				= 2 * BITMAP_POINT_RADIUS,	// in pixels
			BITMAP_END_POINT_RADIUS				= 6.0,		// in pixels
			BITMAP_END_POINT_DIAMETER			= 2 * BITMAP_END_POINT_RADIUS,	// in pixels

			ARC_SEGMENT_POINT_SPACING			= 2.0,		// in pixels

			CW_TEST_PT_2_INDEX_RATIO			= 1 / 1.9999,

			EPSILON								= Double.Epsilon * 5,

			LARGE_ANGLE_CHANGE_IGNORE_THRESHOLD = HALF_PI
			;

		public const String
			NEWLINE_STR							= "\r\n",
			BIG_LINE_DELIMITER					= "========================================",

			LOAD_STROKES_WARNING_MESSAGE_STR	= "Your un-saved strokes will be abandoned!\nAre you sure you want to load new strokes?",
			LOAD_STROKES_WARNING_CAPTION_STR	= "Abandon strokes?",
			SELECT_STROKE_FILE_DIALOG_TITLE_STR	= "Select Stroke File",
			SAVE_STROKE_FILE_DIALOG_TITLE_STR	= "Save Stroke File",
			SAVE_STROKE_FILE_DEFAULT_NAME_STR	= "stroke_data",
			STROKE_FILE_EXTENSION_STR			= ".txt",
			STROKE_FILE_FILTER_STR				= "Text Files (.txt)|*.txt",
			SELECT_SHAPES_DIRECTORY_DIALOG_TITLE_STR	= "Select shape test data directory",
			BAD_DIRECTORY_TITLE_STR				= "Bad Directory",

			VS_STR								= " vs ",
			STROKE_STR							= "Stroke ",
			X_AXIS_LABEL_STR					= "X (px)",
			Y_AXIS_LABEL_STR					= "Y (px)",
			TIMESTAMP_AXIS_LABEL_STR			= "Time (ms)",
			ARC_LENGTH_AXIS_LABEL_STR			= "Arc Length (px)",
			SPEED_AXIS_LABEL_STR				= "Speed (px/ms)",
			CURVATURE_AXIS_LABEL_STR			= "Cuvature (rads)",
			STRAW_VALUE_AXIS_LABEL_STR			= "Straw Value (px)"
			;

		public static Color
			DEFAULT_STROKE_COLOR				= Color.FromArgb(255, 0, 0, 0),
			ERASE_STROKE_COLOR					= Color.FromArgb(255, 255, 40, 0),
			LINE_SEGMENT_COLOR					= Color.FromArgb(140, 250, 71, 56),
			ARC_SEGMENT_COLOR					= Color.FromArgb(130, 55, 49, 250),
			FEATURE_POINT_COLOR					= Color.FromArgb(140, 170, 20, 250),
			BITMAP_HORIZONTAL_GRID_COLOR		= Color.FromArgb(255, 30, 30, 30),
			BITMAP_VERTICAL_GRID_COLOR			= Color.FromArgb(255, 100, 100, 100),
			BITMAP_STROKE_COLOR					= Color.FromArgb(255, 70, 70, 70),
			BITMAP_PIXEL_DOT_DEFAULT_COLOR		= Color.FromArgb(255, 0, 0, 0),
			BITMAP_PIXEL_DOT_1_COLOR			= Color.FromArgb(255, 40, 40, 110),
			BITMAP_PIXEL_DOT_2_COLOR			= Color.FromArgb(255, 100, 30, 100),
			BITMAP_PIXEL_DOT_FIRST_COLOR		= Color.FromArgb(255, 130, 130, 220),
			BITMAP_PIXEL_DOT_LAST_COLOR			= Color.FromArgb(255, 210, 110, 210),
			BITMAP_CELL_COLOR					= Color.FromArgb(255, 0, 0, 0),
			RECOGNITION_PASS_COLOR				= Color.FromArgb(255, 50, 220, 50),
			RECOGNITION_FAIL_COLOR				= Color.FromArgb(255, 220, 50, 50),

			CONFUSION_MATRIX_BACKGROUND_0_COLOR = Color.FromArgb(255, 0, 0, 0),
			CONFUSION_MATRIX_BACKGROUND_1_COLOR = Color.FromArgb(255, 20, 130, 60),
			CONFUSION_MATRIX_BACKGROUND_2_COLOR	= Color.FromArgb(255, 110, 110, 15),
			CONFUSION_MATRIX_BACKGROUND_3_COLOR = Color.FromArgb(255, 140, 20, 20),
			CONFUSION_MATRIX_FOREGROUND_0_COLOR = Color.FromArgb(255, 100, 100, 100),
			CONFUSION_MATRIX_FOREGROUND_1_COLOR	= Color.FromArgb(255, 120, 255, 190),
			CONFUSION_MATRIX_FOREGROUND_2_COLOR	= Color.FromArgb(255, 255, 255, 140),
			CONFUSION_MATRIX_FOREGROUND_3_COLOR	= Color.FromArgb(255, 255, 150, 150),

			F_MEASURES_COLOR					= Color.FromArgb(255, 255, 255, 255)
			;

		public static SolidColorBrush
			DEFAULT_STROKE_BRUSH				= new SolidColorBrush(DEFAULT_STROKE_COLOR),
			ERASE_STROKE_BRUSH					= new SolidColorBrush(ERASE_STROKE_COLOR),
			LINE_SEGMENT_BRUSH					= new SolidColorBrush(LINE_SEGMENT_COLOR),
			ARC_SEGMENT_BRUSH					= new SolidColorBrush(ARC_SEGMENT_COLOR),
			FEATURE_POINT_BRUSH					= new SolidColorBrush(FEATURE_POINT_COLOR),
			BITMAP_HORIZONTAL_GRID_BRUSH		= new SolidColorBrush(BITMAP_HORIZONTAL_GRID_COLOR),
			BITMAP_VERTICAL_GRID_BRUSH			= new SolidColorBrush(BITMAP_VERTICAL_GRID_COLOR),
			BITMAP_STROKE_BRUSH					= new SolidColorBrush(BITMAP_STROKE_COLOR),
			BITMAP_PIXEL_DOT_DEFAULT_BRUSH		= new SolidColorBrush(BITMAP_PIXEL_DOT_DEFAULT_COLOR),
			BITMAP_PIXEL_DOT_1_BRUSH			= new SolidColorBrush(BITMAP_PIXEL_DOT_1_COLOR),
			BITMAP_PIXEL_DOT_2_BRUSH			= new SolidColorBrush(BITMAP_PIXEL_DOT_2_COLOR),
			BITMAP_PIXEL_DOT_FIRST_BRUSH		= new SolidColorBrush(BITMAP_PIXEL_DOT_FIRST_COLOR),
			BITMAP_PIXEL_DOT_LAST_BRUSH			= new SolidColorBrush(BITMAP_PIXEL_DOT_LAST_COLOR),
			BITMAP_CELL_BRUSH					= new SolidColorBrush(BITMAP_CELL_COLOR),
			RECOGNITION_PASS_BRUSH				= new SolidColorBrush(RECOGNITION_PASS_COLOR),
			RECOGNITION_FAIL_BRUSH				= new SolidColorBrush(RECOGNITION_FAIL_COLOR),

			CONFUSION_MATRIX_BACKGROUND_0_BRUSH = new SolidColorBrush(CONFUSION_MATRIX_BACKGROUND_0_COLOR),
			CONFUSION_MATRIX_BACKGROUND_1_BRUSH = new SolidColorBrush(CONFUSION_MATRIX_BACKGROUND_1_COLOR),
			CONFUSION_MATRIX_BACKGROUND_2_BRUSH	= new SolidColorBrush(CONFUSION_MATRIX_BACKGROUND_2_COLOR),
			CONFUSION_MATRIX_BACKGROUND_3_BRUSH	= new SolidColorBrush(CONFUSION_MATRIX_BACKGROUND_3_COLOR),
			CONFUSION_MATRIX_FOREGROUND_0_BRUSH = new SolidColorBrush(CONFUSION_MATRIX_FOREGROUND_0_COLOR),
			CONFUSION_MATRIX_FOREGROUND_1_BRUSH = new SolidColorBrush(CONFUSION_MATRIX_FOREGROUND_1_COLOR),
			CONFUSION_MATRIX_FOREGROUND_2_BRUSH	= new SolidColorBrush(CONFUSION_MATRIX_FOREGROUND_2_COLOR),
			CONFUSION_MATRIX_FOREGROUND_3_BRUSH	= new SolidColorBrush(CONFUSION_MATRIX_FOREGROUND_3_COLOR),

			F_MEASURES_BRUSH					= new SolidColorBrush(F_MEASURES_COLOR)
			;

		#endregion

		public static String ParseShapeIDToDescription(short shapeID)
		{
			switch (shapeID)
			{
				case 1:		return "[1]: Curved arrow";
				case 2:		return "[2]: Straight arrow";
				case 3:		return "[3]: +";
				case 4:		return "[4]: =";
				case 5:		return "[5]: -";
				case 6:		return "[6]: 1/2";
				case 7:		return "[7]: X";
				case 8:		return "[8]: Y";
				case 9:		return "[9]: F";
				case 10:	return "[10]: G";
				case 11:	return "[11]: Sigma";
				case 12:	return "[12]: E";
				case 13:	return "[13]: Positive clockwise";
				case 14:	return "[14]: Square";
				case 15:	return "[15]: ?";

				case 16:	return "[16]: Star (small)*";
				case 17:	return "[17]: Star (medium)*";
				case 18:	return "[18]: Star (large)*";
				case 19:	return "[19]: b*";
				case 20:	return "[20]: d*";
				case 21:	return "[21]: p*";
				case 22:	return "[22]: W (rotated 90)*";
				case 23:	return "[23]: W (rotated 0)*";
				case 24:	return "[24]: W (rotated 45)*";
				case 25:	return "[25]: Square (v1)*";
				case 26:	return "[26]: Square (v2)*";
				case 27:	return "[27]: Square (v3, multi-stroke)*";

				default:	return "unknown";
			}
		}

	}
}
