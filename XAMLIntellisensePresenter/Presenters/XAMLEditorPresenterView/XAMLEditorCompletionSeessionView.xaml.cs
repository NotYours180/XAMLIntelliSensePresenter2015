using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.XAMLIntellisensePresenter.Infrastructure;
namespace Microsoft.VisualStudio.XAMLIntellisensePresenter {

    public partial class XAMLEditorCompletionSeessionView : UserControl {

        #region Declarations

        XAMLEditorPresenterViewModel _viewModel;

        #endregion

        #region Constructor

        internal XAMLEditorCompletionSeessionView(XAMLEditorPresenterViewModel viewModel) {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel;
            _viewModel.Session.Dismissed += new EventHandler(OnSessionDismissed);
            this.Loaded += new RoutedEventHandler(XAMLEditorCompletionSeessionView_Loaded);
        }

        #endregion

        #region Methods

        void XAMLEditorCompletionSeessionView_Loaded(object sender, RoutedEventArgs e) {
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
            if (_viewModel != null && (e.OriginalSource is TextBlock || e.OriginalSource is Image)) {
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

        void FilterButton_Click(Object sender, RoutedEventArgs e) {
            if (_viewModel != null) {
                ToggleButton btn = sender as ToggleButton;
                if (btn == null)
                    return;

                Filter filter = btn.Tag as Filter;

                if (filter == null)
                    return;

                filter.IsChecked = !filter.IsChecked;
                _viewModel.ExecuteFilter(filter);
            }
        }

        void ListView_Loaded(object sender, RoutedEventArgs e) {
            if (_viewModel != null) {

                if (_viewModel.Items.View.CurrentItem != null) {
                    this.listViewCompletions.ScrollIntoView(_viewModel.Items.View.CurrentItem);
                }

                if (_viewModel.Filters != null) {
                    Binding b;
                    ToggleButton btn;
                    foreach (KeyValuePair<String, Filter> item in _viewModel.Filters) {
                        if (item.Value.IsActiveInCurrentSession) {
                            btn = new ToggleButton() {
                                Content = new Image() { Source = item.Value.ImageSource },
                                ToolTip = "Show/hide items matching this image",
                                Focusable = false,
                                Tag = item.Value,
                                IsChecked = item.Value.IsChecked
                            };

                            if (item.Value.IconAutomationText == PresenterConstants.STRING_NAMESPACE_FILTER) {
                                b = new Binding("IsNamespaceFilterChecked");
                                b.Mode = BindingMode.TwoWay;
                                btn.SetBinding(ToggleButton.IsCheckedProperty, b);
                                btn.ToolTip = "Show/hide namespaces and/or attached properties.  (ALT + .) toggles show/hide.";
                            }

                            btn.Style = (Style)this.TryFindResource("filterButtonStyle");
                            btn.Click += new RoutedEventHandler(FilterButton_Click);
                            this.tbFilters.Items.Add(btn);
                        }
                    }
                    this.tbFilters.Items.Add(new Separator() { Margin = new Thickness(7, 0, 7, 0) });
                    Image img = new Image();
                    String packUri = "pack://application:,,,/Microsoft.VisualStudio.XAMLIntellisensePresenter;component/Resources/filter.png";
                    img.Source = new ImageSourceConverter().ConvertFromString(packUri) as ImageSource;
                    btn = new ToggleButton() {
                        Content = img,
                        Style = (Style)this.TryFindResource("filterButtonStyle"),
                        Focusable = false,
                        ToolTip = "Enable/disable the Pascal case narrowing filter.  (ALT + ,) toggles narrowing filter."
                    };
                    b = new Binding("IsNarrowingFilterEnabled");
                    b.Mode = BindingMode.TwoWay;
                    btn.SetBinding(ToggleButton.IsCheckedProperty, b);
                    this.tbFilters.Items.Add(btn);
                }
            }
        }

        #endregion
    }
}
