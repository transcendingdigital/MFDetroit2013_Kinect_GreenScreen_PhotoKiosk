/*
* ThreadedPhotoSubmission.cs
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
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.IO;

namespace KinectGreenScreen.com.transcendingdigital.data
{
    /// <summary>
    /// Used to submit photos to the filesystem or a Drupal 7 CMS. Most people
    /// will not be using Drupal, but the real deployment did.
    /// 
    /// The project that proceeded this one but was not publicly released used
    /// a thread here, but we arent doing anything too complex, just linear
    /// actions that block. A backgroundworker will make things much easier
    /// here.
    /// </summary>
    public class ThreadedPhotoSubmission : DispatcherObject
    {
        // Event
        public event EventHandler ImageSubmissionComplete;

        private ExhibitDrupalDataManager _cms;
        private BackgroundWorker _imageSubmissionBGWorker;
        private WriteableBitmap _workingImageCopy;

        public ThreadedPhotoSubmission()
        {

        }

        public void destroyInternals()
        {
            destroyBGWorker();
        }

        public void encodeAndSubmitUserPhotoToCMS(WriteableBitmap _imageBMP)
        {
            createBGWorker();

            // The currently rendered BMP sits on the main thread but its a
            // copy already anyway.
            _workingImageCopy = _imageBMP;
            _workingImageCopy.Freeze();

            if (_imageSubmissionBGWorker.IsBusy != true)
            {
                // Start the asynchronous operation.
                _imageSubmissionBGWorker.RunWorkerAsync();
            }
        }

        private void doEncodeAndSubmitInThread(WriteableBitmap _imageBMP)
        {
            // -- NOW IN imageSubmissionThread ----------------
            string _fileNameAppend = string.Format("MakerPhoto{0:MM-dd-yyyy_hh_mm_ffftt}_EST", DateTime.Now);
            // create a png bitmap encoder which knows how to save a .png file
            BitmapEncoder encoder = new JpegBitmapEncoder();
            // create frame from the writable bitmap and add to encoder
            encoder.Frames.Add(BitmapFrame.Create(_imageBMP));
            string saveDirAndName = System.AppDomain.CurrentDomain.BaseDirectory + "localFiles\\submittedPhotos\\" + _fileNameAppend + ".jpg";
            MemoryStream memStream = new MemoryStream();
            encoder.Save(memStream);
            // write the new file to disk
            try
            {
                using (FileStream fs = new FileStream(saveDirAndName, FileMode.Create))
                {
                    memStream.WriteTo(fs);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failure writing user submitted photo: " + e.Message);
            }
            byte[] encodedJpegBytes = memStream.GetBuffer();
            memStream.Dispose();
            encoder = null;
            createCMSLink();
            bool publishOrNot = false;
            if (GlobalConfiguration.automaticImageSubmission == 1)
            {
                publishOrNot = true;
            }
            // This blocks
            _cms.submitUserJpeg(ref encodedJpegBytes, _fileNameAppend, _fileNameAppend, _fileNameAppend, _imageBMP.PixelWidth, _imageBMP.PixelHeight, publishOrNot);
            // logout
            destroyCMSLink(false);

            _imageBMP = null;
            encodedJpegBytes = null;

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

        #region BackgroundWorker Items
        private void createBGWorker()
        {
            if (_imageSubmissionBGWorker == null)
            {
                // Will use DispatcherSynchronizationContext
                _imageSubmissionBGWorker = new BackgroundWorker();
                _imageSubmissionBGWorker.WorkerReportsProgress = true;
                _imageSubmissionBGWorker.WorkerSupportsCancellation = true;

                // Wire up event handlers
                _imageSubmissionBGWorker.DoWork += backgroundWorker_DoWork;
                _imageSubmissionBGWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
                _imageSubmissionBGWorker.ProgressChanged += backgroundWorker_ProgressChanged;
            }
        }
        private void destroyBGWorker()
        {
            if (_imageSubmissionBGWorker != null)
            {
                _imageSubmissionBGWorker.DoWork -= backgroundWorker_DoWork;
                _imageSubmissionBGWorker.RunWorkerCompleted -= backgroundWorker_RunWorkerCompleted;
                _imageSubmissionBGWorker.ProgressChanged -= backgroundWorker_ProgressChanged;

                if (_imageSubmissionBGWorker.IsBusy == true)
                {
                    // Canel any in action work
                    _imageSubmissionBGWorker.CancelAsync();
                    // This will cause _backgroundWorker_RunWorkerCompleted to be completed
                }

                _imageSubmissionBGWorker = null;
            }
        }

        void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            destroyBGWorker();

            // Dispatch all is well that ends well
            if (ImageSubmissionComplete != null)
            {
                EventArgs evt = new EventArgs();
                ImageSubmissionComplete(this, evt);
            }

        }
        void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            doEncodeAndSubmitInThread(_workingImageCopy);
        }
        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }
        #endregion
    }
}
