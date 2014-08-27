using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace Helpers.Controls.Converters
{
    /// <summary>
    /// Converter for set alpha for incoming color
    /// </summary>
    public class SetAlphaToColorConverter : IValueConverter
    {
        /// <summary>
        /// Convert this
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Media.Color)
            {
                var currColor = (System.Windows.Media.Color)value;
                string prm = parameter as string;
                if (!string.IsNullOrWhiteSpace(prm))
                {
                    int alpha = -1;
                    if (prm[0] == '#')
                    {
                        prm = prm.Substring(1);
                        try
                        {
                            alpha = int.Parse(prm, System.Globalization.NumberStyles.HexNumber);
                        }
                        catch { }
                    }

                    if (alpha >= 0 || int.TryParse(prm, out alpha))
                    {
                        currColor.A = (byte)((float)currColor.A * (float)alpha / (float)255);
                    }
                    return new SolidColorBrush(currColor);
                }
            }
            return value;
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
