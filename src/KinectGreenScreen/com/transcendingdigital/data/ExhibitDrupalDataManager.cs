/*
* ExhibitDrupalDataManager.cs
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
using System.Xml;
using System.IO;

namespace KinectGreenScreen.com.transcendingdigital.data
{
    /// <summary>
    /// This class provides a specific TO THIS EXHIBIT implementation
    /// needed for parsing custom Drupal content types and views.
    /// </summary>
    public class ExhibitDrupalDataManager
    {
        // Manages the connection to drupal & calls against it
        private DrupalBridge _db = null;
        // Holds the most recent list of image data
        private List<photoDataObject> _ImageData = null;
        // Holds the most recent list of featured media data
        private List<photoDataObject> _FeaturedMediaData = null;

        public ExhibitDrupalDataManager()
        {

        }

        // This is returning a copy
        public List<photoDataObject> getLatestImageData()
        {
            return _ImageData;
        }

        public List<photoDataObject> getLatestFeaturedMediaData()
        {
            return _FeaturedMediaData;
        }

        public void initializeDrupal(string _serverUrl, string _serviceName, string _drupalUserName, string _drupalUserPW)
        {
            if (_db == null)
            {
                _db = new DrupalBridge(_serverUrl, _serviceName, _drupalUserName, _drupalUserPW);
                // Listen for events
                _db.NewDrupalViewData += handleNewViewData;
                // We should only login if we are actually using the CMS
                if (GlobalConfiguration.useDrupal7CMS == 1)
                {
                    _db.login();
                }
            }
        }

        /// <summary>
        /// Should be called before discarding the class. Ensures
        /// all events are removed and everything internally is cleaned up.
        /// </summary>
        public void Close()
        {
            if (_db != null)
            {
                _db.logout();
                // Remove the event listener
                _db.NewDrupalViewData -= handleNewViewData;
            }
        }

        public void submitUserJpeg(ref byte[] _JpegEncodedData, string _contentTitle, string _contentBody, string _altText, int _width, int _height, bool _publish)
        {
            if (_db != null)
            {
                // Random file name
                Guid photoID = System.Guid.NewGuid();
                String photolocation = photoID.ToString() + ".jpg";
                _db.submitFileB64(ref _JpegEncodedData, photolocation, "image/jpeg");
                _db.submitContentTypeUsingPreviousFID(_contentTitle, _contentBody, _altText, _width, _height, _publish);
            }
        }

        /// <summary>
        /// Will re-pull all of the data needed for 
        /// the application configuration.
        /// </summary>
        public void pullApplicationConfiguration()
        {
            if (_db != null)
            {
                _db.getNode(Properties.Settings.Default.drupalConfigurationNode);
            }
        }

        // This should potentially be done async
        public void pullAllSubmittedImages()
        {
            if (_db != null)
            {
                if (GlobalConfiguration.drupalUserSubmittedImageViewName != null)
                {
                    _db.getNamedView(GlobalConfiguration.drupalUserSubmittedImageViewName);
                }
            }
        }

        /// <summary>
        /// Uses a secondary view that only pulls the most recent fifty
        /// </summary>
        public void pullRecentlySubmittedImages()
        {
            if (_db != null)
            {
                if (GlobalConfiguration.drupalUserSubmittedImageViewName != null)
                {
                    _db.getNamedView(GlobalConfiguration.drupalUserSubmittedImageViewName, 50);
                }
            }
        }

        /// <summary>
        /// Event called once drupal returns a list of paths for
        /// images. We get raw string output from the server here
        /// so its up to us to parse the xml ourselves here on
        /// an application specific basis.
        /// 
        /// Our objective here is to extract a list of absolute images paths
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void handleNewViewData(object sender, drupalEventArgs args)
        {
            // args._responseType will be comma delimited for views

            System.Diagnostics.Debug.WriteLine("ExhibitDrupalDataManager - handleNewViewData: ");
            // Create an XmlReader
            using (XmlReader reader = XmlReader.Create(new StringReader(args._theMessage)))
            {
                try
                {

                    if (args._responseType == DrupalBridge.METHOD_NODE_RETRIEVE)
                    {
                        /**
                         * Here is what the response in xml will be like
                         * <result><vid>1921</vid><uid>1</uid><title>Green Screen App Config</title><log></log>
                         * <status>1</status><comment>0</comment><promote>0</promote>
                         * <sticky>0</sticky><ds_switch></ds_switch>
                         * <nid>1921</nid><type>green_screen_configuration_type</type>
                         * <language>und</language><created>1369013915</created>
                         * <changed>1373471644</changed><tnid>0</tnid><translate>0</translate>
                         * <revision_timestamp>1373471644</revision_timestamp>
                         * <revision_uid>1</revision_uid><field_usr_view_name>
                         * <und is_array="true"><item><value>submitted_green_screen_images</value><format/>
                         * <safe_value>submitted_green_screen_images</safe_value></item></und></field_usr_view_name>
                         * <field_cursor_ms_activation><und is_array="true"><item><value>500</value></item>
                         * </und></field_cursor_ms_activation>
                         * <field_ms_timeout><und is_array="true"><item><value>2000</value></item></und></field_ms_timeout>
                         * <field_sub_img_ct_name><und is_array="true"><item><value>green_screen_image</value><format/><safe_value>green_screen_image</safe_value></item></und></field_sub_img_ct_name>
                         * <field_kinect_depth_threshold_flt><und is_array="true"><item><value>2</value></item></und></field_kinect_depth_threshold_flt>
                         * <field_kinect_gs_threshold_flt><und is_array="true"><item><value>2</value></item></und></field_kinect_gs_threshold_flt>
                         * <rdf_mapping><rdftype is_array="true"><item>sioc:Item</item><item>foaf:Document</item></rdftype><title><predicates is_array="true">
                         * <item>dc:title</item></predicates></title><created><predicates is_array="true"><item>dc:date</item>
                         * <item>dc:created</item></predicates><datatype>xsd:dateTime</datatype><callback>date_iso8601</callback></created>
                         * <changed><predicates is_array="true"><item>dc:modified</item></predicates><datatype>xsd:dateTime</datatype><callback>date_iso8601</callback></changed>
                         * <body><predicates is_array="true"><item>content:encoded</item></predicates></body><uid><predicates is_array="true"><item>sioc:has_creator</item></predicates>
                         * <type>rel</type></uid><name><predicates is_array="true"><item>foaf:name</item></predicates></name><comment_count>
                         * <predicates is_array="true"><item>sioc:num_replies</item></predicates><datatype>xsd:integer</datatype></comment_count>
                         * <last_activity><predicates is_array="true"><item>sioc:last_activity_date</item></predicates><datatype>xsd:dateTime</datatype>
                         * <callback>date_iso8601</callback></last_activity></rdf_mapping><name>mfDetroitFun</name><picture>0</picture>
                         * <data>a:2:{s:7:"contact";i:0;s:7:"overlay";i:1;}</data><path>http://192.168.0.21/drupal-7.8/node/1921</path></result>
                         */
                        reader.ReadToFollowing("title");
                        GlobalConfiguration.title = reader.ReadElementContentAsString();

                        reader.ReadToFollowing("field_usr_view_name");
                        reader.ReadToFollowing("value");
                        GlobalConfiguration.drupalUserSubmittedImageViewName = reader.ReadElementContentAsString();

                        reader.ReadToFollowing("field_cursor_ms_activation");
                        reader.ReadToFollowing("value");
                        GlobalConfiguration.cursorGeneralActivationMS = reader.ReadElementContentAsInt();

                        reader.ReadToFollowing("field_ms_timeout");
                        reader.ReadToFollowing("value");
                        GlobalConfiguration.exhibitTimeout = reader.ReadElementContentAsInt();

                        reader.ReadToFollowing("field_sub_img_ct_name");
                        reader.ReadToFollowing("value");
                        GlobalConfiguration.drupalImageSubmissionContentType = reader.ReadElementContentAsString();

                        reader.ReadToFollowing("field_kinect_depth_threshold_flt");
                        reader.ReadToFollowing("value");
                        GlobalConfiguration.kinectDepthThresholdMeters = reader.ReadElementContentAsFloat();

                        reader.ReadToFollowing("field_kinect_gs_threshold_flt");
                        reader.ReadToFollowing("value");
                        GlobalConfiguration.kinectGreenScreenThresholdMeters = reader.ReadElementContentAsFloat();
                    }
                    else if ((args._responseType == DrupalBridge.METHOD_VIEW_RETRIEVE + "," + GlobalConfiguration.drupalUserSubmittedImageViewName))
                    {
                        /**
                         * <result is_array="true">
                         * <item>
                         * <vid>3</vid>
                         * <uid>1</uid>
                         * <title>Second submitted image</title>
                         * <log/>
                         * <status>1</status><comment>0</comment><promote>0</promote><sticky>0</sticky>
                         * <ds_switch/><nid>3</nid><type>maker_faire_image</type><language>und</language>
                         * <created>1335456209</created>
                         * <changed>1335457127</changed><tnid>0</tnid><translate>0</translate>
                         * <revision_timestamp>1335457127</revision_timestamp><revision_uid>1</revision_uid>
                         * <body><und is_array="true"><item><value>Here is the next submitted image.</value><summary/><format>filtered_html</format>
                         * <safe_value><p>Here is the next submitted image.</p></safe_value>
                         * <safe_summary/>
                         * </item></und></body>
                         * <field_submitted_image>
                         *      <und is_array="true">
                         *      <item><fid>90</fid>
                         *      <alt>Here is the next submitted image</alt>
                         *      <title/>
                         *      <width>1280</width>
                         *      <height>853</height>
                         *      <uid>1</uid>
                         *      <filename>IMG_2584.JPG</filename>
                         *      <uri>public://makerFaireImages/IMG_2584.JPG</uri>
                         *      <filemime>image/jpeg</filemime>
                         *      <filesize>233785</filesize><status>1</status>
                         *      <timestamp>1335456209</timestamp>
                         *      <rdf_mapping/>
                         *      </item></und>
                         *    </field_submitted_image>
                         *      <rdf_mapping>
                         *      <rdftype is_array="true">
                         *      <item>sioc:Item</item>
                         *      <item>foaf:Document</item>
                         *      </rdftype>
                         *      <title><predicates is_array="true"><item>dc:title</item></predicates></title>
                         *      <created><predicates is_array="true"><item>dc:date</item>
                         *      <item>dc:created</item></predicates><datatype>xsd:dateTime</datatype>
                         *      <callback>date_iso8601</callback></created>
                         *      <changed><predicates is_array="true"><item>dc:modified</item></predicates>
                         *      <datatype>xsd:dateTime</datatype><callback>date_iso8601</callback></changed>
                         *      <body><predicates is_array="true"><item>content:encoded</item></predicates></body>
                         *      <uid><predicates is_array="true"><item>sioc:has_creator</item></predicates><type>rel</type></uid>
                         *      <name><predicates is_array="true"><item>foaf:name</item></predicates></name>
                         *      <comment_count><predicates is_array="true"><item>sioc:num_replies</item></predicates><datatype>xsd:integer</datatype></comment_count>
                         *      <last_activity><predicates is_array="true"><item>sioc:last_activity_date</item></predicates><datatype>xsd:dateTime</datatype><callback>date_iso8601</callback></last_activity>
                         *      </rdf_mapping>
                         *      <name>tranDAdmin</name><picture>0</picture><data>b:0;</data>
                         *      </item>
                         * <item>
                         *      <vid>2</vid><uid>1</uid><title>Manually Submitted Test Image</title>
                         *      <log/>
                         *      <status>1</status><comment>0</comment><promote>0</promote><sticky>0</sticky><ds_switch/>
                         *      <nid>2</nid><type>maker_faire_image</type><language>und</language>
                         *      <created>1335408113</created><changed>1335457366</changed>
                         *      <tnid>0</tnid><translate>0</translate><revision_timestamp>1335457366</revision_timestamp>
                         *      <revision_uid>1</revision_uid><body><und is_array="true">
                         *      <item><value>This is a lovely manual image.</value>
                         *      <summary/>
                         *      <format>filtered_html</format>
                         *      <safe_value><p>This is a lovely manual image.</p></safe_value><safe_summary/>
                         *      </item></und></body>
                         *    <field_submitted_image>
                         *      <und is_array="true">
                         *      <item>
                         *      <fid>89</fid><alt>A manually submitted user image</alt><title/>
                         *      <width>1280</width>
                         *      <height>853</height>
                         *      <uid>1</uid>
                         *      <filename>IMG_2663.JPG</filename>
                         *      <uri>public://makerFaireImages/IMG_2663.JPG</uri>
                         *      <filemime>image/jpeg</filemime>
                         *      <filesize>276556</filesize>
                         *      <status>1</status>
                         *      <timestamp>1335408113</timestamp>
                         *      <rdf_mapping/>
                         *      </item></und>
                         *    </field_submitted_image>
                         *      <rdf_mapping>
                         *      <rdftype is_array="true">
                         *    <item>sioc:Item</item>
                         *    <item>foaf:Document</item>
                         *      </rdftype>
                         *      <title><predicates is_array="true"><item>dc:title</item></predicates></title>
                         *      <created><predicates is_array="true"><item>dc:date</item>
                         *      <item>dc:created</item>
                         *      </predicates><datatype>xsd:dateTime</datatype><callback>date_iso8601</callback>
                         *      </created><changed><predicates is_array="true">
                         *      <item>dc:modified</item></predicates>
                         *      <datatype>xsd:dateTime</datatype><callback>date_iso8601</callback>
                         *      </changed><body><predicates is_array="true">
                         *      <item>content:encoded</item></predicates></body>
                         *      <uid><predicates is_array="true">
                         *      <item>sioc:has_creator</item></predicates>
                         *      <type>rel</type></uid><name><predicates is_array="true">
                         *      <item>foaf:name</item></predicates></name>
                         *      <comment_count><predicates is_array="true">
                         *      <item>sioc:num_replies</item></predicates>
                         *      <datatype>xsd:integer</datatype></comment_count>
                         *      <last_activity><predicates is_array="true"><item>sioc:last_activity_date</item></predicates>
                         *      <datatype>xsd:dateTime</datatype><callback>date_iso8601</callback></last_activity></rdf_mapping>
                         *      <name>tranDAdmin</name><picture>0</picture><data>b:0;</data>
                         *      </item>
                         *      </result>
                         */

                        _ImageData = new List<photoDataObject>();
                        photoDataObject newGuy;
                        // Save a reference of the base server uri
                        string baseServerURI = "";

                        if (_db != null)
                        {
                            baseServerURI = _db.getServerBaseURI();
                        }

                        // Unfortunateley we need to run through everything for this one
                        while (reader.Read())
                        {
                            //System.Diagnostics.Debug.WriteLine("Name: " + reader.Name + " Value: " + reader.Value.ToString() + " depth: " + reader.Depth);
                            if (reader.Name == "title" && reader.Depth == 2 && reader.NodeType == XmlNodeType.Element)
                            {
                                // Create a new ImageObject and throw it on the hash set
                                newGuy = new photoDataObject("", "");
                                _ImageData.Add(newGuy);

                                // Next read will be the value, so advance it inside here
                                reader.Read();
                                newGuy.title = reader.Value.ToString();
                                System.Diagnostics.Debug.WriteLine("Title at depth: {0} value: {1}", reader.Depth, reader.Value.ToString());
                            }
                            if (reader.Name == "nid" && reader.Depth == 2 && reader.NodeType == XmlNodeType.Element)
                            {
                                // Next read will be the value, so advance it inside here
                                reader.Read();
                                _ImageData[_ImageData.Count - 1].drupalNodeID = Convert.ToInt32(reader.Value.ToString());
                                System.Diagnostics.Debug.WriteLine("node id at depth: {0} value: {1}", reader.Depth, reader.Value.ToString());
                            }
                            if (reader.Name == "alt" && reader.Depth == 5 && reader.NodeType == XmlNodeType.Element)
                            {
                                // Next read will be the value, so advance it inside here
                                reader.Read();
                                _ImageData[_ImageData.Count - 1].body = reader.Value.ToString();
                                System.Diagnostics.Debug.WriteLine("alt at depth: {0} value: {1}", reader.Depth, reader.Value.ToString());
                            }
                            if (reader.Name == "width" && reader.Depth == 5 && reader.NodeType == XmlNodeType.Element)
                            {
                                // Next read will be the value, so advance it inside here
                                reader.Read();
                                _ImageData[_ImageData.Count - 1].width = Convert.ToInt32(reader.Value.ToString());
                                System.Diagnostics.Debug.WriteLine("width at depth: {0} value: {1}", reader.Depth, reader.Value.ToString());
                            }
                            if (reader.Name == "height" && reader.Depth == 5 && reader.NodeType == XmlNodeType.Element)
                            {
                                // Next read will be the value, so advance it inside here
                                reader.Read();
                                _ImageData[_ImageData.Count - 1].height = Convert.ToInt32(reader.Value.ToString());
                                System.Diagnostics.Debug.WriteLine("height at depth: {0} value: {1}", reader.Depth, reader.Value.ToString());
                            }
                            if (reader.Name == "uri" && reader.Depth == 5 && reader.NodeType == XmlNodeType.Element)
                            {
                                // Next read will be the value, so advance it inside here
                                reader.Read();
                                _ImageData[_ImageData.Count - 1].thumbnailPath = reader.Value.ToString();
                                _ImageData[_ImageData.Count - 1].thumbnailPath = _ImageData[_ImageData.Count - 1].thumbnailPath.Replace("public://", baseServerURI + "sites/default/files/");
                                _ImageData[_ImageData.Count - 1].toLoadPath = _ImageData[_ImageData.Count - 1].thumbnailPath;
                                // Absolute path will be something like this: public://makerFaireImages/IMG_2663.JPG
                                // we need to make it a REAL uri not some dumb drupal uri

                                System.Diagnostics.Debug.WriteLine("uri at depth: {0} value: {1} nodeType: {2}", reader.Depth, _ImageData[_ImageData.Count - 1].thumbnailPath, reader.NodeType);
                            }
                            if (reader.Name == "filemime" && reader.Depth == 5 && reader.NodeType == XmlNodeType.Element)
                            {
                                // Next read will be the value, so advance it inside here
                                reader.Read();
                                _ImageData[_ImageData.Count - 1].mimeType = reader.Value.ToString();
                                System.Diagnostics.Debug.WriteLine("filemime at depth: {0} value: {1}", reader.Depth, reader.Value.ToString());
                            }
                            if (reader.Name == "filename" && reader.Depth == 5 && reader.NodeType == XmlNodeType.Element)
                            {
                                // Next read will be the value, so advance it inside here
                                reader.Read();
                                _ImageData[_ImageData.Count - 1].fileName = reader.Value.ToString();
                                System.Diagnostics.Debug.WriteLine("filename at depth: {0} value: {1}", reader.Depth, reader.Value.ToString());
                            }
                            if (reader.Name == "filesize" && reader.Depth == 5 && reader.NodeType == XmlNodeType.Element)
                            {
                                // Next read will be the value, so advance it inside here
                                reader.Read();
                                _ImageData[_ImageData.Count - 1].sizeInBytes = Convert.ToInt32(reader.Value.ToString());
                                System.Diagnostics.Debug.WriteLine("filesize at depth: {0} value: {1}", reader.Depth, reader.Value.ToString());
                            }

                        }

                    }

                }
                catch (XmlException e)
                {
                    System.Diagnostics.Debug.WriteLine("Exception parsing drupal result xml: {0}", e.ToString());
                }
            }

        }
    }
}
