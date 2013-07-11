/*
* InteractivePlayer.cs
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

namespace KinectGreenScreen.com.transcendingdigital.ui
{
    public class InteractivePlayer
    {

        private int _playerID = -1;
        public bool _isMouse = false;
        public DateTime lastUpdated;

        public float[] playerBounds;

        // Cursors - max of 2
        public readonly Dictionary<string, Cursors> playerCursors = new Dictionary<string, Cursors>();

        public InteractivePlayer(int _playerID, bool _isMouse)
        {
            this._playerID = _playerID;
            this._isMouse = _isMouse;

            setupCursors();
            playerBounds = new float[4];
            // Width and Height
            playerBounds[0] = GlobalConfiguration.playerBoundBoxW;
            playerBounds[1] = GlobalConfiguration.playerBoundBoxH;
            // X and Y
            playerBounds[2] = 0;
            playerBounds[3] = 0;
        }

        private void setupCursors()
        {
            if (this._isMouse == true)
            {
                Cursors mouseLeft = new Cursors();
                mouseLeft.initialize(0, this._playerID);
                playerCursors.Add("L", mouseLeft);
            }
            else
            {
                Cursors mouseLeft = new Cursors();
                mouseLeft.initialize(0, this._playerID);
                playerCursors.Add("L", mouseLeft);
                Cursors mouseRight = new Cursors();
                mouseRight.initialize(1, this._playerID);
                playerCursors.Add("R", mouseRight);

            }

        }
    }
}
