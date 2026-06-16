using Xunit;

namespace DG.XrmMockupTest
{
    // DISABLED: Opportunity / OpportunityClose were removed from the environment. The Win/Lose
    // opportunity messages and the opportunity Won/Lost state machine have no equivalent on the
    // available entities (account/contact), so these tests cannot be reproduced. Kept as skipped
    // stubs to document intent; original bodies are in git history (and the OpportunityWinLose
    // plugin is disabled in TestPluginAssembly365).
    public class TestOpportunity : UnitTestBase
    {
        public TestOpportunity(XrmMockupFixture fixture) : base(fixture) { }

        private const string SkipReason =
            "Opportunity/OpportunityClose removed from environment; Win/Lose messages and the opportunity " +
            "state machine have no equivalent on the available entities.";

        [Fact(Skip = SkipReason)]
        public void TestWinOpportunity() { }

        [Fact(Skip = SkipReason)]
        public void TestLoseOpportunity() { }
    }
}
