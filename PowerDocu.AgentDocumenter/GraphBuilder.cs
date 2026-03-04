using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using PowerDocu.Common;
using Rubjerg.Graphviz;
using Svg;
using YamlDotNet.RepresentationModel;

namespace PowerDocu.AgentDocumenter
{
    public class GraphBuilder
    {
        private readonly BotComponent topic;
        private readonly string folderPath;
        private YamlScalarNode actionsKey = new YamlScalarNode("actions");
        private YamlScalarNode conditions = new YamlScalarNode("conditions");
        private YamlScalarNode elseActions = new YamlScalarNode("elseActions");

        public GraphBuilder(string agentName, BotComponent topicToUse, string path)
        {
            topic = topicToUse;
            folderPath = path;
            Directory.CreateDirectory(folderPath + "Resources");
        }

        public void buildTopLevelGraph()
        {
            buildGraph(false);
        }

        public void buildDetailedGraph()
        {
            buildGraph(true);
        }

        private void buildGraph(bool showSubactions)
        {
            RootGraph rootGraph = RootGraph.CreateNew(GraphType.Directed, CharsetHelper.GetSafeName(topic.Name + "(" + topic.getTopicFileName() + ")"));
            Graph.IntroduceAttribute(rootGraph, "rankdir", "TB");
            Graph.IntroduceAttribute(rootGraph, "compound", "true");
            Graph.IntroduceAttribute(rootGraph, "fontname", "helvetica");

            // Add subgraph default attributes
            Graph.IntroduceAttribute(rootGraph, "clusterrank", "local");
            SubGraph.IntroduceAttribute(rootGraph, "style", "filled");
            SubGraph.IntroduceAttribute(rootGraph, "color", "black");
            SubGraph.IntroduceAttribute(rootGraph, "fillcolor", "lightgray");
            SubGraph.IntroduceAttribute(rootGraph, "penwidth", "1");

            Node.IntroduceAttribute(rootGraph, "shape", "rectangle");
            Node.IntroduceAttribute(rootGraph, "color", "");
            Node.IntroduceAttribute(rootGraph, "style", "");
            Node.IntroduceAttribute(rootGraph, "fillcolor", "");
            Node.IntroduceAttribute(rootGraph, "label", "");
            Node.IntroduceAttribute(rootGraph, "fontname", "helvetica");
            Edge.IntroduceAttribute(rootGraph, "label", "");
            var yaml = new YamlStream();
            //pasrse the topic YAML data
            //this is ugly and should't be done, but otherwise YamlDotNet throws an error.... need longterm strategy
            var input = new StringReader(topic.YamlData.Replace("@odata", "odata"));
            yaml.Load(input);
            var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
            var triggerYaml = (YamlMappingNode)mapping.Children[new YamlScalarNode("beginDialog")];
            Node trigger = addTriggerDetails(triggerYaml, rootGraph);
            addActionNodes(triggerYaml, trigger, rootGraph, actionsKey);
            rootGraph.CreateLayout();
            NotificationHelper.SendNotification("  - Created Graph " + folderPath + generateImageFiles(rootGraph, showSubactions) + ".png");
        }

        private Node addTriggerDetails(YamlMappingNode triggerYaml, RootGraph rootGraph)
        {
            string triggerType = triggerYaml.Children[new YamlScalarNode("kind")].ToString();
            Node trigger = rootGraph.GetOrAddNode("Trigger " + topic.Name + " (" + topic.getTopicFileName() + ")");
            trigger.SetAttribute("color", GraphColours.GetColourForAction("Trigger"));
            trigger.SetAttribute("fillcolor", GraphColours.GetFillColourForAction("Trigger"));
            trigger.SetAttribute("style", "filled");
            var svgDocument = SvgDocument.FromSvg<SvgDocument>(AgentIcon.GetIcon("Trigger"));
            //generating the PNG from the SVG with a width of 20px because some SVGs are huge and downscaled, thus can't be shown directly
            using (var bitmap = svgDocument.Draw(20, 0))
            {
                bitmap?.Save(folderPath + @"Resources\Trigger.png");
            }
            string html = $"<table border=\"0\"><tr><td>{createActionHeaderImageTable("Trigger", $"Trigger - {topic.Name}")}</td></tr>";
            switch (triggerType)
            {
                case "OnRecognizedIntent":

                    html += "<tr><td><table border=\"1\"><tr><td>The agent chooses</td></tr></table></td></tr>";
                    html += "<tr><td>Describe what the topic does:</td></tr>";
                    html += "<tr><td><table border=\"1\"><tr><td>This tool can handle queries like these:<br/>";
                    var intentYaml = (YamlMappingNode)triggerYaml.Children[new YamlScalarNode("intent")];
                    if (intentYaml.Children.TryGetValue(new YamlScalarNode("triggerQueries"), out var triggerQueryNode) && triggerQueryNode is YamlSequenceNode triggerQuerySequence)
                    {
                        html += string.Join("<br/>", triggerQuerySequence); //triggerQuerySequence.Children.Select(q => q.ToString()));
                    }
                    html += "</td></tr></table></td></tr>";

                    break;
                case "OnSystemRedirect":
                    break;
                //todo
                /*OnUnknownIntent
                OnError
                OnSignIn
                OnEscalate
                OnUnknownIntent
                OnConversationStart*/
                default:
                    break;
            }
            html += "</table>";
            trigger.SetAttributeHtml("label", html);
            return trigger;
        }

