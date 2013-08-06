/*
* SectionAttract.cs
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
using KinectGreenScreen.com.transcendingdigital.data;
using System.IO;
using System.Timers;
using System.Windows.Media.Animation;
using KinectGreenScreen.com.transcendingdigital.events;

namespace KinectGreenScreen.com.transcendingdigital.ui
{
    /// <summary>
    /// Interaction logic for SectionAttract.xaml
    /// </summary>
    public partial class SectionAttract : UserControl
    {
        // The event delegate
        public delegate void captionRequest(object sender, InitiateCaptionsEventArgs e);
        // The event
        public event captionRequest initiateNewCaption;

        // The event to update the main photo list
        public delegate void updatedPhotoList(object sender, PhotoListStateEventArgs e);
        public event updatedPhotoList incomingPhotoList;

        public bool destroyed;
        private Timer captionRepeatDelay;

        private List<photoDataObject> _allImageData;
        private Timer _imageDisplayDelay;
        private Random _rand = new Random();
        private List<int> _currentImgsIndex = new List<int>();
        private int _numberImgToLoad = 1;

        private bool _pullNewPhotos = false;
        private AsyncPhotoRetrieval _bgPhotoLoader = null;
        private MainWindow _mainWinRef;

        public SectionAttract(bool _refreshPhotos, List<photoDataObject> _photoListRefernce, ref MainWindow _mainWindow)
        {
            _pullNewPhotos = _refreshPhotos;
            _mainWinRef = _mainWindow;

            if (_pullNewPhotos == false && _photoListRefernce != null)
            {
                _allImageData = _photoListRefernce;
            }

            InitializeComponent();
        }

        /// <summary>
        /// Well need to clean up any remaining images
        /// stop all transitions etc
        /// </summary>
        public void destroyInternals()
        {
            destroyBGPhotoLoader(false);
            destroyCaptionDelayTimer(false, 0);
            destroyImageDisplayTimer();

            for (int i = 0; i < mainHolder.Children.Count; i++)
            {
                var curBrdr = mainHolder.Children[i] as Border;
                if (curBrdr != null)
                {
                    curBrdr.Child = null;
                }
            }

            mainHolder.Children.Clear();

        }

        public void jumpToPhotoBtnActivated()
        {
            // Disable hit capability to prevent simultanious multiple
            // hits
            btnJumpPhoto.IsHitTestVisible = false;
            _mainWinRef.createPhotoSectionFromAttract();
        }

        /***
         * Keep in mind this can be called simultaniously
         * one or more times from multiple players
         * or cursors. It will be in a row, but
         * states could not be ideal so handle
         * them.
         * 
         * The IsHitTestVisible down here should stop
         * double hits, right?
         */
        public void confimDenyPrompt(bool _yn)
        {
            // Either way, we need to hide yes and no
            btnNo.IsHitTestVisible = false;
            btnYes.IsHitTestVisible = false;

            // Yes they would like to contribute a photo
            if (_yn == true)
            {
                _mainWinRef.createPhotoSectionFromAttract();
            }
            // No, they do not want to contribute a photo
            else
            {
                hideShowInitialQuestion(false);
            }
        }

        public void hideShowInitialQuestion(bool _yn)
        {
            DoubleAnimation daOpacityHideOrShow = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(1)));
            DoubleAnimation daOpacityHideOrShowJump = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(1)));
            if (_yn == true)
            {
                daOpacityHideOrShow.From = 0;
                daOpacityHideOrShow.To = 1;

                daOpacityHideOrShowJump.From = 1;
                daOpacityHideOrShowJump.To = 0;

                btnJumpPhoto.IsHitTestVisible = false;
                btnNo.IsHitTestVisible = true;
                btnYes.IsHitTestVisible = true;

                // Play some narration
                requestNewCaption(ApplicationStates.STATE_PHOTO_QUESTION, true);
            }
            else
            {
                daOpacityHideOrShow.From = 1;
                daOpacityHideOrShow.To = 0;

                daOpacityHideOrShowJump.From = 0;
                daOpacityHideOrShowJump.To = 1;

                btnNo.IsHitTestVisible = false;
                btnYes.IsHitTestVisible = false;

                btnJumpPhoto.IsHitTestVisible = true;
            }

            jumpPhotoHolder.BeginAnimation(Grid.OpacityProperty, daOpacityHideOrShowJump);
            initialYNPrompt.BeginAnimation(Grid.OpacityProperty, daOpacityHideOrShow);
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            adjustLayoutToAccomodateRealRes();

            setupCaptionDelayTimer(30000);

            if (_pullNewPhotos == true)
            {
                createBGPhotoLoader();
                // We now wait for the event from the background photo loader
            }
            else
            {
                // Pick a random image to display first
                selectRandomImage();
                // Transition the first set of images on starting the process
                loadNewImages();
                // transition them on - events handle the rest
                showNewImages();
            }
        }

        private void adjustLayoutToAccomodateRealRes()
        {
            double availableWidth = GlobalConfiguration.currentScreenW;
            double availableHeight = GlobalConfiguration.currentScreenH - 80;

            mainHolder.Width = availableWidth;
            mainHolder.Height = availableHeight;
        }

        private void createImageDisplayTimer()
        {
            // 10 to 20 seconds
            int _msTime = _rand.Next(10000, 20000);

            _imageDisplayDelay = new Timer(_msTime);
            _imageDisplayDelay.Elapsed += handleImageDisplayUp;
            _imageDisplayDelay.Start();
        }

        private void destroyImageDisplayTimer()
        {
            if (_imageDisplayDelay != null)
            {
                _imageDisplayDelay.Stop();
                _imageDisplayDelay.Elapsed -= handleImageDisplayUp;
                _imageDisplayDelay = null;
            }
        }

        /// <summary>
        /// This isnt in the UI thread so we need to use the dispatcher
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handleImageDisplayUp(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                destroyImageDisplayTimer();
                hideCurrentImages();
            }));
        }

        private void selectRandomImage()
        {
            // Empty anything currently
            _currentImgsIndex.Clear();

            _numberImgToLoad = _rand.Next(1, 10);
            if (_numberImgToLoad > _allImageData.Count - 1)
            {
                _numberImgToLoad = _allImageData.Count - 1;
            }

            if (_allImageData.Count > 1)
            {
                for (int i = 0; i < _numberImgToLoad; i++)
                {
                    int _nextGuy = _rand.Next(0, _allImageData.Count - 1);
                    bool good = false;

                    while (good == false)
                    {
                        good = true;
                        // Ensure we dont have the same one
                        for (int j = 0; j < _currentImgsIndex.Count; j++)
                        {
                            if (_currentImgsIndex[j] == _nextGuy)
                            {
                                good = false;
                            }
                        }

                        if (good == true)
                        {
                            _currentImgsIndex.Add(_nextGuy);
                        }
                        _nextGuy = _rand.Next(0, _allImageData.Count - 1);
                    }
                }
            }
            else
            {
                if(_allImageData.Count > 0) 
                _currentImgsIndex.Add(0);
            }

        }

        private void loadNewImages()
        {

            //MonochromeEffect monoFX = new MonochromeEffect();
            //monoFX.FilterColor = Color.FromArgb(0, 255, 255, 255);
            //monoFX.Contrast = 1.5;

            for (int j = 0; j < _currentImgsIndex.Count; j++)
            {
                // This blocks be careful
                BitmapImage currentImage = new BitmapImage();
                currentImage.BeginInit();
                //currentImage.DecodePixelWidth = 640;
                //currentImage.DecodePixelHeight = 480;
                currentImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                currentImage.CacheOption = BitmapCacheOption.OnLoad;
                currentImage.UriSource = new Uri(_allImageData[_currentImgsIndex[j]].toLoadPath);
                currentImage.EndInit();
                //currentImage.Freeze();

                Image MainImg = new Image();
                MainImg.Stretch = Stretch.Uniform;
                MainImg.Opacity = 1;
                MainImg.Source = currentImage;
                //monoFX.Contrast = (double)(_rand.Next(1, 3) + _rand.NextDouble());
                //MainImg.Effect = monoFX;

                Border myBorder = new Border();
                myBorder.BorderThickness = new Thickness(10);
                myBorder.BorderBrush = new SolidColorBrush(Colors.White);
                myBorder.Margin = new Thickness(8.0);
                myBorder.Opacity = 1;
                myBorder.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                myBorder.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                myBorder.Child = MainImg;

                // Figure out a random placement and tilted angle on the display
                // -45 to 45
                //int angle = _rand.Next(-10, 10);

                // We know that the scroller needs 200 px
                // We know the actual bar needs 74
                double availableWidth = GlobalConfiguration.currentScreenW - 640 - 50;

                // Need to account for bottom of screen 250
                // Need to account for image height 480
                // Need to account for rotated image height 240

                double availableHeight = GlobalConfiguration.currentScreenH - 250 - 480;

                Point newXY = new Point(_rand.Next(50, (int)availableWidth), _rand.Next(0, (int)availableHeight));
                // We want all items all the way to the left or right of the screen
                int lOrR = _rand.Next(0, 100);
                if (lOrR > 50)
                {
                    newXY.X = GlobalConfiguration.currentScreenW + 300;
                }
                else
                {
                    newXY.X = -1000;
                }

                int tOrB = _rand.Next(0, 100);
                if (tOrB > 50)
                {
                    newXY.Y = GlobalConfiguration.currentScreenH + 300;
                }
                else
                {
                    newXY.Y = -1000;
                }

                // Set the tag property to the point
                myBorder.SetValue(FrameworkElement.TagProperty, newXY);

                // Transforms for moving and rotating
                TranslateTransform moveTransform = new TranslateTransform(newXY.X, newXY.Y);
                //RotateTransform rotTransform = new RotateTransform(angle);
                TransformGroup photoTransforms = new TransformGroup();
                photoTransforms.Children.Add(moveTransform);
                //photoTransforms.Children.Add(rotTransform);

                myBorder.RenderTransform = photoTransforms;
                mainHolder.Children.Add(myBorder);
            }
        }

        // Should slide these in from the left and right
        private void showNewImages()
        {
            double delayBuildUp = 0;

            for (int i = 0; i < mainHolder.Children.Count; i++)
            {
                // need to get references to each border
                var curBorder = mainHolder.Children[i] as Border;
                if (curBorder != null)
                {

                    // Figure out a random placement and tilted angle on the display
                    // -45 to 45
                    int angle = _rand.Next(-10, 10);

                    // We know that the scroller needs 200 px
                    // We know the actual bar needs 74
                    double availableWidth = GlobalConfiguration.currentScreenW - 640 - 50;
                    int finX = _rand.Next(50, (int)availableWidth);
                    double availableHeight = GlobalConfiguration.currentScreenH - 250 - 480;
                    int finY = _rand.Next(0, (int)availableHeight);

                    var previousPoints = (Point)curBorder.GetValue(FrameworkElement.TagProperty);
                    if (previousPoints != null)
                    {
                        // Transforms for moving and rotating - need the first positions
                        TranslateTransform moveTransform = new TranslateTransform(previousPoints.X, previousPoints.Y);
                        RotateTransform rotTransform = new RotateTransform(0);
                        TransformGroup photoTransforms = new TransformGroup();
                        photoTransforms.Children.Add(moveTransform);
                        photoTransforms.Children.Add(rotTransform);

                        curBorder.RenderTransform = photoTransforms;

                        SineEase ease = new SineEase();
                        ease.EasingMode = EasingMode.EaseOut;

                        DoubleAnimation daMoveX = new DoubleAnimation();
                        DoubleAnimation daMoveY = new DoubleAnimation();
                        DoubleAnimation daRotate = new DoubleAnimation();
                        daMoveX.To = finX;
                        daMoveY.To = finY;
                        daRotate.To = angle;
                        daMoveX.EasingFunction = ease;
                        daRotate.EasingFunction = ease;
                        daMoveY.EasingFunction = ease;

                        int moveRandTime = _rand.Next(1, 3);
                        daMoveX.Duration = TimeSpan.FromSeconds(moveRandTime);
                        daRotate.Duration = TimeSpan.FromSeconds(_rand.Next(1, 2));
                        daMoveY.Duration = TimeSpan.FromSeconds(moveRandTime);

                        daMoveX.BeginTime = TimeSpan.FromSeconds(delayBuildUp);
                        daMoveY.BeginTime = TimeSpan.FromSeconds(delayBuildUp);
                        daRotate.BeginTime = TimeSpan.FromSeconds(delayBuildUp);
                        delayBuildUp += .3;

                        // Only add a complete listener to the final guy
                        if (i == mainHolder.Children.Count - 1)
                        {
                            daMoveX.Completed += handleAnimationOnComplete;
                        }

                        moveTransform.BeginAnimation(TranslateTransform.XProperty, daMoveX);
                        moveTransform.BeginAnimation(TranslateTransform.YProperty, daMoveY);
                        rotTransform.BeginAnimation(RotateTransform.AngleProperty, daRotate);
                    }

                }
            }
        }

        private void handleAnimationOnComplete(object sender, EventArgs e)
        {
            var daRef = sender as AnimationClock;
            if (daRef != null)
            {
                //daRef.Completed -= handleAnimationOnComplete;
                var daTL = daRef.Timeline as DoubleAnimation;
                if (daTL != null)
                {
                    //daTL.Completed -= handleAnimationOnComplete;
                }
            }

            // Start the hold timer
            createImageDisplayTimer();
        }

        /// <summary>
        /// Want to hide everything with a creepy slow dissolve
        /// </summary>
        private void hideCurrentImages()
        {

            int fadeOutSec = _rand.Next(5, 10);

            for (int i = 0; i < mainHolder.Children.Count; i++)
            {
                var curBrdr = mainHolder.Children[i] as Border;
                if (curBrdr != null)
                {
                    DoubleAnimation outAnimation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(fadeOutSec)));
                    if (i == mainHolder.Children.Count - 1)
                    {
                        outAnimation.Completed += handleImagesHidden;
                    }
                    curBrdr.BeginAnimation(Border.OpacityProperty, outAnimation);
                }
            }
        }


        private void handleImagesHidden(object sender, EventArgs e)
        {
            var daRef = sender as DoubleAnimation;
            if (daRef != null)
            {
                daRef.Completed -= handleImagesHidden;
            }

            for (int i = 0; i < mainHolder.Children.Count; i++)
            {
                var curBrdr = mainHolder.Children[i] as Border;
                if (curBrdr != null)
                {
                    curBrdr.Child = null;
                }
            }

            mainHolder.Children.Clear();

            selectRandomImage();
            loadNewImages();
            showNewImages();
        }

        private void setupCaptionDelayTimer(int _msTime)
        {
            if (captionRepeatDelay == null)
            {
                captionRepeatDelay = new Timer(_msTime);
                captionRepeatDelay.Elapsed += handleCaptionDelayUp;
                captionRepeatDelay.Start();
            }
            else
            {
                destroyCaptionDelayTimer(true, _msTime);
            }
        }

        private void destroyCaptionDelayTimer(bool _callback, int _msTime)
        {
            if (captionRepeatDelay != null)
            {
                captionRepeatDelay.Stop();
                captionRepeatDelay.Elapsed -= handleCaptionDelayUp;
                captionRepeatDelay = null;

                if (_callback == true)
                {
                    setupCaptionDelayTimer(_msTime);
                }
            }
        }

        /// <summary>
        /// This typically runs in a separate thread than the UI thread so
        /// use the dispatcher
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handleCaptionDelayUp(object sender, ElapsedEventArgs e)
        {
            // Play one of the annoying captions and VO's
            // Now create another delay
            Random random = new Random();
            int randomNumber = 1;
            randomNumber = random.Next(1, 3);

                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
                {
                    if (randomNumber == 1)
                    {
                        requestNewCaption(ApplicationStates.STATE_CALLOUT1, true);
                    }
                    else if (randomNumber == 2)
                    {
                        requestNewCaption(ApplicationStates.STATE_CALLOUT2, true);
                    }
                    else if (randomNumber == 3)
                    {
                        requestNewCaption(ApplicationStates.STATE_CALLOUT3, true);
                    }
                }));
            
            


            randomNumber = 0;
            int minRand = 20000;
            int maxRand = 60000;
            randomNumber = random.Next(minRand, maxRand);

            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                setupCaptionDelayTimer(randomNumber);
            }));
        }

        private void requestNewCaption(string _captionKey, bool _queue)
        {
            if (initiateNewCaption != null)
            {
                InitiateCaptionsEventArgs capArgs = new InitiateCaptionsEventArgs(_captionKey, _queue);
                initiateNewCaption(this, capArgs);
            }
        }

        private void createBGPhotoLoader()
        {
            if (_bgPhotoLoader == null)
            {
                _bgPhotoLoader = new AsyncPhotoRetrieval(ApplicationModel.ASYNC_MODE_RETRIEVE_SUBMITTED_MEDIA);
                _bgPhotoLoader.photoLoadProgress += handlePhotoLoadProgress;
                // Starts async ops on the photo loading will dispatch the above event
                // when complete
                _bgPhotoLoader.init();
            }
            else
            {
                destroyBGPhotoLoader(true);
            }
        }
        private void destroyBGPhotoLoader(bool _optionalCallback)
        {
            if (_bgPhotoLoader != null)
            {
                _bgPhotoLoader.photoLoadProgress -= handlePhotoLoadProgress;
                _bgPhotoLoader.destroyInternals();
                _bgPhotoLoader = null;
                if (_optionalCallback == true) createBGPhotoLoader();
            }
        }
        private void handlePhotoLoadProgress(object sender, PhotoLoadEventArgs e)
        {
            _allImageData = e.finalList;
            _pullNewPhotos = false;

            // Dispatch the latest list up so the main app has a copy of it
            if (incomingPhotoList != null)
            {
                PhotoListStateEventArgs newArgs = new PhotoListStateEventArgs(false, e.finalList);
                incomingPhotoList(this, newArgs);
            }

            // Pick a random image to display first
            selectRandomImage();
            // Transition the first set of images on starting the process
            loadNewImages();
            // transition them on - events handle the rest
            showNewImages();
        }
    }
}
