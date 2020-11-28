using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestUpdate : UnitTestBase
    {
        public TestUpdate(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void UpdatingAttributeWithEmptyStringShouldReturnNull()
        {
            var id = orgAdminUIService.Create(new Lead { Subject = "nonemptystring" });
            orgAdminUIService.Update(new Lead { Id = id, Subject = string.Empty });
            var lead = orgAdminService.Retrieve<Lead>(id);
            Assert.Null(lead.Subject);
        }
    }
}
