/*
* GlobalConfiguration.cs
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

namespace KinectGreenScreen.com.transcendingdigital.data
{
    /// <summary>
    /// The settings in this file are updated by the CMS if using a CMS.
    /// If not, the default values are pulled out of Settings.settings
    /// when the application starts.
    /// 
    /// </summary>
    public static class GlobalConfiguration
    {

        public static string title = "";

        /// <summary>
        /// In this scenario it turns out the timeout is used to check
        /// more quickly for tracked players.  If there aren't any we can just
        /// jump right out to the attract!
        /// </summary>
        public static int exhibitTimeout = 2000;

        // Drupal 7 CMS Options
        // If not connected to the CMS these are set in
        // code or from properties in MainWindow
        // ---------------------------------
        public static int useDrupal7CMS = 0; // Setting determines if the Drupal CMS is used
        public static string drupalImageSubmissionContentType = "green_screen_image";
        public static string drupalUserSubmittedImageViewName = "submitted_green_screen_images"; // When using drupal, the name of the view users images are submitted to is needed for submission and retrieval
        public static int cursorGeneralActivationMS = 500; // The time required to hold a cursor over a button to activate it. The speed at which the circle goes in milliseconds.
        public static int automaticImageSubmission = 0; // Determines if images are posted to Drupal as "Published" or "Not Published"
        
        //----------------------------------------------
        public static float kinectDepthThresholdMeters = 1.7f;
        public static float kinectGreenScreenThresholdMeters = 1.7f;
        //---------------------------------------------

        // ---------------------------------
        // These all are updated on application start
        public static double currentScreenW = 0;
        public static double currentScreenH = 0;
        public static int currentKinectScreenW = 0;
        public static int currentKinectScreenH = 0;
        public static float playerBoundBoxW = 200;
        public static float playerBoundBoxH = 0;
        //-----------------------------------

        /// <summary>
        /// Set using keyboard input keys. This determines if the application
        /// uses the advanced rendering that requires emgu cv or the default
        /// demonstrated rendering in the Kinect SDK
        /// </summary>
        public static bool useAdvancedGreenScreenRendering = true;
    }
}
