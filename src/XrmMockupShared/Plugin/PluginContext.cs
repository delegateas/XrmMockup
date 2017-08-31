using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup.Plugin {

    internal class MockupPluginContext : IPluginExecutionContext {

        private ParameterCollection _propertyBag = new ParameterCollection();

        private ParameterCollection _inputParameters = new ParameterCollection();
        private ParameterCollection _outputParameters = new ParameterCollection();
        private ParameterCollection _sharedVariables = new ParameterCollection();

        private EntityImageCollection _preEntityImages = new EntityImageCollection();
        private EntityImageCollection _postEntityImages = new EntityImageCollection();

        public MockupPluginContext() {
            this.CorrelationId = Guid.NewGuid();
        }

        public Guid BusinessUnitId {
            get {
                return (Guid)_propertyBag["BusinessUnitId"];
            }
            set {
                _propertyBag["BusinessUnitId"] = value;
            }
        }

        public Guid CorrelationId {
            get {
                return (Guid)_propertyBag["CorrelationId"];
            }
            set {
                _propertyBag["CorrelationId"] = value;
            }
        }

        public int Depth {
            get {
                return (int)_propertyBag["Depth"];
            }
            set {
                _propertyBag["Depth"] = value;
            }
        }

        public Guid InitiatingUserId {
            get {
                return (Guid)_propertyBag["InitiatingUserId"];
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
                return (bool)_propertyBag["IsExecutingOffline"];
            }
            set {
                _propertyBag["IsExecutingOffline"] = value;
            }
        }

        public bool IsInTransaction {
            get {
                return (bool)_propertyBag["IsInTransaction"];
            }
            set {
                _propertyBag["IsInTransaction"] = value;
            }
        }

        public bool IsOfflinePlayback {
            get {
                return (bool)_propertyBag["IsOfflinePlayback"];
            }
            set {
                _propertyBag["IsOfflinePlayback"] = value;
            }
        }

        public int IsolationMode {
            get {
                return (int)_propertyBag["IsolationMode"];
            }
            set {
                _propertyBag["IsolationMode"] = value;
            }
        }

        public string MessageName {
            get {
                return (string)_propertyBag["MessageName"];
            }
            set {
                _propertyBag["MessageName"] = value;
            }
        }

        public int Mode {
            get {
                return (int)_propertyBag["Mode"];
            }
            set {
                _propertyBag["Mode"] = value;
            }
        }

        public DateTime OperationCreatedOn {
            get {
                return (DateTime)_propertyBag["OperationCreatedOn"];
            }
            set {
                _propertyBag["OperationCreatedOn"] = value;
            }
        }

        public Guid OperationId {
            get {
                return (Guid)_propertyBag["OperationId"];
            }
            set {
                _propertyBag["OperationId"] = value;
            }
        }

        public Guid OrganizationId {
            get {
                return (Guid)_propertyBag["OrganizationId"];
            }
            set {
                _propertyBag["OrganizationId"] = value;
            }
        }

        public string OrganizationName {
            get {
                return (string)_propertyBag["OrganizationName"];
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
                return (EntityReference)_propertyBag["OwningExtension"];
            }
            set {
                _propertyBag["OwningExtension"] = value;
            }
        }

        public IPluginExecutionContext ParentContext {
            get {
                return (IPluginExecutionContext)_propertyBag["ParentContext"];
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
                return (Guid)_propertyBag["PrimaryEntityId"];
            }
            set {
                _propertyBag["PrimaryEntityId"] = value;
            }
        }

        public string PrimaryEntityName {
            get {
                return (string)_propertyBag["PrimaryEntityName"];
            }
            set {
                _propertyBag["PrimaryEntityName"] = value;
            }
        }

        public Guid? RequestId {
            get {
                return (Guid?)_propertyBag["RequestId"];
            }
            set {
                _propertyBag["RequestId"] = value;
            }
        }

        public string SecondaryEntityName {
            get {
                return (string)_propertyBag["SecondaryEntityName"];
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
                return (int)_propertyBag["Stage"];
            }
            set {
                _propertyBag["Stage"] = value;
            }
        }

        public Guid UserId {
            get {
                return (Guid)_propertyBag["UserId"];
            }
            set {
                _propertyBag["UserId"] = value;
            }
        }

        public MockupPluginContext Clone() {
            var clone = new MockupPluginContext();
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
            if (obj is EntityReference entityRef) {
                return new EntityReference(entityRef.LogicalName, entityRef.Id);
            }
            return obj;
        }

    }
}
