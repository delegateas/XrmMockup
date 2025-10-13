using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using System;

namespace DG.Tools.XrmMockup.CustomFunction
{
    internal class UTCNowFunction : ReflectionFunction
    {
        public UTCNowFunction()
            : base("UTCNow", FormulaType.DateTime)
        {
        }
        public static DateTimeValue Execute()
        {
            return FormulaValue.New(DateTime.UtcNow);
        }
    }
}
