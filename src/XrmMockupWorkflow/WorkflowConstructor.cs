﻿using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WorkflowParser;


namespace WorkflowExecuter {
    internal class WorkflowConstructor {

        public enum ParseAs {
            Workflow, RollUp
        }

        public static WorkflowTree Parse(Entity workflow, Dictionary<string, CodeActivity> codeActivites, ParseAs parseAs) {
            if (workflow == null || workflow.LogicalName != LogicalNames.Workflow) {
                throw new WorkflowException($"Entity had logicalname '{LogicalNames.Workflow}' instead of workflow");
            }
            var parsed = Parser.Parse(workflow.GetAttributeValue<string>("xaml"));
            var triggerFields = new HashSet<string>();
            if (workflow.GetAttributeValue<string>("triggeronupdateattributelist") != null) {
                foreach (var field in workflow.GetAttributeValue<string>("triggeronupdateattributelist").Split(',')) {
                    triggerFields.Add(field);
                }
            }

            var arguments = parsed.Members.Properties.Where(p => p.Attributes != null).Select(p => ConvertToArgument(p));
            var input = arguments.Where(a => a.Direction == WorkflowArgument.DirectionType.Input && !a.IsTarget).ToList();
            var output = arguments.Where(a => a.Direction == WorkflowArgument.DirectionType.Output && !a.IsTarget).ToList();

            return new WorkflowTree(CompressTree(parsed.Workflow, parseAs),
                workflow.GetAttributeValue<bool?>("triggeroncreate"), workflow.GetAttributeValue<bool?>("triggerondelete"),
               triggerFields, workflow.GetOptionSetValue<workflow_runas>("runas"), workflow.GetOptionSetValue<Workflow_Scope>("scope"),
               workflow.GetOptionSetValue<workflow_stage>("createstage"), workflow.GetOptionSetValue<workflow_stage>("updatestage"),
               workflow.GetOptionSetValue<workflow_stage>("deletestage"), workflow.GetOptionSetValue<Workflow_Mode>("mode"),
               workflow.GetAttributeValue<EntityReference>("ownerid").Id, workflow.GetAttributeValue<string>("primaryentity"), codeActivites,
               input, output);
        }

        private static WorkflowArgument ConvertToArgument(Property p) {
            return new WorkflowArgument(p.Name, bool.Parse(p.Attributes[0].Value), bool.Parse(p.Attributes[1].Value),
                p.Attributes[2].Value, p.Attributes[3].Value == "Input" ? WorkflowArgument.DirectionType.Input : WorkflowArgument.DirectionType.Output,
                p.Attributes[4].Value);
        }

        public static WorkflowTree ParseRollUp(string xaml) {
            var parsed = Parser.Parse(xaml);
            return new WorkflowTree(CompressTree(parsed.Workflow, ParseAs.RollUp)); ;
        }

        public static WorkflowTree ParseCalculated(string xaml) {
            var parsed = Parser.Parse(xaml);
            return new WorkflowTree(CompressTree(parsed.Workflow, ParseAs.Workflow)); ;
        }

        private static WorkflowNode CompressTree(WorkflowParser.Workflow workflow, ParseAs parseAs) {
            if (parseAs == ParseAs.RollUp) {
                return CompressRollUp(workflow.Activities[0] as WorkflowParser.ActivitySequence);
            }
            if (parseAs == ParseAs.Workflow) {
                return new ActivityList(CompressActivityCollection(workflow.Activities).ToArray());
            }
            throw new NotImplementedException("No logic for parsing that kind of xaml in workflowconstructor");
        }

