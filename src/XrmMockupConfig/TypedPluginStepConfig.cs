using DG.Some.Namespace;
using DG.Tools.XrmMockup;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using XrmMockupConfig;

namespace DG.Some.Namespace.Test
{
    public class TypedPluginStepConfig : ITypedPluginStepConfig 
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

        public TypedPluginStepConfig(EventOperation eventOperation, ExecutionStage executionStage,string logicalName)
        {
            this._LogicalName = logicalName;
            this._EventOperation = eventOperation.ToString();
            this._ExecutionStage = (int)executionStage;
            this._Deployment = (int)Deployment.ServerOnly;
            this._ExecutionMode = (int)ExecutionMode.Synchronous;
            this._ExecutionOrder = 1;
            this._UserContext = Guid.Empty;
        }

        private TypedPluginStepConfig AddFilteredAttribute(Expression<Action<object>> lambda)
        {
            this._FilteredAttributesCollection.Add(GetMemberName(lambda));
            return this;
        }

        public TypedPluginStepConfig AddFilteredAttributes(params Expression<Action<object>>[] lambdas)
        {
            foreach (var lambda in lambdas) this.AddFilteredAttribute(lambda);
            return this;
        }

        public TypedPluginStepConfig SetDeployment(Deployment deployment)
        {
            this._Deployment = (int)deployment;
            return this;
        }

        public TypedPluginStepConfig SetExecutionMode(ExecutionMode executionMode)
        {
            this._ExecutionMode = (int)executionMode;
            return this;
        }

        public TypedPluginStepConfig SetName(string name)
        {
            this._Name = name;
            return this;
        }

        public TypedPluginStepConfig SetExecutionOrder(int executionOrder)
        {
            this._ExecutionOrder = executionOrder;
            return this;
        }

        public TypedPluginStepConfig SetUserContext(Guid userContext)
        {
            this._UserContext = userContext;
            return this;
        }

        public TypedPluginStepConfig AddImage(ImageType imageType)
        {
            return this.AddImage(imageType, null);
        }

        public TypedPluginStepConfig AddImage(ImageType imageType, params Expression<Action<object>>[] attributes)
        {
            return this.AddImage(imageType.ToString(), imageType.ToString(), imageType, attributes);
        }

        public TypedPluginStepConfig AddImage(string name, string entityAlias, ImageType imageType)
        {
            return this.AddImage(name, entityAlias, imageType, null);
        }

        public TypedPluginStepConfig AddImage(string name, string entityAlias, ImageType imageType, params Expression<Action<object>>[] attributes)
        {
            this._Images.Add(new PluginStepImage(name, entityAlias, imageType, attributes));
            return this;
        }

        public IEnumerable<ImageConfig> GetImages()
        {
            foreach (var image in this._Images)
            {
                yield return new ImageConfig(image.Name, image.EntityAlias, image.ImageType, image.Attributes);
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

            public PluginStepImage(string name, string entityAlias, ImageType imageType, Expression<Action<object>>[] attributes)
            {
                this.Name = name;
                this.EntityAlias = entityAlias;
                this.ImageType = (int)imageType;

                if (attributes != null && attributes.Length > 0)
                {
                    this.Attributes = string.Join(",", attributes.Select(x => TypedPluginStepConfig.GetMemberName(x))).ToLower();
                }
                else
                {
                    this.Attributes = null;
                }
            }
        }


        private static string GetMemberName(Expression<Action<object>> lambda)
        {
            MemberExpression body = lambda.Body as MemberExpression;

            if (body == null)
            {
                UnaryExpression ubody = (UnaryExpression)lambda.Body;
                body = ubody.Operand as MemberExpression;
            }

            return body.Member.Name;
        }
    }

}
