using Armoire.Features.Penumbra.Interfaces;

namespace Armoire.Features.Penumbra.UI
{
    public class PenumbraStatusPresenter
    {
        private readonly IPenumbraAnalyzer analyzer;

        // Property to hold the UI state
        public string CurrentReport { get; private set; }

        public PenumbraStatusPresenter(IPenumbraAnalyzer analyzer)
        {
            this.analyzer = analyzer;
            CurrentReport = "Status unknown. Please refresh.";
        }

        public void RefreshReport()
        {
            CurrentReport = analyzer.GetStatusReport();
        }
    }
}