        private static WorkflowNode CompressRollUp(WorkflowParser.ActivitySequence sequence) {
            var activites = sequence.Activities;
            var hierarchicalRelationshipName = (activites[0] as WorkflowParser.ActivitySequence).Variables[0].Default.TrimEdge();
            var filterResult = (activites[1] as WorkflowParser.ActivitySequence).Variables != null ?
                (activites[1] as WorkflowParser.ActivitySequence).Variables[0].Name : null;
            var aggregateResult = (activites[2] as WorkflowParser.ActivitySequence).Variables[0].Name;
            var filter = new List<WorkflowNode>();
            filter.AddRange(CompressVariableCollection((activites[1] as WorkflowParser.ActivitySequence).Variables));
            filter.AddRange(CompressActivityCollection((activites[1] as WorkflowParser.ActivitySequence).Activities));
            var aggregation = new List<WorkflowNode>();
            aggregation.AddRange(CompressVariableCollection((activites[2] as WorkflowParser.ActivitySequence).Variables));
            aggregation.AddRange(CompressActivityCollection((activites[2] as WorkflowParser.ActivitySequence).Activities));
            return new RollUp(hierarchicalRelationshipName, filterResult, aggregateResult, filter, aggregation);
        }

       

        private static WorkflowNode CompressCollection(Collection collection) {
            if (collection.Variables != null) {
                return new ActivityList(CompressVariableCollection(collection.Variables).ToArray());
            }

            if (collection.Activities != null) {
                return new ActivityList(CompressActivityCollection(collection.Activities).ToArray());
            }

            return new Skip();
        }


        private static List<WorkflowNode> CompressVariableCollection(WorkflowParser.Variable[] variables) {
            var nodes = new List<WorkflowNode>();
            if (variables != null) {
                foreach (var variable in variables) {
                    if (variable.Default != null) {
                        nodes.Add(new CreateVariable(new object[][] { new object[] { "", variable.Default } }, variable.Type.Split(':')[1], variable.Name));
                    } else if (variable.DefaultList != null) {
                        if (variable.Type.Contains("XrmTimeSpan")) {
                            var timespan = variable.DefaultList[0].XrmTimeSpan;
                            nodes.Add(new CreateVariable(new object[][] { new[] { timespan.Days, timespan.Hours, timespan.Minutes, timespan.Months, timespan.Years }}, variable.Type.Split(':')[1], variable.Name));
                        } else {
                            nodes.Add(new CreateVariable(new object[][] { new[] { "" }.Concat(variable.DefaultList.Select(l => l.Value)).ToArray() }, variable.Type.Split(':')[1], variable.Name));
                        }
                    }
                }
            }
            return nodes;
        }
        private static List<WorkflowNode> CompressActivityCollection(WorkflowParser.Activity[] activities) {
            var nodes = new List<WorkflowNode>();
            if (activities != null) {
                for (int i = 0; i < activities.Length; i++) {
                    var node = CompressActivity(activities[i]);
                    if (isCondition(node) && activities.Skip(i + 1).Take(activities.Length).Count() > 0) {
                        ((Condition)node).Otherwise = new ActivityList(CompressActivityCollection(
                                activities.Skip(i + 1).Take(activities.Length).ToArray()).ToArray());
                        nodes.Add(node);
                        break;
                    }
                    if (node is ActivityList) {
                        nodes.AddRange((node as ActivityList).Activities);
                    } else {
                        nodes.Add(node);
                    }
                }
            }

            return nodes;
        }


        private static bool isCondition(WorkflowNode node) {
            return node is Condition;
        }

        private static WorkflowNode CompressActivity(WorkflowParser.Activity activity) {
            if (activity is ActivityReference) { return CompressActivityReference(activity as ActivityReference); }
            if (activity is WorkflowParser.GetEntityProperty) {
                return CompressGetEntityProperty(activity as WorkflowParser.GetEntityProperty);
            }
            if (activity is WorkflowParser.TerminateWorkflow) {
                return CompressTerminateWorkflow(activity as WorkflowParser.TerminateWorkflow);
            }

            if (activity is WorkflowParser.SetEntityProperty) {
                return CompressSetEntityProperty(activity as WorkflowParser.SetEntityProperty);
            }

            if (activity is WorkflowParser.CreateEntity) {
                return CompressCreateEntity(activity as WorkflowParser.CreateEntity);
            }

            if (activity is WorkflowParser.UpdateEntity) {
                return CompressUpdateEntity(activity as WorkflowParser.UpdateEntity);
            }

            if (activity is WorkflowParser.AssignEntity) {
                return CompressAssignEntity(activity as WorkflowParser.AssignEntity);
            }

            if (activity is WorkflowParser.SetAttributeValue) {
                return CompressSetAttributeValue(activity as WorkflowParser.SetAttributeValue);
            }

            if (activity is WorkflowParser.SetState) {
                return CompressSetState(activity as WorkflowParser.SetState);
            }

            if (activity is WorkflowParser.Assign) {
                return CompressAssign(activity as WorkflowParser.Assign);
            }

            if (activity is WorkflowParser.ActivitySequence) {
                return CompressSequence(activity as WorkflowParser.ActivitySequence);
            }

            if (activity is WorkflowParser.Collection) {
                return CompressCollection(activity as WorkflowParser.Collection);
            }

            if (activity is WorkflowParser.Null) {
                return new Skip();
            }

            if (activity is WorkflowParser.Postpone) {
                return CompressPostpone(activity as WorkflowParser.Postpone);
            }

            if (activity is WorkflowParser.Persist) {
                return new Persist();
            }

            throw new NotImplementedException("Unknown activity, implement the compressor.");
        }

