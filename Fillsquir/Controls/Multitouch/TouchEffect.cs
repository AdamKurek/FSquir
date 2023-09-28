using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TouchTracking;

namespace Fillsquir.Controls.Multitouch
{
    public class TouchEffect : RoutingEffect
    {
        public event TouchActionEventHandler TouchAction;

        public TouchEffect() : base("TouchTracking.TouchEffect")
        {
        }

        public bool Capture { set; get; }

        public void OnTouchAction(object element, TouchActionEventArgs args)
        {
            TouchAction?.Invoke(element, args);
        }
    }
}
