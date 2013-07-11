/*
* Cursors.xaml.cs
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
using System.Media;
using System.Resources;
using System.Timers;
using System.Windows.Media.Animation;
using KinectGreenScreen.com.transcendingdigital.data;

namespace KinectGreenScreen.com.transcendingdigital.ui
{
    public class customCursorEventArgs : EventArgs
    {
        public DependencyObject activationTarget = null;
        public Thickness activationLocation = new Thickness(0, 0, 0, 0);

        public customCursorEventArgs(DependencyObject _target, Thickness _currentLoc)
        {
            activationTarget = _target;
            activationLocation = _currentLoc;
        }
    }

    /// <summary>
    /// Interaction logic for Cursors.xaml
    /// </summary>
    public partial class Cursors : UserControl
    {
        // The event delegate
        public delegate void onCursorActivation(object sender, customCursorEventArgs e);
        // The event
        public event onCursorActivation cursorActivationUpdate;

        public struct cursorPanCommand
        {
            public bool showL;
            public bool showR;
            public bool showT;
            public bool showB;
            public bool resetAll;
        };

        // FOR MAIN HITTESTING - allows non-activation on
        // unrelated elements
        public bool isActivating = false;

        // For timing activation
        private Timer _activationTimer;

        // 0 for left 1 for right
        private int leftOrRight = -1;
        private int playerIndex = 0;
        private DependencyObject currentHitControlReference = null;

        private readonly SoundPlayer activationSound = new SoundPlayer();

        // ALL the stupid variables for the circular cursor spin RIDICULOUS WPF
        DoubleAnimation size1;
        DoubleAnimation size2;
        DoubleAnimation size3;
        DoubleAnimation size4;

        ScaleTransform scaleTransform1;
        ScaleTransform scaleTransform2;
        ScaleTransform scaleTransform3;
        ScaleTransform scaleTransform4;

        RotateTransform rotateTransform4;
        TransformGroup group4;

        RectangleGeometry clip1;
        RectangleGeometry clip2;
        RectangleGeometry clip3;
        RectangleGeometry clip4;

        public Cursors()
        {
            InitializeComponent();
        }

        public void initialize(int _leftRight, int _playerIndex)
        {
            this.leftOrRight = _leftRight;
            this.playerIndex = _playerIndex;

            // Show the left or right cursor - by default right is visible
            if (this.leftOrRight == 0)
            {
                switch (this.playerIndex)
                {
                    case 0:
                        BaseR0.Visibility = System.Windows.Visibility.Hidden;
                        BaseL0.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 1:
                        BaseR1.Visibility = System.Windows.Visibility.Hidden;
                        BaseL1.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 2:
                        BaseR2.Visibility = System.Windows.Visibility.Hidden;
                        BaseL2.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 3:
                        BaseR3.Visibility = System.Windows.Visibility.Hidden;
                        BaseL3.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 4:
                        BaseR4.Visibility = System.Windows.Visibility.Hidden;
                        BaseL4.Visibility = System.Windows.Visibility.Visible;
                        break;
                    case 5:
                        BaseR5.Visibility = System.Windows.Visibility.Hidden;
                        BaseL5.Visibility = System.Windows.Visibility.Visible;
                        break;

                }

            }
            else if (this.leftOrRight == 1)
            {
                switch (this.playerIndex)
                {
                    case 0:
                        BaseR0.Visibility = System.Windows.Visibility.Visible;
                        BaseL0.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case 1:
                        BaseR1.Visibility = System.Windows.Visibility.Visible;
                        BaseL1.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case 2:
                        BaseR2.Visibility = System.Windows.Visibility.Visible;
                        BaseL2.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case 3:
                        BaseR3.Visibility = System.Windows.Visibility.Visible;
                        BaseL3.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case 4:
                        BaseR4.Visibility = System.Windows.Visibility.Visible;
                        BaseL4.Visibility = System.Windows.Visibility.Hidden;
                        break;
                    case 5:
                        BaseR5.Visibility = System.Windows.Visibility.Visible;
                        BaseL5.Visibility = System.Windows.Visibility.Hidden;
                        break;
                }
            }

            // Set this controls width and height so they arent "Auto"
            // ...this isnt a good practice
            this.Width = 111;
            this.Height = 116;

            this.HorizontalAlignment = HorizontalAlignment.Left;
            this.VerticalAlignment = VerticalAlignment.Top;

            // Setup the activation sound effect
            //System.Reflection.Assembly a = System.Reflection.Assembly.GetEntryAssembly();
            //System.IO.Stream s = a.GetManifestResourceStream("CharlesManualImages.Whoosh1.wav");
            //this.activationSound.Stream = Properties.Resources.Whoosh1;
        }

        // This is continually updated as the cursor moves around the screen. We will commonly get null
        // but if we are over an actual control, then we will get a reference to that control
        public void updateHitReference(ref DependencyObject _hitObject)
        {
            //Console.WriteLine("updateHitRef - " + cActivationThreshold);

            if (_hitObject != null)
            {
                if (_hitObject != currentHitControlReference)
                {
                    isActivating = true;
                    //cActivationThreshold = 0;
                    // Stop and reset the timer
                    if (_activationTimer != null)
                    {
                        _activationTimer.Stop();
                        _activationTimer.Elapsed -= handleTimerActivation;
                        _activationTimer.Close();
                        _activationTimer = null;
                    }

                    // Cancel Sound FX
                    //this.activationSound.Stop();
                    // Cancel Circle Animation

                    // Update the current object reference
                    currentHitControlReference = _hitObject;

                    // Start sound fx
                    //this.activationSound.Play();
                    // start circle animation

                    // Setup the activation timer
                    _activationTimer = new Timer(GlobalConfiguration.cursorGeneralActivationMS);
                    _activationTimer.Elapsed += handleTimerActivation;
                    _activationTimer.Start();

                    doCircularCursorAnimation();
                }
            }
            else
            {
                currentHitControlReference = null;
                //cActivationThreshold = 0;
                if (_activationTimer != null)
                {
                    _activationTimer.Stop();
                    _activationTimer.Elapsed -= handleTimerActivation;
                    _activationTimer.Close();
                    _activationTimer = null;
                }
                // Cancel Sound FX if playing
                //this.activationSound.Stop();
                // Cancel Circle Animation if playing
                destroyCircleTransitionElements();
            }
        }

        public void flushHitReference()
        {
            isActivating = false;

            currentHitControlReference = null;
            //cActivationThreshold = 0;
            if (_activationTimer != null)
            {
                _activationTimer.Stop();
                _activationTimer.Elapsed -= handleTimerActivation;
                _activationTimer.Close();
                _activationTimer = null;
            }
            // Cancel Sound FX if playing
            //this.activationSound.Stop();
            // Cancel Circle Animation if playing
            destroyCircleTransitionElements();
        }

        /// <summary>
        /// These commands tell the cursor how to display
        /// its pan arrows. Its a struct that allows:
        /// 1. Show or hide of all pan arrows
        /// 2. Show or hide of single arrows
        /// 
        /// When this is called, it will be firing in rapid
        /// succession as people are panning.
        /// </summary>
        public void recievePanCommand(cursorPanCommand _command)
        {
            if (_command.showL == true)
            {
                circleContainer.Visibility = System.Windows.Visibility.Hidden;
                arrowContainer.Visibility = System.Windows.Visibility.Visible;
                Lar.Visibility = System.Windows.Visibility.Visible;
            }
            if (_command.showR == true)
            {
                circleContainer.Visibility = System.Windows.Visibility.Hidden;
                arrowContainer.Visibility = System.Windows.Visibility.Visible;
                Rar.Visibility = System.Windows.Visibility.Visible;
            }
            if (_command.showB == true)
            {
                circleContainer.Visibility = System.Windows.Visibility.Hidden;
                arrowContainer.Visibility = System.Windows.Visibility.Visible;
                Bar.Visibility = System.Windows.Visibility.Visible;
            }
            if (_command.showT == true)
            {
                circleContainer.Visibility = System.Windows.Visibility.Hidden;
                arrowContainer.Visibility = System.Windows.Visibility.Visible;
                Tar.Visibility = System.Windows.Visibility.Visible;
            }
            if (_command.resetAll == true)
            {
                arrowContainer.Visibility = System.Windows.Visibility.Hidden;
                circleContainer.Visibility = System.Windows.Visibility.Visible;
                Lar.Visibility = System.Windows.Visibility.Visible;
                Rar.Visibility = System.Windows.Visibility.Visible;
                Tar.Visibility = System.Windows.Visibility.Visible;
                Bar.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void handleTimerActivation(object sender, ElapsedEventArgs e)
        {
            if (_activationTimer != null)
            {
                _activationTimer.Stop();
                _activationTimer.Elapsed -= handleTimerActivation;
                _activationTimer.Close();
                _activationTimer = null;
            }

            // Dispatch and signify control activation!
            Console.WriteLine("**** CONTROL ACTIVATION ******");
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                if (cursorActivationUpdate != null)
                {
                    customCursorEventArgs newCursorData = new customCursorEventArgs(currentHitControlReference, this.Margin);
                    // I guess ScrollUpdate will be null if no one is listening
                    cursorActivationUpdate(this, newCursorData);
                }
            }));
        }

        /// <summary>
        /// Maybe Im just an idiot but this is awfuly hard to do in WPF
        /// </summary>
        private void doCircularCursorAnimation()
        {
            double scaleFactor = 58.0 / 2.0;
            double baseTimeInterval = GlobalConfiguration.cursorGeneralActivationMS / 4.0;

            // Awesome one time instantiation of all this crazy junk
            if (size1 == null)
            {
                size1 = new DoubleAnimation(1, scaleFactor, new Duration(TimeSpan.FromMilliseconds(baseTimeInterval)));
                size2 = new DoubleAnimation(1, scaleFactor, new Duration(TimeSpan.FromMilliseconds(baseTimeInterval)));
                //size2.BeginTime = TimeSpan.FromMilliseconds(baseTimeInterval);
                size3 = new DoubleAnimation(1, scaleFactor, new Duration(TimeSpan.FromMilliseconds(baseTimeInterval)));
                //size3.BeginTime = TimeSpan.FromMilliseconds((baseTimeInterval * 2));
                size4 = new DoubleAnimation(1, scaleFactor, new Duration(TimeSpan.FromMilliseconds(baseTimeInterval)));
                //size4.BeginTime = TimeSpan.FromMilliseconds((baseTimeInterval * 3));

                scaleTransform1 = new ScaleTransform(1, 1, 0, 0);
                scaleTransform2 = new ScaleTransform(1, 1, 58, 0);
                scaleTransform3 = new ScaleTransform(1, 1, 58, 58);
                scaleTransform4 = new ScaleTransform(1, 1, 58, 58);
            }

            destroyCircleTransitionElements();

            scaleTransform1 = new ScaleTransform(1, 1, 0, 0);
            scaleTransform2 = new ScaleTransform(1, 1, 58, 0);
            scaleTransform3 = new ScaleTransform(1, 1, 58, 58);
            scaleTransform4 = new ScaleTransform(1, 1, 58, 58);

            rotateTransform4 = new RotateTransform(180, 58, 87);

            group4 = new TransformGroup();
            group4.Children.Add(rotateTransform4);
            group4.Children.Add(scaleTransform4);

            clip1 = new RectangleGeometry(new Rect(0.0, 0.0, 2.0, 58.0));
            circChunkContainer.Children.Add(clip1);

            clip1.Transform = scaleTransform1;
            size1.Completed += handleSetp1Done;
            scaleTransform1.BeginAnimation(ScaleTransform.ScaleXProperty, size1);
        }
        private void handleSetp1Done(object sender, EventArgs e)
        {
            clip2 = new RectangleGeometry(new Rect(58.0, 0.0, 2.0, 58.0));
            circChunkContainer.Children.Add(clip2);
            clip2.Transform = scaleTransform2;

            size2.Completed += handleSetp2Done;
            scaleTransform2.BeginAnimation(ScaleTransform.ScaleXProperty, size2);

        }
        private void handleSetp2Done(object sender, EventArgs e)
        {
            clip3 = new RectangleGeometry(new Rect(58.0, 58.0, 58.0, 2.0));
            circChunkContainer.Children.Add(clip3);
            clip3.Transform = scaleTransform3;

            size3.Completed += handleSetp3Done;
            scaleTransform3.BeginAnimation(ScaleTransform.ScaleYProperty, size3);

        }
        private void handleSetp3Done(object sender, EventArgs e)
        {
            clip4 = new RectangleGeometry(new Rect(58.0, 58.0, 2.0, 58.0));
            circChunkContainer.Children.Add(clip4);

            clip4.Transform = group4;
            size4.Completed += handleSetp4Done;
            scaleTransform4.BeginAnimation(ScaleTransform.ScaleXProperty, size4);
        }

        private void handleSetp4Done(object sender, EventArgs e)
        {
            destroyCircleTransitionElements();
        }

        private void destroyCircleTransitionElements()
        {
            if (clip1 != null)
            {
                size1.Completed -= handleSetp1Done;
                // In case any are currently running
                scaleTransform1.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                circChunkContainer.Children.Remove(clip1);
                clip1 = null;
            }
            if (clip2 != null)
            {
                size2.Completed -= handleSetp2Done;
                scaleTransform2.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                circChunkContainer.Children.Remove(clip2);
                clip2 = null;
            }
            if (clip3 != null)
            {
                size3.Completed -= handleSetp3Done;
                scaleTransform3.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                circChunkContainer.Children.Remove(clip3);
                clip3 = null;
            }
            if (clip4 != null)
            {
                size4.Completed -= handleSetp4Done;
                scaleTransform4.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                circChunkContainer.Children.Remove(clip4);
                clip4 = null;
            }

        }

    }
}