        private static WorkflowNode CompressPostpone(WorkflowParser.Postpone postpone) {
            return new Postpone(postpone.BlockExecution, postpone.PostponeUntil.TrimEdge());
        }

        private static WorkflowNode CompressSequence(WorkflowParser.ActivitySequence sequence) {
            var nodes = new List<WorkflowNode>();
            nodes.AddRange(CompressVariableCollection(sequence.Variables));
            nodes.AddRange(CompressActivityCollection(sequence.Activities));
            return new ActivityList(nodes.ToArray());
        }

        private static WorkflowNode CompressAssign(WorkflowParser.Assign assign) {
            return new Assign(assign.To.TrimEdge(), assign.Value.TrimEdge());
        }

        private static WorkflowNode CompressSetState(WorkflowParser.SetState setState) {
            return new SetState(setState.Entity.TrimEdge(), setState.EntityId, setState.EntityName,
                new Microsoft.Xrm.Sdk.OptionSetValue(setState.StateArguments.First().ReferenceLiteral.OptionSetValue.Value),
                new Microsoft.Xrm.Sdk.OptionSetValue(setState.StatusArguments.First().ReferenceLiteral.OptionSetValue.Value));
        }

        private static WorkflowNode CompressSetAttributeValue(WorkflowParser.SetAttributeValue setAttributeValue) {
            return new SetAttributeValue(setAttributeValue.Entity.TrimEdge(), setAttributeValue.EntityName);
        }

        private static WorkflowNode CompressAssignEntity(WorkflowParser.AssignEntity assignEntity) {
            Regex reg = new Regex(@"\(.+\,");
            var ownerId = reg.Match(assignEntity.Owner);
            return new AssignEntity(assignEntity.Entity.TrimEdge(), ownerId.Value.TrimEdge());
        }

        private static WorkflowNode CompressUpdateEntity(WorkflowParser.UpdateEntity updateEntity) {
            return new UpdateEntity(updateEntity.Entity.TrimEdge(), updateEntity.EntityName);
        }

        private static WorkflowNode CompressCreateEntity(WorkflowParser.CreateEntity createEntity) {
            if (createEntity.EntityId.Contains("Null")) {
                return new CreateEntity(null, createEntity.Entity.TrimEdge(), createEntity.EntityName);
            }

            // check entity id is correct format
            return new CreateEntity(createEntity.EntityId, createEntity.Entity.TrimEdge(), createEntity.EntityName);
        }

        private static WorkflowNode CompressSetEntityProperty(WorkflowParser.SetEntityProperty setEntityProperty) {
            return new SetEntityProperty(setEntityProperty.Attribute, setEntityProperty.Entity.TrimEdge(), setEntityProperty.Value.TrimEdge());
        }

        private static WorkflowNode CompressGetEntityProperty(WorkflowParser.GetEntityProperty getEntityProperty) {
            var targetType = getEntityProperty.InArguments?.First(a => a.ReferenceLiteral != null);

            return new GetEntityProperty(getEntityProperty.Attribute, getEntityProperty.Entity.TrimEdge(), getEntityProperty.EntityName,
                getEntityProperty.Value.TrimEdge(), targetType?.ReferenceLiteral?.Value?.Split(':')[1]);
        }

