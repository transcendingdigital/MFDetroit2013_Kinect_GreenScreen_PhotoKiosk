/*
* AsyncPhotoRetrieval.cs
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
using System.Windows.Threading;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using KinectGreenScreen.com.transcendingdigital.events;
using KinectGreenScreen.com.transcendingdigital.data;

namespace KinectGreenScreen.com.transcendingdigital.data
{
    /// <summary>
    /// This class is used for pulling the submitted images information by users from the hard 
    /// drive or CMS. It just pulls things like location info but does not actually download
    /// them.
    /// OR
    /// It is used for loading the images used in the chroma key. In this mode it does
    /// actually load the data in advance too.
    /// </summary>
    public class AsyncPhotoRetrieval : DispatcherObject
    {
        // The event delegate
        public delegate void thumbnailProgressUpdate(object sender, PhotoLoadEventArgs e);
        // The event
        public event thumbnailProgressUpdate photoLoadProgress;


        // Stores most recent loaded photos. This is UI thread safe.
        public List<photoDataObject> loadedPhotoData = null;

        private BackgroundWorker _loadAndRenderBGWorker;

        // Communicating with Drupal if Present
        private ExhibitDrupalDataManager _cms;

        // Mode specification can be found in ApplicationModel.cs
        private string _currentMode = ApplicationModel.ASYNC_MODE_RETRIEVE_SUBMITTED_MEDIA;

        public AsyncPhotoRetrieval(string _whatMode)
        {
            _currentMode = _whatMode;
        }
        public void init()
        {
            createBGWorker();
            if (_loadAndRenderBGWorker.IsBusy != true)
            {
                // Start the asynchronous operation.
                _loadAndRenderBGWorker.RunWorkerAsync();
            }
        }
        public void destroyInternals()
        {
            destroyBGWorker();
            destroyCMSLink(false);
        }
        private void createBGWorker()
        {
            if (_loadAndRenderBGWorker == null)
            {
                // Will use DispatcherSynchronizationContext
                _loadAndRenderBGWorker = new BackgroundWorker();
                _loadAndRenderBGWorker.WorkerReportsProgress = true;
                _loadAndRenderBGWorker.WorkerSupportsCancellation = true;

                // Wire up event handlers
                if (_currentMode == ApplicationModel.ASYNC_MODE_RETRIEVE_CHROMA_KEY_MEDIA)
                {
                    _loadAndRenderBGWorker.DoWork += _backgroundWorker_DoChromaKeyWork;
                }
                else if (_currentMode == ApplicationModel.ASYNC_MODE_RETRIEVE_SUBMITTED_MEDIA) 
                {
                    _loadAndRenderBGWorker.DoWork += _backgroundWorker_DoUserMediaWork;
                }

                _loadAndRenderBGWorker.RunWorkerCompleted += _backgroundWorker_RunWorkerCompleted;
                _loadAndRenderBGWorker.ProgressChanged += backgroundWorker1_ProgressChanged;
            }
        }
        private void destroyBGWorker()
        {
            if (_loadAndRenderBGWorker != null)
            {
                if (_currentMode == ApplicationModel.ASYNC_MODE_RETRIEVE_CHROMA_KEY_MEDIA)
                {
                    _loadAndRenderBGWorker.DoWork -= _backgroundWorker_DoChromaKeyWork;
                }
                else if (_currentMode == ApplicationModel.ASYNC_MODE_RETRIEVE_SUBMITTED_MEDIA)
                {
                    _loadAndRenderBGWorker.DoWork -= _backgroundWorker_DoUserMediaWork;
                }
                _loadAndRenderBGWorker.RunWorkerCompleted -= _backgroundWorker_RunWorkerCompleted;
                _loadAndRenderBGWorker.ProgressChanged -= backgroundWorker1_ProgressChanged;

                if (_loadAndRenderBGWorker.IsBusy == true)
                {
                    // Canel any in action work
                    _loadAndRenderBGWorker.CancelAsync();
                    // This will cause _backgroundWorker_RunWorkerCompleted to be completed
                }

                _loadAndRenderBGWorker = null;
            }
        }

        void _backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Console.WriteLine("BG Worker completed");
        }
        void _backgroundWorker_DoUserMediaWork(object sender, DoWorkEventArgs e)
        {
            loadUserMediaInformation();
        }
        void _backgroundWorker_DoChromaKeyWork(object sender, DoWorkEventArgs e)
        {
            loadChromaKeyPhotos();
        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        #region loadUserMedia
        private void loadUserMediaInformation()
        {
            if (loadedPhotoData == null)
            {
                // If using the CMS - pull from that
                if (Properties.Settings.Default.useCMS == true)
                {
                    // Blocks
                    createCMSLink();
                    _cms.pullRecentlySubmittedImages();
                    loadedPhotoData = _cms.getLatestImageData();
                    destroyCMSLink(false);
                }
                else
                {
                // Not using the CMS - Pull from the hard drive
                    loadedPhotoData = new List<photoDataObject>();
                    string saveDirPath = System.AppDomain.CurrentDomain.BaseDirectory + "localFiles\\submittedPhotos\\";

                    // put files into collection
                    DirectoryInfo info = new DirectoryInfo(saveDirPath);
                    FileSystemInfo[] files = info.GetFileSystemInfos();
                    foreach (FileSystemInfo fi in files)
                    {
                        if (((fi.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden) && (fi.Extension == ".jpg" || fi.Extension == ".JPG" || fi.Extension == ".jpeg" || fi.Extension == ".JPEG" || fi.Extension == ".png" || fi.Extension == ".PNG"))
                            loadedPhotoData.Add(new photoDataObject(fi.FullName.Replace("\\", "\\\\"), fi.FullName.Replace("\\", "\\\\")));
                    }

                }

            }

            // Dispatch so everyone knows we are ready
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                if (photoLoadProgress != null)
                {
                    PhotoLoadEventArgs newArgs = new PhotoLoadEventArgs(1.0, loadedPhotoData);
                    photoLoadProgress(this, newArgs);
                }
            }));

            // ----> END OF THREAD/BG WORKER
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
        /// <summary>
        /// Users are instructed to name files like this:
        /// 1_Foreground.png
        /// 1_Background.png
        /// 
        /// 1_Background.png
        /// 
        /// 2_Background_Greyscale.png
        /// 
        /// 2_Foreground_Greyscale.png
        /// 2_Background_Greyscale.png
        /// 
        /// Assuming they name them right, we assume that below.
        /// </summary>
        private void loadChromaKeyPhotos()
        {

            // Start by pulling everything in the directory
            List<chromaKeyImageData> loadedChromaPhotoData = new List<chromaKeyImageData>();
            string saveDirPath = System.AppDomain.CurrentDomain.BaseDirectory + "localFiles\\backgroundImages\\";

            // put files into collection
            DirectoryInfo info = new DirectoryInfo(saveDirPath);
            FileSystemInfo[] files = info.GetFileSystemInfos();
            // Sort them by name and only use pngs
            var Orderedfiles = files.Where(f => f.Extension == ".png" || f.Extension == ".PNG")
                                    .OrderBy(f => f.Name);

            // Current item we are working on at any given time
            chromaKeyImageData currentChromaKeyItem = null;
            
            int currentLoop = 0;
            foreach (FileSystemInfo fi in Orderedfiles)
            {
                currentLoop += 1;

                    // We actually attempt to load each file here into memory.
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.Proxy = null;  //avoids dynamic proxy discovery delay
                        webClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);
                        try
                        {
                            byte[] imageBytes = null;
                            imageBytes = webClient.DownloadData(fi.FullName);
                            try
                            {
                                MemoryStream imageStream = new MemoryStream(imageBytes);
                                BitmapImage currentImage = new BitmapImage();
                                currentImage.BeginInit();
                                currentImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                                currentImage.CacheOption = BitmapCacheOption.OnLoad;
                                currentImage.StreamSource = imageStream;
                                currentImage.EndInit();
                                currentImage.Freeze();
                                imageStream.Close();

                                // Try and extract the index from the filename as we need it
                                // reguardless
                                string resultIndex = Regex.Match(fi.Name, @"\d+").Value;
                                // If that went well we now need to know where to put this guy
                                // Its a new entry
                                if (currentChromaKeyItem == null)
                                {
                                    currentChromaKeyItem = new chromaKeyImageData();
                                }
                                else
                                {
                                    // An exhisting entry
                                    // Try and extract the number
                                    if (resultIndex != "")
                                    {
                                        // Does it match our current index?
                                        int parsedIndex = int.Parse(resultIndex);
                                        if (parsedIndex != currentChromaKeyItem.Index)
                                        {
                                            // Its a new index - add the old entry to the main list
                                            loadedChromaPhotoData.Add(currentChromaKeyItem);
                                            currentChromaKeyItem = new chromaKeyImageData();
                                        }
                                    }
                                    else
                                    {
                                        // Just going to have to assume its new. Shove the last one on the loaded data
                                        loadedChromaPhotoData.Add(currentChromaKeyItem);
                                        currentChromaKeyItem = new chromaKeyImageData();
                                    }
                                }
                                // Set the index
                                currentChromaKeyItem.Index = int.Parse(resultIndex);
                                // OK now figure out if this is Foreground or Background
                                if (fi.Name.ToUpper().Contains("BACKGROUND"))
                                {
                                    // IST A BACKGROUND
                                    currentChromaKeyItem.BackgroundImageNameOnly = fi.Name;
                                    currentChromaKeyItem.BackgroundImageBM = currentImage;
                                }
                                else
                                {
                                    // ITS A FOREGROUND
                                    currentChromaKeyItem.ForegroundImageNameOnly = fi.Name;
                                    currentChromaKeyItem.ForegroundImageBM = currentImage;
                                }
                                // OK now figure out if it needs greyscale
                                if (fi.Name.ToUpper().Contains("GREYSCALE"))
                                {
                                    currentChromaKeyItem.greyscale = true;
                                }
                                // Check if both a foreground and background are present OR this is the last item- if so add it and set the current null
                                // for the next round ALSO check for the last item
                                if ( (currentChromaKeyItem.ForegroundImageBM != null && currentChromaKeyItem.BackgroundImageBM != null) || (currentLoop == Orderedfiles.Count() ) )
                                {
                                    loadedChromaPhotoData.Add(currentChromaKeyItem);
                                    currentChromaKeyItem = null;
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error loading media during chroma key load: " + e.ToString());
                            }
                        }
                        catch (WebException ex)
                        {
                            //do something to report the exception
                            System.Diagnostics.Debug.WriteLine("Exception Downloading Image : " + ex.ToString());
                        }
                    }
            }

            // Dispatch so everyone knows we are ready
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(delegate()
            {
                if (photoLoadProgress != null)
                {
                    PhotoLoadEventArgs newArgs = new PhotoLoadEventArgs(1.0, loadedChromaPhotoData);
                    photoLoadProgress(this, newArgs);
                }
            }));

            // ----> END OF THREAD/BG WORKER
        }
    }
}
