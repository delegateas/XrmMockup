namespace DG.Some.Namespace
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using static SharedPluginsAndCodeactivites.Utility.Enums;

    // StepConfig           : className, ExecutionStage, EventOperation, LogicalName
    // ExtendedStepConfig   : Deployment, ExecutionMode, Name, ExecutionOrder, FilteredAttributes, UserContext
    // ImageTuple           : Name, EntityAlias, ImageType, Attributes
    using ImageTuple = System.Tuple<string, string, int, string>;

    public class PluginStepConfig : IPluginStepConfig 
    {
        public string _LogicalName { get; private set; }
        public string _EventOperation { get; private set; }
        public int _ExecutionStage { get; private set; }

        public string _Name { get; private set; }
        public int _Deployment { get; private set; }
        public int _ExecutionMode { get; private set; }
        public int _ExecutionOrder { get; private set; }
        public Guid _UserContext { get; private set; }

        public Collection<PluginStepImage> _Images = new Collection<PluginStepImage>();
        public Collection<string> _FilteredAttributesCollection = new Collection<string>();

        public string _FilteredAttributes
        {
            get
            {
                if (this._FilteredAttributesCollection.Count == 0) return null;
                return string.Join(",", this._FilteredAttributesCollection).ToLower();
            }
        }

        public PluginStepConfig(string entityLogicalName,EventOperation eventOperation, ExecutionStage executionStage)
        {
            this._LogicalName = entityLogicalName;
            this._EventOperation = eventOperation.ToString();
            this._ExecutionStage = (int)executionStage;
            this._Deployment = (int)Deployment.ServerOnly;
            this._ExecutionMode = (int)ExecutionMode.Synchronous;
            this._ExecutionOrder = 1;
            this._UserContext = Guid.Empty;
        }

        private PluginStepConfig AddFilteredAttribute(string attributeName)
        {
            this._FilteredAttributesCollection.Add(attributeName);
            return this;
        }

        public PluginStepConfig AddFilteredAttributes(params string[] attributeNames)
        {
            foreach (var attributeName in attributeNames) this.AddFilteredAttribute(attributeName);
            return this;
        }

        public PluginStepConfig SetDeployment(Deployment deployment)
        {
            this._Deployment = (int)deployment;
            return this;
        }

        public PluginStepConfig SetExecutionMode(ExecutionMode executionMode)
        {
            this._ExecutionMode = (int)executionMode;
            return this;
        }

        public PluginStepConfig SetName(string name)
        {
            this._Name = name;
            return this;
        }

        public PluginStepConfig SetExecutionOrder(int executionOrder)
        {
            this._ExecutionOrder = executionOrder;
            return this;
        }

        public PluginStepConfig SetUserContext(Guid userContext)
        {
            this._UserContext = userContext;
            return this;
        }

        public PluginStepConfig AddImage(ImageType imageType)
        {
            return this.AddImage(imageType, null);
        }

        public PluginStepConfig AddImage(ImageType imageType, params string[] attributes)
        {
            return this.AddImage(imageType.ToString(), imageType.ToString(), imageType, attributes);
        }

        public PluginStepConfig AddImage(string name, string entityAlias, ImageType imageType)
        {
            return this.AddImage(name, entityAlias, imageType, null);
        }

        public PluginStepConfig AddImage(string name, string entityAlias, ImageType imageType, params string[] attributes)
        {
            this._Images.Add(new PluginStepImage(name, entityAlias, imageType, attributes));
            return this;
        }

        public IEnumerable<ImageTuple> GetImages()
        {
            foreach (var image in this._Images)
            {
                yield return new ImageTuple(image.Name, image.EntityAlias, image.ImageType, image.Attributes);
            }
        }

        /// <summary>
        /// Container for information about images attached to steps
        /// </summary>
        public class PluginStepImage
        {
            public string Name { get; private set; }
            public string EntityAlias { get; private set; }
            public int ImageType { get; private set; }
            public string Attributes { get; private set; }

            public PluginStepImage(string name, string entityAlias, ImageType imageType, string[] attributes)
            {
                this.Name = name;
                this.EntityAlias = entityAlias;
                this.ImageType = (int)imageType;

                if (attributes != null && attributes.Length > 0)
                {
                    this.Attributes = string.Join(",", attributes).ToLower();
                }
                else
                {
                    this.Attributes = null;
                }
            }
        }
    }
}