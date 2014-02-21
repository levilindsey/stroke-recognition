/**
 * Author: Levi Lindsey (llind001@cs.ucr.edu)
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Shapes;

namespace StrokeCollector
{
	public interface Drawable
	{
		/// <summary>
		/// Return the Shape for drawing on the Canvas.
		/// </summary>
		Shape GetShape();

		String GetDataFileEntry();
	}
}
