using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using System;

namespace DG.Tools.XrmMockup.CustomFunction
{
    internal class IsUTCTodayFunction : ReflectionFunction
    {
        public IsUTCTodayFunction()
            : base("IsUTCToday", FormulaType.Boolean, FormulaType.DateTime)
        {
        }
        public static BooleanValue Execute(DateTimeValue date)
        {
            if (date == null)
                return FormulaValue.New(false);

            var utcToday = DateTime.UtcNow.Date;
            var inputValue = date.GetConvertedValue(TimeZoneInfo.Utc);
            return FormulaValue.New(inputValue.Date == utcToday);
        }
    }
}
