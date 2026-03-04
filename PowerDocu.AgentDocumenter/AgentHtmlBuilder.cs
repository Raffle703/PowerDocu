using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using PowerDocu.Common;

namespace PowerDocu.AgentDocumenter
{
    class AgentHtmlBuilder : HtmlBuilder
    {
        private readonly AgentDocumentationContent content;
        private readonly string mainFileName, knowledgeFileName, toolsFileName, agentsFileName, topicsFileName, channelsFileName, settingsFileName;
        private readonly Dictionary<string, string> topicFileNames = new Dictionary<string, string>();

        public AgentHtmlBuilder(AgentDocumentationContent contentdocumentation)
        {
            content = contentdocumentation;
            Directory.CreateDirectory(content.folderPath);
            WriteDefaultStylesheet(content.folderPath);

            mainFileName = ("index-" + content.filename + ".html").Replace(" ", "-");
            knowledgeFileName = ("knowledge-" + content.filename + ".html").Replace(" ", "-");
            toolsFileName = ("tools-" + content.filename + ".html").Replace(" ", "-");
            agentsFileName = ("agents-" + content.filename + ".html").Replace(" ", "-");
            topicsFileName = ("topics-" + content.filename + ".html").Replace(" ", "-");
            channelsFileName = ("channels-" + content.filename + ".html").Replace(" ", "-");
            settingsFileName = ("settings-" + content.filename + ".html").Replace(" ", "-");

            foreach (BotComponent topic in content.agent.GetTopics().OrderBy(o => o.Name).ToList())
            {
                topicFileNames[topic.Name] = ("topic-" + CharsetHelper.GetSafeName(topic.Name) + "-" + content.filename + ".html").Replace(" ", "-");
            }

            addAgentOverview();
            addAgentKnowledgeInfo();
            addAgentTools();
            addAgentAgentsInfo();
            addAgentTopics();
            addAgentChannels();
            addAgentSettings();
            NotificationHelper.SendNotification("Created HTML documentation for " + content.filename);
        }

        private string getNavigationHtml()
        {
            var navItems = new List<(string label, string href)>
            {
                ("Overview", mainFileName),
                ("Knowledge", knowledgeFileName),
                ("Tools", toolsFileName),
                ("Agents", agentsFileName),
                ("Topics", topicsFileName),
                ("Channels", channelsFileName),
                ("Settings", settingsFileName)
            };
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<div class=\"nav-title\">{Encode(content.filename)}</div>");
            sb.Append(NavigationList(navItems));
            return sb.ToString();
        }

