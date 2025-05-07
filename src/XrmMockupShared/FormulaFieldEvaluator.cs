using Microsoft.PowerFx.Dataverse;
using Microsoft.PowerFx;
using Microsoft.Xrm.Sdk;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;

namespace DG.Tools.XrmMockup
{
    internal class FormulaFieldEvaluator
    {
        private readonly IOrganizationService _organizationService;
        private readonly DataverseConnection _dataverseConnection;

        public FormulaFieldEvaluator(IOrganizationServiceFactory serviceFactory)
        {
            _organizationService = serviceFactory.CreateOrganizationService(null);
            _dataverseConnection = SingleOrgPolicy.New(_organizationService);
        }

        public async Task<object> Evaluate(string formula, Entity thisEntity)
        {
            var rowScopeSymbols = _dataverseConnection.GetRowScopeSymbols(thisEntity.LogicalName, true);

            var engine = new RecalcEngine();
            var combinedSymbols = ReadOnlySymbolTable.Compose(rowScopeSymbols, _dataverseConnection.Symbols);
            var checkResult = engine.Check(formula, new ParserOptions(CultureInfo.InvariantCulture), combinedSymbols);
            checkResult.ThrowOnErrors();

            var thisRecord = _dataverseConnection.Marshal(thisEntity);
            var rowScopeValues = ReadOnlySymbolValues.Compose(
                ReadOnlySymbolValues.NewFromRecord(rowScopeSymbols, thisRecord),
                _dataverseConnection.SymbolValues
            );

            var computedValue = await checkResult.GetEvaluator().EvalAsync(CancellationToken.None, new RuntimeConfig(rowScopeValues));
            return computedValue.ToObject();
        }
    }
}
