using System.Text;
using Verse;

namespace StellarisDaughter
{
    public class HediffWithSeverityLabel : HediffWithComps
    {
        public override string LabelBase
        {
            get
            {
                var builder = new StringBuilder();
                builder.Append(base.LabelBase);
                builder.Append(": ");
                builder.Append((Severity / def.maxSeverity).ToStringPercent());
                return builder.ToString();
            }
        }
    }
}
