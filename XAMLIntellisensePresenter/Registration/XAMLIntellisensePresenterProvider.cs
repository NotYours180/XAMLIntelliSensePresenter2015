namespace Microsoft.VisualStudio.XAMLIntellisensePresenter {
    using System;
    using System.ComponentModel.Composition;
    using System.Linq;
    using Microsoft.VisualStudio.Language.Intellisense;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Utilities;

    [Export(typeof(IIntellisensePresenterProvider))]
    [ContentType("XAML")]
    [Order(Before = "Default Completion Presenter")]
    [Name("XAML Intellisense Presenter")]
    internal class XAMLIntellisensePresenterProvider : IIntellisensePresenterProvider {
        [Import(typeof(SVsServiceProvider))]
        IServiceProvider ServiceProvider { get; set; }

        public IIntellisensePresenter TryCreateIntellisensePresenter(IIntellisenseSession session) {
            ICompletionSession completionSession = session as ICompletionSession;
            if (completionSession != null) {
                Int32 count = completionSession.SelectedCompletionSet.Completions.Count();
                if (count < 10) {
                    return null;
                }

                if (IsXMLNSIntelliSense(completionSession)) {
                    return new xmlnsPresenterViewModel(ServiceProvider, completionSession);
                }
                return new XAMLEditorPresenterViewModel(ServiceProvider, completionSession);
            }
            return null;
        }

        Boolean IsXMLNSIntelliSense(ICompletionSession session) {
            Boolean isXMLNSIntelliSense = false;
            Int32 triggerPosition = session.TextView.Caret.Position.BufferPosition.Position;
            for (int i = triggerPosition - 1; i > 0; i--) {
                if (Char.IsWhiteSpace(session.TextView.TextBuffer.CurrentSnapshot.GetText()[i])) {
                    String test = session.TextView.TextBuffer.CurrentSnapshot.GetText().Substring(i, triggerPosition - i);
                    isXMLNSIntelliSense = test.Contains("xmlns:") || test.Contains("xmlns=");
                    break;
                }
            }
            return isXMLNSIntelliSense;
        }
    }
}