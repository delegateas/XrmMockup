﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmContext;
using Xunit;
using Xunit.Sdk;
using DG.Tools.XrmMockup;

namespace DG.XrmMockupTest
{
    public class TestClone : UnitTestBase
    {
        public TestClone(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void CloningAnEntityShouldProperlyDisconnectObjectProperties()
        {
            var account = new Entity("account");
            var money = new Money(123.45M);
            account["marketcap"] = money;
            
            var account2 = (account as Entity).CloneEntity();
            Assert.Equal(123.45M, account2.GetAttributeValue<Money>("marketcap").Value);
            
            money.Value = 333.33M;
            Assert.Equal(123.45M, account2.GetAttributeValue<Money>("marketcap").Value);
        }
    }
}
