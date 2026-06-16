using Xunit;

namespace DG.XrmMockupTest
{
    // DISABLED: the Incident entity and the IncidentResolution activity were removed from the
    // environment. These tests exercise the CloseIncidentRequest message, IncidentResolution
    // creation and the incident Active/Resolved state machine plus resolved-record update
    // restrictions — none of which have an equivalent on the available entities (account/contact
    // use generic SetStateRequest, not Close). Kept as skipped stubs to document intent; original
    // bodies are in git history (and the Closeincidentplugin / IncidentDeleteAllRelatedResolutions-
    // OnClose plugins are disabled in TestPluginAssembly365).
    public class TestIncident : UnitTestBase
    {
        public TestIncident(XrmMockupFixture fixture) : base(fixture) { }

        private const string SkipReason =
            "Incident/IncidentResolution removed from environment; CloseIncident message and the incident " +
            "state machine have no equivalent on the available entities.";

        [Fact(Skip = SkipReason)] public void TestCloseIncidentRequestSuccess() { }
        [Fact(Skip = SkipReason)] public void TestCloseIncidentRequestFailsWhenPreviouslyResolved() { }
        [Fact(Skip = SkipReason)] public void TestCloseIncidentRequestFailsWhenIncidentResolutionMissing() { }
        [Fact(Skip = SkipReason)] public void TestCloseIncidentRequestFailsWhenLogicalNameMissing() { }
        [Fact(Skip = SkipReason)] public void TestCloseIncidentRequestFailsWhenLogicalNameDoesNotExist() { }
        [Fact(Skip = SkipReason)] public void TestCloseIncidentRequestFailsWhenLogicalNameWrong() { }
        [Fact(Skip = SkipReason)] public void TestCloseIncidentRequestFailsWhenStatusMissing() { }
        [Fact(Skip = SkipReason)] public void TestCloseIncidentRequestFailsWhenStatusDoesNotExist() { }
        [Fact(Skip = SkipReason)] public void TestCloseIncidentRequestFailsWhenStateOfStatusWrong() { }
        [Fact(Skip = SkipReason)] public void TestCloseIncidentRequestFailsWhenIncidentidMissing() { }
        [Fact(Skip = SkipReason)] public void TestCloseIncidentRequestFailsWhenIncidentDoesNotExist() { }
        [Fact(Skip = SkipReason)] public void TestCloseIncidentRequestFailsWhenIncidentResolutionAlreadyExists() { }
        [Fact(Skip = SkipReason)] public void TestUpdateResolvedIncidentFailsWhenFieldModificationIsNotAllowed() { }
        [Fact(Skip = SkipReason)] public void TestUpdateResolvedIncidentSucceedsWhenFieldModificationIsAllowed() { }
        [Fact(Skip = SkipReason)] public void TestRemovalOfResolutionsAfterClose() { }
        [Fact(Skip = SkipReason)] public void TestUpdateIncidentAsResolvedFails() { }
        [Fact(Skip = SkipReason)] public void TestCanUpdateOpenIncident() { }
    }
}
