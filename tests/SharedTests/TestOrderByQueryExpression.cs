using System;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestOrderByQueryExpression : UnitTestBase
    {
        [TestMethod]
        public void When_ordering_by_money_fields_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var contact1 = new Contact()
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Fred",
                    LastName = "Bloggs",
                    AnnualIncome = 12345m,
                    TransactionCurrencyId = crm.BaseCurrency
                };
                var contact2 = new Contact()
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Jo",
                    LastName = "Bloggs",
                    AnnualIncome = 678910m,
                    TransactionCurrencyId = crm.BaseCurrency
                };
                crm.PopulateWith(contact1, contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("annualincome", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).AnnualIncome;

                Assert.AreEqual(12345m, firstResultValue.Value);
            }
        }

        [TestMethod]
        public void When_ordering_by_money_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var contact1 = new Contact
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Fred",
                    LastName = "Bloggs",
                    AnnualIncome = 12345m,
                    TransactionCurrencyId = crm.BaseCurrency
                };

                var contact2 = new Contact
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Jo",
                    LastName = "Bloggs",
                    AnnualIncome = 678910m,
                    TransactionCurrencyId = crm.BaseCurrency
                };

                crm.PopulateWith(contact1, contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("annualincome", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).AnnualIncome;

                Assert.AreEqual(678910M, firstResultValue.Value);
            }
        }

        [TestMethod]
        public void When_ordering_by_entity_reference_fields_ascending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var lead1 = new Lead();
                lead1.LastName = "Jordi";
                lead1.Id = orgAdminService.Create(lead1);

                var lead2 = new Lead();
                lead2.LastName = "Skuba";
                lead2.Id = orgAdminService.Create(lead2);

                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1.LastName = "Bloggs";
                contact1.OriginatingLeadId = lead1.ToEntityReference();
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2.LastName = "Bloggs";
                contact2.OriginatingLeadId = lead2.ToEntityReference();
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("originatingleadid", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).OriginatingLeadId;

                Assert.AreEqual("Jordi", firstResultValue.Name);
            }
        }

        [TestMethod]
        public void When_ordering_by_entity_reference_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var lead1 = new Lead();
                lead1.LastName = "Jordi";
                lead1.Id = orgAdminService.Create(lead1);

                var lead2 = new Lead();
                lead2.LastName = "Skuba";
                lead2.Id = orgAdminService.Create(lead2);

                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1.LastName = "Bloggs";
                contact1.OriginatingLeadId = lead1.ToEntityReference();
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2.LastName = "Bloggs";
                contact2.OriginatingLeadId = lead2.ToEntityReference();
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("originatingleadid", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).OriginatingLeadId;

                Assert.AreEqual("Skuba", firstResultValue.Name);
            }
        }

        [TestMethod]
        public void When_ordering_by_optionsetvalue_fields_ascending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1.GenderCode = Contact_GenderCode.Male;
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2.GenderCode = Contact_GenderCode.Female;
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("gendercode", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).GenderCode;

                Assert.AreEqual(Contact_GenderCode.Male, firstResultValue.Value);
            }
        }

        [TestMethod]
        public void When_ordering_by_optionsetvalue_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1.GenderCode = Contact_GenderCode.Male;
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2.GenderCode = Contact_GenderCode.Female;
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("gendercode", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).GenderCode;

                Assert.AreEqual(Contact_GenderCode.Female, firstResultValue.Value);
            }
        }

        [TestMethod]
        public void When_ordering_by_int_fields_ascending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1.NumberOfChildren = 2;
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2.NumberOfChildren = 5;
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("numberofchildren", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).NumberOfChildren;

                Assert.AreEqual(2, firstResultValue.Value);
            }
        }

        [TestMethod]
        public void When_ordering_by_int_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1.NumberOfChildren = 2;
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2.NumberOfChildren = 5;
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("numberofchildren", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).NumberOfChildren;

                Assert.AreEqual(5, firstResultValue.Value);
            }
        }

        [TestMethod]
        public void When_ordering_by_datetime_ascending_fields_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var now = DateTime.Now;
                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1.LastOnHoldTime = now;
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2.LastOnHoldTime = now.AddDays(1);
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("lastonholdtime", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).LastOnHoldTime;

                Assert.AreEqual(now, firstResultValue.Value);
            }
        }

        [TestMethod]
        public void When_ordering_by_datetime_descending_fields_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var now = DateTime.Now;
                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1.LastOnHoldTime = now;
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2.LastOnHoldTime = now.AddDays(1);
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("lastonholdtime", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).LastOnHoldTime;

                Assert.AreEqual(now.AddDays(1), firstResultValue.Value);
            }
        }

        [TestMethod]
        public void When_ordering_by_guid_fields_ascending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var g1 = new Guid(1, 2, 3, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
                var g2 = new Guid(2, 2, 3, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1.Address1_AddressId = g1;
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2.Address1_AddressId = g2;
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("address1_addressid", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).Address1_AddressId;

                Assert.AreEqual(g1, firstResultValue.Value);
            }
        }

        [TestMethod]
        public void When_ordering_by_guid_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var g1 = new Guid(1, 2, 3, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
                var g2 = new Guid(2, 2, 3, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1.Address1_AddressId = g1;
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2.Address1_AddressId = g2;
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("address1_addressid", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).Address1_AddressId;

                Assert.AreEqual(g2, firstResultValue.Value);
            }
        }

        [TestMethod]
        public void When_ordering_by_decimal_fields_ascending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1["exchangerate"] = 20m;
                contact1.Id = orgGodService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2["exchangerate"] = 50m;
                contact2.Id = orgGodService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("exchangerate", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact)["exchangerate"];

                Assert.AreEqual(20m, firstResultValue);
            }
        }

        [TestMethod]
        public void When_ordering_by_decimal_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {
                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1["exchangerate"] = 20m;
                contact1.Id = orgGodService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2["exchangerate"] = 50m;
                contact2.Id = orgGodService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("exchangerate", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact)["exchangerate"];

                Assert.AreEqual(50m, firstResultValue);
            }
        }

        [TestMethod]
        public void When_ordering_by_double_fields_ascending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1.Address1_Latitude = 2;
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2.Address1_Latitude = 5;
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("address1_latitude", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).Address1_Latitude;

                Assert.AreEqual(2, firstResultValue.Value);
            }
        }

        [TestMethod]
        public void When_ordering_by_double_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1.Address1_Latitude = 2;
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2.Address1_Latitude = 5;
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("address1_latitude", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).Address1_Latitude;

                Assert.AreEqual(5, firstResultValue.Value);
            }
        }

        [TestMethod]
        public void When_ordering_by_boolean_fields_ascending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1.DoNotEMail = false;
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2.DoNotEMail = true;
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("donotemail", OrderType.Ascending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).DoNotEMail;

                Assert.IsFalse(firstResultValue.Value);
            }
        }

        [TestMethod]
        public void When_ordering_by_boolean_fields_descending_expected_result_is_returned()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var contact1 = new Contact();
                contact1.FirstName = "Fred";
                contact1.DoNotEMail = false;
                contact1.Id = orgAdminService.Create(contact1);

                var contact2 = new Contact();
                contact2.FirstName = "Jo";
                contact2.DoNotEMail = true;
                contact2.Id = orgAdminService.Create(contact2);

                QueryExpression qry = new QueryExpression(Contact.EntityLogicalName);
                qry.ColumnSet = new ColumnSet(true);
                qry.AddOrder("donotemail", OrderType.Descending);
                var results = orgAdminService.RetrieveMultiple(qry);

                var firstResultValue = (results.Entities[0] as Contact).DoNotEMail;

                Assert.IsTrue(firstResultValue.Value);
            }
        }

        [TestMethod]
        public void When_ordering_by_2_columns_simultaneously_right_result_is_returned_asc_desc()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var account11 = new Account() { Name = "11", ImportSequenceNumber = 1, NumberOfEmployees = 1 };
                var account12 = new Account() { Name = "12", ImportSequenceNumber = 1, NumberOfEmployees = 2 };
                var account21 = new Account() { Name = "21", ImportSequenceNumber = 2, NumberOfEmployees = 1 };
                var account22 = new Account() { Name = "22", ImportSequenceNumber = 2, NumberOfEmployees = 2 };
                var account31 = new Account() { Name = "31", ImportSequenceNumber = 3, NumberOfEmployees = 1 };
                var account32 = new Account() { Name = "32", ImportSequenceNumber = 3, NumberOfEmployees = 2 };

                crm.PopulateWith(account11, account12, account21, account22, account31, account32);

                QueryExpression query = new QueryExpression()
                {
                    EntityName = "account",
                    ColumnSet = new ColumnSet(true),
                    Orders =
                    {
                    new OrderExpression("importsequencenumber", OrderType.Ascending),
                    new OrderExpression("numberofemployees", OrderType.Descending)
                }
                };

                EntityCollection ec = orgAdminService.RetrieveMultiple(query);
                var names = ec.Entities.Select(e => e.ToEntity<Account>().Name).ToList();

                Assert.IsTrue(names[0].Equals("12"), "Test 12 failed");
                Assert.IsTrue(names[1].Equals("11"), "Test 11 failed");
                Assert.IsTrue(names[2].Equals("22"), "Test 22 failed");
                Assert.IsTrue(names[3].Equals("21"), "Test 21 failed");
                Assert.IsTrue(names[4].Equals("32"), "Test 32 failed");
                Assert.IsTrue(names[5].Equals("31"), "Test 31 failed");
            }
        }

        [TestMethod]
        public void When_ordering_by_2_columns_simultaneously_right_result_is_returned_asc_asc()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var account11 = new Account() { Name = "11", ImportSequenceNumber = 1, NumberOfEmployees = 1 };
                var account12 = new Account() { Name = "12", ImportSequenceNumber = 1, NumberOfEmployees = 2 };
                var account21 = new Account() { Name = "21", ImportSequenceNumber = 2, NumberOfEmployees = 1 };
                var account22 = new Account() { Name = "22", ImportSequenceNumber = 2, NumberOfEmployees = 2 };
                var account31 = new Account() { Name = "31", ImportSequenceNumber = 3, NumberOfEmployees = 1 };
                var account32 = new Account() { Name = "32", ImportSequenceNumber = 3, NumberOfEmployees = 2 };

                crm.PopulateWith(account11, account12, account21, account22, account31, account32);

                QueryExpression query = new QueryExpression()
                {
                    EntityName = "account",
                    ColumnSet = new ColumnSet(true),
                    Orders =
                    {
                    new OrderExpression("importsequencenumber", OrderType.Ascending),
                    new OrderExpression("numberofemployees", OrderType.Ascending)
                }
                };

                EntityCollection ec = orgAdminService.RetrieveMultiple(query);
                var names = ec.Entities.Select(e => e.ToEntity<Account>().Name).ToList();
                Assert.IsTrue(names[0].Equals("11"), "Test 11 failed");
                Assert.IsTrue(names[1].Equals("12"), "Test 12 failed");
                Assert.IsTrue(names[2].Equals("21"), "Test 21 failed");
                Assert.IsTrue(names[3].Equals("22"), "Test 22 failed");
                Assert.IsTrue(names[4].Equals("31"), "Test 31 failed");
                Assert.IsTrue(names[5].Equals("32"), "Test 32 failed");
            }
        }

        [TestMethod]
        public void When_ordering_by_2_columns_simultaneously_right_result_is_returned_desc_desc()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var account11 = new Account() { Name = "11", ImportSequenceNumber = 1, NumberOfEmployees = 1 };
                var account12 = new Account() { Name = "12", ImportSequenceNumber = 1, NumberOfEmployees = 2 };
                var account21 = new Account() { Name = "21", ImportSequenceNumber = 2, NumberOfEmployees = 1 };
                var account22 = new Account() { Name = "22", ImportSequenceNumber = 2, NumberOfEmployees = 2 };
                var account31 = new Account() { Name = "31", ImportSequenceNumber = 3, NumberOfEmployees = 1 };
                var account32 = new Account() { Name = "32", ImportSequenceNumber = 3, NumberOfEmployees = 2 };

                crm.PopulateWith(account11, account12, account21, account22, account31, account32);

                QueryExpression query = new QueryExpression()
                {
                    EntityName = "account",
                    ColumnSet = new ColumnSet(true),
                    Orders =
                    {
                    new OrderExpression("importsequencenumber", OrderType.Descending),
                    new OrderExpression("numberofemployees", OrderType.Descending)
                }
                };

                EntityCollection ec = orgAdminService.RetrieveMultiple(query);
                var names = ec.Entities.Select(e => e.ToEntity<Account>().Name).ToList();

                Assert.IsTrue(names[0].Equals("32"), "Test 32 failed");
                Assert.IsTrue(names[1].Equals("31"), "Test 31 failed");
                Assert.IsTrue(names[2].Equals("22"), "Test 22 failed");
                Assert.IsTrue(names[3].Equals("21"), "Test 21 failed");
                Assert.IsTrue(names[4].Equals("12"), "Test 12 failed");
                Assert.IsTrue(names[5].Equals("11"), "Test 11 failed");
            }
        }

        [TestMethod]
        public void When_ordering_by_2_columns_simultaneously_right_result_is_returned_desc_asc()
        {
            using (var context = new Xrm(orgAdminService))
            {

                var account11 = new Account() { Name = "11", ImportSequenceNumber = 1, NumberOfEmployees = 1 };
                var account12 = new Account() { Name = "12", ImportSequenceNumber = 1, NumberOfEmployees = 2 };
                var account21 = new Account() { Name = "21", ImportSequenceNumber = 2, NumberOfEmployees = 1 };
                var account22 = new Account() { Name = "22", ImportSequenceNumber = 2, NumberOfEmployees = 2 };
                var account31 = new Account() { Name = "31", ImportSequenceNumber = 3, NumberOfEmployees = 1 };
                var account32 = new Account() { Name = "32", ImportSequenceNumber = 3, NumberOfEmployees = 2 };

                crm.PopulateWith(account11, account12, account21, account22, account31, account32);

                QueryExpression query = new QueryExpression()
                {
                    EntityName = "account",
                    ColumnSet = new ColumnSet(true),
                    Orders =
                    {
                    new OrderExpression("importsequencenumber", OrderType.Descending),
                    new OrderExpression("numberofemployees", OrderType.Ascending)
                }
                };

                EntityCollection ec = orgAdminService.RetrieveMultiple(query);
                var names = ec.Entities.Select(e => e.ToEntity<Account>().Name).ToList();

                Assert.IsTrue(names[0].Equals("31"), "Test 31 failed");
                Assert.IsTrue(names[1].Equals("32"), "Test 32 failed");
                Assert.IsTrue(names[2].Equals("21"), "Test 21 failed");
                Assert.IsTrue(names[3].Equals("22"), "Test 22 failed");
                Assert.IsTrue(names[4].Equals("11"), "Test 11 failed");
                Assert.IsTrue(names[5].Equals("12"), "Test 12 failed");
            }
        }
    }
}