        private static WorkflowNode CompressTerminateWorkflow(WorkflowParser.TerminateWorkflow terminateWorkflow) {
            OperationStatus status;
            if (terminateWorkflow.Exception.Contains("Microsoft.Xrm.Sdk.OperationStatus.Succeeded")) {
                status = OperationStatus.Succeeded;
            } else if (terminateWorkflow.Exception.Contains("Microsoft.Xrm.Sdk.OperationStatus.Canceled")) {
                status = OperationStatus.Canceled;
            } else {
                throw new WorkflowException($"Unknown exception in terminateWorkflow '{terminateWorkflow.Exception}'");
            }

            Regex reg = new Regex(@"\(.+\,");
            var messageId = reg.Match(terminateWorkflow.Reason);
            return new TerminateWorkflow(status, messageId.Value.TrimEdge());
        }


        private static WorkflowNode CompressActivityReference(ActivityReference activityReference) {
            return CompressActivityReference(activityReference, new List<WorkflowNode>());
        }

        private static WorkflowNode CompressActivityReference(ActivityReference activityReference, List<WorkflowNode> toPrepend) {
            var args = activityReference.Arguments;
            if (args != null && args.Count() > 0) {
                var outArg = args.FirstOrDefault(a => a is WorkflowParser.OutArgument);

                if (!activityReference.AssemblyQualifiedName.Contains("Microsoft.Crm.Workflow.Activities.")) {
                    var regex = new Regex(@"[^\.][\w]+(?=,)");
                    var codeActivityName = regex.Match(activityReference.AssemblyQualifiedName).Value;
                    var inArguments = args.Where(a => a is WorkflowParser.InArgument).ToDictionary(arg => arg.Key, arg => ParseCodeActivityArgument(arg.Value));
                    var outArguments = args.Where(a => a is WorkflowParser.OutArgument).ToDictionary(a => a.Value.TrimEdge(), a => a.Key);
                    return new CallCodeActivity(codeActivityName, inArguments, outArguments);
                }

                switch (args[0].Key) {
                    case "ConditionOperator":
                        if (outArg == null) {
                            throw new WorkflowException("Found no out argument, please check your workflow has the correct format");
                        }
                        if (args.Length == 4) {
                            return new ConditionExp(args[0].Value,
                                CompressParameters(args[1].Value).ToArray(),
                                args[2].Value.TrimEdge(),
                                outArg.Value.TrimEdge());
                        } else {
                            return new ConditionExp(args[0].Value,
                                null,
                                args[1].Value.TrimEdge(),
                                outArg.Value.TrimEdge());
                        }
                    case "ExpressionOperator":
                        if (outArg == null) {
                            throw new WorkflowException("Found no out argument, please check your workflow has the correct format");
                        }
                        return CompressExpressionOperator(args, outArg);
                    case "Condition":
                        if (args[0].Value == "True" || args[0].Value == "False") {
                            return new Condition(args[0].Value,
                            CompressActivity(activityReference.Properties[0]), CompressActivity(activityReference.Properties[1]));
                        }
                        return new Condition(args[0].Value.TrimEdge(),
                            CompressActivity(activityReference.Properties[0]), CompressActivity(activityReference.Properties[1]));
                    case "Wait":
                        if (args[0].Value == "True") {
                            var list = new List<WorkflowNode>();
                            list.Add(new WaitStart());
                            list.AddRange(CompressActivityCollection(activityReference.Properties));
                            return new ActivityList(list.ToArray());
                        }
                        return new ActivityList(CompressActivityCollection(activityReference.Properties).ToArray());

                    case "LogicalOperator":
                        return new LogicalComparison(args[0].Value, args[1].Value.TrimEdge(), args[2].Value.TrimEdge(), outArg.Value.TrimEdge());

                    case "Value":
                        return new ConvertType(args[0].Value.TrimEdge(), args[1].ReferenceLiteral.Value.Split(':')[1], outArg.Value.TrimEdge());

                    default:
                        throw new NotImplementedException($"No implementation for activityreference with key '{args[0].Key}'");
                }
            }

            return new ActivityList(CompressActivityCollection(activityReference.Properties).ToArray());

        }

