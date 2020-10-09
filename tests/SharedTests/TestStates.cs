using System;
using Xunit;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Crm.Sdk.Messages;
using Xunit.Sdk;

namespace DG.XrmMockupTest
{
    public class TestStates : UnitTestBase
    {
        public TestStates(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestSetState()
        {
            var acc = new Account()
            {
                StateCode = AccountState.Active,
                StatusCode = Account_StatusCode.Active
            };
            acc.Id = orgAdminUIService.Create(acc);
            acc.SetState(orgAdminUIService, AccountState.Inactive, Account_StatusCode.Inactive);

            var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
            Assert.Equal(AccountState.Inactive, retrieved.StateCode);
        }

#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013)
        [Fact]
        public void TestStatusTransitions()
        {
            var man = new dg_man()
            {
                statecode = dg_manState.Active,
                statuscode = dg_man_statuscode.Active
            };
            man.Id = orgAdminUIService.Create(man);
            man.SetState(orgAdminUIService, dg_manState.Inactive, dg_man_statuscode.Inactive);

            var retrieved = orgAdminUIService.Retrieve(dg_man.EntityLogicalName, man.Id, new ColumnSet(true)) as dg_man;
            Assert.Equal(dg_manState.Inactive, retrieved.statecode);
            Assert.Equal(dg_man_statuscode.Inactive, retrieved.statuscode);

            try
            {
                man.SetState(orgAdminUIService, dg_manState.Active, dg_man_statuscode.Active);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition()
        {
            var man = new dg_man()
            {
                statecode = dg_manState.Active,
                statuscode = dg_man_statuscode.Active
            };
            man.Id = orgAdminUIService.Create(man);

            var retrieved = orgAdminUIService.Retrieve(dg_man.EntityLogicalName, man.Id, new ColumnSet(true)) as dg_man;

            var request = new IsValidStateTransitionRequest
            {
                Entity = retrieved.ToEntityReference(),
                NewState = dg_manState.Inactive.ToString(), 
                NewStatus = (int)dg_man_statuscode.Inactive
            };

            var response = orgAdminUIService.Execute(request) as IsValidStateTransitionResponse;
            Assert.True(response.IsValid);
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenStateCodeInvalid()
        {
            var man = new dg_man()
            {
                statecode = dg_manState.Active,
                statuscode = dg_man_statuscode.Active
            };
            man.Id = orgAdminUIService.Create(man);

            var retrieved = orgAdminUIService.Retrieve(dg_man.EntityLogicalName, man.Id, new ColumnSet(true)) as dg_man;

            var request = new IsValidStateTransitionRequest
            {
                Entity = retrieved.ToEntityReference(),
                NewState = dg_manState.Active.ToString(),
                NewStatus = (int)dg_man_statuscode.Inactive
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenStatusCodeInvalid()
        {
            var man = new dg_man()
            {
                statecode = dg_manState.Active,
                statuscode = dg_man_statuscode.Active
            };
            man.Id = orgAdminUIService.Create(man);

            var retrieved = orgAdminUIService.Retrieve(dg_man.EntityLogicalName, man.Id, new ColumnSet(true)) as dg_man;

            var request = new IsValidStateTransitionRequest
            {
                Entity = retrieved.ToEntityReference(),
                NewState = dg_manState.Inactive.ToString(),
                NewStatus = (int)dg_man_statuscode.Active
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenEntityIsMissing()
        {
            var request = new IsValidStateTransitionRequest
            {
                NewState = dg_manState.Active.ToString(),
                NewStatus = (int)dg_man_statuscode.Active
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenNewStateIsMissing()
        {
            var man = new dg_man()
            {
                statecode = dg_manState.Active,
                statuscode = dg_man_statuscode.Active
            };
            man.Id = orgAdminUIService.Create(man);

            var retrieved = orgAdminUIService.Retrieve(dg_man.EntityLogicalName, man.Id, new ColumnSet(true)) as dg_man;

            var request = new IsValidStateTransitionRequest
            {
                Entity = retrieved.ToEntityReference(),
                NewStatus = (int)dg_man_statuscode.Active
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenNewStatusIsMissing()
        {
            var man = new dg_man()
            {
                statecode = dg_manState.Active,
                statuscode = dg_man_statuscode.Active
            };
            man.Id = orgAdminUIService.Create(man);

            var retrieved = orgAdminUIService.Retrieve(dg_man.EntityLogicalName, man.Id, new ColumnSet(true)) as dg_man;

            var request = new IsValidStateTransitionRequest
            {
                Entity = retrieved.ToEntityReference(),
                NewState = dg_manState.Active.ToString()
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenRecordDoesNotExist()
        {
            var request = new IsValidStateTransitionRequest
            {
                Entity = new Microsoft.Xrm.Sdk.EntityReference(dg_man.EntityLogicalName, Guid.NewGuid()),
                NewState = dg_manState.Active.ToString(),
                NewStatus = (int)dg_man_statuscode.Active
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenEnforceStateTransitionsFalse()
        {
            var field = new dg_field()
            {
                statecode = dg_fieldState.Active,
                statuscode = dg_field_statuscode.Active,
            };
            field.Id = orgAdminUIService.Create(field);

            var retrieved = orgAdminUIService.Retrieve(dg_field.EntityLogicalName, field.Id, new ColumnSet(true)) as dg_field;

            var request = new IsValidStateTransitionRequest
            {
                Entity = retrieved.ToEntityReference(),
                NewState = dg_fieldState.Inactive.ToString(),
                NewStatus = (int)dg_field_statuscode.Inactive
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }

        [Fact]
        public void TestIsValidStateTransition_FailsWhenSameState()
        {
            var man = new dg_man()
            {
                statecode = dg_manState.Active,
                statuscode = dg_man_statuscode.Active,
            };
            man.Id = orgAdminUIService.Create(man);
            var retrieved = orgAdminUIService.Retrieve(dg_man.EntityLogicalName, man.Id, new ColumnSet(true)) as dg_man;

            var request = new IsValidStateTransitionRequest
            {
                Entity = retrieved.ToEntityReference(),
                NewState = dg_manState.Active.ToString(),
                NewStatus = (int)dg_man_statuscode.Active
            };
            try
            {
                orgAdminUIService.Execute(request);
                throw new XunitException();
            }
            catch (Exception e)
            {
                Assert.IsType<FaultException>(e);
            }
        }
#endif

        [Fact]
        public void TestStateStatuscodeCreate()
        {
            var incidentId = orgAdminService.Create(new Incident { });
            var incident = orgAdminService.Retrieve(Incident.EntityLogicalName, incidentId, new ColumnSet(true)).ToEntity<Incident>();
            Assert.Equal(IncidentState.Active, incident.StateCode);
            Assert.Equal(Incident_StatusCode.OnHold, incident.StatusCode);

            var accId = orgAdminService.Create(new Account { StateCode = AccountState.Inactive });
            var acc = orgAdminService.Retrieve(Account.EntityLogicalName, accId, new ColumnSet(true)).ToEntity<Account>();
            Assert.Equal(AccountState.Active, acc.StateCode);
            Assert.Equal(Account_StatusCode.Somestatus, acc.StatusCode);

            accId = orgAdminService.Create(new Account { StatusCode = Account_StatusCode.Active });
            acc = orgAdminService.Retrieve(Account.EntityLogicalName, accId, new ColumnSet(true)).ToEntity<Account>();
            Assert.Equal(AccountState.Active, acc.StateCode);
            Assert.Equal(Account_StatusCode.Active, acc.StatusCode);
        }


        [Fact]
        public void TestStateStatuscodeUpdate()
        {
            var acc = new Account { };
            acc.Id = orgAdminService.Create(acc);
            var retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)).ToEntity<Account>();
            Assert.Equal(AccountState.Active, retrieved.StateCode);
            Assert.Equal(Account_StatusCode.Somestatus, retrieved.StatusCode);

            acc.StateCode = AccountState.Inactive;
            orgAdminService.Update(acc);
            retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)).ToEntity<Account>();
            Assert.Equal(AccountState.Inactive, retrieved.StateCode);
            Assert.Equal(Account_StatusCode.Inactive, retrieved.StatusCode);

            acc.StateCode = null;
            acc.StatusCode = Account_StatusCode.Active;
            orgAdminService.Update(acc);

            retrieved = orgAdminService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)).ToEntity<Account>();
            Assert.Equal(AccountState.Active, retrieved.StateCode);
            Assert.Equal(Account_StatusCode.Active, retrieved.StatusCode);
        }
    }
}
