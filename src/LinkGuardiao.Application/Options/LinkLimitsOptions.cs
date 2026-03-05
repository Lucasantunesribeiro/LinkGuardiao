namespace LinkGuardiao.Application.Options
{
    public class LinkLimitsOptions
    {
        public const string SectionName = "LinkLimits";

        public int DailyUserCreateLimit { get; set; } = 100;
    }
}
