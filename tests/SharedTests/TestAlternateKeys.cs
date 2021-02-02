#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013 || XRM_MOCKUP_TEST_2015)
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Xunit.Sdk;
using System.ServiceModel;

namespace DG.XrmMockupTest
{
    public class TestAlternateKeys : UnitTestBase
    {
        public TestAlternateKeys(XrmMockupFixture fixture) : base(fixture) { }

        [Fact(Skip = "Alternate key implementation should be based on column values")]
        public void TestAlternateKeysAll()
        {
            using (var context = new Xrm(orgAdminUIService))
            {
                var attributes = new AttributeCollection {
                    { "name", "Burgers" },
                    { "address1_city", "Virum" }
                };
                orgAdminUIService.Create(new Account { Attributes = attributes });

                var keyAttributes = new KeyAttributeCollection {
                    { "name", "Burgers" },
                    { "address1_city", "Virum" }
                };
                var req = new RetrieveRequest
                {
                    Target = new EntityReference
                    {
                        LogicalName = Account.EntityLogicalName,
                        KeyAttributes = keyAttributes
                    }
                };

                var resp = orgAdminUIService.Execute(req) as RetrieveResponse;
                var entity = resp.Entity as Account;
                Assert.Equal("Burgers", entity.Name);
                Assert.Equal("Virum", entity.Address1_City);

                var newAttributes = new AttributeCollection {
                    { "name", "Toast" }
                };
                orgAdminUIService.Update(new Account { KeyAttributes = keyAttributes, Attributes = newAttributes });

                keyAttributes["name"] = "Toast";

                req = new RetrieveRequest
                {
                    Target = new EntityReference
                    {
                        LogicalName = Account.EntityLogicalName,
                        KeyAttributes = keyAttributes
                    }
                };
                resp = orgAdminUIService.Execute(req) as RetrieveResponse;
                var updatedEntity = resp.Entity as Account;
                Assert.Equal("Toast", updatedEntity.Name);
                Assert.Equal("Virum", updatedEntity.Address1_City);
            }
        }

        [Fact]

        public void AltKeyRetrieveWithoutEntityTypeInDb()
        {
            var y = Account.Retrieve_dg_name(orgAdminUIService, "woop", x => x.AccountNumber);
        }

#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)

        [Fact]
        public void TestUniqueOnInsertSingleAttributeKey()
        {
            var u1 = new Entity("mock_uniquesinglekeyonname");
            u1["mock_name"] = "unique1";
            orgAdminService.Create(u1);

            var u2 = new Entity("mock_uniquesinglekeyonname");
            u2["mock_name"] = "unique1";

            var ex = Assert.Throws<FaultException>(() => orgAdminService.Create(u2));
            Assert.Equal("A record that has the attribute values Name already exists. The entity key Name requires that this set of attributes contains unique values. Select unique values and try again.", ex.Message);
        }

        [Fact]
        public void TestUniqueOnUpdateSingleAttributeKey()
        {
            var u1 = new Entity("mock_uniquesinglekeyonname");
            u1["mock_name"] = "unique1";
            orgAdminService.Create(u1);

            var u2 = new Entity("mock_uniquesinglekeyonname");
            u2["mock_name"] = "unique2";
            u2.Id = orgAdminService.Create(u2);

            u2["mock_name"] = "unique1";

            var ex = Assert.Throws<FaultException>(() => orgAdminService.Update(u2));
            Assert.Equal("A record that has the attribute values Name already exists. The entity key Name requires that this set of attributes contains unique values. Select unique values and try again.", ex.Message);
        }

        [Fact]
        public void TestUniqueOnInsertMultiAttributeKey()
        {
            var u1 = new Entity("mock_uniquesinglekeyonnameandintvalue");
            u1["mock_name"] = "unique1";
            u1["mock_intvalue"] = 1;
            orgAdminService.Create(u1);

            var u2 = new Entity("mock_uniquesinglekeyonnameandintvalue");
            u2["mock_name"] = "unique1";
            u2["mock_intvalue"] = 1;

            var ex = Assert.Throws<FaultException>(() => orgAdminService.Create(u2));
            Assert.Equal("A record that has the attribute values Int Value, Name already exists. The entity key NameAndIntValue requires that this set of attributes contains unique values. Select unique values and try again.", ex.Message);
        }

        [Fact]
        public void TestUniqueOnUpdateMultiAttributeKeyBothUpdated()
        {
            var u1 = new Entity("mock_uniquesinglekeyonnameandintvalue");
            u1["mock_name"] = "unique1";
            u1["mock_intvalue"] = 1;
            orgAdminService.Create(u1);

            var u2 = new Entity("mock_uniquesinglekeyonnameandintvalue");
            u2["mock_name"] = "unique2";
            u2["mock_intvalue"] = 2;
            var id2 = orgAdminService.Create(u2);

            var update2 = new Entity("mock_uniquesinglekeyonnameandintvalue");
            update2.Id = id2;
            update2["mock_name"] = "unique1";
            update2["mock_intvalue"] = 1;

            var ex = Assert.Throws<FaultException>(() => orgAdminService.Update(update2));
            Assert.Equal("A record that has the attribute values Int Value, Name already exists. The entity key NameAndIntValue requires that this set of attributes contains unique values. Select unique values and try again.", ex.Message);
        }

