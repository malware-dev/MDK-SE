using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MDK.Views
{
    /// <summary>
    /// A standardized converter for changing <see cref="bool"/> values into <see cref="Visibility"/> values.
    /// </summary>
    public class BooleanVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// The value returned when the input is <c>true</c>
        /// </summary>
        public Visibility TrueVisibility { get; set; } = Visibility.Visible;

        /// <summary>
        /// The value returned when the input is not <c>true</c>
        /// </summary>
        public Visibility FalseVisibility { get; set; } = Visibility.Visible;

        /// <inheritdoc cref="IValueConverter.Convert"/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && (bool)value ? TrueVisibility : FalseVisibility;
        }

        /// <inheritdoc cref="IValueConverter.ConvertBack"/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility && (Visibility)value == TrueVisibility;
        }
    }
}
