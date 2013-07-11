﻿/*
* ApplicationModel.cs
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
    /// Used for the cursors and AsyncPhotoRetrieval mode consts
    /// </summary>
    class ApplicationModel
    {
        // Possible cursors
        public const string CURSOR_TYPE_L = "L";
        public const string CURSOR_TYPE_R = "R";

        // Possible AsyncPhotoRetrieval Modes
        public const string ASYNC_MODE_RETRIEVE_SUBMITTED_MEDIA = "ASYNC_MODE_RETRIEVE_SUBMITTED_MEDIA";
        public const string ASYNC_MODE_RETRIEVE_CHROMA_KEY_MEDIA = "ASYNC_MODE_RETRIEVE_CHROMA_KEY_MEDIA";
    }
}
