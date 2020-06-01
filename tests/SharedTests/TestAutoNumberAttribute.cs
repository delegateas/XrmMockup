#if XRM_MOCKUP_TEST_365
using System;
using System.Globalization;
using System.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;

namespace DG.XrmMockupTest
{
    /// <summary>
    /// Test Auto number attribute.
    /// Entity: AutoNumberEntity (dg_autonumberentity) contains four fields with autonumbering.
    ///     RandomString (dg_randomstring): {RANDSTRING:6}
    ///     Sequential Number (dg_sequentialnumber): {SEQNUM:6}
    ///     DateTimeFormat (dg_datetimeformat): {DATETIMEUTC:yyyy-MM-dd}
    ///     AutoNumberingCombi {dg_autonumberingcombi}: WID-{SEQNUM:5}-{RANDSTRING:6}-{DATETIMEUTC:yyyyMMddhhmmss}
    /// </summary>
    [TestClass]
    public class TestAutoNumberAttribute : UnitTestBase
    {
        private const int StringLength = 6;
        private const int NumberLength = 6;
        private const string DateTimeFormat = "yyyy-MM-dd";

        [TestMethod]
        public void TestRandomString()
        {
            var autoNumberEntity1 = CreateAutoNumberEntity();
            Assert.IsNotNull(autoNumberEntity1.dg_RandomString, "RandomString haven't been set.");
            Assert.AreEqual(StringLength, autoNumberEntity1.dg_sequentialnumber.Length,
                "RandomString length isn't as expected.");

            var autoNumberEntity2 = CreateAutoNumberEntity();
            Assert.IsNotNull(autoNumberEntity2.dg_RandomString, "RandomString haven't been set.");
            Assert.AreEqual(StringLength, autoNumberEntity2.dg_sequentialnumber.Length,
                "RandomString length isn't as expected.");

            Assert.AreNotEqual(autoNumberEntity1.dg_RandomString, autoNumberEntity2.dg_RandomString,
                "The two random string are equal");
        }

        [TestMethod]
        public void TestSeqNum()
        {
            var autoNumberEntity1 = CreateAutoNumberEntity();
            Assert.IsNotNull(autoNumberEntity1.dg_sequentialnumber, "SequentialNumber haven't been set.");
            Assert.AreEqual(NumberLength, autoNumberEntity1.dg_sequentialnumber.Length,
                "SeqNum are not the expected length");

            var autoNumberEntity2 = CreateAutoNumberEntity();
            Assert.IsNotNull(autoNumberEntity2.dg_sequentialnumber, "SequentialNumber haven't been set.");
            Assert.AreEqual(NumberLength, autoNumberEntity2.dg_sequentialnumber.Length,
                "SeqNum are not the expected length");

            Assert.AreNotEqual(autoNumberEntity1.dg_sequentialnumber, autoNumberEntity2.dg_sequentialnumber,
                "Two seq nums are not equal");
            Assert.AreEqual(int.Parse(autoNumberEntity1.dg_sequentialnumber) + 1,
                int.Parse(autoNumberEntity2.dg_sequentialnumber),
                "Two seq nums are not following each other");
        }

        [TestMethod]
        public void TestDateTime()
        {
            var autoNumberEntity = CreateAutoNumberEntity();
            Assert.IsNotNull(autoNumberEntity.dg_DateTimeFormat);

            var parseSuccess = DateTime.TryParseExact(autoNumberEntity.dg_DateTimeFormat, DateTimeFormat,
                System.Globalization.CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _);

            Assert.IsTrue(parseSuccess, "The generated date couldn't be parsed to expected format.");
        }

        [TestMethod]
        public void TestSetAutoNumberSeedRequest()
        {
            const long value = 99;
            var request = new SetAutoNumberSeedRequest
            {
                EntityName = dg_autonumberentity.EntityLogicalName,
                AttributeName = nameof(dg_autonumberentity.dg_sequentialnumber).ToLower(),
                Value = value
            };

            var response = (SetAutoNumberSeedResponse) orgAdminService.Execute(request);
            Assert.AreEqual("SetAutoNumberSeed", response.ResponseName);

            var autoNumberEntity1 = CreateAutoNumberEntity();
            Assert.AreEqual(value.ToString($"D{NumberLength}"), autoNumberEntity1.dg_sequentialnumber,
                "Generate sequential number isn't as expected");
        }

        [TestMethod]
        public void TestSetAutoNumberSeedRequestException()
        {
            const long value = 99;
            var request1 = new SetAutoNumberSeedRequest
            {
                EntityName = "NonExistingEntity2398102",
                AttributeName = nameof(dg_autonumberentity.dg_sequentialnumber).ToLower(),
                Value = value
            };

            try
            {
                orgAdminService.Execute(request1);
            }
            catch (FaultException exp)
            {
                Assert.AreEqual(
                    $"Attribute {request1.AttributeName} of entity {request1.EntityName} is not an Auto Number attribute. Please confirm the inputs " +
                    "for Attribute and Entity correctly map to an Auto Number attribute.", exp.Message);
            }

            var request2 = new SetAutoNumberSeedRequest
            {
                EntityName = dg_autonumberentity.EntityLogicalName,
                AttributeName = "NonExistingAttribute2398102",
                Value = value
            };
            try
            {
                orgAdminService.Execute(request2);
            }
            catch (FaultException exp)
            {
                Assert.AreEqual(
                    $"Attribute {request2.AttributeName} of entity {request2.EntityName} is not an Auto Number attribute. Please confirm the inputs " +
                    "for Attribute and Entity correctly map to an Auto Number attribute.", exp.Message);
            }
        }

