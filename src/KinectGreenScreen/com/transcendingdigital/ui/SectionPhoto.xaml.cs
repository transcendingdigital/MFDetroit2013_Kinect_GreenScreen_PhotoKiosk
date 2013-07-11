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
using KinectGreenScreen.com.transcendingdigital.ui;
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

        // The event to update the main photo list state in case we submit a photo in here
        // This lets the attract loop know it should re-pull photos
        public delegate void updatedPhotoList(object sender, PhotoListStateEventArgs e);
        public event updatedPhotoList requestNewPhotoList;

        public bool destroyed = false;

        // Used for sound effects
        private MediaPlayer soundFXMan;
        private List<string> sfxQueue;

        private MainWindow _mainWin;
        // This holds all of the already loaded images for the chroma key.
        // It lives in Main.
        private List<chromaKeyImageData> _chromaKImages;

        public SectionPhoto()
        {
            InitializeComponent();
        }

        public void init(ref KinectSensor sensor, ref MainWindow _main, ref List<chromaKeyImageData> _loadedChromaKImages)
        {
            _chromaKImages = _loadedChromaKImages;
            // If there is at least one init the green screen with that
            if (_chromaKImages.Count > 0)
            {
                gsView.init(ref sensor, gsView.Width, gsView.Height,_chromaKImages[0]);
            }
            else
            {
            // Bummer use a placeholder
                gsView.init(ref sensor, gsView.Width, gsView.Height, "pack://application:,,,/Resources/BG1.png", "");
            }

            _mainWin = _main;
        }

        public void initiatePhotoTake()
        {
            // Just in case it's a re-take, ensure the buttons are hidden
            // and background selection is hidden
            gsView.freezePhotoOrNot(false);

            if (Canvas.GetTop(btnStack) < (GlobalConfiguration.currentScreenH + btnStack.Height + 30) )
            {
                DoubleAnimation daHeight = new DoubleAnimation(Canvas.GetTop(contentBorder) + contentBorder.ActualHeight + 50, (GlobalConfiguration.currentScreenH + btnStack.ActualHeight + 30), new Duration(TimeSpan.FromSeconds(0.3)));
                
                SineEase ease = new SineEase();
                ease.EasingMode = EasingMode.EaseIn;
                daHeight.EasingFunction = ease;

                // Moves buttons down
                btnStack.BeginAnimation(Canvas.TopProperty, daHeight);
            }
            if (Canvas.GetLeft(stackBGs) < (GlobalConfiguration.currentScreenW + 50))
            {
                DoubleAnimation daFHeight = new DoubleAnimation((GlobalConfiguration.currentScreenW / 2 - stackBGs.ActualWidth / 2), (GlobalConfiguration.currentScreenW + 50), new Duration(TimeSpan.FromSeconds(0.3)));

                SineEase ease2 = new SineEase();
                daFHeight.EasingFunction = ease2;
                daFHeight.BeginTime = TimeSpan.FromSeconds(0.2);
                // Moves backgrounds off to the right
                stackBGs.BeginAnimation(Canvas.LeftProperty, daFHeight);
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
            // Set the countdown text at the top most number
            countDown.Text = Properties.Settings.Default.photoCountdownSeconds.ToString();
            for (int i = 0; i < Properties.Settings.Default.photoCountdownSeconds; i++)
            {
                sfxQueue.Add("TDGenBeep.wav");
            }
            sfxQueue.Add("CameraFlash.mp3");
            playSoundEffect(sfxQueue[0]);
            countDown.Visibility = System.Windows.Visibility.Visible;
        }

        public void confirmDenyPhoto(bool _yesNo)
        {

            // Hide the yes and no buttons
            DoubleAnimation daHeight = new DoubleAnimation(Canvas.GetTop(contentBorder) + contentBorder.ActualHeight + 50, (GlobalConfiguration.currentScreenH + btnStack.ActualHeight + 30), new Duration(TimeSpan.FromSeconds(0.3)));
            // Slide the background selection back in
            DoubleAnimation daFHeight = new DoubleAnimation((GlobalConfiguration.currentScreenW + 50),(GlobalConfiguration.currentScreenW / 2 - stackBGs.ActualWidth / 2), new Duration(TimeSpan.FromSeconds(0.3)));
            SineEase ease = new SineEase();
            ease.EasingMode = EasingMode.EaseIn;
            daHeight.EasingFunction = ease;
            daFHeight.EasingFunction = ease;
            daFHeight.BeginTime = TimeSpan.FromSeconds(0.2);
            btnStack.BeginAnimation(Canvas.TopProperty, daHeight);
            stackBGs.BeginAnimation(Canvas.LeftProperty, daFHeight);
            //btnYes.BeginAnimation(Button.MarginProperty

            // Show take photo
            DoubleAnimation daOpacity = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.5)));
            txtTakePhoto.Text = "TAKE PHOTO";
            txtTakePhoto.BeginAnimation(TextBlock.OpacityProperty, daOpacity);
            txtStartCountdown.BeginAnimation(TextBlock.OpacityProperty, daOpacity);
            takePhoto.BeginAnimation(Image.OpacityProperty, daOpacity);
            takePhoto.IsHitTestVisible = true;

            if (_yesNo == true)
            {
                // Captions
                requestNewCaption(ApplicationStates.STATE_PHOTO_SUBMITTED, false);
                saveImageFrame();
                // Ensure the main knows it needs to update the photo list in attract next time
                if (requestNewPhotoList != null)
                {
                    PhotoListStateEventArgs newArgs = new PhotoListStateEventArgs(true, null);
                    requestNewPhotoList(this, newArgs);
                }
            }
            else
            {
                // Captions
                requestNewCaption(ApplicationStates.STATE_PHOTO_DESTROYED, false);
            }

            // Save photo or not, we can unfreese the video here. Saving is done in a background worker
            gsView.freezePhotoOrNot(false);
        }

        public void swapBackground(string _targetBG, string _targetFG)
        {
            if (gsView != null)
            {
                gsView.changeBackground(_targetBG, _targetFG);
            }
        }
        public void swapBackground(chromaKeyImageData _newData)
        {
            if (gsView != null)
            {
                gsView.changeBackground(_newData);
            }
        }
        public void toggleGreyscale(bool _yesNo)
        {
            if (gsView != null)
            {
                gsView.toggleMonochrome(_yesNo);
            }
        }

        public void destroyInternals()
        {
            // Remove any dynamic thumbnails
            if (_chromaKImages.Count > 0)
            {
                for (int i = 0; i < stackBGs.Children.Count; i++)
                {
                    var aBorder = stackBGs.Children[i] as Border;
                    if (aBorder != null)
                    {
                        var currentChild = aBorder.Child;
                        if (currentChild is WrappedBGThumb)
                        {
                            var aImage = currentChild as WrappedBGThumb;
                            if (aImage != null)
                            {
                                aImage.ClearValue(FrameworkElement.TagProperty);
                                aImage.Source = null;
                                aImage = null;
                            }
                        }
                        aBorder.Child = null;
                    }
                }

                stackBGs.Children.Clear();
            }

            gsView.destroyInternals();
            gsView = null;

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
                        countDown.Text = Properties.Settings.Default.photoCountdownSeconds.ToString();

                        // Take the photo
                        gsView.freezePhotoOrNot( true );

                        // Flash the white
                        DoubleAnimation daFlashOpacity = new DoubleAnimation(0, .8, new Duration(TimeSpan.FromSeconds(0.1)));
                        daFlashOpacity.AutoReverse = true;

                        camFlash.BeginAnimation(Ellipse.OpacityProperty, daFlashOpacity);
                    }
                }
                else
                {
                    sfxQueue = null;
                    // Done
                    destroySFXPlayer();

                    // show yes or no button
                    DoubleAnimation daHeight = new DoubleAnimation((GlobalConfiguration.currentScreenH + btnStack.ActualHeight + 30), Canvas.GetTop(contentBorder) + contentBorder.ActualHeight + 50, new Duration(TimeSpan.FromSeconds(0.3)));

                    SineEase ease = new SineEase();
                    ease.EasingMode = EasingMode.EaseOut;
                    daHeight.EasingFunction = ease;
                    daHeight.BeginTime = TimeSpan.FromSeconds(6);
                    // Add a tiny delay
                    btnStack.BeginAnimation(Canvas.TopProperty, daHeight);

                    // Captions
                    requestNewCaption(ApplicationStates.STATE_LOOKINGGOOD, false);
                    requestNewCaption(ApplicationStates.STATE_YES_NOPHOTO, true);

                    // Re-enable the cursors
                    if (_mainWin != null)
                    {
                        _mainWin.enableDisableAllCursors(true);
                    }
                }
            }
        }

        private void saveImageFrame()
        {
            // SHOW A MESSAGE TO THE USER

            gsView.saveImageFrame();
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

        /// <summary>
        /// I got really sick of XAML layout here and decided to absolute position everything
        /// in calculated positions.  I guess this is probably considered bad, but
        /// it takes a lot less time than fooling with 500 different controls to achieve
        /// simple layout tasks in a static screen size.
        /// </summary>
        private void adjustLayoutToAccomodateRealRes()
        {
            // Top bar is always 135
            // Bottom buttons are 122
            double availableHeight = GlobalConfiguration.currentScreenH - 80;

            // We know the photo button needs 254
            double availableWidth = GlobalConfiguration.currentScreenW - 254;
            double fullW = availableWidth;

            masterParent.Width = GlobalConfiguration.currentScreenW;
            masterParent.Height = availableHeight;

            camFlash.Width = GlobalConfiguration.currentScreenW;
            camFlash.Height = availableHeight;

            // Absolute position all of our elements
            // Dead center the photo area
            Canvas.SetTop(contentBorder, 50);
            Canvas.SetLeft(contentBorder, (GlobalConfiguration.currentScreenW / 2 - contentBorder.ActualWidth / 2));
            
            // Center the countdown number in the photo area
            Canvas.SetTop(countDown, 50 + (contentBorder.ActualHeight / 2 - countDown.DesiredSize.Height / 2));
            Canvas.SetLeft(countDown, Canvas.GetLeft(contentBorder) + (contentBorder.ActualWidth / 2 - countDown.DesiredSize.Width / 2));

            // Position the take photo stack to the right of the photo area
            Canvas.SetTop(takePhotoStack, 50);
            Canvas.SetLeft(takePhotoStack, Canvas.GetLeft(contentBorder) + contentBorder.ActualWidth + 50);

            // Dynamically insert any photos into the background selection area
            if (_chromaKImages != null)
            {
                if(_chromaKImages.Count > 0) {
                    // Remove the placeholders
                    bg1B.Child = null;
                    bg2B.Child = null;
                    bg3B.Child = null;

                    stackBGs.Children.Remove(bg1B);
                    stackBGs.Children.Remove(bg2B);
                    stackBGs.Children.Remove(bg3B);
                }

                for(int i = 0; i < _chromaKImages.Count; i++) {
                    WrappedBGThumb myImage = new WrappedBGThumb();
                    myImage.Width = 203;
                    myImage.Height = 152;
                    myImage.Stretch = Stretch.Fill;
                    myImage.Source = _chromaKImages[i].BackgroundImageBM;
                    // This is used in main to swap the backgrounds on hit testing
                    myImage.SetValue(FrameworkElement.TagProperty, _chromaKImages[i]);

                    Border myBorder = new Border();
                    myBorder.BorderThickness = new Thickness(5);
                    myBorder.BorderBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#E62923"));
                    myBorder.Margin = new Thickness(20, 0, 20, 0);
                    myBorder.Opacity = 1;
                    myBorder.Child = myImage;

                    stackBGs.Children.Add(myBorder);
                }
            }

            // Force the stackBGs to re-render quickly so we get the correct width
            stackBGs.Measure(new Size(GlobalConfiguration.currentScreenW, GlobalConfiguration.currentScreenH));
            // Dead center the background selection under the photo area
            Canvas.SetTop(stackBGs, Canvas.GetTop(contentBorder) + contentBorder.ActualHeight + 50);
            Canvas.SetLeft(stackBGs, GlobalConfiguration.currentScreenW / 2 - stackBGs.DesiredSize.Width / 2);

            // Position the Yes No buttons dead center but offscreen to the bottom
            Canvas.SetLeft(btnStack, GlobalConfiguration.currentScreenW / 2 - btnStack.DesiredSize.Width / 2);
            Canvas.SetTop(btnStack, GlobalConfiguration.currentScreenH + btnStack.DesiredSize.Height + 30);

        }

    }
}
