using System;
using System.Globalization;
using System.Windows.Data;

namespace MDK.Views
{
    /// <summary>
    /// A standardized converter for changing pretty much value into a boolean. Any of the known 
    /// values representing false, null or empty will return false, anything else will return true.
    /// </summary>
    public class ToBooleanConverter : IValueConverter
    {
        /// <inheritdoc cref="IValueConverter.Convert"/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (object.Equals(value, null))
                return false;
            if (object.Equals(value, false))
                return false;
            if (object.Equals(value, (byte)0))
                return false;
            if (object.Equals(value, (sbyte)0))
                return false;
            if (object.Equals(value, (ushort)0))
                return false;
            if (object.Equals(value, (short)0))
                return false;
            if (object.Equals(value, (uint)0))
                return false;
            if (object.Equals(value, 0))
                return false;
            if (object.Equals(value, (ulong)0))
                return false;
            if (object.Equals(value, (long)0))
                return false;
            if (object.Equals(value, (float)0))
                return false;
            if (object.Equals(value, (double)0))
                return false;
            if (object.Equals(value, (decimal)0))
                return false;
            if (object.Equals(value, Guid.Empty))
                return false;
            if (object.Equals(value, DBNull.Value))
                return false;
            if (object.Equals(value, ""))
                return false;

            return true;
        }

        /// <inheritdoc cref="IValueConverter.ConvertBack"/>
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
