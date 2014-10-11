﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace LighterShot
{
    internal static class ScreenShot
    {
        const string ScreenshotsDir = @"c:\Users\dmitr_000\Desktop\Screen shots\lightershot\";

        public static Bitmap GetScreenCapture(Bitmap bitmap)
        {
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(new Point(0, 0), new Point(0, 0), Screen.PrimaryScreen.Bounds.Size);

                return bitmap;
            }
        }

        public static void CaptureImage(Point sourcePoint, Point destinationPoint, Rectangle selectionRectangle, PictureBox pictureBox1)
        {
            using (var bitmap = new Bitmap(selectionRectangle.Width, selectionRectangle.Height))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    var destRect = new Rectangle {Location = destinationPoint, Size = selectionRectangle.Size};
                    var srcRect = new Rectangle {Location = sourcePoint, Size = selectionRectangle.Size};
                    g.DrawImage(pictureBox1.Image, destRect, srcRect, GraphicsUnit.Pixel);
                }

                // update clipboard
                Clipboard.SetImage(bitmap);

                // save to file
                bitmap.Save(ScreenshotsDir + "Screen shot " + DateTime.Now.ToString("yyyy-dd-M HH.mm.ss") + ".png", ImageFormat.Png);
            }
        }
    }
}