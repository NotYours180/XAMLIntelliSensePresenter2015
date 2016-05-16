namespace Microsoft.VisualStudio.XAMLIntellisensePresenter {
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.VisualStudio.Language.Intellisense;

    internal class XAMLEditorPresenterViewModel : FilteredIntelliSensePresenseViewModelBase {
        #region Declarations

        readonly XAMLEditorCompletionSeessionView _view;

        #endregion

        #region IPopupIntellisensePresenter Properties

        public override UIElement SurfaceElement {
            get { return _view; }
        }

        #endregion

        #region Properties

        protected override ListBox CompletionsListBox {
            get {
                if (_view != null && _view.listViewCompletions != null) {
                    return _view.listViewCompletions;
                }
                return null;
            }
        }

        #endregion

        #region Constructors

        internal XAMLEditorPresenterViewModel(IServiceProvider serviceProvider, ICompletionSession session)
            : base(serviceProvider, session) {
            _view = new XAMLEditorCompletionSeessionView(this);
        }

        #endregion

        #region Methods

        protected override Boolean CollectionView_Filter(Object itemToFilter) {
            Completion item = itemToFilter as Completion;
            if (item == null) {
                return false;
            }
            if (item.DisplayText == "x:") {
                return true;
            }

            if (!IsIconFilterEnabled(item.IconAutomationText)) {
                return false;
            }

            String userText = CurrentUserText;
            if (String.IsNullOrWhiteSpace(userText)) {
                return true;
            }

            if (Utilities.IsAllCaps(userText)) {
                String allCaps = item.Properties.GetProperty<String>(STR_ALLCAPS_PROPERTYNAME);
                if (userText.Length > allCaps.Length) {
                    return false;
                }
                return (userText == allCaps.Substring(0, userText.Length));
            }
            if (IsNarrowingFilterEnabled) {
                Int32 i = item.DisplayText.IndexOf(userText, StringComparison.OrdinalIgnoreCase);
                if (i > -1) {
                    return true;
                }
                return false;
            }
            return true;
        }

        #endregion
    }
}