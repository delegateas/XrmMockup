using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestAnnotation : UnitTestBase
    {
        public TestAnnotation(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestAnnotationsWithAttachmentsHaveIsDocumentSet()
        {
            var note = new Entity("annotation");
            note["documentbody"] = "base64string";
            var noteId = orgAdminService.Create(note);

            var checkNote = orgAdminService.Retrieve("annotation", noteId, new ColumnSet(true));
            Assert.True(checkNote.GetAttributeValue<bool>("isdocument"));

        }
    }
}
