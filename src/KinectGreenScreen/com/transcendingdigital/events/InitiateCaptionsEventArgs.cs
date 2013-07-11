using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectGreenScreen.com.transcendingdigital.events
{
    public class InitiateCaptionsEventArgs : EventArgs
    {
        public string targetStateToInitiate = "";
        public bool queueCaption = false;

        public InitiateCaptionsEventArgs(string _whatState, bool _queueCaption)
        {
            targetStateToInitiate = _whatState;
            queueCaption = _queueCaption;
        }
    }
}
