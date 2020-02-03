using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MDK.Views.Options
{
    /// <summary>
    /// Converts enum value to its string representation and back.
    /// </summary>
    public class EnumToStringConverter : IValueConverter
    {
        /// <summary>
        /// Converts a string to an enum value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Enum enumValue = default(Enum);
            if (parameter is Type)
                enumValue = (Enum)Enum.Parse((Type)parameter, value.ToString());
            return enumValue;
        }
        /// <summary>
        /// Converts an enum value to string.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }
    }
}
