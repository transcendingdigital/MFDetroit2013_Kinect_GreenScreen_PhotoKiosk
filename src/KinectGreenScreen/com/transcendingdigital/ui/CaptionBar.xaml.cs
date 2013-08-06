/*
* CaptionBar.xaml.cs
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
using System.Xml;
using System.IO;
using System.Timers;
using System.Windows.Media.Animation;
using KinectGreenScreen.com.transcendingdigital.data;

namespace KinectGreenScreen.com.transcendingdigital.ui
{
    /// <summary>
    /// Interaction logic for CaptionBar.xaml
    /// The caption bar also handles playing audio captions for the exhibit.
    /// Other classes dispatch to Main which in turn calls into this.
    /// </summary>
    public partial class CaptionBar : UserControl
    {
        private MediaPlayer _captionPlayer;
        private Timer _captionTimer;
        private string _currentKey = "";
        private int _currentCaptionIndex = -1;
        private Dictionary<string, captionData> loadedCaptions;
        private string defaultText = "Take your picture and E-mail it home!";
        private DoubleAnimation outAnimation;
        private DoubleAnimation inAnimation;
        private List<string> commandQueue;

        private bool xmlLoaded = false;

        public CaptionBar()
        {
            InitializeComponent();

            // Setup the double animations
            outAnimation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(0.3)));
            outAnimation.Completed += handleOutAnimDone;
            inAnimation = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.3)));

            // Setup the queue list
            commandQueue = new List<string>();
        }

        public void loadNewCaptions(string _WhatOne, bool _queue)
        {
            if (xmlLoaded == true)
            {
                if (_queue == false)
                {
                    // Empty anything possibly in the command queue
                    commandQueue.Clear();
                    commandQueue.Add(_WhatOne);
                    playNewCaption();
                }
                else
                {
                    commandQueue.Add(_WhatOne);
                    // Check to see if something is currently playing
                    if (_captionPlayer == null)
                    {
                        playNewCaption();
                    }
                }
            }
        }

        public void destroyInternals()
        {
            if (outAnimation != null)
            {
                outAnimation.Completed -= handleOutAnimDone;
                outAnimation = null;
            }
            if (inAnimation != null)
            {
                inAnimation = null;
            }

            destroyPlayerAndTimer();
        }

        private void playNewCaption()
        {
            destroyPlayerAndTimer();

            if (commandQueue.Count > 0)
            {
                bool keyPresent = false;
                try
                {
                    _currentKey = commandQueue[0];
                    commandQueue.RemoveAt(0);
                }
                catch (Exception argEx)
                {
                    System.Diagnostics.Debug.WriteLine("Out of range exception on captions: " + argEx.Message);
                }

                try
                {
                    if (loadedCaptions.ContainsKey(_currentKey) )
                    {
                        keyPresent = true;
                    }
                } catch(ArgumentNullException exNull) {
                    System.Diagnostics.Debug.WriteLine("Captions do not contain key: " + _currentKey + " exception: " + exNull);
                }

                if (keyPresent == true)
                {
                    _captionPlayer = new MediaPlayer();
                    _captionPlayer.MediaEnded += handleSoundDone;
                    _captionPlayer.MediaOpened += handleSoundLoaded;
                    _captionPlayer.MediaFailed += handleSoundError;
                    _captionPlayer.Open(new Uri("pack://siteOfOrigin:,,,/localFiles/audio/" + loadedCaptions[_currentKey].audioFile));
                    _captionPlayer.Play();

                   
                        // Get our little timer going to advance the captions
                    _captionTimer = new Timer(500);
                    _captionTimer.Elapsed += handleCaptionTime;
                    _captionTimer.Start();
                    //loadedCaptions[_WhatOne.value].audioFile

                    // Set the intial caption
                    try
                    {
                        captions.Text = loadedCaptions[_currentKey].captionContent[0];
                        _currentCaptionIndex = 0;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("No caption for spot 0 " + e.ToString());
                    }
                }
            }
            else
            {
                // Set the text back to default
                captions.Text = defaultText;
                // Hide the text
                //DoubleAnimation hideAnim = new DoubleAnimation(1,0,new Duration(TimeSpan.FromSeconds(0.3)));
                //captions.BeginAnimation(TextBlock.OpacityProperty, hideAnim);
            }
        }

        /// <summary>
        /// Callback from the out animation to signify things are
        /// hidden so the text can be changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void handleOutAnimDone(object sender, EventArgs e)
        {
            // Change the text
            try
            {
                if (loadedCaptions[_currentKey].captionContent.Count <= _currentCaptionIndex)
                {
                    captions.Text = loadedCaptions[_currentKey].captionContent[_currentCaptionIndex];
                }
                // Bring it back in
                captions.BeginAnimation(TextBlock.OpacityProperty, inAnimation);
            }
            catch (Exception)
            {

            }

        }

        private void populateCaptionGuide()
        {
            XmlTextReader xml = new XmlTextReader(@"localFiles\xml\audioCaptions.xml");
            loadedCaptions = new Dictionary<string, captionData>();
            captionData newCaption = new captionData();
            int currentCaptionKey = 0;

            try
            {
                while (xml.Read())
                {
                    switch (xml.NodeType)
                    {
                        case XmlNodeType.Element: // The node is an element.
                            if (xml.Name == "captionSegment")
                            {
                                newCaption = null;
                                newCaption = new captionData();

                                // Get the attributes
                                while (xml.MoveToNextAttribute())
                                {
                                    if (xml.Name == "key")
                                    {
                                        newCaption.segmentName = xml.Value;
                                    }
                                    if (xml.Name == "file")
                                    {
                                        newCaption.audioFile = xml.Value;
                                    }
                                }
                            }
                            else if (xml.Name == "entry")
                            {
                                // Get the attributes
                                while (xml.MoveToNextAttribute())
                                {
                                    if (xml.Name == "timeInSec")
                                    {
                                        currentCaptionKey = Convert.ToInt32(xml.Value.ToString());
                                        if (newCaption != null)
                                        {
                                            newCaption.captionContent.Add(currentCaptionKey, "");
                                        }
                                    }
                                }
                            }

                            break;
                        case XmlNodeType.CDATA: //Display the text in each element.
                            newCaption.captionContent[currentCaptionKey] = xml.Value;
                            break;
                        case XmlNodeType.EndElement: //Display the end of the element.
                            if (xml.Name == "captionSegment")
                            {
                                loadedCaptions.Add(newCaption.segmentName, newCaption);
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception parsing result xml: {0}", e.ToString());
            }

            // Some of the classes try to load captions
            // before all the xml is loaded
            xmlLoaded = true;
        }

        private void destroyPlayerAndTimer()
        {
            if (_captionTimer != null)
            {
                _captionTimer.Stop();
                _captionTimer.Elapsed -= handleCaptionTime;
                _captionTimer.Close();
                _captionTimer = null;
            }
            if (_captionPlayer != null)
            {
                _captionPlayer.Stop();
                _captionPlayer.MediaEnded -= handleSoundDone;
                _captionPlayer.MediaOpened -= handleSoundLoaded;
                _captionPlayer.MediaFailed -= handleSoundError;
                _captionPlayer.Close();
                _captionPlayer = null;
            }

        }

        private void handleCaptionTime(object sender, ElapsedEventArgs e)
        {
            // This is in a different thread, so we need to use Dispatcher to update the caption
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                if (_captionPlayer != null)
                {
                    if (loadedCaptions[_currentKey].captionContent.ContainsKey(_captionPlayer.Position.Seconds))
                    {
                        if (_captionPlayer.Position.Seconds != _currentCaptionIndex)
                        {
                            _currentCaptionIndex = _captionPlayer.Position.Seconds;
                            // Transition the text off - stopping anything prior just in case
                            captions.BeginAnimation(TextBlock.OpacityProperty, null);
                            captions.BeginAnimation(TextBlock.OpacityProperty, outAnimation);
                            //captions.Text = loadedCaptions[_currentKey].captionContent[_captionPlayer.Position.Seconds];
                        }

                    }
                }
            }));
        }

        private void handleSoundDone(object sender, EventArgs e)
        {
            destroyPlayerAndTimer();
            // Check the queue

            // Play the next caption
            playNewCaption();
        }

        private void handleSoundLoaded(object sender, EventArgs e)
        {

        }

        private void handleSoundError(object sender, EventArgs e)
        {
            destroyPlayerAndTimer();
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            populateCaptionGuide();
            defaultText = Properties.Settings.Default.defaultCaptionText;
            captions.Text = defaultText;
        }
    }
}
