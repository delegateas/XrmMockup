using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace DG.Tools.XrmMockup.Database
{
    internal static class DbAttributeTypeHelper
    {
        private static Dictionary<AttributeTypeCode, Type> _metadataTypeMap = new Dictionary<AttributeTypeCode, Type>() {
            { AttributeTypeCode.BigInt, typeof(long) },
            { AttributeTypeCode.Boolean, typeof(bool) },
            { AttributeTypeCode.DateTime, typeof(DateTime) },
            { AttributeTypeCode.Decimal, typeof(decimal) },
            { AttributeTypeCode.Double, typeof(double) },
            
            { AttributeTypeCode.EntityName, typeof(int) },
            { AttributeTypeCode.Picklist, typeof(int) },
            { AttributeTypeCode.State, typeof(int) },
            { AttributeTypeCode.Status, typeof(int) },

            { AttributeTypeCode.Integer, typeof(int) },
            { AttributeTypeCode.Lookup,  typeof(DbRow) },
            // TODO: Figure out type
            //{ AttributeTypeCode.ManagedProperty, typeof(object) },
            { AttributeTypeCode.Memo, typeof(string) },
            { AttributeTypeCode.Money, typeof(decimal) },
            { AttributeTypeCode.Owner, typeof(DbRow) },
            { AttributeTypeCode.Customer, typeof(DbRow) },
            { AttributeTypeCode.PartyList, typeof(DbRow[]) },
            { AttributeTypeCode.String, typeof(string) },
            { AttributeTypeCode.Uniqueidentifier, typeof(Guid) },
        };

        public static bool IsValidType(AttributeMetadata attrMetadata, object value) {
            if (value == null) return true;
            _metadataTypeMap.TryGetValue(attrMetadata.AttributeType.Value, out Type expectedType);
            if (expectedType == null) {
                throw new NotImplementedException($"Attribute of type '{attrMetadata.AttributeType.Value}' is not implemeted in XrmMockup yet.");
            }
            return expectedType == value.GetType();
        }
    }
}
