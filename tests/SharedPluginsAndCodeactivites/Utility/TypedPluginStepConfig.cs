namespace DG.Some.Namespace
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Linq.Expressions;
    using Microsoft.Xrm.Sdk;
    using static SharedPluginsAndCodeactivites.Utility.Enums;

    // StepConfig           : className, ExecutionStage, EventOperation, LogicalName
    // ExtendedStepConfig   : Deployment, ExecutionMode, Name, ExecutionOrder, FilteredAttributes, UserContext
    // ImageTuple           : Name, EntityAlias, ImageType, Attributes
    using ImageTuple = System.Tuple<string, string, int, string>;

    /// <summary>
    /// Made by Delegate A/S
    /// Class to encapsulate the various configurations that can be made 
    /// to a plugin step.
    /// </summary>
    public class PluginStepConfig<T> : IPluginStepConfig where T : Entity
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
        public PluginStepConfig(EventOperation eventOperation, ExecutionStage executionStage)
        {
            this._LogicalName = Activator.CreateInstance<T>().LogicalName;
            this._EventOperation = eventOperation.ToString();
            this._ExecutionStage = (int)executionStage;
            this._Deployment = (int)Deployment.ServerOnly;
            this._ExecutionMode = (int)ExecutionMode.Synchronous;
            this._ExecutionOrder = 1;
            this._UserContext = Guid.Empty;
        }

        private PluginStepConfig<T> AddFilteredAttribute(Expression<Func<T, object>> lambda)
        {
            this._FilteredAttributesCollection.Add(GetMemberName(lambda));
            return this;
        }

        public PluginStepConfig<T> AddFilteredAttributes(params Expression<Func<T, object>>[] lambdas)
        {
            foreach (var lambda in lambdas) this.AddFilteredAttribute(lambda);
            return this;
        }

        public PluginStepConfig<T> SetDeployment(Deployment deployment)
        {
            this._Deployment = (int)deployment;
            return this;
        }

        public PluginStepConfig<T> SetExecutionMode(ExecutionMode executionMode)
        {
            this._ExecutionMode = (int)executionMode;
            return this;
        }

        public PluginStepConfig<T> SetName(string name)
        {
            this._Name = name;
            return this;
        }

        public PluginStepConfig<T> SetExecutionOrder(int executionOrder)
        {
            this._ExecutionOrder = executionOrder;
            return this;
        }

        public PluginStepConfig<T> SetUserContext(Guid userContext)
        {
            this._UserContext = userContext;
            return this;
        }

        public PluginStepConfig<T> AddImage(ImageType imageType)
        {
            return this.AddImage(imageType, null);
        }

        public PluginStepConfig<T> AddImage(ImageType imageType, params Expression<Func<T, object>>[] attributes)
        {
            return this.AddImage(imageType.ToString(), imageType.ToString(), imageType, attributes);
        }

        public PluginStepConfig<T> AddImage(string name, string entityAlias, ImageType imageType)
        {
            return this.AddImage(name, entityAlias, imageType, null);
        }

        public PluginStepConfig<T> AddImage(string name, string entityAlias, ImageType imageType, params Expression<Func<T, object>>[] attributes)
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

            public PluginStepImage(string name, string entityAlias, ImageType imageType, Expression<Func<T, object>>[] attributes)
            {
                this.Name = name;
                this.EntityAlias = entityAlias;
                this.ImageType = (int)imageType;

                if (attributes != null && attributes.Length > 0)
                {
                    this.Attributes = string.Join(",", attributes.Select(x => PluginStepConfig<T>.GetMemberName(x))).ToLower();
                }
                else
                {
                    this.Attributes = null;
                }
            }
        }

        private static string GetMemberName(Expression<Func<T, object>> lambda)
        {
            if (!(lambda.Body is MemberExpression body))
            {
                var ubody = (UnaryExpression)lambda.Body;
                body = ubody.Operand as MemberExpression;
            }

            return body.Member.Name;
        }
    }
}