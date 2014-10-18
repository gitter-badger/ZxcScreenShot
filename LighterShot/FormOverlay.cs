﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace LighterShot
{
    public partial class FormOverlay : Form
    {
        #region:::::::::::::::::::::::::::::::::::::::::::Form level declarations:::::::::::::::::::::::::::::::::::::::::::

        public enum ClickAction
        {
            NoClick = 0,
            Dragging,
            Outside,
            TopSizing,
            BottomSizing,
            LeftSizing,
            TopLeftSizing,
            BottomLeftSizing,
            RightSizing,
            TopRightSizing,
            BottomRightSizing,
            DrawingTool
        }

        public enum CursPos
        {
            WithinSelectionArea = 0,
            OutsideSelectionArea,
            TopLine,
            BottomLine,
            LeftLine,
            RightLine,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private readonly ToolsPainter toolsPainter = new ToolsPainter();

        public Point ClickPoint = new Point();
        public ClickAction CurrentAction;
        public Point CurrentBottomRight = new Point();
        public Point CurrentTopLeft = new Point();
        public Point DragClickRelative = new Point();
        public bool LeftButtonDown = false;
        public bool RectangleDrawn = false;

        public int RectangleHeight = new int();
        public int RectangleWidth = new int();

        public Form InstanceRef { get; set; }

        // tells that user has clicked any of Tool buttons
        private DrawingTool.DrawingToolType _goingToDrawTool = DrawingTool.DrawingToolType.NotDrawingTool;

        private Bitmap screenBitmap;

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                e = null;
            }
            base.OnMouseClick(e);
        }

        #endregion

        #region:::::::::::::::::::::::::::::::::::::::::::Mouse Event Handlers & Drawing Initialization:::::::::::::::::::::::::::::::::::::::::::

        public FormOverlay()
        {
            InstanceRef = null;

            InitializeComponent();

            Cursor = Cursors.Arrow;
            this.ControlBox = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            //            this.Opacity = 0.1D;
            //            this.TransparencyKey = System.Drawing.Color.White;

            pictureBox1.MouseDown += mouse_Click;
            pictureBox1.MouseDoubleClick += mouse_DClick;
            pictureBox1.MouseUp += mouse_Up;
            pictureBox1.MouseMove += mouse_Move;

            panelTools.MouseMove += panel_mouse_move;
            buttonDrawRect.MouseMove += panel_mouse_move;
            buttonDrawLine.MouseMove += panel_mouse_move;
            buttonDrawArrow.MouseMove += panel_mouse_move;
            buttonDrawColor.MouseMove += panel_mouse_move;
            buttonDone.MouseMove += panel_mouse_move;

            KeyDown += key_down;
            KeyUp += key_up;
            buttonCancel.KeyDown += key_down;
            buttonCancel.KeyUp += key_up;
            buttonDrawRect.KeyDown += key_down;
            buttonDrawRect.KeyUp += key_up;
            buttonDrawLine.KeyDown += key_down;
            buttonDrawLine.KeyUp += key_up;
            buttonDrawArrow.KeyDown += key_down;
            buttonDrawArrow.KeyUp += key_up;
            buttonDrawColor.KeyDown += key_down;
            buttonDrawColor.KeyUp += key_up;
            panelTools.KeyDown += key_down;
            panelTools.KeyUp += key_up;

            screenBitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Size.Width, Screen.PrimaryScreen.Bounds.Size.Height);
            ScreenShot.GetScreenCapture(screenBitmap);

            pictureBox1.Image = screenBitmap;

            toolsPainter.Clear();

            timer1.Enabled = true;
        }

        #endregion

        public void SaveSelection()
        {
            //Allow 250 milliseconds for the screen to repaint itself (we don't want to include this form in the capture)
            Thread.Sleep(250);

            var startPoint = new Point(CurrentTopLeft.X, CurrentTopLeft.Y);
            var bounds = new Rectangle(CurrentTopLeft.X, CurrentTopLeft.Y, CurrentBottomRight.X - CurrentTopLeft.X,
                CurrentBottomRight.Y - CurrentTopLeft.Y);

            ScreenShot.CaptureImage(startPoint, Point.Empty, bounds, pictureBox1, toolsPainter);

//            MessageBox.Show(@"Area saved to clipboard and file", @"Lightershot", MessageBoxButtons.OK);

            if (InstanceRef != null)
            {
                InstanceRef.Show();
            }
            Close();
        }

        public void key_down(object sender, KeyEventArgs e)
        {
            if (e.Shift && CurrentAction == ClickAction.DrawingTool)
            {
                toolsPainter.DrawStraightLatest(true);
            }
        }

        public void key_up(object sender, KeyEventArgs e)
        {
            if (!e.Shift && CurrentAction == ClickAction.DrawingTool)
            {
                toolsPainter.DrawStraightLatest(false);
            }
            if (e.KeyCode.ToString() == "C" && e.Control && RectangleDrawn)
            {
                SaveSelection();
            }
            if (e.KeyCode.ToString() == "Z" && e.Control && CurrentAction != ClickAction.DrawingTool)
            {
                if (toolsPainter.Undo())
                {
                    pictureBox1.Invalidate();
                }
            }
        }

        private void SetClickAction()
        {
            switch (UiUtils.UpdateCursorAndGetCursorPosition(this, CurrentTopLeft, CurrentBottomRight, _goingToDrawTool == DrawingTool.DrawingToolType.NotDrawingTool))
            {
                case CursPos.BottomLine:
                    CurrentAction = ClickAction.BottomSizing;
                    break;
                case CursPos.TopLine:
                    CurrentAction = ClickAction.TopSizing;
                    break;
                case CursPos.LeftLine:
                    CurrentAction = ClickAction.LeftSizing;
                    break;
                case CursPos.TopLeft:
                    CurrentAction = ClickAction.TopLeftSizing;
                    break;
                case CursPos.BottomLeft:
                    CurrentAction = ClickAction.BottomLeftSizing;
                    break;
                case CursPos.RightLine:
                    CurrentAction = ClickAction.RightSizing;
                    break;
                case CursPos.TopRight:
                    CurrentAction = ClickAction.TopRightSizing;
                    break;
                case CursPos.BottomRight:
                    CurrentAction = ClickAction.BottomRightSizing;
                    break;
                case CursPos.WithinSelectionArea:
                    CurrentAction = ClickAction.Dragging;
                    break;
                case CursPos.OutsideSelectionArea:
                    CurrentAction = ClickAction.Outside;
                    break;
            }
        }

        private void ResizeSelection()
        {
            if (CurrentAction == ClickAction.LeftSizing)
            {
                if (Cursor.Position.X < CurrentBottomRight.X - 10)
                {
                    //Erase the previous rectangle
                    CurrentTopLeft.X = Cursor.Position.X;
                    RectangleWidth = CurrentBottomRight.X - CurrentTopLeft.X;
                }
            }
            if (CurrentAction == ClickAction.TopLeftSizing)
            {
                if (Cursor.Position.X < CurrentBottomRight.X - 10 && Cursor.Position.Y < CurrentBottomRight.Y - 10)
                {
                    //Erase the previous rectangle
                    CurrentTopLeft.X = Cursor.Position.X;
                    CurrentTopLeft.Y = Cursor.Position.Y;
                    RectangleWidth = CurrentBottomRight.X - CurrentTopLeft.X;
                    RectangleHeight = CurrentBottomRight.Y - CurrentTopLeft.Y;
                }
            }
            if (CurrentAction == ClickAction.BottomLeftSizing)
            {
                if (Cursor.Position.X < CurrentBottomRight.X - 10 && Cursor.Position.Y > CurrentTopLeft.Y + 10)
                {
                    //Erase the previous rectangle
                    CurrentTopLeft.X = Cursor.Position.X;
                    CurrentBottomRight.Y = Cursor.Position.Y;
                    RectangleWidth = CurrentBottomRight.X - CurrentTopLeft.X;
                    RectangleHeight = CurrentBottomRight.Y - CurrentTopLeft.Y;
                }
            }
            if (CurrentAction == ClickAction.RightSizing)
            {
                if (Cursor.Position.X > CurrentTopLeft.X + 10)
                {
                    //Erase the previous rectangle
                    CurrentBottomRight.X = Cursor.Position.X;
                    RectangleWidth = CurrentBottomRight.X - CurrentTopLeft.X;
                }
            }
            if (CurrentAction == ClickAction.TopRightSizing)
            {
                if (Cursor.Position.X > CurrentTopLeft.X + 10 && Cursor.Position.Y < CurrentBottomRight.Y - 10)
                {
                    //Erase the previous rectangle
                    CurrentBottomRight.X = Cursor.Position.X;
                    CurrentTopLeft.Y = Cursor.Position.Y;
                    RectangleWidth = CurrentBottomRight.X - CurrentTopLeft.X;
                    RectangleHeight = CurrentBottomRight.Y - CurrentTopLeft.Y;
                }
            }
            if (CurrentAction == ClickAction.BottomRightSizing)
            {
                if (Cursor.Position.X > CurrentTopLeft.X + 10 && Cursor.Position.Y > CurrentTopLeft.Y + 10)
                {
                    //Erase the previous rectangle
                    CurrentBottomRight.X = Cursor.Position.X;
                    CurrentBottomRight.Y = Cursor.Position.Y;
                    RectangleWidth = CurrentBottomRight.X - CurrentTopLeft.X;
                    RectangleHeight = CurrentBottomRight.Y - CurrentTopLeft.Y;
                }
            }
            if (CurrentAction == ClickAction.TopSizing)
            {
                if (Cursor.Position.Y < CurrentBottomRight.Y - 10)
                {
                    //Erase the previous rectangle
                    CurrentTopLeft.Y = Cursor.Position.Y;
                    RectangleHeight = CurrentBottomRight.Y - CurrentTopLeft.Y;
                }
            }
            if (CurrentAction == ClickAction.BottomSizing)
            {
                if (Cursor.Position.Y > CurrentTopLeft.Y + 10)
                {
                    //Erase the previous rectangle
                    CurrentBottomRight.Y = Cursor.Position.Y;
                    RectangleHeight = CurrentBottomRight.Y - CurrentTopLeft.Y;
                }
            }
            UpdateUi();
        }

        private void MoveDrawingTool()
        {
            toolsPainter.MoveLatestTo(new Point(Cursor.Position.X - CurrentTopLeft.X, Cursor.Position.Y - CurrentTopLeft.Y));

            UpdateUi();
        }

        private void DragSelection()
        {
            //Ensure that the rectangle stays within the bounds of the screen

            if (Cursor.Position.X - DragClickRelative.X > 0 &&
                Cursor.Position.X - DragClickRelative.X + RectangleWidth < Screen.PrimaryScreen.Bounds.Width)
            {
                CurrentTopLeft.X = Cursor.Position.X - DragClickRelative.X;
                CurrentBottomRight.X = CurrentTopLeft.X + RectangleWidth;
            }
            else
                //Selection area has reached the right side of the screen
                if (Cursor.Position.X - DragClickRelative.X > 0)
                {
                    CurrentTopLeft.X = Screen.PrimaryScreen.Bounds.Width - RectangleWidth;
                    CurrentBottomRight.X = CurrentTopLeft.X + RectangleWidth;
                }
                    //Selection area has reached the left side of the screen
                else
                {
                    CurrentTopLeft.X = 0;
                    CurrentBottomRight.X = CurrentTopLeft.X + RectangleWidth;
                }

            if (Cursor.Position.Y - DragClickRelative.Y > 0 &&
                Cursor.Position.Y - DragClickRelative.Y + RectangleHeight < Screen.PrimaryScreen.Bounds.Height)
            {
                CurrentTopLeft.Y = Cursor.Position.Y - DragClickRelative.Y;
                CurrentBottomRight.Y = CurrentTopLeft.Y + RectangleHeight;
            }
            else
                //Selection area has reached the bottom of the screen
                if (Cursor.Position.Y - DragClickRelative.Y > 0)
                {
                    CurrentTopLeft.Y = Screen.PrimaryScreen.Bounds.Height - RectangleHeight;
                    CurrentBottomRight.Y = CurrentTopLeft.Y + RectangleHeight;
                }
                    //Selection area has reached the top of the screen
                else
                {
                    CurrentTopLeft.Y = 0;
                    CurrentBottomRight.Y = CurrentTopLeft.Y + RectangleHeight;
                }

            UpdateUi();
        }

        private void DrawSelection()
        {
            Cursor = Cursors.Arrow;

            //Calculate X Coordinates
            if (Cursor.Position.X < ClickPoint.X)
            {
                CurrentTopLeft.X = Cursor.Position.X;
                CurrentBottomRight.X = ClickPoint.X;
            }
            else
            {
                CurrentTopLeft.X = ClickPoint.X;
                CurrentBottomRight.X = Cursor.Position.X;
            }

            //Calculate Y Coordinates
            if (Cursor.Position.Y < ClickPoint.Y)
            {
                CurrentTopLeft.Y = Cursor.Position.Y;
                CurrentBottomRight.Y = ClickPoint.Y;
            }
            else
            {
                CurrentTopLeft.Y = ClickPoint.Y;
                CurrentBottomRight.Y = Cursor.Position.Y;
            }

            UpdateUi();
        }

        private void FormOverlay_Load(object sender, EventArgs e)
        {
        }

        private void FormOverlay_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (InstanceRef != null)
            {
                InstanceRef.Show();
            }
            else
            {
                Application.Exit();
            }
        }

        #region:::::::::::::::::::::::::::::::::::::::::::Mouse Buttons:::::::::::::::::::::::::::::::::::::::::::

        private void mouse_Click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                LeftButtonDown = true;
                ClickPoint = new Point(MousePosition.X, MousePosition.Y);

                if (_goingToDrawTool == DrawingTool.DrawingToolType.NotDrawingTool)
                {
                    panelTools.Visible = false;
                    panelOutput.Visible = false;
                    SetClickAction();
                }
                else
                {
                    CurrentAction = ClickAction.DrawingTool;
                    Cursor = Cursors.Hand;
                    toolsPainter.Push(new DrawingTool
                    {
                        Type = _goingToDrawTool,
                        From = new Point(ClickPoint.X - CurrentTopLeft.X, ClickPoint.Y - CurrentTopLeft.Y),
                        To = new Point(ClickPoint.X - CurrentTopLeft.X, ClickPoint.Y - CurrentTopLeft.Y),
                        Color = buttonDrawColor.BackColor,
                        DrawStraight = false
                    });
                }

                if (RectangleDrawn)
                {
                    RectangleHeight = CurrentBottomRight.Y - CurrentTopLeft.Y;
                    RectangleWidth = CurrentBottomRight.X - CurrentTopLeft.X;
                    DragClickRelative.X = Cursor.Position.X - CurrentTopLeft.X;
                    DragClickRelative.Y = Cursor.Position.Y - CurrentTopLeft.Y;
                }
            }
        }

        private void mouse_DClick(object sender, MouseEventArgs e)
        {
            if (RectangleDrawn)
            {
                SaveSelection();
            }
        }

        private void mouse_Up(object sender, MouseEventArgs e)
        {
            RectangleDrawn = true;
            LeftButtonDown = false;
            CurrentAction = ClickAction.NoClick;
            UpdatePanelPosition(force: true);
            panelOutput.Visible = panelTools.Visible = true;
        }

        private void panel_mouse_move(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Arrow;
        }

        private void mouse_Move(object sender, MouseEventArgs e)
        {
            if (LeftButtonDown && !RectangleDrawn)
            {
                DrawSelection();
            }

            if (RectangleDrawn)
            {
                var pos = UiUtils.UpdateCursorAndGetCursorPosition(this, CurrentTopLeft, CurrentBottomRight, _goingToDrawTool == DrawingTool.DrawingToolType.NotDrawingTool);
                if (pos == CursPos.WithinSelectionArea)
                {
                    if (_goingToDrawTool != DrawingTool.DrawingToolType.NotDrawingTool ||
                        CurrentAction == ClickAction.DrawingTool)
                    {
                        Cursor = Cursors.Hand;
                    }
                    else
                    {
                        Cursor = Cursors.SizeAll;
                    }
                }

                if (CurrentAction == ClickAction.Dragging)
                {
                    DragSelection();
                }

                if (CurrentAction == ClickAction.DrawingTool)
                {
                    MoveDrawingTool();
                }

                if (CurrentAction != ClickAction.Dragging && CurrentAction != ClickAction.Outside &&
                    CurrentAction != ClickAction.DrawingTool)
                {
                    ResizeSelection();
                }
            }
        }

        #endregion

        private void UpdateUi()
        {
            UpdatePanelPosition();

            // redraw rectangle
            pictureBox1.Invalidate();
        }

        private void UpdatePanelPosition(Boolean force = false)
        {
            if (force || panelTools.Visible)
            {
                // move panel
                if (CurrentBottomRight.X + 10 + panelTools.Width + 10 < Screen.PrimaryScreen.Bounds.Width)
                {
                    // panel fits on the right
                    panelTools.Left = CurrentBottomRight.X + 10;
                }
                else
                {
                    // place panel on the left
                    panelTools.Left = CurrentTopLeft.X - panelTools.Width - 10;
                }
                panelTools.Top = Math.Max(10, CurrentBottomRight.Y - panelTools.Height);

                // move panel
                if (CurrentBottomRight.Y + 10 + panelOutput.Height + 10 < Screen.PrimaryScreen.Bounds.Height)
                {
                    // panel fits in the bottom
                    panelOutput.Top = CurrentBottomRight.Y + 10;
                }
                else
                {
                    // place panel on top
                    panelOutput.Top = CurrentTopLeft.Y - panelOutput.Height - 10;
                }
                panelOutput.Left = Math.Max(10, CurrentBottomRight.X - panelOutput.Width - 10);
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            do_paint(e.Graphics);
        }

        private void do_paint(Graphics graphics) {
            var box = new Rectangle(CurrentTopLeft.X, CurrentTopLeft.Y, CurrentBottomRight.X - CurrentTopLeft.X,
                CurrentBottomRight.Y - CurrentTopLeft.Y); // new Rectangle(100, 50, 120, 70);

            var extendedBox = new Rectangle(CurrentTopLeft.X - 20, CurrentTopLeft.Y + 20, CurrentBottomRight.X - CurrentTopLeft.X + 40,
                CurrentBottomRight.Y - CurrentTopLeft.Y + 40);

            DrawWorkarea(graphics, box);
            DrawFrame(graphics, box);
            toolsPainter.DrawAllTools(graphics, CurrentTopLeft, CurrentTopLeft, CurrentBottomRight);

            graphics.DrawString(string.Format(@"{0}x{1} @ {2},{3}", box.Width, box.Height, box.Left, box.Top), DefaultFont, Brushes.White, box.Left, box.Top - 20);
        }

        private void DrawWorkarea(Graphics graphics, Rectangle box)
        {
            graphics.SetClip(box, CombineMode.Exclude);
            using (var b = new SolidBrush(Color.FromArgb(128, 0, 0, 0)))
            {
                graphics.FillRectangle(b, ClientRectangle);
            }
            graphics.ResetClip();
        }

        private void DrawFrame(Graphics graphics, Rectangle box)
        {
            var tlCorner = new Rectangle(box.Left - 2, box.Top - 2, 5, 5);
            var trCorner = new Rectangle(box.Left + box.Width - 2, box.Top - 2, 5, 5);
            var blCorner = new Rectangle(box.Left - 2, box.Top + box.Height - 2, 5, 5);
            var brCorner = new Rectangle(box.Left + box.Width - 2, box.Top + box.Height - 2, 5, 5);

            if (_goingToDrawTool == DrawingTool.DrawingToolType.NotDrawingTool)
            {
                using (var borderPen = new Pen(Brushes.White, 1))
                {
                    graphics.DrawRectangle(borderPen, tlCorner);
                    graphics.DrawRectangle(borderPen, trCorner);
                    graphics.DrawRectangle(borderPen, blCorner);
                    graphics.DrawRectangle(borderPen, brCorner);
                }
            }

            float[] dashValues = { 3, 3 };
            using (var dashedPen = new Pen(Color.White, 1) { DashPattern = dashValues })
            {
                graphics.DrawLine(dashedPen, new Point(box.Left + 4, box.Top),
                    new Point(box.Left + box.Width - 2, box.Top));
                graphics.DrawLine(dashedPen, new Point(box.Left + box.Width, box.Top + 4),
                    new Point(box.Left + box.Width, box.Top + box.Height - 2));
                graphics.DrawLine(dashedPen, new Point(box.Left + box.Width - 2, box.Top + box.Height),
                    new Point(box.Left + 2, box.Top + box.Height));
                graphics.DrawLine(dashedPen, new Point(box.Left, box.Top + box.Height - 2),
                    new Point(box.Left, box.Top + 2));
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void buttonDrawRect_Click(object sender, EventArgs e)
        {
            _goingToDrawTool = DrawingTool.DrawingToolType.Rectangle;
            buttonDrawRect.Enabled = false;
            buttonDrawLine.Enabled = true;
            buttonDrawArrow.Enabled = true;
        }

        private void buttonDrawLine_Click(object sender, EventArgs e)
        {
            _goingToDrawTool = DrawingTool.DrawingToolType.Line;
            buttonDrawRect.Enabled = true;
            buttonDrawLine.Enabled = false;
            buttonDrawArrow.Enabled = true;
        }

        private void buttonDrawArrow_Click(object sender, EventArgs e)
        {
            _goingToDrawTool = DrawingTool.DrawingToolType.Arrow;
            buttonDrawRect.Enabled = true;
            buttonDrawLine.Enabled = true;
            buttonDrawArrow.Enabled = false;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // labelInfo.Text = _goingToDrawTool.ToString() + @"/" + CurrentAction.ToString();
        }

        private void buttonDrawColor_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                buttonDrawColor.BackColor = colorDialog1.Color;
            }
        }

        private void buttonDrawArrow_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void buttonDrawArrow_KeyUp(object sender, KeyEventArgs e)
        {
        }

        private void buttonDone_Click(object sender, EventArgs e)
        {
            _goingToDrawTool = DrawingTool.DrawingToolType.NotDrawingTool;
            buttonDrawRect.Enabled = true;
            buttonDrawLine.Enabled = true;
            buttonDrawArrow.Enabled = true;
        }
    }
}