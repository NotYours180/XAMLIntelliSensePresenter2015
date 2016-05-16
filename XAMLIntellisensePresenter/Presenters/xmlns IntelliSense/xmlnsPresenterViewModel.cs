namespace Microsoft.VisualStudio.XAMLIntellisensePresenter {
    using System;
    using System.Collections;
    using System.Windows;
    using System.Windows.Controls;
    using EnvDTE;
    using Microsoft.VisualStudio.Language.Intellisense;

    internal class xmlnsPresenterViewModel : FilteredIntelliSensePresenseViewModelBase {
        #region Declarations

        static Boolean _listOnlySolutionAssemblies;
        static Boolean _listSchemas;
        readonly DTE _dte;
        Hashtable _solutionAssemblyNames;
        readonly xmlnsCompletionSessionView _view;

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

        protected override String CurrentUserText {
            get {
                if (CompletionSession != null) {
                    String userText = CompletionSession.SelectedCompletionSet.ApplicableTo.GetText(CompletionSession.SelectedCompletionSet.ApplicableTo.TextBuffer.CurrentSnapshot);
                    if (userText.StartsWith("clr-namespace:")) {
                        userText = userText.Replace("clr-namespace:", "");
                        if (userText.Contains(";assembly=")) {
                            userText = userText.Replace(";assembly=", " (");
                            userText = userText + ")";
                        }
                    }
                    return userText;
                }
                return String.Empty;
            }
        }

        public Boolean ListOnlySolutionAssemblies {
            get { return _listOnlySolutionAssemblies; }
            set {
                _listOnlySolutionAssemblies = value;
                RaisePropertyChanged("ListOnlySolutionAssemblies");
                Refresh();
            }
        }

        public Boolean ListSchemas {
            get { return _listSchemas; }
            set {
                _listSchemas = value;
                RaisePropertyChanged("ListSchemas");
                Refresh();
            }
        }

        #endregion

        #region Constructors

        static xmlnsPresenterViewModel() {
            _listOnlySolutionAssemblies = false;
            _listSchemas = true;
        }

        internal xmlnsPresenterViewModel(IServiceProvider serviceProvider, ICompletionSession session)
            : base(serviceProvider, session) {
            _dte = serviceProvider.GetService(typeof(DTE)) as DTE;
            LoadAssemblyNames();
            _view = new xmlnsCompletionSessionView(this);
        }

        #endregion

        #region Methods

        protected override Boolean CollectionView_Filter(Object itemToFilter) {
            var item = itemToFilter as Completion;
            if (item == null) {
                return false;
            }
            //parsing of the AssemblyName was not added to the base class for perf considerations
            Int32 index = item.DisplayText.IndexOf("(", System.StringComparison.Ordinal);
            if (index > -1) {
                if (ListOnlySolutionAssemblies && _solutionAssemblyNames != null && !_solutionAssemblyNames.ContainsKey(item.DisplayText.Substring(index + 1).Replace(")", ""))) {
                    return false;
                }
            } else {
                if (!ListSchemas) {
                    return false;
                }
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

        void LoadAssemblyNames() {
            _solutionAssemblyNames = new Hashtable();
            if (_dte != null) {
                foreach (Project p in _dte.Solution.Projects) {
                    if (p.Kind == Constants.vsProjectKindUnmodeled) {
                        continue;
                    }
                    if (!_solutionAssemblyNames.ContainsKey(p.Name)) {
                        _solutionAssemblyNames.Add(p.Name, null);
                    }
                }
            }
        }

        #endregion
    }
}