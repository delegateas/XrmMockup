using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq.Expressions;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;

namespace DG.XrmContext
{

    public enum EmptyEnum { }

    public abstract partial class ExtendedEntity<State, Status> : Entity
        where State : struct, IComparable, IConvertible, IFormattable
        where Status : struct, IComparable, IConvertible, IFormattable
    {

        public ExtendedEntity(string entityName) : base(entityName) { }

        public ExtendedEntity(string entityName, Guid id) : base(entityName)
        {
            Id = id;
        }

        protected string GetDebuggerDisplay(string primaryNameAttribute)
        {
            string display = GetType().Name;

            var name = GetAttributeValue<string>(primaryNameAttribute);
            if (!string.IsNullOrEmpty(name)) display += string.Format(" ({0})", name);
            if (Id != Guid.Empty) display += string.Format(" [{0}]", Id);

            return display;
        }

        protected T? GetOptionSetValue<T>(string attributeName) where T : struct, IComparable, IConvertible, IFormattable
        {
            var optionSet = GetAttributeValue<OptionSetValue>(attributeName);
            if (optionSet != null)
            {
                return (T)Enum.ToObject(typeof(T), optionSet.Value);
            }
            else
            {
                return null;
            }
        }

        protected void SetOptionSetValue<T>(string attributeName, T value)
        {
            if (value != null)
            {
                SetAttributeValue(attributeName, new OptionSetValue((int)(object)value));
            }
            else
            {
                SetAttributeValue(attributeName, null);
            }
        }

        protected decimal? GetMoneyValue(string attributeName)
        {
            var money = GetAttributeValue<Money>(attributeName);
            if (money != null)
            {
                return money.Value;
            }
            else
            {
                return null;
            }
        }

        protected void SetMoneyValue(string attributeName, decimal? value)
        {
            if (value.HasValue)
            {
                SetAttributeValue(attributeName, new Money(value.Value));
            }
            else
            {
                SetAttributeValue(attributeName, null);
            }
        }

        protected IEnumerable<T> GetEntityCollection<T>(string attributeName) where T : Entity
        {
            var collection = GetAttributeValue<EntityCollection>(attributeName);
            if (collection != null && collection.Entities != null)
            {
                return collection.Entities.Select(x => x as T);
            }
            else
            {
                return null;
            }
        }

        protected void SetEntityCollection<T>(string attributeName, IEnumerable<T> entities) where T : Entity
        {
            if (entities != null)
            {
                SetAttributeValue(attributeName, new EntityCollection(new List<Entity>(entities)));
            }
            else
            {
                SetAttributeValue(attributeName, null);
            }
        }

        protected void SetId(string primaryIdAttribute, Guid? guid)
        {
            base.Id = guid.GetValueOrDefault();
            SetAttributeValue(primaryIdAttribute, guid);
        }


        protected KeyValuePair<string, object>[] DeltaAttributes;

        public void TagForDelta()
        {
            DeltaAttributes = new KeyValuePair<string, object>[Attributes.Count];
            Attributes.CopyTo(DeltaAttributes, 0);
        }

        public void PerformDelta()
        {
            if (DeltaAttributes == null) return;
            var guid = Id;

            foreach (var prev in DeltaAttributes)
            {
                if (!Attributes.ContainsKey(prev.Key)) continue;
                if (Object.Equals(Attributes[prev.Key], prev.Value)) Attributes.Remove(prev.Key);
            }
            if (guid != Guid.Empty) Id = guid;
        }



        public static T Retrieve<T>(IOrganizationService service, Guid id, params Expression<Func<T, object>>[] attributes) where T : Entity
        {
            return service.Retrieve(id, attributes);
        }

        public SetStateResponse SetState(IOrganizationService service, State state)
        {
            return SetState(service, state, (Status)(object)-1);
        }

        public SetStateResponse SetState(IOrganizationService service, State state, Status status)
        {
            return service.Execute(MakeSetStateRequest(state, status)) as SetStateResponse;
        }

        public AssignResponse AssignTo(IOrganizationService service, EntityReference owner)
        {
            return service.Execute(MakeAssignRequest(owner)) as AssignResponse;
        }

        public SetStateRequest MakeSetStateRequest(State state, Status status)
        {
            var req = new SetStateRequest();
            req.EntityMoniker = ToEntityReference();
            req.State = new OptionSetValue((int)(object)state);
            req.Status = new OptionSetValue((int)(object)status);
            return req;
        }

        public AssignRequest MakeAssignRequest(EntityReference owner)
        {
            return new AssignRequest() { Assignee = owner, Target = ToEntityReference() };
        }

        public CreateRequest MakeCreateRequest()
        {
            return new CreateRequest() { Target = this };
        }

        public UpdateRequest MakeUpdateRequest()
        {
            return new UpdateRequest() { Target = this };
        }

        public DeleteRequest MakeDeleteRequest()
        {
            return new DeleteRequest() { Target = ToEntityReference() };
        }

    }

