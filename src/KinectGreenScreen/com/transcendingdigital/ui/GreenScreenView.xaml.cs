/*
* GreenScreenView.xaml.cs
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Effects;
using Microsoft.Kinect;
using KinectGreenScreen.com.microsoft.ui;
using KinectGreenScreen.com.transcendingdigital.data;
using KinectGreenScreen.com.transcendingdigital.effects;

namespace KinectGreenScreen.com.transcendingdigital.ui
{
    /// <summary>
    /// Interaction logic for GreenScreenView.xaml
    /// </summary>
    public partial class GreenScreenView : UserControl
    {
        private KinectSensor _sensor;
        private KFWIcon _noKinect;
        private Image _kColorImage;
        private Image _bgImage;
        private Image _fgImage;
        private Image _blurMaskSource;
        
        // Values used for the images from kinect
        private GreenScreenImplementation _greenScreenProcessor;
        private WriteableBitmap colorBitmap;
        private WriteableBitmap maskBitmap;
        private VisualBrush blurMaskVisualBrush;
        private BlurEffect blurMaskEffect;

        private bool _gotColor = false;
        private bool _gotDepth = false;
        private bool freezePhoto = false;
        private ThreadedPhotoSubmission _photoSubmitter;

        private double _thisHeight = 480;
        private double _thisWidth = 640;

        private int _postblurAmmount = Properties.Settings.Default.kinectPostBlurInt;
        private double _depthLeftOffset = Properties.Settings.Default.kinectDepthLeftOffset;
        private double _depthTopOffset = Properties.Settings.Default.kinectDepthTopOffset;

        // A custom shader effect used to make the user show up black and white
        private MonochromeEffect _monoFX;

        public GreenScreenView()
        {
            InitializeComponent();
        }
        public void init(ref KinectSensor sensor, double _width, double _height, string _initialBG, string _initialFG)
        {
            _sensor = sensor;
            _thisWidth = _width;
            _thisHeight = _height;

            setupKinectOrNoK(_initialBG, _initialFG);
            // Ensure we only have ONE photo submitter
            _photoSubmitter = new ThreadedPhotoSubmission();
            _photoSubmitter.ImageSubmissionComplete += handlePhotoSubmittedToCMS;

        }
        /// <summary>
        /// In this version the chromaKeyImageData contains already loaded BitmapImages
        /// </summary>
        /// <param name="sensor"></param>
        /// <param name="_width"></param>
        /// <param name="_height"></param>
        /// <param name="_initialBG"></param>
        /// <param name="_initialFG"></param>
        public void init(ref KinectSensor sensor, double _width, double _height, chromaKeyImageData _initialChromaKData)
        {
            _sensor = sensor;
            _thisWidth = _width;
            _thisHeight = _height;

            setupKinectOrNoK(_initialChromaKData);
            // Ensure we only have ONE photo submitter
            _photoSubmitter = new ThreadedPhotoSubmission();
            _photoSubmitter.ImageSubmissionComplete += handlePhotoSubmittedToCMS;

        }
        public void destroyInternals()
        {
            if (_sensor != null)
            {
                _sensor.AllFramesReady -= kinectAllFramesReady;
            }
            if (_greenScreenProcessor != null)
            {
                _greenScreenProcessor.frameReadyForDisplay -= greenScreenFrameReady;
                _greenScreenProcessor.destroyInternals();
                _greenScreenProcessor = null;
            }

            if (_bgImage != null)
            {
                if (kiddieHolder.Children.Contains(_bgImage))
                {
                    kiddieHolder.Children.Remove(_bgImage);
                }

                _bgImage.Source = null;
            }

            if (_fgImage != null)
            {
                if (kiddieHolder.Children.Contains(_fgImage))
                {
                    kiddieHolder.Children.Remove(_fgImage);
                }

                _fgImage.Source = null;
            }

            if (_kColorImage != null)
            {

                if (kiddieHolder.Children.Contains(_kColorImage))
                {
                    kiddieHolder.Children.Remove(_kColorImage);
                }

                _kColorImage.Effect = null;
                _kColorImage.Source = null;
                _kColorImage = null;
                this.colorBitmap = null;

                _monoFX = null;
            }
            if (_noKinect != null)
            {
                if (kiddieHolder.Children.Contains(_noKinect))
                {
                    kiddieHolder.Children.Remove(_noKinect);
                }
                _noKinect = null;
            }
            if (_photoSubmitter != null)
            {
                _photoSubmitter.ImageSubmissionComplete -= handlePhotoSubmittedToCMS;
                _photoSubmitter.destroyInternals();
                _photoSubmitter = null;
            }
           
        }
        public void freezePhotoOrNot(bool _freeze)
        {
            freezePhoto = _freeze;
        }
        public void changeBackground(string _newBGPath, string _newFGPath)
        {
            if (_bgImage != null)
            {
                if (_newBGPath != "")
                {
                    _bgImage.Source = new BitmapImage(new Uri(_newBGPath));
                }
            }
            if (_fgImage != null)
            {
                if (_newFGPath != "")
                {
                    _fgImage.Source = new BitmapImage(new Uri(_newFGPath));
                }
                else
                {
                    _fgImage.Source = null;
                }
            }
        }
        public void changeBackground(chromaKeyImageData _newKeyPhotoData)
        {
            if (_bgImage != null)
            {
                if (_newKeyPhotoData.BackgroundImageBM != null)
                {
                    _bgImage.Source = _newKeyPhotoData.BackgroundImageBM;
                }
            }
            if (_fgImage != null)
            {
                if (_newKeyPhotoData.ForegroundImageBM != null)
                {
                    _fgImage.Source = _newKeyPhotoData.ForegroundImageBM;
                }
                else
                {
                    _fgImage.Source = null;
                }
            }
        }
        public void toggleMonochrome(bool _yesNo)
        {
            if (_yesNo == true)
            {
                if (_monoFX == null)
                {
                    _monoFX = new MonochromeEffect();
                    _monoFX.FilterColor = Color.FromArgb(0, 255, 255, 255);
                    _monoFX.Contrast = Properties.Settings.Default.monochromeEffectContrastMultiplier;
                }
                if (this._kColorImage != null)
                {
                    this._kColorImage.Effect = _monoFX;
                }
            }
            else
            {
                if (this._kColorImage != null)
                {
                    this._kColorImage.Effect = null;
                }
            }
        }

        private void setupKinectOrNoK(string _initalBGPath, string _initialFGPath)
        {
            // Create the background image reguardless
            _bgImage = new Image();
            _bgImage.Name = "backgroundImage";
            _bgImage.Width = _thisWidth;
            _bgImage.Height = _thisHeight;
            _bgImage.Stretch = Stretch.Fill;
            kiddieHolder.Children.Add(_bgImage);

            _bgImage.Source = new BitmapImage(new Uri(_initalBGPath));

            if (_sensor != null)
            {
                // Create the image
                _kColorImage = new Image();
                _kColorImage.Name = "UserPhoto";
                _kColorImage.Width = 640 * Properties.Settings.Default.kinectDepthImageScale;
                _kColorImage.Height = 480 * Properties.Settings.Default.kinectDepthImageScale;
                kiddieHolder.Children.Add(_kColorImage);
                // This does fill or exceede the visual area but the kinects depth stream is not as large as the color stream..so we need to offset the left and
                // top by a uniform ammount as we scale too.
                Canvas.SetTop(_kColorImage, _depthTopOffset);
                Canvas.SetLeft(_kColorImage, _depthLeftOffset);

                this.colorBitmap = new WriteableBitmap(this._sensor.ColorStream.FrameWidth, this._sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.maskBitmap = new WriteableBitmap( _sensor.DepthStream.FrameWidth, _sensor.DepthStream.FrameHeight,96,96,PixelFormats.Bgra32,null);
                this._kColorImage.Source = this.colorBitmap;

                // colorBitmap serves as our main source for the player image - BUT we have an opacity mask on that guy too which is generated
                // from an image brush made by our "green screen" data. By having that secondary image we can use fast WPF blurs on it and do other
                // manipulations
                _blurMaskSource = new Image();
                blurMaskEffect = new BlurEffect();
                blurMaskEffect.Radius = _postblurAmmount;

                blurMaskVisualBrush = new VisualBrush();
                blurMaskVisualBrush.Visual = _blurMaskSource;
                _blurMaskSource.Effect = blurMaskEffect;

                this._kColorImage.OpacityMask = this.blurMaskVisualBrush;

                // Init the green screen work horse
                _greenScreenProcessor = new GreenScreenImplementation(this._sensor);
                _blurMaskSource.Source = _greenScreenProcessor.greenScreenMask;
                _greenScreenProcessor.frameReadyForDisplay += greenScreenFrameReady;

                _sensor.AllFramesReady += kinectAllFramesReady;
            }
            else
            {
                // Create the kinect needed
                _noKinect = new KFWIcon();
                _noKinect.Name = "kwfLogo";
                _noKinect.Width = 328;
                _noKinect.Height = 180;
                _noKinect.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                _noKinect.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                kiddieHolder.Children.Add(_noKinect);
            }

            // Foreground too, but it may not have anything
            _fgImage = new Image();
            _fgImage.Name = "foregroundImage";
            _fgImage.Width = _thisWidth;
            _fgImage.Height = _thisHeight;
            _fgImage.Stretch = Stretch.Fill;
            kiddieHolder.Children.Add(_fgImage);

            if (_initialFGPath != "")
            {
                _fgImage.Source = new BitmapImage(new Uri(_initialFGPath));
            }
        }
        private void setupKinectOrNoK(chromaKeyImageData _initalData)
        {
            // Create the background image reguardless
            _bgImage = new Image();
            _bgImage.Name = "backgroundImage";
            _bgImage.Width = _thisWidth;
            _bgImage.Height = _thisHeight;
            _bgImage.Stretch = Stretch.Fill;
            kiddieHolder.Children.Add(_bgImage);

            _bgImage.Source = _initalData.BackgroundImageBM;

            if (_sensor != null)
            {
                // Create the image
                _kColorImage = new Image();
                _kColorImage.Name = "UserPhoto";
                _kColorImage.Width = 640 * Properties.Settings.Default.kinectDepthImageScale;
                _kColorImage.Height = 480 * Properties.Settings.Default.kinectDepthImageScale;
                kiddieHolder.Children.Add(_kColorImage);
                // This does fill or exceede the visual area but the kinects depth stream is not as large as the color stream..so we need to offset the left and
                // top by a uniform ammount as we scale too.
                Canvas.SetTop(_kColorImage, _depthTopOffset);
                Canvas.SetLeft(_kColorImage, _depthLeftOffset);

                this.colorBitmap = new WriteableBitmap(this._sensor.ColorStream.FrameWidth, this._sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                this.maskBitmap = new WriteableBitmap(_sensor.DepthStream.FrameWidth, _sensor.DepthStream.FrameHeight, 96, 96, PixelFormats.Bgra32, null);
                this._kColorImage.Source = this.colorBitmap;

                // colorBitmap serves as our main source for the player image - BUT we have an opacity mask on that guy too which is generated
                // from an image brush made by our "green screen" data. By having that secondary image we can use fast WPF blurs on it and do other
                // manipulations
                _blurMaskSource = new Image();
                blurMaskEffect = new BlurEffect();
                blurMaskEffect.Radius = _postblurAmmount;

                blurMaskVisualBrush = new VisualBrush();
                blurMaskVisualBrush.Visual = _blurMaskSource;
                //_blurMaskSource.Source = maskBitmap;
                _blurMaskSource.Effect = blurMaskEffect;

                this._kColorImage.OpacityMask = this.blurMaskVisualBrush;

                // Init the green screen work horse
                _greenScreenProcessor = new GreenScreenImplementation(this._sensor);
                _blurMaskSource.Source = _greenScreenProcessor.greenScreenMask;
                _greenScreenProcessor.frameReadyForDisplay += greenScreenFrameReady;

                _sensor.AllFramesReady += kinectAllFramesReady;
            }
            else
            {
                // Create the kinect needed
                _noKinect = new KFWIcon();
                _noKinect.Name = "kwfLogo";
                _noKinect.Width = 328;
                _noKinect.Height = 180;
                _noKinect.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                _noKinect.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                kiddieHolder.Children.Add(_noKinect);
            }

            // Foreground too, but it may not have anything
            _fgImage = new Image();
            _fgImage.Name = "foregroundImage";
            _fgImage.Width = _thisWidth;
            _fgImage.Height = _thisHeight;
            _fgImage.Stretch = Stretch.Fill;
            kiddieHolder.Children.Add(_fgImage);

            if (_initalData.ForegroundImageBM != null)
            {
                _fgImage.Source = _initalData.ForegroundImageBM;
            }
        }
        private void kinectAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (freezePhoto == false)
            {
                using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
                {
                    if (null != colorFrame)
                    {
                        if (_greenScreenProcessor != null)
                        {
                            _greenScreenProcessor.incomingColorFrame(colorFrame);
                            _gotColor = true;
                        }
                    }
                }
                using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
                {
                    if (null != depthFrame)
                    {
                        if (_greenScreenProcessor != null)
                        {
                            _greenScreenProcessor.incomingDepthFrame(depthFrame);
                            _gotDepth = true;
                        }
                    }
                }

                if (_gotDepth == true && _gotColor == true)
                {
                    // We have to do the color coordinate mapping here as it has to happen in the
                    // same thread as the kinect
                    _sensor.CoordinateMapper.MapDepthFrameToColorFrame(
                    DepthImageFormat.Resolution640x480Fps30,
                    _greenScreenProcessor._depthPixels,
                    ColorImageFormat.RgbResolution640x480Fps30,
                    _greenScreenProcessor._colorCoordinates);

                    _greenScreenProcessor.doProcessing();
                    _gotDepth = false;
                    _gotColor = false;
                }
            }
        }
        private void greenScreenFrameReady(object sender, EventArgs e)
        {
            // Write the pixel data into our bitmap
            
            this.colorBitmap.WritePixels(
                new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                _greenScreenProcessor._colorPixels,
                this.colorBitmap.PixelWidth * sizeof(int),
                0);
           
            

           // if (_blurMaskSource.Opacity == 0)
            //{
             //   if (_greenScreenProcessor.greenScreenMask != null)
              //  {
                   // _blurMaskSource.Source = _greenScreenProcessor.greenScreenMask;
                    //_blurMaskSource.Opacity = 1;
               // }
            //}

            // Write the mask data into the opacticy mask source of the colorBitmap
            /*
            this.maskBitmap.WritePixels(
            new Int32Rect(0, 0, this.maskBitmap.PixelWidth, this.maskBitmap.PixelHeight),
            _greenScreenProcessor._greenScreenPixelData,
            this.maskBitmap.PixelWidth * ((this.maskBitmap.Format.BitsPerPixel + 7) / 8),
            0);
             * */
        }
        public void saveImageFrame()
        {
            // For memory testing when not using kinect
            WriteableBitmap currentFrameCopy;
 
            currentFrameCopy = new WriteableBitmap((int)kiddieHolder.ActualWidth, (int)kiddieHolder.ActualHeight, 96, 96, PixelFormats.Pbgra32, null);
            currentFrameCopy.Lock();

            RenderTargetBitmap rendrBMP = new RenderTargetBitmap((int)kiddieHolder.ActualWidth, (int)kiddieHolder.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            rendrBMP.Render(kiddieHolder);
            rendrBMP.CopyPixels(new Int32Rect(0, 0, (int)kiddieHolder.ActualWidth, (int)kiddieHolder.ActualHeight), currentFrameCopy.BackBuffer, currentFrameCopy.BackBufferStride * currentFrameCopy.PixelHeight, currentFrameCopy.BackBufferStride);
            currentFrameCopy.Unlock();

            currentFrameCopy.Freeze();
            _photoSubmitter.encodeAndSubmitUserPhotoToCMS(currentFrameCopy);

            // Wierd WPF bug maybe messes up the visual brush. We can fix it just by doing this
            if (blurMaskVisualBrush != null)
            {
                blurMaskVisualBrush.Visual = null;
                blurMaskVisualBrush.Visual = _blurMaskSource;
            }
        }
        private void handlePhotoSubmittedToCMS(object sender, EventArgs e)
        {
            // Who cares
        }
    }
}
