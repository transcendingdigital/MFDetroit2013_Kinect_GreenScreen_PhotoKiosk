using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectGreenScreen.com.transcendingdigital.data
{
    class captionData
    {

        public string segmentName;
        public string audioFile;
        public Dictionary<int, string> captionContent;

        public captionData()
        {
            segmentName = "";
            audioFile = "";
            captionContent = new Dictionary<int, string>();
        }
    }
}
