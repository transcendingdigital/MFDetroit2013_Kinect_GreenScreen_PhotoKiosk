/*
* SectionPhoto.xaml.cs
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
using Microsoft.Kinect;
using System.Globalization;
using System.IO;
using System.Timers;
using System.Windows.Media.Animation;
using KinectGreenScreen.com.transcendingdigital.events;
using KinectGreenScreen.com.transcendingdigital.data;
using KinectGreenScreen.com.microsoft.ui;

namespace KinectGreenScreen.com.transcendingdigital.ui
{
    /// <summary>
    /// Interaction logic for SectionPhoto.xaml
    /// </summary>
    public partial class SectionPhoto : UserControl
    {
        // The event delegate
        public delegate void captionRequest(object sender, InitiateCaptionsEventArgs e);
        // The event
        public event captionRequest initiateNewCaption;

        public bool destroyed = false;

        private KinectSensor _sensor;
        private KFWIcon _noKinect;
        private Image _kColorImage;

        // Used for sound effects
        private MediaPlayer soundFXMan;
        private List<string> sfxQueue;

        // Values used for the images from kinect
        private WriteableBitmap colorBitmap;
        private byte[] colorPixels;
        private bool freezePhoto = false;
        private ThreadedPhotoSubmission _photoSubmitter;

        private MainWindow _mainWin;

        public SectionPhoto()
        {
            InitializeComponent();
        }

        public void init(ref KinectSensor sensor, ref MainWindow _main)
        {
            _sensor = sensor;
            _mainWin = _main;

            setupKinectOrNoK();
            // Ensure we only have ONE photo submitter
            _photoSubmitter = new ThreadedPhotoSubmission();
            _photoSubmitter.ImageSubmissionComplete += handlePhotoSubmittedToCMS;
        }

        public void initiatePhotoTake()
        {
            // Just in case it's a re-take
            freezePhoto = false;
            if (YNSpacer.Height < 150)
            {
                DoubleAnimation daHeight = new DoubleAnimation(YNSpacer.Height, 150, new Duration(TimeSpan.FromSeconds(0.3)));
                DoubleAnimation daFHeight = new DoubleAnimation(YNSpacer2.Height, 150, new Duration(TimeSpan.FromSeconds(0.3)));
                SineEase ease = new SineEase();
                ease.EasingMode = EasingMode.EaseIn;
                daHeight.EasingFunction = ease;
                daFHeight.EasingFunction = ease;
                daFHeight.BeginTime = TimeSpan.FromSeconds(0.2);
                YNSpacer2.BeginAnimation(Canvas.HeightProperty, daFHeight);
                YNSpacer.BeginAnimation(Canvas.HeightProperty, daHeight);
            }

            if (sfxQueue != null)
            {
                sfxQueue = null;
            }

            // Disable cursors - way too distracting
            if (_mainWin != null)
            {
                _mainWin.enableDisableAllCursors(false);
            }

            // Hide take photo
            DoubleAnimation daOpacityHide = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.5)));
            txtTakePhoto.BeginAnimation(TextBlock.OpacityProperty, daOpacityHide);
            txtStartCountdown.BeginAnimation(TextBlock.OpacityProperty, daOpacityHide);
            takePhoto.BeginAnimation(Button.OpacityProperty, daOpacityHide);
            takePhoto.IsHitTestVisible = false;
            // Hide the button

            sfxQueue = new List<string>();
            sfxQueue.Add("TDGenBeep.wav");
            sfxQueue.Add("TDGenBeep.wav");
            sfxQueue.Add("TDGenBeep.wav");
            sfxQueue.Add("CameraFlash.mp3");
            playSoundEffect(sfxQueue[0]);
            countDown.Visibility = System.Windows.Visibility.Visible;
        }

        public void confirmDenyPhoto(bool _yesNo)
        {

            // Hide the yes and no buttons
            DoubleAnimation daHeight = new DoubleAnimation(0, 150, new Duration(TimeSpan.FromSeconds(0.3)));
            DoubleAnimation daFHeight = new DoubleAnimation(0, 150, new Duration(TimeSpan.FromSeconds(0.3)));
            SineEase ease = new SineEase();
            ease.EasingMode = EasingMode.EaseIn;
            daHeight.EasingFunction = ease;
            daFHeight.EasingFunction = ease;
            daFHeight.BeginTime = TimeSpan.FromSeconds(0.2);
            YNSpacer2.BeginAnimation(Canvas.HeightProperty, daFHeight);
            YNSpacer.BeginAnimation(Canvas.HeightProperty, daHeight);
            //btnYes.BeginAnimation(Button.MarginProperty

            // Show take photo
            DoubleAnimation daOpacity = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.5)));
            txtTakePhoto.Text = "TAKE PHOTO";
            txtTakePhoto.BeginAnimation(TextBlock.OpacityProperty, daOpacity);
            txtStartCountdown.BeginAnimation(TextBlock.OpacityProperty, daOpacity);

            if (_yesNo == true)
            {
                // Captions
                requestNewCaption(ApplicationStates.STATE_PHOTO_SUBMITTED, false);
                saveImageFrame();
            }
            else
            {
                // Captions
                requestNewCaption(ApplicationStates.STATE_PHOTO_DESTROYED, false);
                freezePhoto = false;
            }
        }

        public void destroyInternals()
        {
            if (_sensor != null)
            {
                _sensor.ColorFrameReady -= kinectColorFrameReady;
            }

            if (_kColorImage != null)
            {
                if (kiddieHolder.Children.Contains(_kColorImage))
                {
                    kiddieHolder.Children.Remove(_kColorImage);
                }

                _kColorImage.Source = null;
                _kColorImage = null;
                this.colorPixels = null;
                this.colorBitmap = null;
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

            destroySFXPlayer();

            // Re-enable the cursors just in case
            if (_mainWin != null)
            {
                _mainWin.enableDisableAllCursors(true);
            }

            destroyed = true;
        }

        private void playSoundEffect(string _fileName)
        {
            destroySFXPlayer();

            soundFXMan = new MediaPlayer();
            soundFXMan.MediaEnded += handleSFXDone;
            soundFXMan.MediaFailed += handleSFXError;
            soundFXMan.Open(new Uri("pack://siteOfOrigin:,,,/localFiles/audio/" + _fileName));
            soundFXMan.Play();
        }

        private void destroySFXPlayer()
        {
            if (soundFXMan != null)
            {

                soundFXMan.Stop();
                soundFXMan.MediaEnded -= handleSFXDone;
                soundFXMan.MediaFailed -= handleSFXError;
                soundFXMan.Close();
                soundFXMan = null;
            }
        }

        private void handleSFXDone(object sender, EventArgs e)
        {
            nextSFXStep();
        }
        private void handleSFXError(object sender, EventArgs e)
        {
            nextSFXStep();
        }
        private void nextSFXStep()
        {
            if (sfxQueue != null)
            {
                sfxQueue.RemoveAt(0);
                if (sfxQueue.Count > 0)
                {
                    playSoundEffect(sfxQueue[0]);
                    if (countDown.Visibility == System.Windows.Visibility.Visible)
                    {
                        countDown.Text = Convert.ToString(sfxQueue.Count - 1);
                    }

                    if (sfxQueue.Count == 1)
                    {
                        // Hide the count
                        countDown.Visibility = System.Windows.Visibility.Hidden;
                        countDown.Text = "3";

                        // Take the photo
                        freezePhoto = true;

                        // Flash the white
                        //DoubleAnimation daFlashW = AnimationHelper.CreateDoubleAnimation(0, this.Width, null, new Duration(TimeSpan.FromSeconds(0.2)));
                        //DoubleAnimation daFlashH = AnimationHelper.CreateDoubleAnimation(0, this.Height, null, new Duration(TimeSpan.FromSeconds(0.2)));
                        DoubleAnimation daFlashOpacity = new DoubleAnimation(0, .8, new Duration(TimeSpan.FromSeconds(0.1)));
                        daFlashOpacity.AutoReverse = true;

                        //camFlash.BeginAnimation(Ellipse.WidthProperty, daFlashW);
                        //camFlash.BeginAnimation(Ellipse.HeightProperty, daFlashH);
                        camFlash.BeginAnimation(Ellipse.OpacityProperty, daFlashOpacity);
                    }
                }
                else
                {
                    sfxQueue = null;
                    // Done
                    destroySFXPlayer();

                    // Show retake photo
                    txtTakePhoto.Text = "RE-TAKE PHOTO";
                    DoubleAnimation daOpacity = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.5)));
                    daOpacity.BeginTime = TimeSpan.FromSeconds(6);
                    txtTakePhoto.BeginAnimation(TextBlock.OpacityProperty, daOpacity);
                    txtStartCountdown.BeginAnimation(TextBlock.OpacityProperty, daOpacity);
                    takePhoto.BeginAnimation(Button.OpacityProperty, daOpacity);
                    takePhoto.IsHitTestVisible = true;

                    // show yes or no button
                    DoubleAnimation daHeight = new DoubleAnimation(150, 0, new Duration(TimeSpan.FromSeconds(0.3)));
                    DoubleAnimation daHeightF = new DoubleAnimation(150, 0, new Duration(TimeSpan.FromSeconds(0.3)));
                    SineEase ease = new SineEase();
                    ease.EasingMode = EasingMode.EaseOut;
                    daHeight.EasingFunction = ease;
                    daHeightF.EasingFunction = ease;
                    daHeight.BeginTime = TimeSpan.FromSeconds(6);
                    daHeightF.BeginTime = TimeSpan.FromSeconds(6.2);
                    // Add a tiny delay
                    YNSpacer.BeginAnimation(Canvas.HeightProperty, daHeight);
                    YNSpacer2.BeginAnimation(Canvas.HeightProperty, daHeightF);

                    // Captions
                    requestNewCaption(ApplicationStates.STATE_LOOKINGGOOD, false);
                    requestNewCaption(ApplicationStates.STATE_SUBMISSION_AGREE, true);
                    requestNewCaption(ApplicationStates.STATE_YES_NOPHOTO, true);

                    // Re-enable the cursors
                    if (_mainWin != null)
                    {
                        _mainWin.enableDisableAllCursors(true);
                    }
                }
            }
        }

        private void setupKinectOrNoK()
        {
            if (_sensor != null)
            {
                // Create the image
                _kColorImage = new Image();
                _kColorImage.Name = "UserPhoto";
                _kColorImage.Width = contentBorder.Width;
                _kColorImage.Height = contentBorder.Height;
                kiddieHolder.Children.Add(_kColorImage);

                this.colorPixels = new byte[this._sensor.ColorStream.FramePixelDataLength];
                this.colorBitmap = new WriteableBitmap(this._sensor.ColorStream.FrameWidth, this._sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                this._kColorImage.Source = this.colorBitmap;

                _sensor.ColorFrameReady += kinectColorFrameReady;
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
        }

        private void kinectColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            if (_kColorImage != null)
            {
                if (freezePhoto == false)
                {
                    using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
                    {
                        if (colorFrame != null)
                        {
                            // Copy the pixel data from the image to a temporary array
                            colorFrame.CopyPixelDataTo(this.colorPixels);

                            // Write the pixel data into our bitmap
                            this.colorBitmap.WritePixels(
                                new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                                this.colorPixels,
                                this.colorBitmap.PixelWidth * sizeof(int),
                                0);
                        }
                    }
                }
            }
        }

        private void saveImageFrame()
        {
            // SHOW A MESSAGE TO THE USER

            // For memory testing when not using kinect
            WriteableBitmap currentFrameCopy;
            if (this.colorBitmap == null)
            {
                currentFrameCopy = new WriteableBitmap((int)kiddieHolder.ActualWidth, (int)kiddieHolder.ActualHeight, 96, 96, PixelFormats.Pbgra32, null);
                currentFrameCopy.Lock();

                RenderTargetBitmap rendrBMP = new RenderTargetBitmap((int)kiddieHolder.ActualWidth, (int)kiddieHolder.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                rendrBMP.Render(kiddieHolder);
                rendrBMP.CopyPixels(new Int32Rect(0, 0, (int)kiddieHolder.ActualWidth, (int)kiddieHolder.ActualHeight), currentFrameCopy.BackBuffer, currentFrameCopy.BackBufferStride * currentFrameCopy.PixelHeight, currentFrameCopy.BackBufferStride);
                currentFrameCopy.Unlock();
            }
            else
            {
                currentFrameCopy = this.colorBitmap.Clone();
            }
            currentFrameCopy.Freeze();

            _photoSubmitter.encodeAndSubmitUserPhotoToCMS(currentFrameCopy);
        }

        private void handlePhotoSubmittedToCMS(object sender, EventArgs e)
        {
            freezePhoto = false;
        }

        private void requestNewCaption(string _captionKey, bool _queue)
        {
            if (initiateNewCaption != null)
            {
                InitiateCaptionsEventArgs capArgs = new InitiateCaptionsEventArgs(_captionKey, _queue);
                initiateNewCaption(this, capArgs);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            requestNewCaption(ApplicationStates.STATE_POSITION_PHOTO, false);

            adjustLayoutToAccomodateRealRes();
        }

        private void adjustLayoutToAccomodateRealRes()
        {
            // Top bar is always 135
            // Bottom is 150
            // Bottom buttons are 122
            double availableHeight = GlobalConfiguration.currentScreenH - 407;
            double fullH = GlobalConfiguration.currentScreenH - 285;

            // We know the photo button needs 254
            double availableWidth = GlobalConfiguration.currentScreenW - 254;
            double fullW = availableWidth;

            //mainGrid.Width = GlobalConfiguration.currentScreenW;
            //mainGrid.Height = availableHeight;

            // Clamp width to a 16x9 ratio
            double sixteenNineRatioW = (16 * availableHeight) / 9;
            if (availableWidth > sixteenNineRatioW)
            {
                availableWidth = sixteenNineRatioW;
            }
            leftSpacer.Width = ((fullW - availableWidth) * .5);
            leftSpacer2.Width = leftSpacer.Width;

            contentBorder.Width = availableWidth;
            contentBorder.Height = availableHeight;
            YNBabysitter1.Width = availableWidth * .5;
            YNBabysitter2.Width = availableWidth * .5;

            masterParent.Width = GlobalConfiguration.currentScreenW;
            masterParent.Height = fullH;

            camFlash.Width = GlobalConfiguration.currentScreenW;
            camFlash.Height = fullH;

            //masterStack.Height = fullH;
            masterStack.Width = GlobalConfiguration.currentScreenW;

        }

    }
}