        [TestMethod]
        public void TestGetNextAutoNumber()
        {
            const long seedValue = 255L;
            orgAdminService.Execute(new SetAutoNumberSeedRequest
            {
                EntityName = dg_autonumberentity.EntityLogicalName,
                AttributeName = nameof(dg_autonumberentity.dg_sequentialnumber).ToLower(),
                Value = seedValue
            });

            var request = new GetNextAutoNumberValueRequest
            {
                EntityName = dg_autonumberentity.EntityLogicalName,
                AttributeName = nameof(dg_autonumberentity.dg_sequentialnumber).ToLower()
            };

            var resp = orgAdminService.Execute(request) as GetNextAutoNumberValueResponse;

            Assert.AreEqual(seedValue + 1, resp.NextAutoNumberValue);
            Assert.AreEqual(seedValue + 1, resp.Results["NextAutoNumberValue"]);
        }

        [TestMethod]
        public void TestGetNextAutoNumberException()
        {
            var request1 = new GetNextAutoNumberValueRequest()
            {
                EntityName = "NonExistingEntity2398102",
                AttributeName = nameof(dg_autonumberentity.dg_sequentialnumber).ToLower(),
            };

            try
            {
                orgAdminService.Execute(request1);
            }
            catch (FaultException exp)
            {
                Assert.AreEqual(
                    $"Attribute {request1.AttributeName} of entity {request1.EntityName} is not an Auto Number attribute. Please confirm the inputs " +
                    "for Attribute and Entity correctly map to an Auto Number attribute.", exp.Message);
            }

            var request2 = new GetNextAutoNumberValueRequest()
            {
                EntityName = dg_autonumberentity.EntityLogicalName,
                AttributeName = "NonExistingAttribute2398102",
            };
            try
            {
                orgAdminService.Execute(request2);
            }
            catch (FaultException exp)
            {
                Assert.AreEqual(
                    $"Attribute {request2.AttributeName} of entity {request2.EntityName} is not an Auto Number attribute. Please confirm the inputs " +
                    "for Attribute and Entity correctly map to an Auto Number attribute.", exp.Message);
            }
        }

        [TestMethod]
        public void TestGetAutoNumberSeedBase()
        {
            var getAutoNumberSeedRequest1 = new GetAutoNumberSeedRequest
            {
                EntityName = dg_autonumberentity.EntityLogicalName,
                AttributeName = nameof(dg_autonumberentity.dg_sequentialnumber).ToLower()
            };

            var response1 = orgAdminService.Execute(getAutoNumberSeedRequest1) as GetAutoNumberSeedResponse;

            Assert.AreEqual(1000L, response1?.AutoNumberSeedValue, "Default seed isn't as expected");
        }

        [TestMethod]
        public void TestGetAutoNumberSeedUpdateSeed()
        {
            var getAutoNumberSeedRequest1 = new GetAutoNumberSeedRequest
            {
                EntityName = dg_autonumberentity.EntityLogicalName,
                AttributeName = nameof(dg_autonumberentity.dg_sequentialnumber).ToLower()
            };

            var response1 = orgAdminService.Execute(getAutoNumberSeedRequest1) as GetAutoNumberSeedResponse;

            Assert.AreEqual(1000L, response1?.AutoNumberSeedValue, "Default seed isn't as expected");

            const long seedValue = 123456L;
            orgAdminService.Execute(new SetAutoNumberSeedRequest
            {
                EntityName = dg_autonumberentity.EntityLogicalName,
                AttributeName = nameof(dg_autonumberentity.dg_sequentialnumber).ToLower(),
                Value = seedValue
            });

            var getAutoNumberSeedRequest2 = new GetAutoNumberSeedRequest
            {
                EntityName = dg_autonumberentity.EntityLogicalName,
                AttributeName = nameof(dg_autonumberentity.dg_sequentialnumber).ToLower()
            };

            var response2 = orgAdminService.Execute(getAutoNumberSeedRequest2) as GetAutoNumberSeedResponse;

            Assert.AreEqual(seedValue, response2?.AutoNumberSeedValue, "Default seed isn't as expected");
        }

        [TestMethod]
        public void TestGetAutoNumberSeedBaseCreateRecords()
        {
            var getAutoNumberSeedRequest1 = new GetAutoNumberSeedRequest
            {
                EntityName = dg_autonumberentity.EntityLogicalName,
                AttributeName = nameof(dg_autonumberentity.dg_sequentialnumber).ToLower()
            };

            var response1 = orgAdminService.Execute(getAutoNumberSeedRequest1) as GetAutoNumberSeedResponse;

            Assert.AreEqual(1000L, response1?.AutoNumberSeedValue, "Default seed isn't as expected");

            CreateAutoNumberEntity();

            var getAutoNumberSeedRequest2 = new GetAutoNumberSeedRequest
            {
                EntityName = dg_autonumberentity.EntityLogicalName,
                AttributeName = nameof(dg_autonumberentity.dg_sequentialnumber).ToLower()
            };

            var response2 = orgAdminService.Execute(getAutoNumberSeedRequest2) as GetAutoNumberSeedResponse;

            Assert.AreEqual(1000L, response2?.AutoNumberSeedValue, "Default seed isn't as expected");
        }

        private dg_autonumberentity CreateAutoNumberEntity()
        {
            var autoNumberEntity = new dg_autonumberentity();
            autoNumberEntity.Id = orgAdminService.Create(autoNumberEntity);
            return dg_autonumberentity.Retrieve(orgAdminService, autoNumberEntity.Id);
        }
    }
}
#endif