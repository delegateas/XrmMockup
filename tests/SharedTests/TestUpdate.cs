using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestUpdate : UnitTestBase
    {
        [TestMethod]
        public void UpdatingAttributeWithEmptyStringShouldReturnNull()
        {
            var id = orgAdminUIService.Create(new Lead { Subject = "nonemptystring" });
            orgAdminUIService.Update(new Lead { Id = id, Subject = string.Empty });
            var lead = orgAdminService.Retrieve<Lead>(id);
            Assert.IsNull(lead.Subject);
        }
    }
}