    public interface IEntity
    {
        object this[string attributeName] { get; set; }
        AttributeCollection Attributes { get; set; }
        EntityState? EntityState { get; set; }
        ExtensionDataObject ExtensionData { get; set; }
        FormattedValueCollection FormattedValues { get; }
        Guid Id { get; set; }
        string LogicalName { get; set; }
        RelatedEntityCollection RelatedEntities { get; }
        string RowVersion { get; set; }
        bool Contains(string attributeName);
        T GetAttributeValue<T>(string attributeLogicalName);
        T ToEntity<T>() where T : Entity;
        EntityReference ToEntityReference();
    }

    public abstract partial class ExtendedOrganizationServiceContext : OrganizationServiceContext
    {

        public ExtendedOrganizationServiceContext(IOrganizationService service) : base(service) { }

        public U Load<T, U>(T entity, Expression<Func<T, U>> loaderFunc) where T : Entity
        {
            LoadProperty(entity, XrmExtensions.GetMemberName(loaderFunc));
            return loaderFunc.Compile().Invoke(entity);
        }

        public IEnumerable<U> LoadEnumeration<T, U>(T entity, Expression<Func<T, IEnumerable<U>>> loaderFunc) where T : Entity
        {
            return Load(entity, loaderFunc) ?? new List<U>();
        }

    }

    public static class XrmExtensions
    {

        public static T Retrieve<T>(this IOrganizationService service, Guid id, params Expression<Func<T, object>>[] attributes) where T : Entity
        {
            return service.Retrieve(Activator.CreateInstance<T>().LogicalName, id, GetColumnSet(attributes)).ToEntity<T>();
        }


        public static AssignResponse Assign(this IOrganizationService service, EntityReference target, EntityReference owner)
        {
            var req = new AssignRequest() { Target = target, Assignee = owner };
            return service.Execute(req) as AssignResponse;
        }

        public static SetStateResponse SetState<T, U>(this IOrganizationService service, ExtendedEntity<T, U> entity, T state)
                where T : struct, IComparable, IConvertible, IFormattable
                where U : struct, IComparable, IConvertible, IFormattable
        {
            return entity.SetState(service, state);
        }

        public static SetStateResponse SetState<T, U>(this IOrganizationService service, ExtendedEntity<T, U> entity, T state, U status)
                where T : struct, IComparable, IConvertible, IFormattable
                where U : struct, IComparable, IConvertible, IFormattable
        {
            return entity.SetState(service, state, status);
        }

        public static List<ExecuteMultipleResponseItem> PerformAsBulk<T>(this IOrganizationService service, IEnumerable<T> requests, bool continueOnError = true, int chunkSize = 1000) where T : OrganizationRequest
        {
            var arr = requests.ToArray();
            var splitReqs = from i in Enumerable.Range(0, arr.Length)
                            group arr[i] by i / chunkSize;

            var resps = new List<ExecuteMultipleResponseItem>();
            foreach (var rs in splitReqs)
            {
                var req = new ExecuteMultipleRequest();
                req.Requests = new OrganizationRequestCollection();
                req.Requests.AddRange(rs);
                req.Settings = new ExecuteMultipleSettings();
                req.Settings.ContinueOnError = continueOnError;
                req.Settings.ReturnResponses = true;
                var resp = service.Execute(req) as ExecuteMultipleResponse;
                resps.AddRange(resp.Responses);
            }
            return resps;
        }

        public static ColumnSet GetColumnSet<T>(params Expression<Func<T, object>>[] attributes)
        {
            if (attributes == null) return new ColumnSet();
            if (attributes.Length == 0) return new ColumnSet(true);
            return new ColumnSet(attributes.Select(a => GetMemberName(a).ToLower()).ToArray());
        }

        public static string GetMemberName<T, U>(Expression<Func<T, U>> lambda)
        {
            MemberExpression body = lambda.Body as MemberExpression;
            if (body == null)
            {
                UnaryExpression ubody = (UnaryExpression)lambda.Body;
                body = ubody.Operand as MemberExpression;
            }
            return body.Member.Name;
        }

        public static bool ContainsAttributes<T>(this T entity, params Expression<Func<T, object>>[] attrGetters) where T : Entity
        {
            if (attrGetters == null) return true;
            return attrGetters.Select(a => GetMemberName(a).ToLower()).All(a => entity.Contains(a));
        }

        public static bool RemoveAttributes<T>(this T entity, params Expression<Func<T, object>>[] attrGetters) where T : Entity
        {
            if (attrGetters == null) return false;
            return attrGetters.Select(a => GetMemberName(a).ToLower()).Any(a => entity.Attributes.Remove(a));
        }
    }
}
