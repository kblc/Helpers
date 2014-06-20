using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Helpers.Converters
{
    public class ApplyConverter : IValueConverter
    {
        private enum ApplyAction { Division, Multiplication, Subtraction, Addition };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double curVal = (double)value;
            string paramline = parameter as string;
            if (!string.IsNullOrEmpty(paramline))
            {
                var items = paramline.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                double add;

                string item0 = items[0];
                ApplyAction action = ApplyAction.Addition;

                if (item0.Length > 0)
                {
                    if (item0[0] == '-')
                    { 
                        action = ApplyAction.Subtraction;
                        item0 = item0.Substring(1);
                    }
                    else if (item0[0] == '*')
                    {
                        action = ApplyAction.Multiplication;
                        item0 = item0.Substring(1);
                    }
                    else if (item0[0] == '/')
                    {
                        action = ApplyAction.Division;
                        item0 = item0.Substring(1);
                    }
                    else if (item0[0] == '+')
                    {
                        action = ApplyAction.Addition;
                        item0 = item0.Substring(1);
                    }
                }

                if (items.Length > 0 && double.TryParse(item0, out add))
                {
                    switch(action)
                    {
                        case(ApplyAction.Addition):
                            curVal += add;
                            break;
                        case (ApplyAction.Subtraction):
                            curVal -= add;
                            break;
                        case (ApplyAction.Division):
                            curVal /= add;
                            break;
                        case (ApplyAction.Multiplication):
                            curVal *= add;
                            break;
                    }

                    double min;
                    if (items.Length > 1 && double.TryParse(items[1], out min))
                    {
                        curVal = Math.Max(curVal, min);

                        double max;
                        if (items.Length > 2 && double.TryParse(items[2], out max))
                        {
                            curVal = Math.Min(curVal, max);
                        }
                    }
                }
            }
            return System.Convert.ChangeType(curVal, targetType);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
