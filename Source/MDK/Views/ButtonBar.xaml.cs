using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace MDK.Views
{
    /// <summary>
    /// Interaction logic for ButtonBar.xaml
    /// </summary>
    [ContentProperty("Buttons")]
    [DefaultProperty("Buttons")]
    public partial class ButtonBar : UserControl
    {
        /// <summary>
        /// The dependency property key for <see cref="ButtonsProperty"/>
        /// </summary>
        public static readonly DependencyPropertyKey ButtonsPropertyKey = DependencyProperty.RegisterReadOnly(
            "Buttons", typeof(ObservableCollection<Button>), typeof(ButtonBar),
            new PropertyMetadata(null));

        /// <summary>
        /// The dependency property backend for the <see cref="Buttons"/> property
        /// </summary>
        public static readonly DependencyProperty ButtonsProperty = ButtonsPropertyKey.DependencyProperty;

        /// <summary>
        /// Creates a new instance of the button bar
        /// </summary>
        public ButtonBar()
        {
            SetValue(ButtonsPropertyKey, new ObservableCollection<Button>());
            InitializeComponent();
        }

        /// <summary>
        /// Returns the buttons collection for the button bar.
        /// </summary>
        public ObservableCollection<Button> Buttons => (ObservableCollection<Button>)GetValue(ButtonsProperty);
    }
}
