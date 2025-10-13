using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using System;

namespace DG.Tools.XrmMockup.CustomFunction
{
    internal class UTCTodayFunction : ReflectionFunction
    {
        public UTCTodayFunction()
            : base("UTCToday", FormulaType.DateTime)
        {
        }

        public static DateTimeValue Execute()
        {
            return FormulaValue.New(DateTime.UtcNow.Date);
        }
    }
}
