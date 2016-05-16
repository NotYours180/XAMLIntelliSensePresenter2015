namespace Microsoft.VisualStudio.XAMLIntellisensePresenter {
    using System;

    internal class Utilities {
        public static Boolean IsAllCaps(String s) {
            if (s.Length > 1) {
                foreach (Char c in s) {
                    if (!Char.IsUpper(c)) {
                        return false;
                    }
                }
                return true;
            } else {
                return false;
            }
        }
    }
}