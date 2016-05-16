namespace Microsoft.VisualStudio.XAMLIntellisensePresenter {
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using Microsoft.VisualStudio.Language.Intellisense;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.XAMLIntellisensePresenter.Infrastructure;

    internal abstract class FilteredIntelliSensePresenseViewModelBase : IPopupIntellisensePresenter, IIntellisenseCommandTarget, INotifyPropertyChanged {
        #region Declarations

        protected const String STR_ALLCAPS_PROPERTYNAME = "AllCaps";
        const String STR_REPLACE_REGEX = "[0-9a-z.]";

        static Boolean _isNarrowingFilterEnabled;
        static Boolean _isNamespaceFilterChecked = true;

        delegate void Universal_Delegate();

        readonly ICompletionSession _completionSession;
        readonly CollectionViewSource _cvs;
        static readonly IDictionary<String, Filter> _filters;

        #endregion

        #region IPopupIntellisensePresenter Properties

        public string SpaceReservationManagerName {
            get { return "completion"; }
        }

        public abstract UIElement SurfaceElement { get; }

        public IIntellisenseSession Session {
            get { return _completionSession; }
        }

        public double Opacity {
            get { return SurfaceElement.Opacity; }
            set { SurfaceElement.Opacity = value; }
        }

        public Text.Adornments.PopupStyles PopupStyles {
            get { return Text.Adornments.PopupStyles.PositionClosest; }
        }

        public ITrackingSpan PresentationSpan {
            get {
                SnapshotSpan span = this._completionSession.SelectedCompletionSet.ApplicableTo.GetSpan(this._completionSession.TextView.TextSnapshot);
                NormalizedSnapshotSpanCollection spans = this._completionSession.TextView.BufferGraph.MapUpToBuffer(span, this._completionSession.SelectedCompletionSet.ApplicableTo.TrackingMode, this._completionSession.TextView.TextBuffer);
                if (spans.Count <= 0) {
                    throw new InvalidOperationException("Completion Session Applicable-To Span is invalid.  It doesn't map to a span in the session's text view.");
                }
                SnapshotSpan span2 = spans[0];
                return this._completionSession.TextView.TextBuffer.CurrentSnapshot.CreateTrackingSpan(span2.Span, SpanTrackingMode.EdgeInclusive);
            }
        }

        #endregion

        #region IIntellisenseCommandTarget Properties

        public bool ExecuteKeyboardCommand(IntellisenseKeyboardCommand command) {
            switch (command) {
                case IntellisenseKeyboardCommand.Up:
                    SelectCompletion(-1);
                    return true;
                case IntellisenseKeyboardCommand.PageUp:
                    SelectCompletion(-10);
                    return true;
                case IntellisenseKeyboardCommand.Down:
                    SelectCompletion(1);
                    return true;
                case IntellisenseKeyboardCommand.PageDown:
                    SelectCompletion(10);
                    return true;
                case IntellisenseKeyboardCommand.Escape:
                    this.Session.Dismiss();
                    return true;
                case IntellisenseKeyboardCommand.DecreaseFilterLevel:
                    // user pressed ALT + ,
                    if (_filters.ContainsKey(PresenterConstants.STRING_NAMESPACE_FILTER)) {
                        this.IsNamespaceFilterChecked = !IsNamespaceFilterChecked;
                        _filters[PresenterConstants.STRING_NAMESPACE_FILTER].IsChecked = this.IsNamespaceFilterChecked;
                        ExecuteFilter(_filters[PresenterConstants.STRING_NAMESPACE_FILTER]);
                    }
                    return true;
                case IntellisenseKeyboardCommand.IncreaseFilterLevel:
                    // user pressed ALT + .
                    this.IsNarrowingFilterEnabled = !IsNarrowingFilterEnabled;
                    return true;
            }
            return false;
        }

        #endregion

        #region Properties

        public IServiceProvider ServiceProvider { get; private set; }

        public IDictionary<String, Filter> Filters {
            get { return _filters; }
        }

        protected abstract ListBox CompletionsListBox { get; }

        protected ICompletionSession CompletionSession {
            get { return _completionSession; }
        }

        protected virtual String CurrentUserText {
            get {
                if (_completionSession != null) {
                    String userText = _completionSession.SelectedCompletionSet.ApplicableTo.GetText(_completionSession.SelectedCompletionSet.ApplicableTo.TextBuffer.CurrentSnapshot);
                    Int32 index = userText.LastIndexOf(".", StringComparison.Ordinal);
                    if (index > -1) {
                        if (index < userText.Length - 1) {
                            userText = userText.Substring(index);
                        } else {
                            userText = String.Empty;
                        }
                    }
                    return userText;
                }
                return String.Empty;
            }
        }

        public CollectionViewSource Items {
            get { return _cvs; }
        }

        public Boolean IsNarrowingFilterEnabled {
            get { return _isNarrowingFilterEnabled; }
            set {
                _isNarrowingFilterEnabled = value;
                RaisePropertyChanged("IsNarrowingFilterEnabled");
                Refresh();
            }
        }

        public Boolean IsNamespaceFilterChecked {
            get { return _isNamespaceFilterChecked; }
            set {
                _isNamespaceFilterChecked = value;
                RaisePropertyChanged("IsNamespaceFilterChecked");
                Refresh();
            }
        }

        #endregion

        #region Events

#pragma warning disable 0067
        public event EventHandler PresentationSpanChanged;
#pragma warning restore 0067

#pragma warning disable 0067
        public event EventHandler SurfaceElementChanged;
#pragma warning restore 0067

#pragma warning disable 0067
        public event EventHandler<ValueChangedEventArgs<Text.Adornments.PopupStyles>> PopupStylesChanged;
#pragma warning restore 0067

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructor

        static FilteredIntelliSensePresenseViewModelBase() {
            _isNarrowingFilterEnabled = true;
            _filters = new Dictionary<String, Filter>();
        }

        protected FilteredIntelliSensePresenseViewModelBase(IServiceProvider serviceProvider, ICompletionSession session) {
            this.ServiceProvider = serviceProvider;
            _completionSession = session;
            LoadCapsAndIcons();
            _cvs = new CollectionViewSource();
            _cvs.Source = _completionSession.SelectedCompletionSet.Completions;
            _cvs.View.Filter = CollectionView_Filter;
            _cvs.View.CollectionChanged += View_CollectionChanged;
            _completionSession.TextView.TextBuffer.Changed += TextBuffer_Changed;
            _completionSession.Dismissed += CompletionSession_Dismissed;
        }

        #endregion

        #region Methods

        public Boolean IsIconFilterEnabled(String iconAutomationText) {
            if (_filters.ContainsKey(iconAutomationText)) {
                return _filters[iconAutomationText].IsActiveInCurrentSession && _filters[iconAutomationText].IsChecked;
            }
            return true;
        }

        public void ExecuteFilter(Filter filter) {
            if (_filters.ContainsKey(filter.IconAutomationText)) {
                _filters[filter.IconAutomationText].IsChecked = filter.IsChecked;
            }
            _cvs.View.Refresh();
        }

        protected void LoadCapsAndIcons() {
            foreach (Filter item in Filters.Values) {
                item.IsActiveInCurrentSession = false;
            }

            Regex regex = new Regex(STR_REPLACE_REGEX);
            foreach (CompletionSet cs in _completionSession.CompletionSets) {
                foreach (Completion item in cs.Completions) {
                    if (!String.IsNullOrWhiteSpace(item.IconAutomationText)) {
                        item.Properties.AddProperty(STR_ALLCAPS_PROPERTYNAME, regex.Replace(item.DisplayText, String.Empty));

                        if (!_filters.ContainsKey(item.IconAutomationText)) {
                            if (item.IconAutomationText != "9") {
                                _filters.Add(item.IconAutomationText, new Filter {IsActiveInCurrentSession = true, IconAutomationText = item.IconAutomationText, IsChecked = true, ImageSource = item.IconSource.CloneCurrentValue()});
                            }
                        } else {
                            _filters[item.IconAutomationText].IsActiveInCurrentSession = true;
                        }
                    }
                }
            }
        }

        void CompletionSession_Dismissed(object sender, EventArgs e) {
            if (_cvs != null && _cvs.View != null) {
                _cvs.View.CollectionChanged -= View_CollectionChanged;
            }

            if (_completionSession != null) {
                _completionSession.TextView.TextBuffer.Changed -= TextBuffer_Changed;
                _completionSession.Dismissed -= CompletionSession_Dismissed;
            }
            OnDismissed();
        }

        protected virtual void OnDismissed() {
            _cvs.Source = null;
        }

        protected abstract Boolean CollectionView_Filter(Object itemToFilter);

        void TextBuffer_Changed(object sender, TextContentChangedEventArgs e) {
            Refresh();
        }

        void ScrollFeedbackViewToLastItem() {
            if (_cvs != null && _cvs.View != null && _cvs.View.CurrentItem != null && !_cvs.View.IsCurrentBeforeFirst && !_cvs.View.IsCurrentAfterLast) {
                CompletionsListBox.ScrollIntoView(_cvs.View.CurrentItem);
            }
        }

        void UIThreadRefresh() {
            String userText = CurrentUserText;
            if (!String.IsNullOrWhiteSpace(userText)) {
                if (Utilities.IsAllCaps(userText)) {
                    foreach (Completion item in _cvs.View) {
                        String allCaps = item.Properties.GetProperty<String>(STR_ALLCAPS_PROPERTYNAME);
                        if (userText.Length > allCaps.Length) {
                            continue;
                        }
                        if (userText == allCaps.Substring(0, userText.Length)) {
                            _completionSession.SelectedCompletionSet.SelectionStatus = new CompletionSelectionStatus(item, true, true);
                            _cvs.View.MoveCurrentTo(item);
                            //required ScrollIntoView hack to get it to work consistently
                            CompletionsListBox.Dispatcher.BeginInvoke(new Universal_Delegate(ScrollFeedbackViewToLastItem), System.Windows.Threading.DispatcherPriority.Send);
                            return;
                        }
                    }
                } else {
                    foreach (Completion item in _cvs.View) {
                        if (item.DisplayText.StartsWith(userText, StringComparison.OrdinalIgnoreCase)) {
                            _completionSession.SelectedCompletionSet.SelectionStatus = new CompletionSelectionStatus(item, true, true);
                            _cvs.View.MoveCurrentTo(item);
                            //required ScrollIntoView hack to get it to work consistently
                            CompletionsListBox.Dispatcher.BeginInvoke(new Universal_Delegate(ScrollFeedbackViewToLastItem), System.Windows.Threading.DispatcherPriority.Send);
                            return;
                        }
                    }

                    if (IsNarrowingFilterEnabled) {
                        foreach (Completion item in _cvs.View) {
                            if (item.DisplayText.IndexOf(userText, StringComparison.OrdinalIgnoreCase) > -1) {
                                _completionSession.SelectedCompletionSet.SelectionStatus = new CompletionSelectionStatus(item, true, true);
                                _cvs.View.MoveCurrentTo(item);
                                //required ScrollIntoView hack to get it to work consistently
                                CompletionsListBox.Dispatcher.BeginInvoke(new Universal_Delegate(ScrollFeedbackViewToLastItem), System.Windows.Threading.DispatcherPriority.Send);
                                return;
                            }
                        }
                    }
                }
            } else {
                _cvs.View.MoveCurrentToFirst();
                _completionSession.SelectedCompletionSet.SelectionStatus = new CompletionSelectionStatus((Completion)_cvs.View.CurrentItem, false, true);
                if (CompletionsListBox.Items.Count > 0) {
                    //required ScrollIntoView hack to get it to work consistently
                    CompletionsListBox.Dispatcher.BeginInvoke(new Universal_Delegate(ScrollFeedbackViewToLastItem), System.Windows.Threading.DispatcherPriority.Send);
                }
            }
        }

        void View_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            UIThreadRefresh();
        }

        public void Refresh() {
            try {
                if (_cvs == null || _cvs.View == null) {
                    return;
                }

                _cvs.View.Refresh();

                //when Refresh is completed the View_CollectionChanged method will be called.
            } catch (Exception) {
                //ignore - happens is another thread shuts down this object
            }
        }

        internal void Commit() {
            if (_completionSession != null) {
                _completionSession.Commit();
            }
        }

        public void SetCompletion(Completion completion) {
            if (_completionSession != null) {
                _completionSession.SelectedCompletionSet.SelectionStatus = new CompletionSelectionStatus(completion, true, true);
            }
        }

        void SelectCompletion(Int32 relativeIndex) {
            if (_cvs != null && _cvs.View != null && _completionSession != null) {
                Int32 newPostion = _cvs.View.CurrentPosition + relativeIndex;
                if (relativeIndex >= 0) {
                    try {
                        if (!_cvs.View.MoveCurrentToPosition(newPostion)) {
                            _cvs.View.MoveCurrentToLast();
                        }
                    } catch (Exception) {
                        _cvs.View.MoveCurrentToLast();
                    }
                } else {
                    try {
                        if (!_cvs.View.MoveCurrentToPosition(newPostion)) {
                            _cvs.View.MoveCurrentToFirst();
                        }
                    } catch (Exception) {
                        _cvs.View.MoveCurrentToFirst();
                    }
                }
                _completionSession.SelectedCompletionSet.SelectionStatus = new CompletionSelectionStatus((Completion)this.Items.View.CurrentItem, true, true);
                CompletionsListBox.Dispatcher.BeginInvoke(new Universal_Delegate(ScrollFeedbackViewToLastItem), System.Windows.Threading.DispatcherPriority.Send);
            }
        }

        protected void RaisePropertyChanged(String propertyName) {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}