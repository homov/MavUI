using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace HeatController.Droid.Helpers
{

    internal class AndroidOS
    {
        static Version ParseVersion(string version)
        {
            if (Version.TryParse(version, out var number))
                return number;

            if (int.TryParse(version, out var major))
                return new Version(major, 0);

            return new Version(0, 0);
        }
        
            
        public static bool IsAndroidVersionAtLeast(int major, int minor = 0, int build = 0, int revision = 0)
           => IsOSVersionAtLeast(major, minor, build, revision);
        
        private static bool IsOSVersionAtLeast(int major, int minor, int build, int revision)
        {
            // if (Build.VERSION.SdkInt != ( BuildVersionCodes )major)
            //     return Build.VERSION.SdkInt >( BuildVersionCodes )major;
            Version current = System.Environment.OSVersion.Version;
            
            current = new Version( (int)Build.VERSION.SdkInt, 0,0,0 );
        
            if (current.Major != major)
            {
                return current.Major > major;
            }
            if (current.Minor != minor)
            {
                return current.Minor > minor;
            }
            if (current.Build != build)
            {
                return current.Build > build;
            }
        
            return current.Revision >= revision
                || (current.Revision == -1 && revision == 0);
        }
    }
}