        private static string ParseCodeActivityArgument(string argument) {
            var directCastRegex = new Regex(@"[^(]+(?=,)");
            if (argument.Contains("DirectCast(")) {
                return directCastRegex.Match(argument).Value;
            }
            return argument.TrimEdge();
        }

        private static WorkflowNode CompressExpressionOperator(WorkflowParser.Argument[] args, WorkflowParser.Argument outArg) {
            switch (args[0].Value) {
                case "CreateCrmType": {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Trim().Replace("\"", "")).ToArray());
                        if (args[2].ReferenceLiteral != null) {
                            return new CreateVariable(parameters.ToArray(), args[2].ReferenceLiteral.Value.Split(':')[1],
                                outArg.Value.TrimEdge());
                        } else if (args[1].Value.Contains("EntityReference")) {
                            return new CreateVariable(parameters.ToArray(), "EntityReference", outArg.Value.TrimEdge());
                        } else {
                            throw new NotImplementedException("Unknown amount of arguments in CreateCrmType, implement the required functionality.");
                        }
                    }

                case "SelectFirstNonNull": {
                        var parameters = CompressParameters(args[1].Value).FirstOrDefault();
                        if (parameters == null) {
                            throw new WorkflowException($"Incorrect format for parameters '{args[1].Value}'");
                        }
                        return new SelectFirstNonNull(
                            parameters.Split(',').Select(p => p.Trim()).ToArray(), outArg.Value.TrimEdge());
                    }

                case "Multiply":
                case "Divide":
                case "Subtract":
                case "Add": {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new Arithmetic(parameters.ToArray(), args[2].ReferenceLiteral.Value?.Split(':')[1],
                                outArg.Value.TrimEdge(), args[0].Value);

                    }
                case "Sum":
                case "Maximum":
                case "Minimum":
                case "Average": {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new Aggregate(parameters.ToArray(), outArg.Value.TrimEdge(), args[0].Value);

                    }
                case "Concat": {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new Concat(parameters.ToArray(), outArg.Value.TrimEdge());

                    }
                case "AddDays":
                case "AddHours":
                case "AddMonths":
                case "AddWeeks":
                case "AddYears": {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new AddTime(parameters.ToArray(), outArg.Value.TrimEdge(), args[0].Value);
                    }
                case "SubtractDays":
                case "SubtractHours":
                case "SubtractMonths":
                case "SubtractWeeks":
                case "SubtractYears": {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new SubtractTime(parameters.ToArray(), outArg.Value.TrimEdge(), args[0].Value);
                    }
                case "DiffInDays":
                case "DiffInHours":
                case "DiffInMinutes":
                case "DiffInMonths":
                case "DiffInWeeks":
                case "DiffInYears": {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new DiffInTime(parameters.ToArray(), outArg.Value.TrimEdge(), args[0].Value);
                    }
                case "TrimLeft":
                case "TrimRight": {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new Trim(parameters.ToArray(), outArg.Value.TrimEdge(), args[0].Value);
                    }

                case "RetrieveLastExecutionTime": {
                        return new RetrieveLastExecutionTime(outArg.Value.TrimEdge());
                    }
                case "RetrieveCurrentTime": {
                        return new RetrieveCurrentTime(outArg.Value.TrimEdge());
                    }
                case "CustomOperationArguments": {
                        var parameters = CompressParameters(args[1].Value)
                            .Select(group => group.Split(',').Select(p => p.Replace("\"", string.Empty).Trim()).ToArray());
                        return new CustomOperationArguments(parameters.ToArray(), outArg.Value.TrimEdge());
                    }
                default:
                    throw new NotImplementedException($"No implementation for expression operator '{args[0].Value}'");
            }
        }

        private static IEnumerable<string> CompressParameters(string value) {
            Regex reg = new Regex(@"\{(.+)\}|(,)");
            return reg.Matches(value).Cast<Match>().Select(m => m.Value.Trim('{', '}', ' '));
        }
    }
}