        private Node addActionNodes(YamlMappingNode actionsYaml, Node prevNode, RootGraph rootGraph, YamlScalarNode filterKey, SubGraph parentCluster = null)
        {
            Node returnNode = null;
            if (actionsYaml.Children.TryGetValue(filterKey, out var actionsNode) && actionsNode is YamlSequenceNode actionsSequence)
            {
                foreach (var action in actionsSequence)
                {
                    Node clusterExitNode = null;
                    Node actionNode = null;
                    SubGraph conditionCluster = null;
                    string actionName = CharsetHelper.GetSafeName(((YamlMappingNode)action).Children[new YamlScalarNode("kind")].ToString() + ((YamlMappingNode)action).Children[new YamlScalarNode("id")].ToString());
                    string actionType = GetActionType((YamlMappingNode)action);
                    if (actionType != "ConditionGroup")
                    {
                        actionNode = rootGraph.GetOrAddNode(actionName);
                        actionNode.SetAttribute("color", GraphColours.GetColourForAction(actionType));
                        actionNode.SetAttribute("fillcolor", GraphColours.GetFillColourForAction(actionType));
                        actionNode.SetAttribute("style", "filled");
                        parentCluster?.AddExisting(actionNode);
                        returnNode = actionNode;
                    }
                    string displayName = "";
                    if (((YamlMappingNode)action).Children.TryGetValue(new YamlScalarNode("displayName"), out var displayNameNode))
                    {
                        displayName = System.Web.HttpUtility.HtmlEncode(displayNameNode.ToString());
                    }
                    var svgDocument = SvgDocument.FromSvg<SvgDocument>(AgentIcon.GetIcon(actionType));
                    //generating the PNG from the SVG with a width of 16px because some SVGs are huge and downscaled, thus can't be shown directly
                    using (var bitmap = svgDocument.Draw(20, 0))
                    {
                        bitmap?.Save(folderPath + @"Resources\" + actionType + ".png");
                    }
                    switch (actionType)
                    {
                        case "AdaptiveCard":

                            string adapativeCardHtml = "<table border=\"0\">";
                            adapativeCardHtml += $"<tr><td>{createActionHeaderImageTable(actionType, $"Adaptive Card: {displayName}")}</td></tr>";

                            // Parse the card JSON content
                            if (((YamlMappingNode)action).Children.TryGetValue(new YamlScalarNode("card"), out var cardNode))
                            {
                                try
                                {
                                    string cardJsonString = cardNode.ToString();
                                    if (cardJsonString.StartsWith('='))
                                    {
                                        cardJsonString = cardJsonString.Substring(1);
                                    }
                                    JObject cardJson = JObject.Parse(cardJsonString);

                                    adapativeCardHtml += "<tr><td><table border=\"1\">";

                                    // Process card body elements
                                    if (cardJson["body"] is JArray bodyArray)
                                    {
                                        foreach (var element in bodyArray)
                                        {
                                            string elementHtml = RenderCardElement(element as JObject);
                                            if (!string.IsNullOrEmpty(elementHtml))
                                            {
                                                adapativeCardHtml += $"<tr><td>{elementHtml}</td></tr>";
                                            }
                                        }
                                    }
                                    // Process card actions
                                    if (cardJson["actions"] is JArray actionsArray)
                                    {
                                        adapativeCardHtml += "<tr><td><b>Actions:</b></td></tr>";
                                        foreach (var cardAction in actionsArray)
                                        {
                                            string actionHtml = RenderCardAction(cardAction as JObject);
                                            if (!string.IsNullOrEmpty(actionHtml))
                                            {
                                                adapativeCardHtml += $"<tr><td>{actionHtml}</td></tr>";
                                            }
                                        }
                                    }
                                    adapativeCardHtml += "</table></td></tr>";
                                }
                                catch (JsonException ex)
                                {
                                    adapativeCardHtml += $"<tr><td>Error parsing adaptive card JSON: {ex.Message}</td></tr>";
                                }
                            }
                            else
                            {
                                adapativeCardHtml += "<tr><td>Adaptive Card definition not found</td></tr>";
                            }

                            // Process output binding if present
                            if (((YamlMappingNode)action).Children.TryGetValue(new YamlScalarNode("output"), out var outputNode) &&
                                outputNode is YamlMappingNode outputMapping &&
                                outputMapping.Children.TryGetValue(new YamlScalarNode("binding"), out var bindingNode) &&
                                bindingNode is YamlMappingNode outputBinding)
                            {
                                adapativeCardHtml += "<tr><td><table border=\"1\">";
                                adapativeCardHtml += "<tr><td><b>Output Binding:</b></td></tr>";
                                foreach (var outputItem in outputBinding.Children)
                                {
                                    adapativeCardHtml += $"<tr><td><b>{outputItem.Key}:</b> {outputItem.Value}</td></tr>";
                                }
                                adapativeCardHtml += "</table></td></tr>";
                            }

                            adapativeCardHtml += "</table>";
                            actionNode.SetAttributeHtml("label", adapativeCardHtml);
                            break;
                        case "Question":
                            string questionHtml = "<table border=\"0\">";
                            questionHtml += $"<tr><td>{createActionHeaderImageTable(actionType, $"Question: {displayName}")}</td></tr>";

                            // Get the prompt text
                            if (((YamlMappingNode)action).Children.TryGetValue(new YamlScalarNode("prompt"), out var promptNode))
                            {
                                string promptText = promptNode.ToString();
                                questionHtml += "<tr><td><table border=\"1\">";
                                questionHtml += $"<tr><td>{generateMultiLineText(System.Web.HttpUtility.HtmlEncode(promptText))}</td></tr>";
                                questionHtml += "</table></td></tr>";
                            }


                            // Get the entity type
                            if (((YamlMappingNode)action).Children.TryGetValue(new YamlScalarNode("entity"), out var entityNode))
                            {
                                string entityType = entityNode.ToString();
                                questionHtml += $"<tr><td><b>Identify:</b> {entityType}</td></tr>";
                            }

                            // Get the variable name
                            if (((YamlMappingNode)action).Children.TryGetValue(new YamlScalarNode("variable"), out var variableNode))
                            {
                                string variableName = variableNode.ToString();
                                questionHtml += $"<tr><td><b>Save user response as:</b><br/> {variableName}</td></tr>";
                            }

                            // Check for choices if it's a choice question
                            if (((YamlMappingNode)action).Children.TryGetValue(new YamlScalarNode("choices"), out var choicesNode) && choicesNode is YamlSequenceNode choicesSequence)
                            {
                                questionHtml += "<tr><td><b>Choices:</b></td></tr>";
                                questionHtml += "<tr><td><table border=\"1\">";
                                foreach (var choice in choicesSequence)
                                {
                                    if (choice is YamlMappingNode choiceMap)
                                    {
                                        string choiceText = "";
                                        if (choiceMap.Children.TryGetValue(new YamlScalarNode("value"), out var valueNode))
                                        {
                                            choiceText = valueNode.ToString();
                                        }
                                        if (choiceMap.Children.TryGetValue(new YamlScalarNode("synonyms"), out var synonymsNode) && synonymsNode is YamlSequenceNode synonymsSeq)
                                        {
                                            var synonymsList = synonymsSeq.Select(s => s.ToString()).ToList();
                                            choiceText += $" (synonyms: {string.Join(", ", synonymsList)})";
                                        }
                                        questionHtml += $"<tr><td>{System.Web.HttpUtility.HtmlEncode(choiceText)}</td></tr>";
                                    }
                                    else
                                    {
                                        // Simple string choice
                                        questionHtml += $"<tr><td>{System.Web.HttpUtility.HtmlEncode(choice.ToString())}</td></tr>";
                                    }
                                }
                                questionHtml += "</table></td></tr>";
                            }

                            questionHtml += "</table>";
                            actionNode.SetAttributeHtml("label", questionHtml);
                            break;
                        case "ConditionGroup":
                            // Create the condition cluster - use the parentCluster if we're nested
                            conditionCluster = ((Graph)(parentCluster != null ? parentCluster : rootGraph)).GetOrAddSubgraph("cluster_" + CharsetHelper.GetSafeName(actionName));

                            // Use SetAttribute instead of SafeSetAttribute for better reliability
                            conditionCluster.SetAttribute("style", "filled");
                            conditionCluster.SetAttribute("fillcolor", GraphColours.GetFillColourForAction(actionType));
                            conditionCluster.SetAttribute("color", GraphColours.GetColourForAction(actionType));
                            conditionCluster.SetAttribute("penwidth", "1");

                            // Track the last nodes in each condition branch
                            var lastNodes = new List<Node>();
                            // Collect all nodes that should be at the same level
                            var sameLevelNodes = new List<Node>();
                            //get the conditions node, then loop through the items inside which may have actions
                            if (((YamlMappingNode)action).Children.TryGetValue(conditions, out var conditionsNode) && conditionsNode is YamlSequenceNode conditionsSequence)
                            {
                                foreach (var condition in conditionsSequence)
                                {
                                    //add the condition node
                                    Node conditionNode = rootGraph.GetOrAddNode("conditionnode-" + CharsetHelper.GetSafeName(((YamlMappingNode)condition).Children[new YamlScalarNode("id")].ToString()));
                                    conditionNode.SetAttribute("color", GraphColours.GetColourForAction(actionType));
                                    conditionNode.SetAttribute("fillcolor", GraphColours.GetFillColourForAction(actionType));
                                    conditionNode.SetAttribute("style", "filled");
                                    string conditionHtml = $"<table border=\"0\"><tr><td>{createActionHeaderImageTable(actionType, "Condition")}</td></tr>";
                                    string conditionString = System.Web.HttpUtility.HtmlEncode(((YamlMappingNode)condition).Children[new YamlScalarNode("condition")].ToString());
                                    if (conditionString.StartsWith('='))
                                    {
                                        conditionString = conditionString.Substring(1);
                                    }
                                    conditionHtml += "<tr><td><table border=\"1\"><tr><td>" + generateMultiLineText(conditionString) + "</td></tr></table></td></tr></table>";
                                    conditionNode.SetAttributeHtml("label", conditionHtml);

                                    // Create edge from previous node to condition node
                                    Edge edge = rootGraph.GetOrAddEdge(prevNode, conditionNode, "edge to " + "conditionnode-" + CharsetHelper.GetSafeName(((YamlMappingNode)condition).Children[new YamlScalarNode("id")].ToString()));
                                    edge.SetAttribute("weight", "1");
                                    conditionCluster.AddExisting(conditionNode);

                                    // Process actions and get the last node in this branch - PASS conditionCluster as parentCluster for nesting
                                    Node lastNodeInBranch = addActionNodes((YamlMappingNode)condition, conditionNode, rootGraph, actionsKey, conditionCluster);
                                    lastNodes.Add(lastNodeInBranch ?? conditionNode);
                                    sameLevelNodes.Add(conditionNode);
                                }
                            }

                            // Handle else actions if present
                            if (((YamlMappingNode)action).Children.TryGetValue(elseActions, out var elseActionsYamlNode))
                            {
                                //add the else actions node
                                Node elseActionsNode = rootGraph.GetOrAddNode("elseactionsnode-" + actionName);
                                elseActionsNode.SetAttribute("color", GraphColours.GetColourForAction(actionType));
                                elseActionsNode.SetAttribute("fillcolor", GraphColours.GetFillColourForAction(actionType));
                                elseActionsNode.SetAttribute("style", "filled");
                                string conditionHtml = "<table border=\"0\"><tr><td>All Other Conditions</td></tr></table>";
                                elseActionsNode.SetAttributeHtml("label", conditionHtml);
                                Edge edge = rootGraph.GetOrAddEdge(prevNode, elseActionsNode, "edge to " + "elseactionsnode-" + actionName);
                                edge.SetAttribute("weight", "1");
                                conditionCluster.AddExisting(elseActionsNode);

                                // Process else actions - PASS conditionCluster as parentCluster for nesting
                                Node lastElseNode = addActionNodes((YamlMappingNode)action, elseActionsNode, rootGraph, elseActions, conditionCluster);
                                lastNodes.Add(lastElseNode ?? elseActionsNode);
                                sameLevelNodes.Add(elseActionsNode);
                            }

                            // Create exit node AFTER processing all conditions
                            clusterExitNode = rootGraph.GetOrAddNode(actionName + "_cluster_exit");
                            clusterExitNode.SetAttribute("margin", "0");
                            clusterExitNode.SetAttribute("style", "invis");
                            clusterExitNode.SetAttribute("width", "0");
                            clusterExitNode.SetAttribute("height", "0");
                            clusterExitNode.SetAttribute("shape", "point");
                            conditionCluster.AddExisting(clusterExitNode);

                            // Connect all last nodes to the exit node with invisible edges
                            foreach (var lastNode in lastNodes)
                            {
                                if (lastNode != null)
                                {
                                    Edge exitEdge = rootGraph.GetOrAddEdge(lastNode, clusterExitNode, "to_exit_" + lastNode.GetName());
                                    exitEdge.SetAttribute("style", "invis");
                                    exitEdge.SetAttribute("weight", "100");
                                    exitEdge.SetAttribute("minlen", "1");
                                }
                            }

                            returnNode = clusterExitNode;
                            prevNode = clusterExitNode;
                            break;
                        case "AIModel":
                            string aiModelHtml = $"<table border=\"0\"><tr><td>{createActionHeaderImageTable(actionType, "Prompt")}</td></tr>";
                            //todo check why displayName is sometimes not available, and how it is rendered
                            aiModelHtml += $"<tr><td>{displayName}</td></tr></table>";
                            //todo
                            actionNode.SetAttributeHtml("label", aiModelHtml);
                            break;
                        case "Message":
                            YamlScalarNode messageYaml = null;
                            var activityYaml = ((YamlMappingNode)action).Children[new YamlScalarNode("activity")];
                            if (activityYaml.GetType().Equals(typeof(YamlMappingNode)))
                            {
                                messageYaml = (YamlScalarNode)((YamlSequenceNode)((YamlMappingNode)activityYaml).Children[new YamlScalarNode("text")]).First();
                                //todo there may also be a speak node, or maybe even no text
                            }
                            else if (activityYaml.GetType().Equals(typeof(YamlScalarNode)))
                            {
                                messageYaml = (YamlScalarNode)activityYaml;
                            }
                            actionNode.SetAttributeHtml("label", $"<table border=\"1\"><tr><td>{createActionHeaderImageTable(actionType, "Message")}</td></tr><tr><td>" + CharsetHelper.GetSafeName(messageYaml.Value).Replace("\n", "<br/>") + "</td></tr></table>");
                            break;
                        case "SetVariable":
                            var variableYaml = (YamlScalarNode)((YamlMappingNode)action).Children[new YamlScalarNode("variable")];
                            var valueYaml = (YamlScalarNode)((YamlMappingNode)action).Children[new YamlScalarNode("value")];
                            string html = $"<table border=\"0\"><tr><td>{createActionHeaderImageTable(actionType, "Set Variable")}</td></tr>";
                            html += "<tr><td><table border=\"1\"><tr><td>" + variableYaml.Value + "</td></tr></table></td></tr>";
                            html += "<tr><td>To Value</td></tr>";
                            string variableValue = valueYaml.Value;
                            if (variableValue.StartsWith('='))
                            {
                                variableValue = variableValue.Substring(1);
                            }
                            html += "<tr><td><table border=\"1\"><tr><td>" + generateMultiLineText(variableValue) + "</td></tr></table></td></tr></table>"; ;
                            actionNode.SetAttributeHtml("label", html);
                            break;
                        case "CancelAllDialogs":
                            actionNode.SetAttributeHtml("label", $"<table border=\"0\"><tr><td>{createActionHeaderImageTable(actionType, "End all topics")}</td></tr><tr><td></td></tr></table>");
                            //todo use displayName if it exists instead of default text
                            break;
                        case "LogCustomTelemetry":
                            actionNode.SetAttributeHtml("label", $"<table border=\"0\"><tr><td>{createActionHeaderImageTable(actionType, "Log custom telemetry event")}</td></tr><tr><td></td></tr></table>");
                            //todo use displayName if it exists instead of default text
                            break;
                        default:
                            actionNode.SetAttribute("label", CharsetHelper.GetSafeName(actionName));
                            break;
                    }
                    if (conditionCluster == null)
                    {
                        Edge edge = rootGraph.GetOrAddEdge((Node)prevNode, actionNode, actionName);
                    }
                    if (actionType != "ConditionGroup")
                    {
                        prevNode = actionNode;
                    }
                }
            }
            return returnNode;
        }

        private object createActionHeaderImageTable(string actionType, string headerText)
        {
            return $"<table border=\"0\"><tr><td width=\"24\"><img src=\"{folderPath + @"Resources\" + actionType}.png\" /></td><td>{headerText}</td></tr></table>";
        }

        private string GetActionType(YamlMappingNode action)
        {
            string actionType = ((YamlMappingNode)action).Children[new YamlScalarNode("kind")].ToString();
            switch (actionType)
            {
                case "AdaptiveCardPrompt":
                    return "AdaptiveCard";
                case "ConditionGroup":
                    return "ConditionGroup";
                case "InvokeAIBuilderModelAction":
                    return "AIModel";
                case "SendActivity":
                    return "Message";
                case "SetVariable":
                    return "SetVariable";
                case "LogCustomTelemetryEvent":
                    return "LogCustomTelemetry";
                case "Question":
                    return "Question";
                default:
                    return actionType; //return the type as is if not recognized
            }
        }

        private string generateImageFiles(RootGraph rootGraph, bool showSubactions)
        {
            //Generate image files
            string filename = topic.getTopicFileName() + (showSubactions ? "-detailed" : "");
            rootGraph.ToPngFile(folderPath + filename + ".png");
            rootGraph.ToSvgFile(folderPath + filename + ".svg");
            return filename;
        }

        //splits a text into multiple lines (<br/> for line breaks), with each line having a maximum of 65 characters
        private string generateMultiLineText(string text)
        {
            string[] words = text.Split(' ');
            string multiLineText = "";
            int lineLength = 0;
            for (var counter = 0; counter < words.Length; counter++)
            {
                lineLength += words[counter].Length + 1;
                if (lineLength >= 65)
                {
                    multiLineText += "<br/>";
                    lineLength = 0;
                }
                multiLineText = multiLineText + words[counter] + " ";
            }
            return multiLineText;
        }

        private string RenderCardElement(JObject element)
        {
            if (element == null) return "";

            string elementType = element["type"]?.ToString() ?? "";
            return elementType switch
            {
                "TextBlock" => RenderTextBlock(element),
                "Input.Text" => RenderInputText(element),
                "Input.ChoiceSet" => RenderInputChoiceSet(element),
                /*"Input.Date" => RenderInputDate(element),
                "Input.Number" => RenderInputNumber(element),
                "Input.Toggle" => RenderInputToggle(element),
                "Container" => RenderContainer(element),
                "ColumnSet" => RenderColumnSet(element),
                "Image" => RenderImage(element),
                "FactSet" => RenderFactSet(element),
                "ActionSet" => RenderActionSet(element),*/
                _ => $"<i>Element: {elementType}</i>"
            };
        }

        private string RenderCardAction(JObject action)
        {
            if (action == null) return "";

            string actionType = action["type"]?.ToString() ?? "";
            string title = action["title"]?.ToString() ?? "Action";

            return actionType switch
            {
                "Action.Submit" => $"<b>Submit:</b> {title}",
                "Action.OpenUrl" => $"<b>Open URL:</b> {title} → {action["url"]?.ToString() ?? ""}",
                "Action.ShowCard" => $"<b>Show Card:</b> {title}",
                "Action.Execute" => $"<b>Execute:</b> {title}",
                "Action.ToggleVisibility" => $"<b>Toggle Visibility:</b> {title}",
                _ => $"<b>{actionType}:</b> {title}"
            };
        }

        private string RenderTextBlock(JObject element)
        {
            string text = element["text"]?.ToString() ?? "";
            return generateMultiLineText(System.Web.HttpUtility.HtmlEncode(text));
        }

        private string RenderInputText(JObject element)
        {
            string id = element["id"]?.ToString() ?? "";
            string placeholder = element["placeholder"]?.ToString() ?? "";
            string label = element["label"]?.ToString() ?? "";
            bool isMultiline = element["isMultiline"]?.ToObject<bool>() ?? false;

            string inputType = isMultiline ? "Multi-line Text" : "Text";
            return $"<b>{inputType} Input:</b> {generateMultiLineText(System.Web.HttpUtility.HtmlEncode(label))}<br/>{generateMultiLineText(System.Web.HttpUtility.HtmlEncode(placeholder))}<br/>";
        }

        private string RenderInputChoiceSet(JObject element)
        {
            string id = element["id"]?.ToString() ?? "";
            string label = element["label"]?.ToString() ?? "";
            bool isMultiSelect = element["isMultiSelect"]?.ToObject<bool>() ?? false;

            string choicesText = "";
            if (element["choices"] is JArray choices)
            {
                var choiceList = new List<string>();
                foreach (var choice in choices)
                {
                    string title = choice["title"]?.ToString() ?? "";
                    choiceList.Add(System.Web.HttpUtility.HtmlEncode(title));
                }
                choicesText = string.Join("<br/> ", choiceList);
            }

            string selectType = isMultiSelect ? "Multi-Select" : "Single-Select";
            return $"<b>{selectType}:</b><br/> {generateMultiLineText(System.Web.HttpUtility.HtmlEncode(label))}<br/>{choicesText}";
        }

        private string RenderInputDate(JObject element)
        {
            string id = element["id"]?.ToString() ?? "";
            string label = element["label"]?.ToString() ?? "";
            string min = element["min"]?.ToString() ?? "";
            string max = element["max"]?.ToString() ?? "";

            string constraints = "";
            if (!string.IsNullOrEmpty(min) || !string.IsNullOrEmpty(max))
            {
                constraints = $"<br/>Range: {min} to {max}";
            }

            return $"<b>Date Input:</b> {label} (ID: {id}){constraints}";
        }

        private string RenderInputNumber(JObject element)
        {
            string id = element["id"]?.ToString() ?? "";
            string label = element["label"]?.ToString() ?? "";
            string min = element["min"]?.ToString() ?? "";
            string max = element["max"]?.ToString() ?? "";

            string constraints = "";
            if (!string.IsNullOrEmpty(min) || !string.IsNullOrEmpty(max))
            {
                constraints = $"<br/>Range: {min} to {max}";
            }

            return $"<b>Number Input:</b> {label} (ID: {id}){constraints}";
        }

        private string RenderInputToggle(JObject element)
        {
            string id = element["id"]?.ToString() ?? "";
            string title = element["title"]?.ToString() ?? "";
            string valueOn = element["valueOn"]?.ToString() ?? "true";
            string valueOff = element["valueOff"]?.ToString() ?? "false";

            return $"<b>Toggle:</b> {title} (ID: {id})<br/>Values: {valueOff}/{valueOn}";
        }

        private string RenderContainer(JObject element)
        {
            int itemCount = 0;
            if (element["items"] is JArray items)
            {
                itemCount = items.Count;
            }

            return $"<b>Container</b> with {itemCount} nested item(s)";
        }

        private string RenderColumnSet(JObject element)
        {
            int columnCount = 0;
            if (element["columns"] is JArray columns)
            {
                columnCount = columns.Count;
            }

            return $"<b>Column Set</b> with {columnCount} column(s)";
        }

        private string RenderImage(JObject element)
        {
            string url = element["url"]?.ToString() ?? "";
            string altText = element["altText"]?.ToString() ?? "";
            string size = element["size"]?.ToString() ?? "auto";

            return $"<b>Image:</b> {(!string.IsNullOrEmpty(altText) ? altText : "No alt text")}<br/>Size: {size}";
        }

        private string RenderFactSet(JObject element)
        {
            int factCount = 0;
            if (element["facts"] is JArray facts)
            {
                factCount = facts.Count;
                var factList = new List<string>();
                foreach (var fact in facts.Take(3)) // Show first 3 facts
                {
                    string title = fact["title"]?.ToString() ?? "";
                    string value = fact["value"]?.ToString() ?? "";
                    factList.Add($"{title}: {value}");
                }
                string factText = string.Join("<br/>", factList);
                if (factCount > 3) factText += $"<br/>... and {factCount - 3} more";
                return $"<b>Fact Set</b> ({factCount} facts):<br/>{factText}";
            }

            return $"<b>Fact Set</b> with {factCount} fact(s)";
        }

        private string RenderActionSet(JObject element)
        {
            int actionCount = 0;
            if (element["actions"] is JArray actions)
            {
                actionCount = actions.Count;
            }

            return $"<b>Action Set</b> with {actionCount} action(s)";
        }

        // Add this method to create constraint edges between parallel nodes
        //TODO potentially no longer required
        private void CreateLevelConstraints(RootGraph rootGraph, SubGraph cluster, List<Node> parallelNodes)
        {
            if (parallelNodes.Count <= 1) return;
            Console.WriteLine(topic.Name);
            foreach (Node node in parallelNodes)
            {
                Console.WriteLine("Parallel Node: " + node.GetName());
            }
            Console.WriteLine();
            // Create invisible edges between parallel nodes to maintain same level
            for (int i = 0; i < parallelNodes.Count - 1; i++)
            {
                Edge constraintEdge = rootGraph.GetOrAddEdge(parallelNodes[i], parallelNodes[i + 1], "constraint_" + i);
                constraintEdge.SetAttribute("style", "invis");
                constraintEdge.SetAttribute("constraint", "false"); // Don't affect layout, just ranking
                constraintEdge.SetAttribute("weight", "0");
            }
            // Create a rank subgraph
            string rankName = "rank_" + parallelNodes[0].GetName() + "_group";
            SubGraph rankGroup = cluster.GetOrAddSubgraph(rankName);
            rankGroup.SetAttribute("rank", "same");
            rankGroup.SetAttribute("style", "invis");

            foreach (var node in parallelNodes)
            {
                rankGroup.AddExisting(node);
            }
        }
    }

