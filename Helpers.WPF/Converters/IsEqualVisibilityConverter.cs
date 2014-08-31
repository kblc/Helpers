using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;


namespace Helpers.WPF.Converters
{
    /// <summary>
    /// This converter use for check similarity value and parameter for equals and then convert it to Visibility
    /// </summary>
    public class IsEqualVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool res = (bool)new IsEqualsConverter().Convert(value, targetType, parameter, culture);
            return new System.Windows.Controls.BooleanToVisibilityConverter().Convert(res, targetType, parameter, culture);
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
