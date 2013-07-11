/*
* EmguUtilities.cs
* http://www.transcendingdigital.com
*
*
* Copyright (c) 2013 Transcending Digital LLC
* This file is part of a Kinect For Windows green screen experience 
* initially deployed at Maker Faire Detroit 2013. This application
* works in concert with HTML5 tablet applications for e-mailing photos.
*
* Permission is hereby granted, free of charge, to any person
* obtaining a copy of this software and associated documentation
* files (the "Software"), to deal in the Software without
* restriction, including without limitation the rights to use,
* copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the
* Software is furnished to do so, subject to the following
* conditions:
*
* The above copyright notice and this permission notice shall be
* included in all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
* OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
* NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
* HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
* WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
* FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
* OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;

using Emgu.CV;
using System.Runtime.InteropServices;

namespace KinectGreenScreen.com.transcendingdigital.effects
{
    public class EmguUtilities
    {
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static System.Drawing.Bitmap ToBitmap(BitmapSource bitmapsource)
        {
            System.Drawing.Bitmap bitmap;
            using (var outStream = new MemoryStream())
            {
                // from System.Media.BitmapImage to System.Drawing.Bitmap
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new System.Drawing.Bitmap(outStream);
                return bitmap;
            }
        }

        /// <summary>
        /// Convert an IImage to a WPF BitmapSource. The result can be used in the Set Property of Image.Source
        /// </summary>
        /// <param name="image">The Emgu CV Image</param>
        /// <returns>The equivalent BitmapSource</returns>
        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }

        /// <summary>
        /// This replicates ofPolyline.getSmoothed in openFrameworks
        /// openFrameworks allows you to compose an ofPolyline consisting of tons of
        /// connected points.  Here we only have connected series of two points LineSegment2D's
        /// The array of those LineSegment2D's makes up as a total the whole connected shape.
        /// 
        /// So to do this right, we need to gather all the points from all the inputSegments
        /// do the ops on them and output all the collected points.
        /// </summary>
        /// <param name="inputSegments"></param>
        /// <param name="smoothingSize"></param>
        /// <param name="smoothingShape"></param>
        /// <returns></returns>
        public static System.Drawing.Point[] getSmoothedContour(System.Drawing.Point[] inputSegments, int smoothingSize, float smoothingShape)
        {

            int n = inputSegments.Length;
            // make a copy of this polyline..may not be a deep copy oh well
            System.Drawing.Point[] result = (System.Drawing.Point[])inputSegments.Clone();

            // Clamp parameters
            if (smoothingSize < 0)
            {
                smoothingSize = 0;
            }
            else if (smoothingSize > n)
            {
                smoothingSize = n;
            }
            if (smoothingShape < 0)
            {
                smoothingShape = 0;
            }
            else if (smoothingShape > 1)
            {
                smoothingShape = 1;
            }

            // precompute weights and normalization
            List<float> weights = new List<float>();

            // side weights
            for (int i = 0; i < smoothingSize; i++)
            {
                // This is what ofx does here float curWeight = ofMap(i, 0, smoothingSize, 1, smoothingShape);
                float curWeight = 0;
                // Get the ratio of input to smothing size
                float ratio = (float)i / smoothingSize;
                // Get a new computed value between 1 and the smoothing shape
                curWeight = smoothingShape * ratio;
                weights.Add(curWeight);
            }

            for (int i = 0; i < n; i++)
            {

                float sum = 1; // center weight

                for (int j = 1; j < smoothingSize; j++)
                {
                    System.Drawing.Point cur = new System.Drawing.Point(0, 0);
                    int leftPosition = i - j;
                    int rightPosition = i + j;

                    if (leftPosition < 0)
                    {
                        leftPosition += n;
                    }
                    if (leftPosition >= 0)
                    {
                        cur.X += inputSegments[leftPosition].X;
                        cur.Y += inputSegments[leftPosition].Y;

                        sum += weights[j];
                    }
                    if (rightPosition >= n)
                    {
                        rightPosition -= n;
                    }
                    if (rightPosition < n)
                    {
                        cur.X += inputSegments[rightPosition].X;
                        cur.Y += inputSegments[rightPosition].Y;

                        sum += weights[j];
                    }

                    result[i].X += (int)((float)cur.X * weights[j]);
                    result[i].Y += (int)((float)cur.Y * weights[j]);
                }

                // result[i] /= sum;
                result[i].X = (int)((float)result[i].X / sum);
                result[i].Y = (int)((float)result[i].Y / sum);
            }

            return result;
        }

        /// <summary>
        /// This variation also shifts each resulting point some number of pixels to the right or left
        /// There is some sort of error in mapping points in the Kinect SDK 1.7 maybe it isnt an error
        /// but there is an offset to the left on everything even in the SDK samples. 
        /// 
        /// If you look at the IR samples, this shift does not appear to be the IR shadow. Maybe its an overcompenstation?
        /// </summary>
        /// <param name="inputSegments"></param>
        /// <param name="smoothingSize"></param>
        /// <param name="smoothingShape"></param>
        /// <param name="rightPixelShift">pass in 0 for no shift. Negative number to move everything left or positive to move everything right.</param>
        /// <returns></returns>
        public static System.Drawing.Point[] getSmoothedContour(System.Drawing.Point[] inputSegments, int smoothingSize, float smoothingShape, int rightLeftXPixelShift)
        {

            int n = inputSegments.Length;
            // make a copy of this polyline..may not be a deep copy oh well
            System.Drawing.Point[] result = (System.Drawing.Point[])inputSegments.Clone();

            // Clamp parameters
            if (smoothingSize < 0)
            {
                smoothingSize = 0;
            }
            else if (smoothingSize > n)
            {
                smoothingSize = n;
            }
            if (smoothingShape < 0)
            {
                smoothingShape = 0;
            }
            else if (smoothingShape > 1)
            {
                smoothingShape = 1;
            }

            // precompute weights and normalization
            List<float> weights = new List<float>();

            // side weights
            for (int i = 0; i < smoothingSize; i++)
            {
                // This is what ofx does here float curWeight = ofMap(i, 0, smoothingSize, 1, smoothingShape);
                float curWeight = 0;
                // Get the ratio of input to smothing size
                float ratio = (float)i / smoothingSize;
                // Get a new computed value between 1 and the smoothing shape
                curWeight = smoothingShape * ratio;
                weights.Add(curWeight);
            }

            for (int i = 0; i < n; i++)
            {

                float sum = 1; // center weight

                for (int j = 1; j < smoothingSize; j++)
                {
                    System.Drawing.Point cur = new System.Drawing.Point(0, 0);
                    int leftPosition = i - j;
                    int rightPosition = i + j;

                    if (leftPosition < 0)
                    {
                        leftPosition += n;
                    }
                    if (leftPosition >= 0)
                    {
                        cur.X += inputSegments[leftPosition].X;
                        cur.Y += inputSegments[leftPosition].Y;

                        sum += weights[j];
                    }
                    if (rightPosition >= n)
                    {
                        rightPosition -= n;
                    }
                    if (rightPosition < n)
                    {
                        cur.X += inputSegments[rightPosition].X;
                        cur.Y += inputSegments[rightPosition].Y;

                        sum += weights[j];
                    }

                    result[i].X += (int)((float)cur.X * weights[j]);
                    result[i].Y += (int)((float)cur.Y * weights[j]);
                }

                // result[i] /= sum;
                result[i].X = (int)((float)result[i].X / sum) + rightLeftXPixelShift;
                result[i].Y = (int)((float)result[i].Y / sum);
            }

            return result;
        }

    }
}
