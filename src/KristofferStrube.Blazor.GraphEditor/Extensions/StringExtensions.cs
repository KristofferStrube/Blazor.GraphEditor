using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KristofferStrube.Blazor.GraphEditor.Extensions;

internal static class StringExtensions
{
    internal static double ParseAsDouble(this string s)
    {
        return double.Parse(s, CultureInfo.InvariantCulture);
    }
}