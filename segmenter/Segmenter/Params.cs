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
	public enum PointProcessing
	{
		Raw, Smoothed, Resampled, FeaturePts, Segments, PointsAndSegments, InkPointsAndSegments
	}

	public enum SegmentationAlgorithm
	{
		EndPointsOnly, ShortStraw, SpeedSeg, CustomSegWOPostProcess, CustomSeg
	}

	public enum AxisValue
	{
		X, Y, Timestamp, ArcLength, Speed, Curvature, StrawValue
	}

	public enum SaveValue
	{
		DefaultInts, DefaultDbls, ResampledInts, ResampledDbls, FeaturePointsInts, 
		FeaturePointsDbls, ArcLength, SpeedRaw, SpeedSmoothed, CurvatureRaw, CurvatureSmoothed,
		StrawValue, Segments, FeaturePoints
	}

	class Params
	{

		#region CONST_VALUES

		public const char
			INTRA_POINT_SAVE_DELIMITER			= '\t'
			;

		public static char[]
			INTRA_POINT_LOAD_DELIMITERS			= new char[] { '\t', ' ', ',' }
			;

		public const int
			SPEED_SEG_P_PMR						= 7,

			CUSTOM_SEG_P_INDEX_PMR				= 3,

			SHORT_STRAW_ADD_LINE_STOP_DISTANCE	= 4,
			STRAW_VALUE_W						= 3,

			MIN_POINT_COUNT						= 10,

			MENU_WIDTH							= 140,

			BUFFER_SIZE							= 512
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

			GAUSSIAN_SIDE_RATIO					= 0.2248,
			GAUSSIAN_CENTER_RATIO				= 0.5504,

			CURVATURE_COLOR_STRENGTH			= 10000,
			TWO_PI								= 2 * Math.PI,

			MIN_WIDTH							= 0.5,		// in pixels
			MAX_WIDTH							= 4.0,		// in pixels

			MIN_WIDTH_SPEED						= 1.88,		// in pixels per millisecond
			MAX_WIDTH_SPEED						= 0.05,		// in pixels per millisecond

			ERASE_STROKE_THICKNESS				= 2.4,		// in pixels
			DEFAULT_STROKE_THICKNESS			= 1.2,		// in pixels
			LINE_SEGMENT_THICKNESS				= 3.0,		// in pixels
			ARC_SEGMENT_THICKNESS				= 3.0,		// in pixels
			FEATURE_POINT_THICKNESS				= 3.0,		// in pixels

			FEATURE_POINT_RADIUS				= 7.0,		// in pixels

			ARC_SEGMENT_POINT_SPACING			= 2.0,		// in pixels

			CW_TEST_PT_2_INDEX_RATIO			= 1 / 1.9999,

			EPSILON								= Double.Epsilon * 5
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
			FEATURE_POINT_COLOR					= Color.FromArgb(140, 170, 20, 250)
			;

		public static SolidColorBrush
			DEFAULT_STROKE_BRUSH				= new SolidColorBrush(DEFAULT_STROKE_COLOR),
			ERASE_STROKE_BRUSH					= new SolidColorBrush(ERASE_STROKE_COLOR),
			LINE_SEGMENT_BRUSH					= new SolidColorBrush(LINE_SEGMENT_COLOR),
			ARC_SEGMENT_BRUSH					= new SolidColorBrush(ARC_SEGMENT_COLOR),
			FEATURE_POINT_BRUSH					= new SolidColorBrush(FEATURE_POINT_COLOR)
			;

		#endregion

	}
}
