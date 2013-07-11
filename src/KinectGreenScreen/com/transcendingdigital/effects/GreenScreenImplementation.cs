/*
* GreenScreenImplementation.cs
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
using Microsoft.Kinect;
using System.Windows.Media.Imaging;
using System.Reflection;

using Emgu.CV;

using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows;

using KinectGreenScreen.com.transcendingdigital.data;

namespace KinectGreenScreen.com.transcendingdigital.effects
{
    /// <summary>
    /// The idea behind this class is that we send in our
    /// depth and color data as fast as possible,
    /// this class does any operations it needs to on it
    /// then triggers events when its ready.
    /// 
    /// This class may do things like:
    /// 1. Create some sort of processing queue
    /// 2. Drop frames if necessary
    /// 3. Output frames slower than they are input
    /// 4. Create other threads to help with processing
    /// 
    /// Things may get really heavy in here or other subordinate
    /// classes but the key idea of manipulating that raw pixel
    /// data on color and depth is what this class does.
    /// 
    /// 
    /// ADDITIONAL FUNCTIONALITY
    /// This section also uses the emgu CV project to access openCV. 
    /// (Thats where the fifteen or so .dlls came from in the project)
    /// We feed depth frame image data into a blob detector and average
    /// out all the "contours" or points making up the user mask.
    /// 
    /// Input to openCV must not contain alpha, the mask must be white
    /// and all other areas in the image black.
    /// </summary>
    public class GreenScreenImplementation
    {
        // The event
        public event EventHandler frameReadyForDisplay;

        // Working Image and Depth Data
        public DepthImagePixel[] _depthPixels;
        public byte[] _colorPixels;
        public ColorImagePoint[] _colorCoordinates;
        public int[] _greenScreenPixelData;
        // Acessed in both threads
        private KinectSensor _sensorRef = null;
        private int _depthStreamFrameHeight = 480;
        private int _depthStreamFrameWidth = 640;
        private int _gsStride = 0;
        private int _gsBufferSize = 0;
        private IntPtr _pBackBuffer;

        public WriteableBitmap greenScreenMask = null;


        private Emgu.CV.Structure.Bgra black = new Emgu.CV.Structure.Bgra(0, 0, 0, 255);
        private Int32Rect _copyArea;

        private BackgroundWorker _BGWorker;
        public bool RUNNING = false;

        // Configurable items
        private int opaquePixelValue = -1;
        private float _depthThreshold = (GlobalConfiguration.kinectGreenScreenThresholdMeters * 1000);

        public GreenScreenImplementation(KinectSensor _sRef)
        {
            // Init the raw arrays for holding the pixel data
            _sensorRef = _sRef;
            _depthStreamFrameHeight = _sensorRef.DepthStream.FrameHeight;
            _depthStreamFrameWidth = _sensorRef.DepthStream.FrameWidth;

            // We need to do a lot of setup here as some of this is accessed in
            // another thread
            _colorPixels = new byte[ _sRef.ColorStream.FramePixelDataLength ];
            _depthPixels = new DepthImagePixel[ _sRef.DepthStream.FramePixelDataLength ];
            _colorCoordinates = new ColorImagePoint[_sRef.DepthStream.FramePixelDataLength ];
            _greenScreenPixelData = new int[_sRef.DepthStream.FramePixelDataLength];
            greenScreenMask = new WriteableBitmap(_sRef.DepthStream.FrameWidth, _sRef.DepthStream.FrameHeight, 96, 96, PixelFormats.Bgra32, null);
            _gsStride = greenScreenMask.BackBufferStride;
            _gsBufferSize = greenScreenMask.BackBufferStride * greenScreenMask.PixelHeight;

            _copyArea = new Int32Rect(0, 0, _sRef.DepthStream.FrameWidth, _sRef.DepthStream.FrameHeight);

            // Depth threshold is in meters and needs to be in mm here 
            _depthThreshold = ( GlobalConfiguration.kinectGreenScreenThresholdMeters * 1000 );
        }
        public void destroyInternals()
        {
            destroyBGWorker();
        }
        public void incomingDepthFrame(DepthImageFrame _df)
        {
                _df.CopyDepthImagePixelDataTo(_depthPixels);
        }
        public void incomingColorFrame(ColorImageFrame _cf)
        {
                _cf.CopyPixelDataTo(_colorPixels);
        }
        /// <summary>
        /// Originally this just ran doImageProcessing here and it operated directly on a bitmapsource
        /// so (greenScreenMask) was originally operated on in the UI thread. On a core I5 this averaged
        /// like 50% cpu. I know there is overhead on creating and destroying the BG workers here and the
        /// right way is to create another thread that runs forever and just does this.
        /// 
        /// There may not be any preformance benefit to having this running in the background worker.
        /// </summary>
        public void doProcessing()
        {
            
            if (RUNNING == false)
            {
                RUNNING = true;
                // Lock the green screen image so we can write to its pointer from a different
                // thread
                greenScreenMask.Lock();
                // Sets current pointer to the back buffer so we can access it in another thread
                _pBackBuffer = greenScreenMask.BackBuffer;
                createBGWorker();
                _BGWorker.RunWorkerAsync();
            }
            
        }
        private void doImageProcessing()
        {
            // Translate our most recent color coordinates - Done before the bg worker as
            // we cant acess the sensor inside another thread

            // Clear the green screen
            Array.Clear(_greenScreenPixelData, 0, _greenScreenPixelData.Length);
            // Emgu CV Image
            using (Image<Emgu.CV.Structure.Gray, byte> emguOriginal = new Image<Emgu.CV.Structure.Gray, byte>(640, 480))
            {
                byte[, ,] emguData = emguOriginal.Data;

                // We have to iterate the whole depth image
                for (int y = 0; y < _depthStreamFrameHeight; ++y)
                {
                    for (int x = 0; x < _depthStreamFrameWidth; ++x)
                    {
                        // calculate index into depth array
                        int depthIndex = x + (y * _sensorRef.DepthStream.FrameWidth);

                        DepthImagePixel depthPixel = _depthPixels[depthIndex];

                        // retrieve the depth to color mapping for the current depth pixel
                        ColorImagePoint colorImagePoint = _colorCoordinates[depthIndex];

                        // scale color coordinates to depth resolution
                        int colorInDepthX = colorImagePoint.X;
                        int colorInDepthY = colorImagePoint.Y;

                        // make sure the depth pixel maps to a valid point in color space
                        // check y > 0 and y < depthHeight to make sure we don't write outside of the array
                        // check x > 0 instead of >= 0 since to fill gaps we set opaque current pixel plus the one to the left
                        // because of how the sensor works it is more correct to do it this way than to set to the right
                        if (colorInDepthX > 0 && colorInDepthX < _depthStreamFrameWidth && colorInDepthY >= 0 && colorInDepthY < _depthStreamFrameHeight)
                        {
                            // calculate index into the green screen pixel array
                            int greenScreenIndex = colorInDepthX + (colorInDepthY * _depthStreamFrameWidth);

                            // OK emgu needs a black and white only image.
                            if (depthPixel.Depth < _depthThreshold && depthPixel.Depth != 0)
                            {
                                // set opaque
                                _greenScreenPixelData[greenScreenIndex] = opaquePixelValue;

                                // compensate for depth/color not corresponding exactly by setting the pixel 
                                // to the left to opaque as well
                                _greenScreenPixelData[greenScreenIndex - 1] = opaquePixelValue;

                                // Emgu needs an all black image with pure white where the depth data is
                                emguData[colorInDepthY, colorInDepthX, 0] = 255;

                                // set the pixel before this white too. We dont need this in blob detection as the blobs will fill in
                                // it just ends up adding extra on all the left edges
                                /*
                                if (colorInDepthX - 1 > -1)
                                {
                                    emguData[colorInDepthY, colorInDepthX - 1, 0] = 255;
                                }
                                */
                            }
                        }
                    }
                }

                    // emguCV work
                    Emgu.CV.Cvb.CvBlobs resultingBlobs = new Emgu.CV.Cvb.CvBlobs();
                    Emgu.CV.Cvb.CvBlobDetector bDetect = new Emgu.CV.Cvb.CvBlobDetector();
                    uint numLabeledPixels = bDetect.Detect(emguOriginal, resultingBlobs);

                    Image<Emgu.CV.Structure.Bgra, double> blobImg = new Image<Emgu.CV.Structure.Bgra, double>(emguOriginal.Width, emguOriginal.Height, new Emgu.CV.Structure.Bgra(0, 0, 0, 0));
                    foreach (Emgu.CV.Cvb.CvBlob targetBlob in resultingBlobs.Values)
                    {
                        using (MemStorage mem_BlobContours = new MemStorage())
                        {
                            Contour<System.Drawing.Point> allContourPointsInBlob = targetBlob.GetContour(mem_BlobContours);

                            // If thre are more than five points smooth them
                            if (allContourPointsInBlob.Total > 5)
                            {

                                System.Drawing.Point[] originalPoints = allContourPointsInBlob.ToArray();
                                System.Drawing.Point[] smoothedPoints = EmguUtilities.getSmoothedContour(originalPoints, 6, (float)0.5, Properties.Settings.Default.kinectGreenScreenMaskXPixelShift);

                                //------------- FILL -----------------------------------
                                // Sweet shove em back into a contour collection

                                MemStorage finalFillStorage = new MemStorage();
                                Contour<System.Drawing.Point> finalFillContours = new Contour<System.Drawing.Point>(finalFillStorage);
                                finalFillContours.PushMulti(smoothedPoints, Emgu.CV.CvEnum.BACK_OR_FRONT.BACK);
                                blobImg.Draw(finalFillContours, black, -1);

                                // ------------ END FILL ------------------------------
                            }
                        }
                    }

                    // Converts an emgu cv image to a bitmapsource
                    BitmapSource finalRef = EmguUtilities.ToBitmapSource(blobImg);
                    finalRef.Freeze();
                    // Ensure the greenScreenMask is locked before doing this
                    // copy pixels - I get the feeling this isnt supposed to be used on bigger areas but it seems like the fastest way to do it?
                    finalRef.CopyPixels(_copyArea, _pBackBuffer, _gsBufferSize, _gsStride);
                    // Just in case dispose of the image
                    blobImg.Dispose();
                    //emguEroded.Dispose();
            }

            // make a copy to be more thread-safe - we really dont need this anymore but  oh well
          /*
            EventHandler handler = frameReadyForDisplay;
            if (handler != null)
            {
                // invoke the subscribed event-handler(s)
                handler(this, EventArgs.Empty);
            }
            */
        }

        private void doPlainImageProcessing()
        {
            // Translate our most recent color coordinates - Done before the bg worker as
            // we cant acess the sensor inside another thread

            // Clear the green screen
            Array.Clear(_greenScreenPixelData, 0, _greenScreenPixelData.Length);

                // We have to iterate the whole depth image
                for (int y = 0; y < _depthStreamFrameHeight; ++y)
                {
                    for (int x = 0; x < _depthStreamFrameWidth; ++x)
                    {
                        // calculate index into depth array
                        int depthIndex = x + (y * _sensorRef.DepthStream.FrameWidth);

                        DepthImagePixel depthPixel = _depthPixels[depthIndex];

                        // retrieve the depth to color mapping for the current depth pixel
                        ColorImagePoint colorImagePoint = _colorCoordinates[depthIndex];

                        // scale color coordinates to depth resolution
                        int colorInDepthX = colorImagePoint.X;
                        int colorInDepthY = colorImagePoint.Y;

                        // make sure the depth pixel maps to a valid point in color space
                        // check y > 0 and y < depthHeight to make sure we don't write outside of the array
                        // check x > 0 instead of >= 0 since to fill gaps we set opaque current pixel plus the one to the left
                        // because of how the sensor works it is more correct to do it this way than to set to the right
                        if (colorInDepthX > 0 && colorInDepthX < _depthStreamFrameWidth && colorInDepthY >= 0 && colorInDepthY < _depthStreamFrameHeight)
                        {
                            // calculate index into the green screen pixel array
                            int greenScreenIndex = colorInDepthX + (colorInDepthY * _depthStreamFrameWidth);

                            // OK emgu needs a black and white only image.
                            if (depthPixel.Depth < _depthThreshold && depthPixel.Depth != 0)
                            {
                                // set opaque
                                _greenScreenPixelData[greenScreenIndex] = opaquePixelValue;

                                // compensate for depth/color not corresponding exactly by setting the pixel 
                                // to the left to opaque as well
                                _greenScreenPixelData[greenScreenIndex - 1] = opaquePixelValue;
                            }
                        }
                    }
                }
               
                BitmapSource finalRef = BitmapSource.Create(_copyArea.Width, _copyArea.Height, 96, 96, PixelFormats.Bgra32, null, _greenScreenPixelData, _gsStride);
                finalRef.Freeze();
                finalRef.CopyPixels(_copyArea, _pBackBuffer, _gsBufferSize, _gsStride);
        }
        /**
         * This will get you straight opaque and non opaque pixels for a raw green screen
         * without emgu cv inclusion
        public void doProcessing()
        {
            // Translate our most recent color coordinates
            _sensorRef.CoordinateMapper.MapDepthFrameToColorFrame(
                DepthImageFormat.Resolution640x480Fps30,
                _depthPixels,
                ColorImageFormat.RgbResolution640x480Fps30,
                _colorCoordinates);
            // Clear the green screen
            Array.Clear(_greenScreenPixelData, 0, _greenScreenPixelData.Length);

                // We have to iterate the whole depth image
                for (int y = 0; y < _sensorRef.DepthStream.FrameHeight; ++y)
                {
                    for (int x = 0; x < _sensorRef.DepthStream.FrameWidth; ++x)
                    {
                        // calculate index into depth array
                        int depthIndex = x + (y * _sensorRef.DepthStream.FrameWidth);

                        DepthImagePixel depthPixel = _depthPixels[depthIndex];

                        // retrieve the depth to color mapping for the current depth pixel
                        ColorImagePoint colorImagePoint = _colorCoordinates[depthIndex];

                            // scale color coordinates to depth resolution
                            int colorInDepthX = colorImagePoint.X;
                            int colorInDepthY = colorImagePoint.Y;

                            // make sure the depth pixel maps to a valid point in color space
                            // check y > 0 and y < depthHeight to make sure we don't write outside of the array
                            // check x > 0 instead of >= 0 since to fill gaps we set opaque current pixel plus the one to the left
                            // because of how the sensor works it is more correct to do it this way than to set to the right
                            if (colorInDepthX > 0 && colorInDepthX < _sensorRef.DepthStream.FrameWidth && colorInDepthY >= 0 && colorInDepthY < _sensorRef.DepthStream.FrameHeight)
                            {
                                // calculate index into the green screen pixel array
                                int greenScreenIndex = colorInDepthX + (colorInDepthY * _sensorRef.DepthStream.FrameWidth);

                                if (depthPixel.Depth < _depthThreshold && depthPixel.Depth != 0)
                                {
                                    // set opaque
                                    _greenScreenPixelData[greenScreenIndex] = opaquePixelValue;

                                    // compensate for depth/color not corresponding exactly by setting the pixel 
                                    // to the left to opaque as well
                                    _greenScreenPixelData[greenScreenIndex - 1] = opaquePixelValue;
                                }
                            }
                    }
                }

                // make a copy to be more thread-safe
                EventHandler handler = frameReadyForDisplay;
                if (handler != null)
                {
                    // invoke the subscribed event-handler(s)
                    handler(this, EventArgs.Empty);
                }
        }
         * */

        #region BackgroundWorker Items
        private void createBGWorker()
        {
            if (_BGWorker == null)
            {
                // Will use DispatcherSynchronizationContext
                _BGWorker = new BackgroundWorker();
                _BGWorker.WorkerReportsProgress = false;
                _BGWorker.WorkerSupportsCancellation = true;

                // Wire up event handlers
                _BGWorker.DoWork += backgroundWorker_DoWork;
                _BGWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
            }
        }
        private void destroyBGWorker()
        {
            if (_BGWorker != null)
            {
                _BGWorker.DoWork -= backgroundWorker_DoWork;
                _BGWorker.RunWorkerCompleted -= backgroundWorker_RunWorkerCompleted;

                if (_BGWorker.IsBusy == true)
                {
                    // Canel any in action work
                    _BGWorker.CancelAsync();
                    // This will cause _backgroundWorker_RunWorkerCompleted to be completed
                }

                _BGWorker = null;
            }
        }

        void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            destroyBGWorker();
            // Unlock the writablebitmap as we are back in the main thread again here
            greenScreenMask.AddDirtyRect(_copyArea);
            greenScreenMask.Unlock();

            RUNNING = false;

            EventHandler handler = frameReadyForDisplay;
            if (handler != null)
            {
                // invoke the subscribed event-handler(s)
                handler(this, EventArgs.Empty);
            }

        }
        void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (GlobalConfiguration.useAdvancedGreenScreenRendering == true)
            {
                doImageProcessing();
            }
            else
            {
                doPlainImageProcessing();
            }
        }
        #endregion
    }
}
