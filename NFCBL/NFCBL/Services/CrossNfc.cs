using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace NFCBL.Services
{
    public static partial class CrossNFC
    {
        static Lazy<INFC> _implementation = new Lazy<INFC>(() => CreateNFC());

        /// <summary>
        /// Gets if the plugin is supported on the current platform.
        /// </summary>
        public static bool IsSupported => _implementation.Value != null;

        /// <summary>
        /// Legacy Mode (Supporting Mifare Classic on iOS)
        /// </summary>
        static bool _legacy = false;

        public static bool Legacy
        {
            get
            {
                return _legacy;
            }

            set
            {
                _legacy = value;

                _implementation = new Lazy<INFC>(() => CreateNFC());
            }
        }

        private static INFC CreateNFC()
        {
           return DependencyService.Get<INFC>();
        }

        /// <summary>
        /// Current plugin implementation to use
        /// </summary>
        public static INFC Current
        {
            get
            {
                INFC ret = _implementation.Value;
                if (ret == null)
                {
                    throw NotImplementedInReferenceAssembly();
                }
                return ret;
            }
        }


        internal static Exception NotImplementedInReferenceAssembly() =>
            new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");

    }
}
