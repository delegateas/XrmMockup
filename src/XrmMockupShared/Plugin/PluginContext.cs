using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup {
    internal class PluginContext : IPluginExecutionContext {

        private ParameterCollection _propertyBag = new ParameterCollection();

        private ParameterCollection _inputParameters = new ParameterCollection();
        private ParameterCollection _outputParameters = new ParameterCollection();
        private ParameterCollection _sharedVariables = new ParameterCollection();

        private EntityImageCollection _preEntityImages = new EntityImageCollection();
        private EntityImageCollection _postEntityImages = new EntityImageCollection();

        public int? ExtensionDepth;

        public PluginContext() {
            this.CorrelationId = Guid.NewGuid();
        }

        public Guid BusinessUnitId {
            get {
                _propertyBag.TryGetValue("BusinessUnitId", out object BusinessUnitId);
                return (Guid)BusinessUnitId;
            }
            set {
                _propertyBag["BusinessUnitId"] = value;
            }
        }

        public Guid CorrelationId {
            get {
                _propertyBag.TryGetValue("CorrelationId", out object CorrelationId);
                return (Guid)CorrelationId;
            }
            set {
                _propertyBag["CorrelationId"] = value;
            }
        }

        public int Depth {
            get {
                _propertyBag.TryGetValue("Depth", out object Depth);
                return (int)Depth;
            }
            set {
                _propertyBag["Depth"] = value;
            }
        }

        public Guid InitiatingUserId {
            get {
                _propertyBag.TryGetValue("InitiatingUserId", out object InitiatingUserId);
                return (Guid)InitiatingUserId;
            }
            set {
                _propertyBag["InitiatingUserId"] = value;
            }
        }

        public ParameterCollection InputParameters {
            get {
                return _inputParameters;
            }
        }

        public bool IsExecutingOffline {
            get {
                _propertyBag.TryGetValue("IsExecutingOffline", out object IsExecutingOffline);
                return (bool)IsExecutingOffline;
            }
            set {
                _propertyBag["IsExecutingOffline"] = value;
            }
        }

        public bool IsInTransaction {
            get {
                _propertyBag.TryGetValue("IsInTransaction", out object IsInTransaction);
                return (bool)IsInTransaction;
            }
            set {
                _propertyBag["IsInTransaction"] = value;
            }
        }

        public bool IsOfflinePlayback {
            get {
                _propertyBag.TryGetValue("IsOfflinePlayback", out object IsOfflinePlayback);
                return (bool)IsOfflinePlayback;
            }
            set {
                _propertyBag["IsOfflinePlayback"] = value;
            }
        }

        public int IsolationMode {
            get {
                _propertyBag.TryGetValue("IsolationMode", out object IsolationMode);
                return (int)IsolationMode;
            }
            set {
                _propertyBag["IsolationMode"] = value;
            }
        }

        public string MessageName {
            get {
                _propertyBag.TryGetValue("MessageName", out object MessageName);
                return (string)MessageName;
            }
            set {
                _propertyBag["MessageName"] = value;
            }
        }

        public int Mode {
            get {
                _propertyBag.TryGetValue("Mode", out object Mode);
                return (int)Mode;
            }
            set {
                _propertyBag["Mode"] = value;
            }
        }

        public DateTime OperationCreatedOn {
            get {
                _propertyBag.TryGetValue("OperationCreatedOn", out object OperationCreatedOn);
                return (DateTime)OperationCreatedOn;
            }
            set {
                _propertyBag["OperationCreatedOn"] = value;
            }
        }

        public Guid OperationId {
            get {
                _propertyBag.TryGetValue("OperationId", out object OperationId);
                return (Guid)OperationId;
            }
            set {
                _propertyBag["OperationId"] = value;
            }
        }

        public Guid OrganizationId {
            get {
                _propertyBag.TryGetValue("OrganizationId", out object OrganizationId);
                return (Guid)OrganizationId;
            }
            set {
                _propertyBag["OrganizationId"] = value;
            }
        }

        public string OrganizationName {
            get {
                _propertyBag.TryGetValue("OrganizationName", out object OrganizationName);
                return (string)OrganizationName;
            }
            set {
                _propertyBag["OrganizationName"] = value;
            }
        }

        public ParameterCollection OutputParameters {
            get {
                return _outputParameters;
            }
        }

        public EntityReference OwningExtension {
            get {
                _propertyBag.TryGetValue("OwningExtension", out object OwningExtension);
                return (EntityReference)OwningExtension;
            }
            set {
                _propertyBag["OwningExtension"] = value;
            }
        }

        public IPluginExecutionContext ParentContext {
            get {
                _propertyBag.TryGetValue("ParentContext", out object ParentContext);
                return (IPluginExecutionContext)ParentContext;
            }
            set {
                _propertyBag["ParentContext"] = value;
            }
        }

        public EntityImageCollection PostEntityImages {
            get {
                return _postEntityImages;
            }
        }

        public EntityImageCollection PreEntityImages {
            get {
                return _preEntityImages;
            }
        }

        public Guid PrimaryEntityId {
            get {
                _propertyBag.TryGetValue("PrimaryEntityId", out object PrimaryEntityId);
                return (Guid)PrimaryEntityId;
            }
            set {
                _propertyBag["PrimaryEntityId"] = value;
            }
        }

        public string PrimaryEntityName {
            get {
                _propertyBag.TryGetValue("PrimaryEntityName", out object PrimaryEntityName);
                return (string)PrimaryEntityName;
            }
            set {
                _propertyBag["PrimaryEntityName"] = value;
            }
        }

        public Guid? RequestId {
            get {
                _propertyBag.TryGetValue("RequestId", out object RequestId);
                return (Guid?)RequestId;
            }
            set {
                _propertyBag["RequestId"] = value;
            }
        }

        public string SecondaryEntityName {
            get {
                _propertyBag.TryGetValue("SecondaryEntityName", out object SecondaryEntityName);
                return (string)SecondaryEntityName;
            }
            set {
                _propertyBag["SecondaryEntityName"] = value;
            }
        }

        public ParameterCollection SharedVariables {
            get {
                return _sharedVariables;
            }
        }

        public int Stage {
            get {
                _propertyBag.TryGetValue("Stage", out object Stage);
                return (int)Stage;
            }
            set {
                _propertyBag["Stage"] = value;
            }
        }

        public Guid UserId {
            get {
                _propertyBag.TryGetValue("UserId", out object UserId);
                return (Guid)UserId;
            }
            set {
                _propertyBag["UserId"] = value;
            }
        }

        public PluginContext Clone() {
            var clone = new PluginContext();
            CloneDataCollection(this._propertyBag, clone._propertyBag, CloneFunc);
            CloneDataCollection(this._inputParameters, clone._inputParameters, CloneFunc);
            CloneDataCollection(this._outputParameters, clone._outputParameters, CloneFunc);
            CloneDataCollection(this._sharedVariables, clone._sharedVariables, CloneFunc);

            CloneDataCollection(this._postEntityImages, clone._postEntityImages, x => x.CloneEntity());
            CloneDataCollection(this._preEntityImages, clone._preEntityImages, x => x.CloneEntity());
            return clone;
        }

        private void CloneDataCollection<T>(DataCollection<string,T> from, DataCollection<string, T> to, Func<T,T> cloneHelper) {
            foreach (var keyVal in from) {
                to[keyVal.Key] = cloneHelper(keyVal.Value);
            }
        }

        private object CloneFunc(object obj) {
            var entityRef = obj as EntityReference;
            if (entityRef != null) {
                return new EntityReference(entityRef.LogicalName, entityRef.Id);
            }
            return obj;
        }

    }
}
