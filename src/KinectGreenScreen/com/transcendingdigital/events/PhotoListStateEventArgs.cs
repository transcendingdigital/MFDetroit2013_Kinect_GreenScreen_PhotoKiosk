using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KinectGreenScreen.com.transcendingdigital.data;

namespace KinectGreenScreen.com.transcendingdigital.events
{
    public class PhotoListStateEventArgs : EventArgs
    {
        public bool newState = false;
        public List<photoDataObject> photoList = null;

        public PhotoListStateEventArgs(bool _newState, List<photoDataObject> _referenceList)
        {
            newState = _newState;
            photoList = _referenceList;
        }
    }
}
