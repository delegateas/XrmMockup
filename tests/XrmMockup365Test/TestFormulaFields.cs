using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using System;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestFormulaFields : UnitTestBase, IOrganizationServiceFactory
    {
        public TestFormulaFields(XrmMockupFixture fixture) : base(fixture) { }

        [Theory]
        [InlineData("123", "1 + 1", "2")]
        [InlineData("123", "name & \" - \" & accountnumber", "Test - 123")]
        [InlineData("123", "Concatenate(name, \" - \", Text(accountnumber))", "Test - 123")]
        [InlineData("123", "If(Decimal(accountnumber) > 0, name & \" - \" & accountnumber, \"No Account Number\")", "Test - 123")]
        [InlineData("0", "If(Decimal(accountnumber) > 0, name & \" - \" & accountnumber, \"No Account Number\")", "No Account Number")]
        public async System.Threading.Tasks.Task CanEvaluateExpressionOnEntity(string accountNumber, string formula, string expected)
        {
            var evaluator = new FormulaFieldEvaluator(this);

            var account = Create(new Account { Name = "Test", AccountNumber = accountNumber });
            var result = await evaluator.Evaluate(formula, account);
            Assert.Equal(expected, result.ToString());
        }

        [Fact]
        public async System.Threading.Tasks.Task CanEvaluateExpressionWithRelatedEntity()
        {
            var evaluator = new FormulaFieldEvaluator(this);

            var adminUserId = Guid.Parse("3b961284-cd7a-4fa3-af7e-89802e88dd5c");
            var animal = Create(new dg_animal { dg_name = "Fluffy", OwnerId = new EntityReference(SystemUser.EntityLogicalName, adminUserId) });
            var animalFood = Create(new dg_animalfood { dg_AnimalId = new EntityReference(dg_animal.EntityLogicalName, animal.Id), dg_name = "Meatballs" });

            var result = await evaluator.Evaluate("\"Test: \" & dg_animalowner", animal);
            Assert.Equal("Test: Fluffy is a very good animal, and Admin loves them very much", result);

            result = await evaluator.Evaluate("If(dg_AnimalId.dg_name = \"Fluffy\", \"Test: \" & dg_AnimalId.dg_name & \" eats \" & dg_name, \"New telegraph, who dis?\")", animalFood);
            Assert.Equal("Test: Fluffy eats Meatballs", result);
        }

        [Fact(Skip = "Function not implemented in Eval yet")]
        public async System.Threading.Tasks.Task CanEvaluateExpressionWithUtcToday()
        {
            var evaluator = new FormulaFieldEvaluator(this);

            var result = await evaluator.Evaluate("UTCToday()", new dg_animal());
            Assert.NotNull(result);
        }

        private TEntity Create<TEntity>(TEntity entity) where TEntity : Entity
        {
            var id = orgAdminService.Create(entity);

            return orgAdminService.Retrieve(entity.LogicalName, id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true)).ToEntity<TEntity>();
        }

        public IOrganizationService CreateOrganizationService(Guid? userId)
        {
            if (userId == null || userId == Guid.Empty)
            {
                return orgAdminService;
            }
            else if (userId == testUser1.Id)
            {
                return testUser1Service;
            }
            else if (userId == testUser2.Id)
            {
                return testUser2Service;
            }
            else if (userId == testUser3.Id)
            {
                return testUser3Service;
            }
            else if (userId == testUser4.Id)
            {
                return testUser4Service;
            }
            else
            {
                throw new ArgumentException($"Unknown userId: {userId}");
            }
        }
    }
}
