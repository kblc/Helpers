using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;


namespace Helpers.WPF.Converters
{
    public class IsNotEqualVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool res = (bool)new IsNotEqualsConverter().Convert(value, targetType, parameter, culture);
            return new System.Windows.Controls.BooleanToVisibilityConverter().Convert(res, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
