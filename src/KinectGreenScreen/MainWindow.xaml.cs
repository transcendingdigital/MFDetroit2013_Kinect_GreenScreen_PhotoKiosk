/*
* MainWindow.xaml.cs
* http://www.transcendingdigital.com
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
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using Microsoft.Kinect;
using KinectGreenScreen.com.transcendingdigital.data;
using KinectGreenScreen.com.transcendingdigital.events;
using KinectGreenScreen.com.transcendingdigital.ui;
using KinectGreenScreen.com.transcendingdigital.ui.buttons;

namespace KinectGreenScreen
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HitTestResult HitResult = null;

        private readonly Dictionary<int, InteractivePlayer> players = new Dictionary<int, InteractivePlayer>();
        private SectionAttract sec_attract;
        private SectionPhoto sec_photo;
        private System.Timers.Timer inactivityTimer;
        private ExhibitDrupalDataManager _cms;
        private bool _triggerPhotoRefresh = true;
        private bool _disableBackToAttract = false;
        // This is the main list of most recent photos - it gets updated by the attract
        // section on demand.
        private List<photoDataObject> _primaryPhotoReference;
        // This is the main list of loaded in memory chroma key images - it is only
        // loaded on start and kept in memory.
        private List<chromaKeyImageData> _primaryChromaImageList;
        private AsyncPhotoRetrieval _chromaImageLoader;

        #region KinectVariables
        private KinectSensor _sensor;
        const int skeletonCount = 6;
        Skeleton[] allSkeletons;
        bool closing = false;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Called from the attract loop to jump directly to the
        /// photo section
        /// </summary>
        public void createPhotoSectionFromAttract()
        {
            createPhotoSection();
            ChangeContent(sec_photo);
        }
        private void adjustLayoutToRes()
        {
            // We know the top bar is 135px
            // Bottom is 150px high

            dockDude.Width = GlobalConfiguration.currentScreenW;
            dockDude.Height = GlobalConfiguration.currentScreenH;

            transitionHolder.Height = GlobalConfiguration.currentScreenH - 135;

            // Adjust the position of the gear
            Canvas.SetLeft(bgGear, (GlobalConfiguration.currentScreenW / 2 - bgGear.Source.Width/2) );
            Canvas.SetTop(bgGear, ( GlobalConfiguration.currentScreenH - bgGear.Source.Height) + 60);
        }

        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.useCMS == true)
            {
                GlobalConfiguration.useDrupal7CMS = 1;
                if (Properties.Settings.Default.drupalPublishSubmittedMedia == true)
                {
                    GlobalConfiguration.automaticImageSubmission = 1;
                }
                refreshCMSConfig();
            }
            else
            {
                // Sets CMS configurable items from properties
                GlobalConfiguration.useDrupal7CMS = 0;
                GlobalConfiguration.drupalImageSubmissionContentType = Properties.Settings.Default.drupalSubmissionContentType;
                GlobalConfiguration.drupalUserSubmittedImageViewName = Properties.Settings.Default.drupalSubmittedImageViewName;
                GlobalConfiguration.cursorGeneralActivationMS = Properties.Settings.Default.exhibitCursorActivationMS;
                if (Properties.Settings.Default.drupalPublishSubmittedMedia == true)
                {
                    GlobalConfiguration.automaticImageSubmission = 1;
                }
                GlobalConfiguration.kinectDepthThresholdMeters = Properties.Settings.Default.kinectSensingDepthCutoffMeters;
                GlobalConfiguration.kinectGreenScreenThresholdMeters = Properties.Settings.Default.kinectGreenScreenDepthThresholdMeters;

            }
            GlobalConfiguration.currentScreenW = SystemParameters.FullPrimaryScreenWidth;
            GlobalConfiguration.currentScreenH = SystemParameters.FullPrimaryScreenHeight;
            GlobalConfiguration.currentKinectScreenW = (int)SystemParameters.FullPrimaryScreenWidth;
            GlobalConfiguration.currentKinectScreenH = (int)SystemParameters.FullPrimaryScreenHeight;
            // Set the players bounding box - should be smaller than 640x480 but still the same WxH ratio
            // of the app or else will induce faster up/down skew
            GlobalConfiguration.playerBoundBoxW = Properties.Settings.Default.kinectPlayerBoundingBoxW;
            float whRatio = (float)(GlobalConfiguration.currentScreenH / GlobalConfiguration.currentScreenW);
            GlobalConfiguration.playerBoundBoxH = ((float)GlobalConfiguration.playerBoundBoxW * whRatio);

            // Hide the mouse cursor
            Mouse.OverrideCursor = System.Windows.Input.Cursors.None;
            GlobalConfiguration.useAdvancedGreenScreenRendering = Properties.Settings.Default.useEmguCV;

            // Setup the kinect
            setupKinect();

            adjustLayoutToRes();

            // Loads the chroma key photos then creates the sections
            createChromaPhotoLoad();
            // SEE handleChromaPhotoLoadComplete
        }
        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWindow.MouseMove -= Window_MouseMove;

            closing = true;
            if (_sensor != null)
            {
                cleanUpKinect(_sensor);
            }
        }
        /// <summary>
        /// On certain keyboard input we do some things like show the cursor or swap
        /// the green screen rendering methodology. This is pretty standard
        /// on most exhibits.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Toggles the default Kinect Green Screen or this one
            if (e.Key.ToString().ToLower() == "g")
            {
                if (Properties.Settings.Default.useEmguCV == true)
                {
                    if (GlobalConfiguration.useAdvancedGreenScreenRendering == true)
                    {
                        GlobalConfiguration.useAdvancedGreenScreenRendering = false;
                    }
                    else
                    {
                        GlobalConfiguration.useAdvancedGreenScreenRendering = true;
                    }
                }

            // Hides and shows the mouse
            } else if (e.Key.ToString().ToLower() == "m")
            {
                if (Mouse.OverrideCursor == System.Windows.Input.Cursors.None)
                {
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
                }
                else
                {
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.None;
                }
            }
        }

        #region Kinect
        /// <summary>
        /// Can only handle one kinect. Make sure to put these in try catch blocks
        /// in case the target system well doesnt have a kinect.
        /// </summary>
        private void setupKinect()
        {
            try
            {
                foreach (var potentialSensor in KinectSensor.KinectSensors)
                {
                    if (potentialSensor.Status == KinectStatus.Connected)
                    {
                        _sensor = potentialSensor;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception trying to find Kinect " + ex.Message);
            }
            try
            {
                if (_sensor != null)
                {
                    _sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);
                    _sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

                    // Settings for the color stream
                    ColorCameraSettings colorSettings = _sensor.ColorStream.CameraSettings;
                    // First reset everything in case they were not defaults to start
                    colorSettings.ResetToDefault();

                    // Auto Exposure or Not
                    //--------------------------------------
                    if (Properties.Settings.Default.kinectAutoExposure == true)
                    {
                        colorSettings.AutoExposure = true;
                        // Gets or sets the brightness or lightness as distinct from hue or saturation. The range is [0.0, 1.0]; the default value is 0.2156.
                        colorSettings.Brightness = Properties.Settings.Default.kinectAutoExposureBrightness;
                    }
                    else
                    {
                        colorSettings.AutoExposure = false;
                        // Gets or sets the frame interval, in units of 1/10,000 of a second. The range is [0, 4000]; the default value is 0.
                        colorSettings.FrameInterval = Properties.Settings.Default.kinectManualExposureFrameInterval;
                        // Gets or sets the exposure time in increments of 1/10,000 of a second. The range is [0.0, 4000]; the default value is 0.0.
                        // BZZZ WRONG - SDK 1.7 throws an exception if this is 0 when autoexposure is false
                        colorSettings.ExposureTime = Properties.Settings.Default.kinectManualExposureTime;
                        // Gets or sets the gain, which is a multiplier for the RGB color values. The range is [1.0, 16.0]; the default value is 1.0
                        colorSettings.Gain = Properties.Settings.Default.kinectGain;
                    }
                    //-----------------------------------------

                    // Advanced Color Settings
                    //-----------------------------------------
                    if (Properties.Settings.Default.kinectAutoWhiteBalance == true)
                    {
                        colorSettings.AutoWhiteBalance = Properties.Settings.Default.kinectAutoWhiteBalance;
                    }
                    else
                    {
                        colorSettings.AutoWhiteBalance = Properties.Settings.Default.kinectAutoWhiteBalance;
                        // Gets or sets the white balance, which is a color temperature in degrees Kelvin. The range is [2700, 6500]; the default value is 2700.
                        colorSettings.WhiteBalance = Properties.Settings.Default.kinectManualWhiteBalanceValue;
                    }
                    // Gets or sets the contrast, which is the amount of difference between lights and darks. The range is [0.5, 2.0]; the default value is 1.0.
                    colorSettings.Contrast = Properties.Settings.Default.kinectManualContrast;
                    // Gets or sets gamma, which is a nonlinear operation for coding luminance data. The range is [1.0, 2.8]; the default value is 2.2.
                    colorSettings.Gamma = Properties.Settings.Default.kinectManualGamma;
                    // Gets or sets the hue, which describes the shade of a color. The range is [–22.0, 22.0]; the default value is 0.0.
                    colorSettings.Hue = Properties.Settings.Default.kinectManualHue;
                    // Gets or sets the saturation, which is the colorfulness of a color relative to its own brightness. The range is [0.0, 2.0]; the default value is 1.0.
                    colorSettings.Saturation = Properties.Settings.Default.kinectManualSaturation;
                    // Gets or sets the sharpness, which describes the amount of detail visible. The range is [0, 1.0]; the default value is 0.5.
                    colorSettings.Sharpness = Properties.Settings.Default.kinectManualSharpness;
                    //-----------------------------------------

                    var parameters = new TransformSmoothParameters
                    {
                        Smoothing = 0.3f,
                        Correction = 0.0f,
                        Prediction = 0.0f,
                        JitterRadius = 1.0f,
                        MaxDeviationRadius = 0.5f
                    };

                    // Seated mode
                    if (Properties.Settings.Default.kinectSeatedModeEnabled == true)
                    {
                        _sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                    }
                    else
                    {
                        _sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                    }

                    _sensor.SkeletonStream.Enable(parameters);

                    _sensor.SkeletonFrameReady += SkeletonsReady;

                    // Temporarily add only one kinect player with L and R hands
                    //appState.AddInteractivePlayer(false, ref masterParent, this);

                    try
                    {
                        _sensor.Start();
                    }
                    catch (System.IO.IOException)
                    {
                        // Should throw an application conflict because something else is using it
                        throw;
                    }

                    // Start the timeout timer
                    // Checks for active players
                    setupInactivityTimer();
                }
            }
            catch (Exception ex2)
            {
                System.Diagnostics.Debug.WriteLine("Exception initializing kinect settings " + ex2.Message);
            }


            try
            {
                if (_sensor == null)
                {
                    // We're in mouse mode!
                    //mouseMode = true;
                    // Add a listener
                    mainWindow.MouseMove += Window_MouseMove;

                    InteractivePlayer newPlayer = new InteractivePlayer(players.Count, true);
                    this.players.Add(0, newPlayer);
                    // Listen to events on the cursor
                    this.players[0].playerCursors[ApplicationModel.CURSOR_TYPE_L].cursorActivationUpdate += handleControlActivation;
                    masterParent.Children.Add(this.players[0].playerCursors[ApplicationModel.CURSOR_TYPE_L]);
                }
            }
            catch (Exception ex3)
            {
                System.Diagnostics.Debug.WriteLine("Exception initializing mouse settings " + ex3.Message);
            }
            
        }

        private void cleanUpKinect(KinectSensor sensor)
        {
            sensor.Stop();
            sensor.SkeletonFrameReady -= this.SkeletonsReady;
        }

        private void SkeletonsReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (closing == true)
            {
                return;
            }

            // Lets open up the skeleton frame and see how many we have
            using (SkeletonFrame skeleTors = e.OpenSkeletonFrame())
            {
                if (skeleTors != null)
                {
                    // If this is the very first run or we had no players and now have players, initialize
                    // our global skeletons
                    if ((this.allSkeletons == null) || (this.allSkeletons.Length != skeleTors.SkeletonArrayLength))
                    {
                        this.allSkeletons = new Skeleton[skeleTors.SkeletonArrayLength];
                    }

                    // Copy the skeleton data into our local copy
                    skeleTors.CopySkeletonDataTo(this.allSkeletons);

                    int skeletonSlot = 0;

                    // Loop through each available skelleton
                    foreach (Skeleton oneSkeleton in this.allSkeletons)
                    {
                        // Its an active player - and it isnt beyond the depth threshold - depth is in meters here...0.9..1.345..etc
                        if (oneSkeleton.TrackingState == SkeletonTrackingState.Tracked && (oneSkeleton.Position.Z <= GlobalConfiguration.kinectDepthThresholdMeters))
                        {
                            // If we have no players and this is the first one and we are in the attract mode
                            // but the question is not visible, show it
                            if (this.players.Count == 0)
                            {
                                if (sec_attract != null && sec_attract.destroyed == false)
                                {
                                   
                                    if (sec_attract.initialYNPrompt.Opacity == 0)
                                    {
                                        sec_attract.hideShowInitialQuestion(true);
                                    }
                                     
                                }
                            }

                            // Do we have cursors on screen for it yet?
                            if (!players.ContainsKey(skeletonSlot))
                            {
                                InteractivePlayer newPlayer = new InteractivePlayer(skeletonSlot, false);
                                this.players.Add(skeletonSlot, newPlayer);
                                // Listen to events on the cursor
                                this.players[skeletonSlot].playerCursors[ApplicationModel.CURSOR_TYPE_L].cursorActivationUpdate += handleControlActivation;
                                masterParent.Children.Add(this.players[skeletonSlot].playerCursors[ApplicationModel.CURSOR_TYPE_L]);
                                masterParent.Children.Add(this.players[skeletonSlot].playerCursors[ApplicationModel.CURSOR_TYPE_R]);
                                this.players[skeletonSlot].playerCursors[ApplicationModel.CURSOR_TYPE_R].cursorActivationUpdate += handleControlActivation;
                            }

                            this.players[skeletonSlot].lastUpdated = DateTime.Now;

                            // Establish the normalization rectangle based on the players head.  We increase sensitivity per player 
                            // by establishing a small rectangle around them representing their hand mobility then transform cursors to that
                            // 0 and 1 are set on intialization from the global configuration they are the W and H
                            ColorImagePoint headP = this._sensor.CoordinateMapper.MapSkeletonPointToColorPoint(oneSkeleton.Joints[JointType.Head].Position, ColorImageFormat.RgbResolution640x480Fps30);
                            // 2 and 3 are the real x and y positions
                            this.players[skeletonSlot].playerBounds[2] = headP.X - (this.players[skeletonSlot].playerBounds[0] * (float)0.5);
                            this.players[skeletonSlot].playerBounds[3] = headP.Y;
                            //System.Diagnostics.Debug.WriteLine("Head x: " + headP.X + " y: " + headP.Y + " boundsX: " + this.players[skeletonSlot].playerBounds[2] + " boundsY: " + this.players[skeletonSlot].playerBounds[3]);

                            // Update the L and R hand
                            //Point pt = new Point(oneSkeleton.Joints[JointType.HandRight].Position.X, oneSkeleton.Joints[JointType.HandRight].Position.Y);
                            // ---- RIGHT HAND
                            ColorImagePoint colP = this._sensor.CoordinateMapper.MapSkeletonPointToColorPoint(oneSkeleton.Joints[JointType.HandRight].Position, ColorImageFormat.RgbResolution640x480Fps30);
                            //System.Diagnostics.Debug.WriteLine("O Hand x: " + colP.X + " y: " + colP.Y);

                            // ---- Translate the right hand to player bounds ------
                            colP = translateFullScreenPointToPlayerBounds(colP, skeletonSlot);

                            // ----- STEP 3 ---- Translate our final coordinates to the UI size -----|
                            colP = normalizePointToRes(colP, GlobalConfiguration.currentKinectScreenW, GlobalConfiguration.currentKinectScreenH, (int)this.players[skeletonSlot].playerBounds[0], (int)this.players[skeletonSlot].playerBounds[1]);

                            // Now these need to be transformed into the real UI Res
                            players[skeletonSlot].playerCursors[ApplicationModel.CURSOR_TYPE_R].Margin = new Thickness(colP.X - 111 * .5, colP.Y - 116 * .5, 0, 0);

                            // Hit test
                            KinectGreenScreen.com.transcendingdigital.ui.Cursors targR = players[skeletonSlot].playerCursors[ApplicationModel.CURSOR_TYPE_R];
                            doHitTesting(new Point(colP.X, colP.Y), ref targR);

                            // ---- LEFT HAND 
                            colP = this._sensor.CoordinateMapper.MapSkeletonPointToColorPoint(oneSkeleton.Joints[JointType.HandLeft].Position, ColorImageFormat.RgbResolution640x480Fps30);

                            // ---- Translate the left hand to player bounds ------
                            colP = translateFullScreenPointToPlayerBounds(colP, skeletonSlot);

                            // ----- STEP 3 ---- Translate our final coordinates to the UI size -----|
                            colP = normalizePointToRes(colP, GlobalConfiguration.currentKinectScreenW, GlobalConfiguration.currentKinectScreenH, (int)this.players[skeletonSlot].playerBounds[0], (int)this.players[skeletonSlot].playerBounds[1]);
                            players[skeletonSlot].playerCursors[ApplicationModel.CURSOR_TYPE_L].Margin = new Thickness(colP.X - 111 * .5, colP.Y - 116 * .5, 0, 0);

                            // Hit test left hand
                            KinectGreenScreen.com.transcendingdigital.ui.Cursors targL = players[skeletonSlot].playerCursors[ApplicationModel.CURSOR_TYPE_L];
                            doHitTesting(new Point(colP.X, colP.Y), ref targL);
                        }

                        skeletonSlot++;
                    }

                    // Clean up any cursors for players no longer available
                    // --- handled by inactivity timer
                }
            }

        }

        ColorImagePoint translateFullScreenPointToPlayerBounds(ColorImagePoint colP, int skeletonSlot)
        {
            // ----- STEP 1 ----Normalize the joint inside the little player area -----|
            if (colP.X < this.players[skeletonSlot].playerBounds[2])
            {
                colP.X = (int)this.players[skeletonSlot].playerBounds[2];
            }
            else if (colP.X > (this.players[skeletonSlot].playerBounds[2] + this.players[skeletonSlot].playerBounds[0]))
            {
                colP.X = (int)(this.players[skeletonSlot].playerBounds[2] + this.players[skeletonSlot].playerBounds[0]);
            }
            // ----- Normalize the Y
            if (colP.Y < this.players[skeletonSlot].playerBounds[3])
            {
                colP.Y = (int)this.players[skeletonSlot].playerBounds[3];
            }
            else if (colP.Y > (this.players[skeletonSlot].playerBounds[3] + this.players[skeletonSlot].playerBounds[1]))
            {
                colP.Y = (int)(this.players[skeletonSlot].playerBounds[3] + this.players[skeletonSlot].playerBounds[1]);
            }

            // ----- STEP 2 ----Translate to the correct coordinate -----|
            colP.X = colP.X - (int)this.players[skeletonSlot].playerBounds[2];
            colP.Y = colP.Y - (int)this.players[skeletonSlot].playerBounds[3];

            return colP;
        }

        ColorImagePoint normalizePointToRes(ColorImagePoint _inputPoint, int _finW, int _finH, int _origMaxW, int _origMaxH)
        {
            ColorImagePoint returnPoint = new ColorImagePoint();
            returnPoint.X = 0;
            returnPoint.Y = 0;

            // Adjust W
            returnPoint.X = _finW * _inputPoint.X / _origMaxW;

            // Adjust H
            returnPoint.Y = _finH * _inputPoint.Y / _origMaxH;

            return returnPoint;
        }
        /// <summary>
        /// Disables or enables all active cursors
        /// </summary>
        /// <param name="_enableDisable"> true to enable false to disable</param>
        public void enableDisableAllCursors(bool _enableDisable)
        {
            if (this.players != null)
            {
                foreach (KeyValuePair<int, InteractivePlayer> playerSlot in this.players)
                {
                    if (_enableDisable == true)
                    {
                        this.players[playerSlot.Key].playerCursors[ApplicationModel.CURSOR_TYPE_L].cursorActivationUpdate += handleControlActivation;
                        this.players[playerSlot.Key].playerCursors[ApplicationModel.CURSOR_TYPE_L].Visibility = System.Windows.Visibility.Visible;

                        if (this.players[playerSlot.Key].playerCursors.ContainsKey(ApplicationModel.CURSOR_TYPE_R))
                        {
                            this.players[playerSlot.Key].playerCursors[ApplicationModel.CURSOR_TYPE_R].cursorActivationUpdate += handleControlActivation;
                            this.players[playerSlot.Key].playerCursors[ApplicationModel.CURSOR_TYPE_R].Visibility = System.Windows.Visibility.Visible;
                        }
                    }
                    else
                    {
                        this.players[playerSlot.Key].playerCursors[ApplicationModel.CURSOR_TYPE_L].cursorActivationUpdate -= handleControlActivation;
                        this.players[playerSlot.Key].playerCursors[ApplicationModel.CURSOR_TYPE_L].Visibility = System.Windows.Visibility.Hidden;
                        if (this.players[playerSlot.Key].playerCursors.ContainsKey(ApplicationModel.CURSOR_TYPE_R))
                        {
                            this.players[playerSlot.Key].playerCursors[ApplicationModel.CURSOR_TYPE_R].Visibility = System.Windows.Visibility.Hidden;
                            this.players[playerSlot.Key].playerCursors[ApplicationModel.CURSOR_TYPE_R].cursorActivationUpdate -= handleControlActivation;
                        }
                    }
                }
            }
        }

        #endregion
        #region hitTesting
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            Point pt = e.GetPosition(mainWindow);
            KinectGreenScreen.com.transcendingdigital.ui.Cursors targL = players[0].playerCursors[ApplicationModel.CURSOR_TYPE_L];
            doHitTesting(pt, ref targL);
            players[0].playerCursors[ApplicationModel.CURSOR_TYPE_L].Margin = new Thickness(pt.X - players[0].playerCursors[ApplicationModel.CURSOR_TYPE_L].Width / 2, pt.Y - players[0].playerCursors[ApplicationModel.CURSOR_TYPE_L].Height / 2, 0, 0);
        }
        /// <summary>
        /// This handles the actual hittesting.
        /// We base this off of wrapping our target dependencyObjects
        /// in custom class wrappers so we can disembiguate them.
        /// Works fine if the point is from a mouse or kinect skeleton
        /// joints.
        /// </summary>
        /// <param name="_mainPoint"></param>
        private void doHitTesting(Point _mainPoint, ref KinectGreenScreen.com.transcendingdigital.ui.Cursors targetCursor)
        {
            VisualTreeHelper.HitTest(mainWindow, new HitTestFilterCallback(MyHitTestFilter), new HitTestResultCallback(MyHitTestResult), new PointHitTestParameters(_mainPoint));
            //_TestObject, null, new HitTestResultCallback(MyHitTestResult), new PointHitTestParameters(_testPoint)

            if (HitResult != null)
            {
                if (HitResult.VisualHit is buttonNo)
                {
                    // Update the target cursor - only if its something we are interested in
                    DependencyObject cursorResult = HitResult.VisualHit as DependencyObject;
                    targetCursor.updateHitReference(ref cursorResult);
                    System.Diagnostics.Debug.WriteLine("Hit NoButton");
                }
                else if (HitResult.VisualHit is buttonYes)
                {
                    // Update the target cursor - only if its something we are interested in
                    DependencyObject cursorResult = HitResult.VisualHit as DependencyObject;
                    targetCursor.updateHitReference(ref cursorResult);
                    System.Diagnostics.Debug.WriteLine("Hit YesButton");
                }
                else if (HitResult.VisualHit is buttonTakePhoto)
                {
                    // Update the target cursor - only if its something we are interested in
                    DependencyObject cursorResult = HitResult.VisualHit as DependencyObject;
                    targetCursor.updateHitReference(ref cursorResult);
                    System.Diagnostics.Debug.WriteLine("Hit TakePhotoButton");
                }
                else if (HitResult.VisualHit is buttonTPPrompt)
                {
                    // Update the target cursor - only if its something we are interested in
                    DependencyObject cursorResult = HitResult.VisualHit as DependencyObject;
                    targetCursor.updateHitReference(ref cursorResult);
                }
                else if (HitResult.VisualHit is WrappedBGThumb)
                {
                    // Update the target cursor - only if its something we are interested in
                    DependencyObject cursorResult = HitResult.VisualHit as DependencyObject;
                    targetCursor.updateHitReference(ref cursorResult);
                }
                else
                {
                    // Flush the target cursor ONLY if it is activating
                    if (targetCursor.isActivating == true)
                    {
                        targetCursor.flushHitReference();
                        System.Diagnostics.Debug.WriteLine("Test of point: " + _mainPoint.X + " y: " + _mainPoint.Y + " is null");
                    }
                }

            }
            else
            {
                // Flush the target cursor ONLY if it is activating
                if (targetCursor.isActivating == true)
                {
                    targetCursor.flushHitReference();
                    System.Diagnostics.Debug.WriteLine("Test of point: " + _mainPoint.X + " y: " + _mainPoint.Y + " is null");
                }
            }
        }
        /// <summary>
        /// It looks like isHitTestVisible is ignored when doing manual
        /// hittest results. Man this is so convoluted.
        /// The only reason I had to add this now was to properly
        /// deal with isHitTestVisible and hidden elements
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private HitTestFilterBehavior MyHitTestFilter(DependencyObject o)
        {
            var uiElement = o as UIElement;
            if ((uiElement != null) && (!uiElement.IsHitTestVisible || !uiElement.IsVisible))
            {
                return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
            }
            else
            {
                return HitTestFilterBehavior.Continue;
            }
        }
        /// <summary>
        /// GAHHH I DO NOT WANT TO DO IT THIS WAY
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private HitTestResultBehavior MyHitTestResult(HitTestResult result)
        {
            // Add the hit test result to the list that will be processed after the enumeration.
            if (result.VisualHit.GetType() == typeof(buttonTakePhoto))
            {
                this.HitResult = result;
                return HitTestResultBehavior.Stop;
            }
            else if (result.VisualHit.GetType() == typeof(buttonYes))
            {
                this.HitResult = result;
                return HitTestResultBehavior.Stop;
            }
            else if (result.VisualHit.GetType() == typeof(buttonNo))
            {
                this.HitResult = result;
                return HitTestResultBehavior.Stop;
            }
            else if (result.VisualHit.GetType() == typeof(WrappedBGThumb))
            {
                this.HitResult = result;
                return HitTestResultBehavior.Stop;
            }
            else if (result.VisualHit.GetType() == typeof(buttonTPPrompt))
            {
                this.HitResult = result;
                return HitTestResultBehavior.Stop;
            }

            this.HitResult = result;
            return HitTestResultBehavior.Continue;
        }
        /// <summary>
        /// This is an event that happens when a user holds a cursor over an activatable
        /// element for a certain amount of time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void handleControlActivation(object sender, customCursorEventArgs e)
        {
            if (e.activationTarget is buttonTakePhoto)
            {
                if (sec_photo != null)
                {
                    // DISABLE DETECTION OF PLAYERS FOR GOING BACK TO ATTRACT IN CASE PEOPLE ARE POSING AND
                    // WE LOOSE ALL OF THEM
                    _disableBackToAttract = true;
                    sec_photo.initiatePhotoTake();
                }
            }
            else if (e.activationTarget is buttonYes)
            {
                if (sec_photo != null)
                {
                    sec_photo.confirmDenyPhoto(true);
                }
                if (sec_attract != null)
                {
                    sec_attract.confimDenyPrompt(true);
                }

                // RE-ENABLE DETECTION OF PLAYERS FOR GOING BACK TO THE ATTRACT
                _disableBackToAttract = false;
            }
            else if (e.activationTarget is buttonNo)
            {
                if (sec_photo != null)
                {
                    sec_photo.confirmDenyPhoto(false);
                }
                if (sec_attract != null)
                {
                    sec_attract.confimDenyPrompt(false);
                }
            }
            else if (e.activationTarget is buttonTPPrompt)
            {
                if (sec_attract != null)
                {
                    sec_attract.jumpToPhotoBtnActivated();
                }
            }
            else if (e.activationTarget is WrappedBGThumb)
            {
                if (sec_photo != null)
                {
                    WrappedBGThumb tmpTarget = e.activationTarget as WrappedBGThumb;
                    if(tmpTarget != null) {
                        if (_primaryChromaImageList.Count > 0)
                        {
                            // In SectionPhoto we store the actual ref in the tag
                            var tagObj = tmpTarget.GetValue(FrameworkElement.TagProperty);
                            chromaKeyImageData targetImgData = tagObj as chromaKeyImageData;
                            if (targetImgData != null)
                            {
                                sec_photo.swapBackground(targetImgData);
                                sec_photo.toggleGreyscale(targetImgData.greyscale);
                            }
                        }
                        else
                        {
                            bool _monochrome = false;

                            string _bgBase = tmpTarget.Source.ToString();
                            string _fgBase = "";
                            int targetIndex = -1;
                            targetIndex = _bgBase.IndexOf("BG1");
                            if (targetIndex != -1)
                            {
                                //_fgBase = "pack://application:,,,/Resources/BG1.png";
                            }
                            targetIndex = _bgBase.IndexOf("BG2");
                            if (targetIndex != -1)
                            {
                               // _fgBase = "pack://application:,,,/Resources/BG2.png";
                            }
                            targetIndex = _bgBase.IndexOf("BG3");
                            if (targetIndex != -1)
                            {
                               // _fgBase = "pack://application:,,,/Resources/BG3.png";
                                _monochrome = true;
                            }

                            // We need to seach specific photos that have a foreground too
                            sec_photo.swapBackground(_bgBase, _fgBase);
                            // Toggle monochrome if we need to
                            // It may be more performant to adjust the saturation on the Kinect color camera here
                            sec_photo.toggleGreyscale(_monochrome);
                        }
                    }
                }
            }
        }
        #endregion
        #region applicationLogic
        /// <summary>
        /// All main sections dispatch to this requesting new captions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handleNewCaptionVORequest(object sender, InitiateCaptionsEventArgs e)
        {
            if (captionMaster != null)
            {
                captionMaster.loadNewCaptions(e.targetStateToInitiate, e.queueCaption);
            }
        }
        /// <summary>
        /// All main sections dispatch to this to update the state of the attracts photo list.
        /// If users submit a new photo in the photo section, this will update to let the
        /// attract section know it needs to pull new photos.
        /// 
        /// Once the attract section pulls the latest photos, it then updates here to
        /// signify it has the most recent photos
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handlePhotoListStateUpdate(object sender, PhotoListStateEventArgs e)
        {
            _triggerPhotoRefresh = e.newState;
            if (e.photoList != null)
            {
                _primaryPhotoReference = e.photoList;
            }
        }
        private void createAttractSection()
        {
            if (sec_attract == null)
            {
                sec_attract = new SectionAttract(_triggerPhotoRefresh, _primaryPhotoReference, ref mainWindow);
                sec_attract.initiateNewCaption += handleNewCaptionVORequest;
                sec_attract.incomingPhotoList += handlePhotoListStateUpdate;
                // will need to add it some how
            }
            else
            {
                destroyAttractSection(true);
            }
        }
        private void destroyAttractSection(bool _callback)
        {
            if (sec_attract != null)
            {
                sec_attract.destroyInternals();
                sec_attract.initiateNewCaption -= handleNewCaptionVORequest;
                sec_attract.incomingPhotoList -= handlePhotoListStateUpdate;
                if (sec_attract is IDisposable)
                {
                    (sec_attract as IDisposable).Dispose();
                }
                sec_attract = null;

                if (_callback == true)
                {
                    createAttractSection();
                }
            }
        }

        private void createPhotoSection()
        {
            if (sec_photo == null)
            {
                sec_photo = new SectionPhoto();
                sec_photo.initiateNewCaption += handleNewCaptionVORequest;
                sec_photo.requestNewPhotoList += handlePhotoListStateUpdate;
                sec_photo.init(ref _sensor, ref mainWindow, ref _primaryChromaImageList);
            }
            else
            {
                destroyPhotoSection(true);
            }
        }
        private void destroyPhotoSection(bool _callback)
        {
            if (sec_photo != null)
            {
                sec_photo.initiateNewCaption -= handleNewCaptionVORequest;
                sec_photo.requestNewPhotoList -= handlePhotoListStateUpdate;
                sec_photo.destroyInternals();
                if (sec_photo is IDisposable)
                {
                    (sec_photo as IDisposable).Dispose();
                }
                sec_photo = null;

                if (_callback == true)
                {
                    createPhotoSection();
                }
            }
        }
        private void setupInactivityTimer()
        {
            if (inactivityTimer == null)
            {
                inactivityTimer = new System.Timers.Timer(GlobalConfiguration.exhibitTimeout);
                inactivityTimer.Elapsed += handleInactivityTimer;
                inactivityTimer.Start();
            }
            else
            {
                destroyIntactivityTimer(true);
            }
        }
        private void destroyIntactivityTimer(bool _callback)
        {
            if (inactivityTimer != null)
            {
                inactivityTimer.Stop();
                inactivityTimer.Elapsed -= handleInactivityTimer;
                inactivityTimer = null;

                if (_callback == true)
                {
                    setupInactivityTimer();
                }
            }
        }
        private void handleInactivityTimer(object sender, ElapsedEventArgs e)
        {

            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                List<int> keysToRemove = new List<int>();

                foreach (KeyValuePair<int, InteractivePlayer> playerSlot in this.players)
                {
                    if (DateTime.Now.Subtract(playerSlot.Value.lastUpdated).TotalMilliseconds > 750)
                    {
                        keysToRemove.Add(playerSlot.Key);

                        this.players[playerSlot.Key].playerCursors[ApplicationModel.CURSOR_TYPE_L].cursorActivationUpdate += handleControlActivation;
                        this.players[playerSlot.Key].playerCursors[ApplicationModel.CURSOR_TYPE_R].cursorActivationUpdate += handleControlActivation;
                        masterParent.Children.Remove(this.players[playerSlot.Key].playerCursors[ApplicationModel.CURSOR_TYPE_L]);
                        masterParent.Children.Remove(this.players[playerSlot.Key].playerCursors[ApplicationModel.CURSOR_TYPE_R]);
                    }
                }

                foreach (int toRemoveKey in keysToRemove) // Loop through List with foreach
                {
                    this.players.Remove(toRemoveKey);
                }

                if (this.players.Count == 0)
                {
                    //Show the attract - if not disabled
                    if (_disableBackToAttract == false)
                    {
                        if (sec_attract == null)
                        {
                            destroyAllSections();
                            createAttractSection();
                            ChangeContent(sec_attract);
                        }
                        else
                        {
                            if (sec_attract != null)
                            {

                                if (sec_attract.initialYNPrompt.Opacity != 0)
                                {
                                    sec_attract.hideShowInitialQuestion(false);
                                }

                            }
                        }
                    }
                }

            }));

            destroyIntactivityTimer(true);
        }
        private void destroyAllSections()
        {
            destroyPhotoSection(false);
            destroyAttractSection(false);
        }
        private void ChangeContent(UIElement newContent)
        {
            if (transitionHolder.Children.Count == 0)
            {
                transitionHolder.Children.Add(newContent);
                return;
            }

            if (transitionHolder.Children.Count == 1)
            {
                transitionHolder.IsHitTestVisible = false;
                UIElement oldContent = transitionHolder.Children[0];

                // An anoynmous function....interesting
                EventHandler onAnimationCompletedHandler = delegate(object sender, EventArgs e)
                {
                    transitionHolder.IsHitTestVisible = true;
                    transitionHolder.Children.Remove(oldContent);

                    // All these have destroyInternals on them ideally would have extended
                    // a common class to not have to do all this casting
                    try
                    {
                        SectionAttract att = oldContent as SectionAttract;
                        if (att != null)
                        {
                            destroyAttractSection(false);
                        }
                        SectionPhoto phot = oldContent as SectionPhoto;
                        if (phot != null)
                        {
                            destroyPhotoSection(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Who cares
                        Console.WriteLine("Animation Helper Exception destroying internals on one of the main sections - " + ex.ToString());
                    }
                };

                // ---- SLide animation -----
                double leftStart = Canvas.GetLeft(oldContent);
                Canvas.SetLeft(newContent, leftStart - GlobalConfiguration.currentScreenW);

                transitionHolder.Children.Add(newContent);

                if (double.IsNaN(leftStart))
                {
                    leftStart = 0;
                }

                // -----> WHERE IS WIDTH COMING FROM??? THE WINDOW??
                DoubleAnimation outAnimation = new DoubleAnimation(leftStart, leftStart + GlobalConfiguration.currentScreenW, new Duration(TimeSpan.FromSeconds(0.5)));
                DoubleAnimation inAnimation = new DoubleAnimation(leftStart - GlobalConfiguration.currentScreenW, leftStart, new Duration(TimeSpan.FromSeconds(0.5)));
                inAnimation.Completed += onAnimationCompletedHandler;

                // Easing
                SineEase ease = new SineEase();
                ease.EasingMode = EasingMode.EaseOut;
                outAnimation.EasingFunction = ease;
                inAnimation.EasingFunction = ease;
                oldContent.BeginAnimation(Canvas.LeftProperty, outAnimation);
                newContent.BeginAnimation(Canvas.LeftProperty, inAnimation);
            }
        }
        #endregion

        #region CMS
        private void refreshCMSConfig()
        {
            createCMSLink();
            _cms.pullApplicationConfiguration();
            destroyCMSLink(false);
        }
        private void createCMSLink()
        {
            if (_cms == null)
            {
                _cms = new ExhibitDrupalDataManager();
                _cms.initializeDrupal(Properties.Settings.Default.drupalURL, Properties.Settings.Default.drupalServiceEndpoint, Properties.Settings.Default.drupalServiceUser, Properties.Settings.Default.drupalServicePassword);
            }
            else
            {
                destroyCMSLink(true);
            }
        }
        private void destroyCMSLink(bool _callback)
        {
            if (_cms != null)
            {
                _cms.Close();
                _cms = null;

                if (_callback == true)
                {
                    createCMSLink();
                }
            }
        }
        #endregion

        #region dynamic chroma key photos
        /// <summary>
        /// This uses a background worker so it wont block. Will suck up some memory though
        /// as it loads all the photos into memory.
        /// </summary>
        private void createChromaPhotoLoad()
        {
            if (_chromaImageLoader == null)
            {
                _chromaImageLoader = new AsyncPhotoRetrieval(ApplicationModel.ASYNC_MODE_RETRIEVE_CHROMA_KEY_MEDIA);
                _chromaImageLoader.photoLoadProgress += handleChromaPhotoLoadComplete;
                _chromaImageLoader.init();
            }
        }
        private void destroyChromaPhotoLoad()
        {
            if (_chromaImageLoader != null)
            {
                _chromaImageLoader.photoLoadProgress -= handleChromaPhotoLoadComplete;
                _chromaImageLoader.destroyInternals();
                _chromaImageLoader = null;
            }
        }
        private void handleChromaPhotoLoadComplete(object sender, PhotoLoadEventArgs e)
        {
            if (e.finalChromaList != null)
            {
                _primaryChromaImageList = e.finalChromaList;
            }

            // Init the first UI section
            //createPhotoSection();
            createAttractSection();
            transitionHolder.Children.Add(sec_attract);
        }
        #endregion

    }
}
