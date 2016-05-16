namespace Microsoft.VisualStudio.XAMLIntellisensePresenter {
    using System;
    using System.Windows.Media;

    public class Filter {
        public String IconAutomationText { get; set; }
        public ImageSource ImageSource { get; set; }
        public Boolean IsActiveInCurrentSession { get; set; }
        public Boolean IsChecked { get; set; }

        public Filter() {
        }
    }
}