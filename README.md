Maker Faire Detroit 2013 Kinect Green Screen Photo Kiosk
==============================

This kiosk application was created for [Maker Faire Detroit 2013](http://www.makerfairedetroit.com/) to accompany two tablet based email kiosks. This project is in no way affiliated with The Henry Ford or Maker Faire.  It was an independent project. The experience will be deployed in the exhibit space on a Dell Optiplex 790, Phillips 42 inch monitor, and the Kinect For Windows sensor.

##### Motivation
The green screen photo/video kiosk is one of the most popular attractions we implement professionally in Museums and Visitor Centers. The Kinect has a long way to go before it can replace the industry standard color based chroma key; but with acceptance of slight key quality degradation, the Kinect serves as a wonderful portable or pop up green screen experience.

The incorporation of the Kinect has been important because event the highest quality commercial touchscreens have a deployment lifetime averaging three years. With the more recent “seated mode” available in the official Kinect SDK, the sensor has opened the door to greater accessibility for users with special needs which is always an important consideration with interactive experiences.

##### Company Website
[Transcending Digital](http://www.transcendingdigital.com)
	
UI Screen Shots
--------------------------------------------------
##### With Emgu CV Features Turned On
![Confirm or deny prompt on the photo provided by Dang Argyle With Emgu CV turned on](https://raw.github.com/transcendingdigital/MFDetroit2013_Kinect_GreenScreen_PhotoKiosk/master/githubImages/MFDetKiosk2.jpg "Confirm or deny prompt on the photo provided by Dang Argyle With Emgu CV turned on")
![Fun with the Ford Rouge plant licensed photo in greyscale with Emgu CV turned on](https://raw.github.com/transcendingdigital/MFDetroit2013_Kinect_GreenScreen_PhotoKiosk/master/githubImages/MFDetKiosk3.jpg "Fun with the Ford Rouge plant licensed photo in greyscale with Emgu CV turned on")
![Attract loop featuring randomly submitted test images](https://raw.github.com/transcendingdigital/MFDetroit2013_Kinect_GreenScreen_PhotoKiosk/master/githubImages/MFDetKiosk1.jpg "Attract loop featuring randomly submitted test images")

##### With Emgu CV Features Turned Off (Still the Postblur Though)
![A licensed image seen with the default Kinect Depth Mask including postblur](https://raw.github.com/transcendingdigital/MFDetroit2013_Kinect_GreenScreen_PhotoKiosk/master/githubImages/MFDetKiosk4_NoEMGU.jpg "A licensed image seen with the default Kinect Depth Mask including postblur")
![A licensed image of Detroit with the default Kinect Depth Mask including postblur](https://raw.github.com/transcendingdigital/MFDetroit2013_Kinect_GreenScreen_PhotoKiosk/master/githubImages/MFDetKiosk5_NoEMGU.jpg "A licensed image of Detroit with the default Kinect Depth Mask including postblur")
	
Kinect Hardware
--------------------------------------------------

You can actually use this application without a Kinect For Windows, the green screen effect just wont work.  When no Kinect is detected on the system by default the mouse becomes player one's left hand. This is useful when programming UI items.

There are two Kinect devices, the Xbox Kinect and the stand alone [Kinect For Windows USB hardware](http://www.microsoft.com/en-us/kinectforwindows/). This experience was built and tested on the Kinect For 
Windows standalone hardware that has improved capability over the Xbox version. A hacked Xbox sensor probably will not work with this application.

About the Green Screen Effect
--------------------------------------------------
It isn't really a defect of the Kinect For Windows sensor, but more of a constraint that the depth data is noisy. It's generated by shining an infrared grid on people, so there are bound to be some issues.  
This project attempts to help smooth out and improve the noisy default data by:

1. Using a blur on the depth data. The most preformant way of doing this after a lot of research was to use the default Blur effect in WPF or another custom shader based blur.
2. Using emgu cv to detect blobs in the depth data, then average the points that make up the blobs along more straight lines as observed in openframeworks.
3. Shifting the entire mask left or right to help with IR shadows.
4. Scale up the mask and underlying image with an offset to get rid of the border of missing depth data inside of the color frame.

General Configuration Options
--------------------------------------------------
There are a LOT of configuration options in this application, but they will hopefully help you tailor the exhibit to fit your environment.  Most configuration is handled in the application config file: 

KinectGreenScreen.exe.config

This file is in an xml format and you can change the settings by modifying the entries found in the value nodes, then saving the modified file.

``` defaultCaptionText ``` - The default text seen at the top of the interface between audible captions.

``` useCMS ``` - If you are using Drupal set this to True, if not leave False.

``` KinectPlayerBoundingBoxW ``` - This is a calibration setting to help reduce the amount a person has to move their hands left and right to control the cursors on the screen.  By default a person would have to run all the way left or right across the screen to move cursors around.  This application creates a virtual bounding box around the player using their head as a center point. If things seem too sensitive, increase this number, if things seem not sensitive enough, decrease this number.

``` drupalURL ``` - If you are going to use a Drupal 7 content management system with this application, this should be the default URL to the Drupal instance.

``` drupalServiceEndpoint ``` - The name of a Drupal services 3 endpoint you have created for the application if using Drupal 7.

``` drupalServiceUser ``` - The account user name of the user that the application will log in to Drupal using. This user should have sufficient privileges to access the custom content types you have set up.

``` drupalServicePassword ``` - The password for the account name specified in drupalServiceUser.

``` drupalConfigurationNode ``` - If you are using Drupal 7, this is the node ID of the node that holds configuration options.

``` kinectSeatedModeEnabled ``` - You can turn the Kinect SDK seated mode on or off by setting this to True or False. In deployments it has been good practice to just keep seated mode on all the time. It seems to pick up standing users well but also will pick up users in a wheel chair or sitting down.

``` kinectDepthLeftOffset ``` - This actually moves the whole depth square area to the left or right.  If you look at the raw data, even when scaled or set identical the Kinect depth data is inside a square inside of the color frame with borders all around it. This exhibit offsets the depth data and scales it in coordination with the color frame to get a full image.

``` kinectPostBlurInt ``` - This is the amount of blur that is applied to the mask edges. It's the mask only, the masked area is not blurred. This helps smooth out the depth sensor jaggies.  Emgu CV is not required. This is a standard wpf Image Blur filter as it was very fast when put up against many other blur techniques. You can set it to 0 for no blur. A value of around 5 seems to work well when using Emgu or not.

``` kinectGreenScreenDepthThreshold ``` - This is the number of meters away that objects are visible within the mask.

``` kinectSensingDepthCutoffMeters ``` - This is the number of meters away from the sensor that the application will detect users. The Kinect can really see out to around six meters, but you may want to shorten that distance in experiences with foot traffic behind them.

``` kinectDepthTopOffset ``` - This configuration option goes along with kinectDepthLeftOffset, it is how much the depth image is shifted upward to compensate for it being scaled while still fitting in the frame.

``` kinectDepthImageScale ``` - The depth mask is scaled using this value then offset using kinectDepthLeftOffset and kinectDepthTopOffset in order to compensate for the bounding box of absent depth data inside a Kinect color frame.

``` photoCountdownSeconds ``` - The number of seconds in the countdown when users take their picture.

``` monochromeEffectContrastMultiplier ``` - Before realizing the Kinect itself could be shifted into greyscale mode using the advanced 1.7 SDK settings, greyscale was implemented using a custom HLSL shader. You can set the contrast on the greyscale effect using this setting.

``` kinectAutoExposure ``` -Set to True if you want the Kinect to automatically adjust the color image settings, False if you want to manually adjust them. If you set this to False, you can then set the kinectManualExposureFrameInterval, and kinectManualExposureTime to control the brightness.

``` kinectAutoExposureBrightness ``` - When kinectAutoExposure is set to True you can use this general value to set the brightness. The Kinect default is 0.2156

``` kinectGain ``` - If you have kinectAutoExposure to True or False this value can still be set as a multiplier to set the gain. The Kinect default is 1 or no gain. You should only increase this if you have no choice as it will degrade image quality.

``` kinectManualExposureFrameInterval ``` - If you have kinectAutoExposure set to False you can use this to set the frame interval, in units of 1/10,000 of a second. The range is [0, 4000]; the default value is 0.

``` kinectManualExposureTime ``` -  If you have kinectAutoExposure set to False you can use this to set the exposure time in increments of 1/10,000 of a second. The range is [1, 4000]; the default value is 1. If you set it to 0 as the 1.7 SDK documentation suggests an exception will be thrown! 

``` kinectAutoWhiteBalance ``` -  Set this to True to have the Kinect auto white balance or False to manually set the white balance.

``` kinectManualWhiteBalanceValue ``` - If you have kinectAutoWhiteBalance set to False you can use this to set the color temperature in degrees Kelvin, the range is 2700 to 6500 the default is 2700.

``` kinectManualHue ``` - You can use this to adjust the hue. The range is -22 to 22 with a default of 0.

``` kinectManualContrast ``` - You can adjust the contrast using this value. The range is 0.5 to 2.0 with a default of 1.0.

``` kinectManualGamma ``` - You can manually set the gamma using this value. Range 1.0 to 2.8 with a default of 2.2.

``` kinectManualSaturation ``` - You can set the saturation in a range of 0.0 to 2.0. The default is 1.0

``` kinectManualSharpness ```  You can manually set the sharpness of the color image. The range is 0 to 1.0 with a default of 0.5.

``` kinectGreenScreenMaskXPixelShift ``` - This is independent of the other shift options. This will actually shift the visible mask left or right to help fix things like IR shadows. It requires emgu CV to work.

``` useEmguCV ``` - Set to True or False. Requires the Emgu CV dll's if you set it to true.  If you do not have the dll's or they are a different version the application will not work with this enabled.

``` drupalSubmissionContentType ``` - If using Drupal, this is the name of a custom content type that has specific fields that the interactive submits to.

``` drupalSubmittedImageViewName ``` - If using Drupal, this is the name of a view that lists the most recent submitted images.

``` exhibitTimeoutMS ``` - The number of milliseconds between checking for players detected by the Kinect. 1000 milliseconds is one second. If no players are found then the application will revert to the main screen.

``` drupalPublishSubmittedMedia ``` - When set to true, content published to Drupal will automatically be published.  In scenerios where you want to review all content before it is publicly published when using Drupal, you can set this to False and publish it once reviewed.

``` exhibitCursorActivationMS ``` - The number of milliseconds it takes a hand cursor to activate a UI item. This is also how fast the circle will animate around the hand cursor.

How to Turn on Enhanced Green Screen Support With Emgu CV
--------------------------------------------------
Emgu CV is a .NET wrapper for the powerful computer vision project Open CV. 
By default the features that incorporate Emgu CV are turned off to reduce the download size and compatibility of the project.

#####Turning on Emgu CV will:
1. Smooth out the green screen mask greatly.
2. Reduce the number of holes created in tough areas like hair
3. Allow you to shift the mask left or right to help align the mask more accurately

This project was put together using Emgu 2.4.9-alpha. (Version 2.4.2 was built to require an Nvidia GPU). You will need Emgu 2.4.9-alpha to match the references included in this project.

Depending on if you have a 32 or 64 bit system, you will need to incorporate the correct version of the Emgu CV dll's. You will also need to set the Visual Studio debug and build options to match 64 or 32 bit platforms.

#####How to turn it on:
1. In the file KinectGreenScreen.exe.config set the useEmguCV setting to True
2. Download libemgucv-windows-universal-gpu-2.4.9.1847.zip from [http://sourceforge.net/projects/emgucv/files/emgucv/2.4.9-alpha/](http://sourceforge.net/projects/emgucv/files/emgucv/2.4.9-alpha/)
3. Unzip the archive, and take a look in the bin directory. If you have a 32 bit system you are interested in the x86 folder, for 64 bit the x64 folder.
4. Copy the following dll (and test exe) files from the /bin/x86 or /bin/x64 folder into the same directory as KinectGreenScreen.exe
5. After copying the files, run cvextern_test.exe by double clicking it to ensure that the emgu cv files will run on your system.

#####Emgu CV File List:
1. cublas64_50_35.dll
2. cudart64_50_35.dll
3. cufft64_50_35.dll
4. cvextern.dll
5. cvextern_test.exe
6. npp64_50_35.dll
7. opencv_calib3d249.dll
8. opencv_contrib249.dll
9. opencv_core249.dll
10. opencv_featues2d249.dll
11. opencv_ffmpeg249_64.dll
12. opencv_flann249.dll
13. opencv_gpu249.dll
14. opencv_highgui249.dll
15. opencv_imgproc249.dll
16. opencv_legacy249.dll
17. opencv_ml249.dll
18. opencv_nonfree249.dll
19. opencv_objdetect249.dll
20. opencv_photo249.dll
21. opencv_stitching249.dll
22. opencv_video249.dll
23. opencv_videostab249.dll

How to Switch Green Screen Foregrounds, Backgrounds, and Greyscale
--------------------------------------------------
You can easily swap out the green screen foregrounds, backgrounds, and toggle greyscale by the names of the images in the /localFiles/backgroundImages. You can also set scenes up with only a background.  Depending on how many files you have named appropriately in /localFiles/backgroundImages the user interface will attempt to center those images in the available area. In 1920x1080 at 90 dpi, about five unique backgrounds fit in the UI.

1. Make sure your image files are in png format with alpha transparency.
2. Label your images in numerical order starting at 1.
3. Follow the number with an underscore “_” specifying “Background” or “Foreground”.
4. If you also want greyscale, follow that with another underscore “_” and they keyword “Greyscale”

Here are a few examples:
#####Example 1 – Three images, number 2 has only a background, number 3 is greyscale.

* 1_Background.png
* 1_Foreground.png
* 2_Background.png
* 3_Background_Greyscale.png
* 3_Foreground_Greyscale.png

#####Example 2 – One image with  a background and foreground
* 1_Background.png
* 1_Foreground.png

#####Example 3 – Four images. One with only a background and greyscale, Two in color with a background only, three color with a foreground and background, four with a background and foreground in greyscale.

* 1_Background_Greyscale.png
* 2_Background.png
* 3_Background.png
* 3_Foreground.png
* 4_Background_Greyscale.png
* 4_Foreground_Greyscale.png

How to Use the Drupal 7 CMS
--------------------------------------------------

By default, this application is configured to operate in a stand alone mode not using a CMS.  The actual deployment used a Drupal 7 CMS for the added capability of quickly reviewing or removing user media without interrupting the exhibit.

You can use options in the configuration settings to enable the use of a Drupal 7 CMS that has been correctly configured to talk to this exhibit.  If you have never used a Drupal CMS, some of the terminology in this section may be confusing.

###### Drupal Module Summary
1. [Services 7.x-3.x](https://drupal.org/project/Services)
2. [Views](https://drupal.org/project/Views)
3. [Services Views](https://drupal.org/project/services_views)

To use Drupal with these projects, you will need to first add the “Services 3” module.  Currently, you will need to use a 7.x-3.3 or lower version. 7.x-3.4 without notice implemented additional special token security requirements which will cause logout, and file creation to fail. (Ironically other aspects work despite sparse services module documentation recommending the token in headers, so this project has held off an implementation.)

Second you need to install the “Views” module. Views is crazy awesome.

Third you should install Services Views.

Now, armed with modules you should go into the module settings and enable Views, Services, Services Views, then the REST, XMLRPC Server, and Views UI. 

The next step is to create a content type for our submitted images.  In Drupal 7 you can do this by navigating to Structure, then Content Types. Select “Add content type”. An important constraint to get your Drupal to work with this application out of the box is that the database field names need to match exactly.  If they don't, it really isn't that big of a deal as you can modify src/KinectGreenScreen/com/transcendingdigital/data/ExhibitDrupalDataManager in the method “handleNewViewData” to correctly parse your fields.

##### Submitted Images Content Type Machine Name

green_screen_image

##### Submitted Images Fields

1. Title – The standard title, you can remove the body
2. field_submitted_image of Field “Image” Widget “Image”

The next step is to create a content type for application configurations that can be set in Drupal. Naming isn't important, but the field values are.  Just like the green_screen_image the exhibit needs the database field names to match.  Here is a screen shot of the field settings:

![Drupal configuration node content type field settings](https://raw.github.com/transcendingdigital/MFDetroit2013_Kinect_GreenScreen_PhotoKiosk/master/githubImages/MFDetKiosk_DrupalConfigNodeSettings.jpg "Drupal configuration node content type field settings")
	
After you have created the content type you should create an actual configuration node with data.  Do this by navigating to Content -> Add Content, then create a configuration type.  After you have created the configuration node page, take note of its id.  You can typically see this in the URL address bar when viewing or editing content.

The next thing we need to do is create a view for the green_screen_image content type. In Drupal terms the view will provide a list of green_screen_image entry's that can be displayed in a million different ways. Views can be displayed within a Drupal website or also using the “Views Services” module you can use views to pull a list of data about our submitted green_screen_image content types.

The piece of information that is important about your view in order to be compatible with this application is it's machine name when you create it.

##### View Machine Name

submitted_green_screen_images

The view settings really don't matter that much, but here is a screen shot of what my view settings look like for pulling the most recent 50 in most recently posted date order.  (Some of the earlier 7.x “Views” and “Views Services” modules would only pull 10 items no matter what settings I used).

![Drupal view settings](https://raw.github.com/transcendingdigital/MFDetroit2013_Kinect_GreenScreen_PhotoKiosk/master/githubImages/MFDetKiosk_DrupalViewsSettings.jpg "Drupal view settings")

##### Setup Services

On to the next in these never ending setup steps, you should navigate to the “Services” settings and configure a services endpoint for the items we setup above. You can get to services through Structure -> Services. 
In services you should select “Add”. Keep note of the “Path to endpoint” that you setup, as the application needs it to be set in the configuration. You should select the “REST” server in the drop down.  Make sure to check “Session authentication”. That tells the service that other apps will use traditional login and cookies to access the data.

Back on the main services screen you should “Edit Resources” on the service you have created.  You should check the following:

1. Views -> retrieve
2. User -> login
3. User -> logout
4. File -> create
5. File -> create_raw
6. node -> retrieve
7. node -> create
8. node -> update
9. node -> files
10. node -> attach_file

Assuming everything has went well so far, the final step in Drupal is to set permissions. You can access permissions from People -> Permissions.  Ensure the user you will have the application log in as has permissions to create and update your green_screen_image content type.

##### Set the application configuration entries

``` useCMS ``` - You should set this to True.

``` drupalURL ``` - Set this to the root URL of your Drupal CMS.

``` drupalServiceEndpoint ``` - Set this to the name of the "Path to Endpoint" that you setup during services configuration.

``` drupalServiceUser ``` - Set this to the account username you want the exhibit to log in as and has privileges for the content types and views we setup above.

``` drupalServicePassword ``` - The password for the account name specified in drupalServiceUser.

``` drupalConfigurationNode ``` - Set this to the node id of the configuration node.

``` drupalSubmissionContentType ``` - Set this to green_screen_image if thats what you set your content type up as.

``` drupalSubmittedImageViewName ``` - Set this to submitted_green_screen_images if thats what you named your view.

``` drupalPublishSubmittedMedia ``` - If you want to review media in Drupal before users can acess it, set this to False otherwise leave it True.

External Libraries Used
--------------------------------------------------
[Kinect For Windows SDK](http://www.microsoft.com/en-us/kinectforwindows/)
	
[EMGU CV](http://www.emgu.com/wiki/index.php/Main_Page)
	
Licenses
--------------------------------------------------

This project is licensed under the freeBSD license.

[Kinect For Windows SDK EULA](http://www.microsoft.com/en-us/kinectforwindows/develop/sdk-eula.aspx)

EMGU CV is licensed under the [GNU Lesser General Public License V3](http://www.gnu.org/licenses/lgpl.txt)