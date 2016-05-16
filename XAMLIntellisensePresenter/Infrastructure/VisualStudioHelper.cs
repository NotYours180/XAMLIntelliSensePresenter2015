using OleInterop = Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.XAMLIntellisensePresenter {
    using System;
    using System.Runtime.InteropServices;
    using EnvDTE;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.TextManager.Interop;

    /// <summary>
    /// Provides helper functionality for Visual Studio
    /// </summary>
    internal class VisualStudioHelper {
        internal System.IServiceProvider ServiceProvider { get; private set; }

        internal VisualStudioHelper(ITextView view)
            : this(view.TextBuffer) {
        }

        internal VisualStudioHelper(ITextBuffer textBuffer) {
            this.ServiceProvider = GetServiceProviderFromTextBuffer(textBuffer);
        }

        internal void Navigate(string uri) {
            if (!string.IsNullOrEmpty(uri)) {
                if (this.ServiceProvider != null) {
                    DTE vs = this.ServiceProvider.GetService(typeof(DTE)) as DTE;
                    if (vs != null) {
                        vs.ItemOperations.Navigate(uri, vsNavigateOptions.vsNavigateOptionsDefault);
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000")]
        static System.IServiceProvider GetServiceProviderFromTextBuffer(ITextBuffer textBuffer) {
            if (textBuffer.Properties.ContainsProperty(typeof(IVsTextBuffer))) {
                OleInterop.IObjectWithSite objectWithSite = textBuffer.Properties.GetProperty<OleInterop.IObjectWithSite>(typeof(IVsTextBuffer));
                if (objectWithSite != null) {
                    Guid serviceProviderGuid = typeof(OLE.Interop.IServiceProvider).GUID;
                    IntPtr ppServiceProvider = IntPtr.Zero;
                    // Get the service provider pointer using the Guid of the OleInterop ServiceProvider
                    objectWithSite.GetSite(ref serviceProviderGuid, out ppServiceProvider);

                    if (ppServiceProvider != IntPtr.Zero) {
                        // Create a System.ServiceProvider with the OleInterop ServiceProvider
                        OleInterop.IServiceProvider oleInteropServiceProvider = (OleInterop.IServiceProvider)Marshal.GetObjectForIUnknown(ppServiceProvider);
                        return new ServiceProvider(oleInteropServiceProvider);
                    }
                }
            }

            return null;
        }
    }
}