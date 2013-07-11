/*
* PhotoLoadEventArgs.cs
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
using KinectGreenScreen.com.transcendingdigital.data;

namespace KinectGreenScreen.com.transcendingdigital.events
{
    public class PhotoLoadEventArgs : EventArgs
    {
        public double progress = 0.0;
        public List<photoDataObject> finalList = null;
        public List<chromaKeyImageData> finalChromaList = null;

        public PhotoLoadEventArgs(double _progress, List<photoDataObject> _loadedInject)
        {
            progress = _progress;
            finalList = _loadedInject;
        }

        public PhotoLoadEventArgs(double _progress, List<chromaKeyImageData> _loadedInject)
        {
            progress = _progress;
            finalChromaList = _loadedInject;
        }
    }
}
