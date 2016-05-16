using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.XAMLIntellisensePresenter {

    public partial class xmlnsCompletionSessionView : UserControl {

        #region Declarations

        xmlnsPresenterViewModel _viewModel;

        #endregion

        #region Constructor

        internal xmlnsCompletionSessionView(xmlnsPresenterViewModel viewModel) {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel;
            _viewModel.Session.Dismissed += new EventHandler(OnSessionDismissed);
            this.Loaded += new RoutedEventHandler(xmlnsCompletionSessionView_Loaded);
        }

        #endregion

        #region Methods

        void xmlnsCompletionSessionView_Loaded(object sender, RoutedEventArgs e) {
            if (_viewModel != null)
                _viewModel.Refresh();
        }

        void OnSessionDismissed(Object sender, EventArgs e) {
            if (_viewModel != null)
                _viewModel.Session.Dismissed -= new EventHandler(OnSessionDismissed);

            SurrenderFocus();
        }

        void SurrenderFocus() {
            if (_viewModel != null) {
                IWpfTextView view = this._viewModel.Session.TextView as IWpfTextView;
                if (view != null) {
                    Keyboard.Focus(view.VisualElement);
                }
            }
        }

        void ListView_SelectionChanged(Object sender, SelectionChangedEventArgs e) {
            SurrenderFocus();
        }

        void ListView_MouseDoubleClick(Object sender, MouseButtonEventArgs e) {
            if (_viewModel != null) {
                ListBox lb = sender as ListBox;
                if (lb != null && lb.SelectedItem != null) {
                    _viewModel.SetCompletion((Completion)lb.SelectedItem);
                }
                _viewModel.Commit();
            }
        }

        void ListView_MouseLeftButtonDown(Object sender, MouseButtonEventArgs e) {
            SurrenderFocus();
        }

        void ToggleButton_Click(Object sender, RoutedEventArgs e) {
            SurrenderFocus();
        }

        void ListView_Loaded(object sender, RoutedEventArgs e) {
            if (_viewModel.Items.View.CurrentItem != null) {
                this.listViewCompletions.ScrollIntoView(_viewModel.Items.View.CurrentItem);
            }
        }
        #endregion
    }
}