        private string buildMetadataTable()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(TableStart("Property", "Value"));
            sb.Append(TableRow("Agent Name", content.agent.Name));
            if (!String.IsNullOrEmpty(content.agent.IconBase64))
            {
                Directory.CreateDirectory(content.folderPath);
                Bitmap agentLogo = ImageHelper.ConvertBase64ToBitmap(content.agent.IconBase64);
                string logoFileName = $"agentlogo-{content.filename.Replace(" ", "-")}.png";
                agentLogo.Save(content.folderPath + logoFileName);
                sb.Append(TableRowRaw("Agent Logo", Image("Agent Logo", logoFileName)));
                agentLogo.Dispose();
            }
            sb.Append(TableRow(content.headerDocumentationGenerated, PowerDocuReleaseHelper.GetTimestampWithVersion()));
            sb.AppendLine(TableEnd());
            return sb.ToString();
        }

        private void addAgentOverview()
        {
            StringBuilder body = new StringBuilder();
            body.AppendLine(Heading(1, $"Agent - {content.filename}"));
            body.AppendLine(buildMetadataTable());

            body.AppendLine(Heading(2, content.Details));
            body.AppendLine(Heading(3, content.Description));
            body.AppendLine(Paragraph(content.agent.GetDescription()));
            body.AppendLine(Heading(3, content.Orchestration));
            body.AppendLine(Paragraph($"{content.OrchestrationText} - {content.agent.GetOrchestration()}"));
            body.AppendLine(Heading(3, content.ResponseModel));
            body.AppendLine(Paragraph(content.agent.GetResponseModel()));
            body.AppendLine(Heading(3, content.Instructions));
            body.AppendLine(Paragraph(content.agent.GetInstructions()));

            body.AppendLine(Heading(3, content.Knowledge));
            foreach (BotComponent knowledgeSource in content.agent.GetKnowledge())
            {
                body.AppendLine(Paragraph(knowledgeSource.Name));
            }

            body.AppendLine(Heading(3, content.WebSearch));
            body.AppendLine(Paragraph("TODO"));
            body.AppendLine(Heading(3, content.Triggers));
            body.AppendLine(Paragraph("TODO"));
            body.AppendLine(Heading(3, content.Agents));
            body.AppendLine(Paragraph("TODO"));

            body.AppendLine(Heading(3, content.Topics));
            body.AppendLine(BulletListStart());
            foreach (BotComponent topic in content.agent.GetTopics().OrderBy(o => o.Name))
            {
                string topicFile = topicFileNames.GetValueOrDefault(topic.Name, "#");
                body.AppendLine(BulletItemRaw(Link(topic.Name, topicFile)));
            }
            body.AppendLine(BulletListEnd());

            body.AppendLine(Heading(3, content.SuggestedPrompts));
            body.AppendLine(Paragraph(content.SuggestedPromptsText));
            body.Append(TableStart("Prompt Title", "Prompt"));
            Dictionary<string, string> conversationStarters = content.agent.GetSuggestedPrompts();
            foreach (var kvp in conversationStarters.OrderBy(x => x.Key))
            {
                body.Append(TableRow(kvp.Key, kvp.Value));
            }
            body.AppendLine(TableEnd());

            SaveHtmlFile(Path.Combine(content.folderPath, mainFileName),
                WrapInHtmlPage($"Agent - {content.filename}", body.ToString(), getNavigationHtml()));
        }

        private void addAgentKnowledgeInfo()
        {
            StringBuilder body = new StringBuilder();
            body.AppendLine(Heading(1, $"Agent - {content.filename}"));
            body.AppendLine(buildMetadataTable());
            body.AppendLine(Heading(2, content.Knowledge));
            body.AppendLine(Paragraph("Knowledge sources for this agent."));
            foreach (BotComponent knowledgeSource in content.agent.GetKnowledge())
            {
                body.AppendLine(Heading(3, knowledgeSource.Name));
            }
            SaveHtmlFile(Path.Combine(content.folderPath, knowledgeFileName),
                WrapInHtmlPage($"Knowledge - {content.filename}", body.ToString(), getNavigationHtml()));
        }

        private void addAgentTools()
        {
            StringBuilder body = new StringBuilder();
            body.AppendLine(Heading(1, $"Agent - {content.filename}"));
            body.AppendLine(buildMetadataTable());
            body.AppendLine(Heading(2, content.Tools));
            body.AppendLine(Paragraph("Tools available for this agent."));
            SaveHtmlFile(Path.Combine(content.folderPath, toolsFileName),
                WrapInHtmlPage($"Tools - {content.filename}", body.ToString(), getNavigationHtml()));
        }

        private void addAgentAgentsInfo()
        {
            StringBuilder body = new StringBuilder();
            body.AppendLine(Heading(1, $"Agent - {content.filename}"));
            body.AppendLine(buildMetadataTable());
            body.AppendLine(Heading(2, content.Agents));
            body.AppendLine(Paragraph("Sub-agents for this agent."));
            SaveHtmlFile(Path.Combine(content.folderPath, agentsFileName),
                WrapInHtmlPage($"Agents - {content.filename}", body.ToString(), getNavigationHtml()));
        }

        private void addAgentTopics()
        {
            StringBuilder body = new StringBuilder();
            body.AppendLine(Heading(1, $"Agent - {content.filename}"));
            body.AppendLine(buildMetadataTable());
            body.AppendLine(Heading(2, content.Topics));
            body.Append(TableStart("Name", "Type", "Trigger", "Enabled"));
            foreach (BotComponent topic in content.agent.GetTopics().OrderBy(o => o.Name).ToList())
            {
                string topicFile = topicFileNames.GetValueOrDefault(topic.Name, "#");
                body.Append(TableRowRaw(Link(topic.Name, topicFile), "TODO", Encode(topic.GetTriggerTypeForTopic()), "TODO"));
            }
            body.AppendLine(TableEnd());

            SaveHtmlFile(Path.Combine(content.folderPath, topicsFileName),
                WrapInHtmlPage($"Topics - {content.filename}", body.ToString(), getNavigationHtml()));

            // Build per-topic pages
            foreach (BotComponent topic in content.agent.GetTopics().OrderBy(o => o.Name).ToList())
            {
                StringBuilder topicBody = new StringBuilder();
                topicBody.AppendLine(Heading(1, $"Agent - {content.filename}"));
                topicBody.AppendLine(buildMetadataTable());
                topicBody.AppendLine(Heading(2, "Topic: " + topic.Name));
                topicBody.AppendLine(Paragraph("Trigger: " + topic.GetTriggerTypeForTopic()));

                string topicFile = topicFileNames.GetValueOrDefault(topic.Name, topic.Name + ".html");
                SaveHtmlFile(Path.Combine(content.folderPath, topicFile),
                    WrapInHtmlPage($"Topic: {topic.Name}", topicBody.ToString(), getNavigationHtml()));
            }
        }

        private void addAgentChannels()
        {
            StringBuilder body = new StringBuilder();
            body.AppendLine(Heading(1, $"Agent - {content.filename}"));
            body.AppendLine(buildMetadataTable());
            body.AppendLine(Heading(2, "Channels"));
            body.AppendLine(Paragraph("Channel configuration for this agent."));
            SaveHtmlFile(Path.Combine(content.folderPath, channelsFileName),
                WrapInHtmlPage($"Channels - {content.filename}", body.ToString(), getNavigationHtml()));
        }

        private void addAgentSettings()
        {
            StringBuilder body = new StringBuilder();
            body.AppendLine(Heading(1, $"Agent - {content.filename}"));
            body.AppendLine(buildMetadataTable());

            string[] sections = new[] { "Generative AI", "Security", "Connection settings",
                "Authoring canvas", "Entities", "Skills", "Voice", "Languages",
                "Language understanding", "Component collections", "Advanced" };
            foreach (string section in sections)
            {
                body.AppendLine(Heading(2, section));
                body.AppendLine(Paragraph("TODO"));
            }

            SaveHtmlFile(Path.Combine(content.folderPath, settingsFileName),
                WrapInHtmlPage($"Settings - {content.filename}", body.ToString(), getNavigationHtml()));
        }
    }
}
