/*
* ApplicationStates.cs
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

namespace KinectGreenScreen.com.transcendingdigital.data
{
    /// <summary>
    /// Mainly just used for the captions
    /// </summary>
    public class ApplicationStates
    {

        public const string STATE_CALLOUT1 = "ButtonStateTesting.ActivityModel.STATE_CALLOUT1";
        public const string STATE_CALLOUT2 = "ButtonStateTesting.ActivityModel.STATE_CALLOUT2";
        public const string STATE_CALLOUT3 = "ButtonStateTesting.ActivityModel.STATE_CALLOUT3";
        public const string STATE_PHOTO_QUESTION = "ButtonStateTesting.ActivityModel.STATE_PHOTO_QUESTION";
        public const string STATE_LOOKINGGOOD = "ButtonStateTesting.ActivityModel.STATE_LOOKINGGOOD";
        public const string STATE_PHOTO_DESTROYED = "ButtonStateTesting.ActivityModel.STATE_PHOTO_DESTROYED";
        public const string STATE_PHOTO_SUBMITTED = "ButtonStateTesting.ActivityModel.STATE_PHOTO_SUBMITTED";
        public const string STATE_POSITION_PHOTO = "ButtonStateTesting.ActivityModel.STATE_POSITION_PHOTO";
        public const string STATE_YES_NOPHOTO = "ButtonStateTesting.ActivityModel.STATE_YES_NOPHOTO";

        public string value = "";
    }
}
