/*
* photoDataObject.cs
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
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace KinectGreenScreen.com.transcendingdigital.data
{
    /// <summary>
    /// Used for pulling lists of images. This is used for
    /// pulling information about submitted users photos in the
    /// attract loop
    /// </summary>
    public class photoDataObject
    {
        public int drupalNodeID { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public string mimeType { get; set; }
        public string fileName { get; set; }
        public int sizeInBytes { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public BitmapImage realThumbnail { get; set; }
        public string thumbnailPath { get; set; }
        public string toLoadPath { get; set; }
        public bool thumbLoaded { get; set; }

        public photoDataObject(string _thumbPath, string _fullSizePath)
        {
            thumbnailPath = _thumbPath;
            toLoadPath = _fullSizePath;
            // This is used to determine whos been loaded
            thumbLoaded = false;
        }
    }
}