        [Fact]
        public void TestUniqueOnUpdateMultiAttributeKeyOneUpdated()
        {
            var u1 = new Entity("mock_uniquesinglekeyonnameandintvalue");
            u1["mock_name"] = "unique1";
            u1["mock_intvalue"] = 1;
            orgAdminService.Create(u1);

            var u2 = new Entity("mock_uniquesinglekeyonnameandintvalue");
            u2["mock_name"] = "unique1";
            u2["mock_intvalue"] = 2;
            var id2 = orgAdminService.Create(u2);

            var update2 = new Entity("mock_uniquesinglekeyonnameandintvalue");
            update2.Id = id2;
            update2["mock_intvalue"] = 1;

            var ex = Assert.Throws<FaultException>(() => orgAdminService.Update(update2));
            Assert.Equal("A record that has the attribute values Int Value, Name already exists. The entity key NameAndIntValue requires that this set of attributes contains unique values. Select unique values and try again.", ex.Message);
        }

        [Fact]
        public void TestUniqueOnInsertMultiAttributeKeyNoFalsePositive()
        {
            var u1 = new Entity("mock_uniquesinglekeyonnameandintvalue");
            u1["mock_name"] = "unique1";
            u1["mock_intvalue"] = "1";
            orgAdminService.Create(u1);

            var u2 = new Entity("mock_uniquesinglekeyonnameandintvalue");
            u2["mock_name"] = "unique1";
            u2["mock_intvalue"] = "2";

            orgAdminService.Create(u2);
        }

        [Fact]
        public void TestUniqueOnInsertMultipleKeys()
        {
            var u1 = new Entity("mock_uniquemultiplekeys");
            u1["mock_name"] = "unique1";
            u1["mock_decimalfield"] = 3.62M;
            orgAdminService.Create(u1);

            var u2 = new Entity("mock_uniquemultiplekeys");
            u2["mock_name"] = "unique2";
            u2["mock_decimalfield"] = 3.63M;
            orgAdminService.Create(u2);

            var u3 = new Entity("mock_uniquemultiplekeys");
            u3["mock_name"] = "unique3";
            u3["mock_decimalfield"] = 3.62M;

            var ex = Assert.Throws<FaultException>(() => orgAdminService.Create(u3));
            Assert.Equal("A record that has the attribute values Decimal Field already exists. The entity key Decimal requires that this set of attributes contains unique values. Select unique values and try again.", ex.Message);
        }

        [Fact]
        public void TestUniqueOnInsertLookup()
        {
            var contact = new Contact() { FirstName = "matt" };
            contact.Id = orgAdminService.Create(contact);

            var u1 = new Entity("mock_uniquesinglekeyonlookup");
            u1["mock_name"] = "unique1";
            u1["mock_contactlookup"] = contact.ToEntityReference();
            orgAdminService.Create(u1);


            var u3 = new Entity("mock_uniquesinglekeyonlookup");
            u3["mock_name"] = "unique3";
            u3["mock_contactlookup"] = contact.ToEntityReference();

            var ex = Assert.Throws<FaultException>(() => orgAdminService.Create(u3));
            Assert.Equal("A record that has the attribute values ContactLookup already exists. The entity key Contact requires that this set of attributes contains unique values. Select unique values and try again.", ex.Message);
        }

        [Fact]
        public void TestUniqueOnUpdateLookup()
        {
            var contact = new Contact() { FirstName = "matt" };
            contact.Id = orgAdminService.Create(contact);

            var u1 = new Entity("mock_uniquesinglekeyonlookup");
            u1["mock_name"] = "unique1";
            u1["mock_contactlookup"] = contact.ToEntityReference();
            orgAdminService.Create(u1);

            var u3 = new Entity("mock_uniquesinglekeyonlookup");
            u3["mock_name"] = "unique3";
            var id3 = orgAdminService.Create(u3);

            var update3 = new Entity("mock_uniquesinglekeyonlookup") { Id = id3 };
            update3["mock_contactlookup"] = contact.ToEntityReference();


            var ex = Assert.Throws<FaultException>(() => orgAdminService.Update(update3));
            Assert.Equal("A record that has the attribute values ContactLookup already exists. The entity key Contact requires that this set of attributes contains unique values. Select unique values and try again.", ex.Message);
        }

        [Fact]
        public void TestUniqueOnUpdateMultipleKeys()
        {
            var u1 = new Entity("mock_uniquemultiplekeys");
            u1["mock_name"] = "unique1";
            u1["mock_decimalfield"] = 3.62M;
            orgAdminService.Create(u1);

            var u2 = new Entity("mock_uniquemultiplekeys");
            u2["mock_name"] = "unique2";
            u2["mock_decimalfield"] = 3.63M;
            orgAdminService.Create(u2);

            var u3 = new Entity("mock_uniquemultiplekeys");
            u3["mock_name"] = "unique3";
            u3["mock_decimalfield"] = 3.61M;
            var id3 = orgAdminService.Create(u3);

            var update3 = new Entity("mock_uniquemultiplekeys") { Id = id3 };
            update3["mock_decimalfield"] = 3.62M;
            
            var ex = Assert.Throws<FaultException>(() => orgAdminService.Update(update3));
            Assert.Equal("A record that has the attribute values Decimal Field already exists. The entity key Decimal requires that this set of attributes contains unique values. Select unique values and try again.", ex.Message);
        }
#endif
    }
}
#endif