using System;
using Android.Util;

namespace Dibbi
{
    public class Methods
    {
        public static string DurationToString(int duration)
        {
            TimeSpan ts = TimeSpan.FromMilliseconds((double)duration);
            return string.Format("{0} min, {1} sec", ts.Minutes, ts.Seconds);
        }

        public static string DurationToTimeClockString(int duration)
        {
            TimeSpan ts = TimeSpan.FromSeconds((double)duration);
            string seconds;

            if (ts.Seconds < 10)
                seconds = string.Format("0{0}", ts.Seconds);
            else
                seconds = ts.Seconds.ToString();

            return string.Format("{0}:{1}", ts.Minutes, seconds);
        }
    }

    public class PropertyEventArgs<T> : EventArgs
    {
        public PropertyEventArgs(T prop)
        {
            property = prop;
        }

        private T property;

        public T Property
        {
            get
            {
                return property;
            }
            set
            {
                property = value;
            }
        }
    }
}