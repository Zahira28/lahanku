using System.Windows;
using System.Windows.Controls;

namespace Lahanku.Controls
{
    public partial class Sidebar : UserControl
    {
        // "Ringkasan", "Dampak" or "Profil"
        public static readonly DependencyProperty ActiveItemProperty = DependencyProperty.Register(
            nameof(ActiveItem), typeof(string), typeof(Sidebar),
            new PropertyMetadata("Ringkasan", (d, _) => ((Sidebar)d).Highlight()));

        public string ActiveItem
        {
            get => (string)GetValue(ActiveItemProperty);
            set => SetValue(ActiveItemProperty, value);
        }

        public Sidebar()
        {
            InitializeComponent();
            Highlight();
        }

        private void Highlight()
        {
            NavSummary.Tag = ActiveItem == "Ringkasan" ? "active" : null;
            NavImpact.Tag = ActiveItem == "Dampak" ? "active" : null;
            NavProfile.Tag = ActiveItem == "Profil" ? "active" : null;
        }
    }
}
