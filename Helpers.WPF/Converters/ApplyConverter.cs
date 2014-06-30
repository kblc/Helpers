using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Helpers;
using System.Windows.Data;

namespace Helpers.WPF.Converters
{
    public class ApplyConverter : IValueConverter
    {
        private enum ApplyAction { Division, Multiplication, Subtraction, Addition };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool wasException = false;
            var logSession = Log.SessionStart(string.Format("Helpers.WPF.Converters.ApplyConverter.Convert(value:'{0}', targetType:'{1}', parameter:'{2}')", value, targetType.Name, parameter), true);
            try
            {
                double curVal = (double)value;
                string paramline = parameter as string;
                if (!string.IsNullOrEmpty(paramline))
                {
                    var items = paramline.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                    double add;

                    Helpers.Log.Add(logSession, string.Format("Items count: '{0}'", items.Length));

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

                    Helpers.Log.Add(logSession, string.Format("Action: '{0}'", action));
                    Helpers.Log.Add(logSession, string.Format("Item0: '{0}'", item0));

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

                        Helpers.Log.Add(logSession, "Check Item1...");
                        double min;
                        if (items.Length > 1 && double.TryParse(items[1], out min))
                        {
                            Helpers.Log.Add(logSession, string.Format("Item1: '{0}'", items[1]));
                            curVal = Math.Max(curVal, min);

                            Helpers.Log.Add(logSession, "Check Item2...");
                            double max;
                            if (items.Length > 2 && double.TryParse(items[2], out max))
                            {
                                Helpers.Log.Add(logSession, string.Format("Item2: '{0}'", items[2]));
                                curVal = Math.Min(curVal, max);
                            }
                        }
                    }
                }

                Helpers.Log.Add(logSession, string.Format("Result: '{0}'; Try to convert to targetType and return.", curVal));
                return System.Convert.ChangeType(curVal, targetType);
            }
            catch(Exception ex)
            {
                wasException = true;
                throw ex;
            }
            finally
            {
                Log.SessionEnd(logSession, wasException);
            }           
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
