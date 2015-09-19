using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Helpers;
using System.Windows.Data;

namespace Helpers.WPF.Converters
{
    /// <summary>
    /// ApplyConverter provide any math operations with parameter (e.g '+' (default), '-', '/', '*')
    /// </summary>
    public class ApplyConverter : IValueConverter
    {
        private enum ApplyAction { Division, Multiplication, Subtraction, Addition };

        /// <summary>
        /// IValueConverter.Convert() function implementation
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="targetType">Result target type</param>
        /// <param name="parameter">String with math operation (e.g. -10, *2, /3)</param>
        /// <param name="culture">Converter culture</param>
        /// <returns>Convert result value</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            using (var logSession = Log.Session(string.Format("Helpers.WPF.Converters.ApplyConverter.Convert(value:'{0}', targetType:'{1}', parameter:'{2}')", value, targetType.Name, parameter)))
                try
                {
                    double curVal = double.Parse(value.ToString());
                    string paramline = parameter as string;
                    if (!string.IsNullOrEmpty(paramline))
                    {
                        var items = paramline.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                        double add;

                        logSession.Add(string.Format("Items count: '{0}'", items.Length));

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

                        logSession.Add(string.Format("Action: '{0}'", action));
                        logSession.Add(string.Format("Item0: '{0}'", item0));

                        if (items.Length > 0 && double.TryParse(item0, out add))
                        {
                            switch (action)
                            {
                                case (ApplyAction.Addition):
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

                            logSession.Add("Check Item1...");
                            double min;
                            if (items.Length > 1 && double.TryParse(items[1], out min))
                            {
                                logSession.Add(string.Format("Item1: '{0}'", items[1]));
                                curVal = Math.Max(curVal, min);

                                logSession.Add("Check Item2...");
                                double max;
                                if (items.Length > 2 && double.TryParse(items[2], out max))
                                {
                                    logSession.Add(string.Format("Item2: '{0}'", items[2]));
                                    curVal = Math.Min(curVal, max);
                                }
                            }
                        }
                    }

                    logSession.Add(string.Format("Result: '{0}'; Try to convert to targetType and return.", curVal));
                    var res = System.Convert.ChangeType(curVal, targetType);
                    logSession.Clear();
                    return res;
                }
                catch (Exception ex)
                {
                    logSession.Add(ex);
                    throw ex;
                }
        }

        /// <summary>
        /// IValueConverter.ConvertBack() function implementation
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="targetType">Result target type</param>
        /// <param name="parameter">String with math operation (e.g. -10, *2, /3)</param>
        /// <param name="culture">Converter culture</param>
        /// <returns>Return value not implemented</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
