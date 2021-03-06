﻿using System.Drawing;

namespace ZxcScreenShot.ui
{
    class DrawingTool
    {
        public enum DrawingToolType
        {
            NotDrawingTool = 0,
            Rectangle,
            Line,
            Arrow,
            FilledRectangle
        }

        public DrawingToolType Type { get; set; }

        public Point From { get; set; }
        public Point To { get; set; }
        public Color Color { get; set; }
        public bool DrawStraight { get; set; }
    }
}