    public static class GraphColours
    {
        public static string TriggerColour = "#0077ff";
        public static string TriggerFillColour = "#e7f4ff";
        public static string SetVariableFillColour = "#e7f4ff";
        public static string SetVariableColour = "#118dff";
        public static string ConditionFillColour = "#e7f4ff";
        public static string ConditionColour = "#118dff";
        public static string MessageColour = "#672367";
        public static string MessageFillColour = "#f0e9f0";
        public static string AdaptiveCardColour = "#672367";
        public static string AdaptiveCardFillColour = "#f0e9f0";
        public static string AIModelCardColour = "#672367";
        public static string AIModelCardFillColour = "#f0e9f0";
        public static string CancelAllDialogsColour = "#6bb700";
        public static string CancelAllDialogsFillColour = "#f0f8e6";
        public static string LogCustomTelemetryColour = "#242424";
        public static string LogCustomTelemetryFillColour = "#edebe9";
        public static string QuestionColour = "#672367";
        public static string QuestionFillColour = "#f0e9f0";

        public static string GetColourForAction(string actionType)
        {
            return actionType switch
            {
                "Trigger" => TriggerColour,
                "Message" => MessageColour,
                "Question" => QuestionColour,
                "CancelAllDialogs" => CancelAllDialogsColour,
                "SetVariable" => SetVariableColour,
                "ConditionGroup" => ConditionColour,
                "LogCustomTelemetry" => LogCustomTelemetryColour,
                "AdaptiveCard" => AdaptiveCardColour,
                "AIModel" => AIModelCardColour,
                _ => "black",
            };
        }

        public static string GetFillColourForAction(string actionType)
        {
            string colour = actionType switch
            {
                "Trigger" => TriggerFillColour,
                "Message" => MessageFillColour,
                "Question" => QuestionFillColour,
                "CancelAllDialogs" => CancelAllDialogsFillColour,
                "SetVariable" => SetVariableFillColour,
                "ConditionGroup" => ConditionFillColour,
                "LogCustomTelemetry" => LogCustomTelemetryFillColour,
                "AdaptiveCard" => AdaptiveCardFillColour,
                "AIModel" => AIModelCardFillColour,
                _ => "red",
            };
            return colour;
        }
    }
}
