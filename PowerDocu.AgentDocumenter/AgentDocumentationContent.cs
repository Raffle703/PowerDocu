
using System.Collections.Generic;
using System.IO;
using PowerDocu.Common;

namespace PowerDocu.AgentDocumenter
{

    class AgentDocumentationContent
    {
        public string folderPath,
           filename;
        public AgentEntity agent;
        public DocumentationContext context;

        public string headerDocumentationGenerated = "Documentation generated at";
        public string Details = "Details";
        public string Description = "Description";
        public string Orchestration = "Orchestration";
        public string OrchestrationText = "Use generative AI to determine how best to respond to users and events.";
        public string ResponseModel = "Response model";
        public string Instructions = "Instructions";
        public string Knowledge = "Knowledge";
        public string WebSearch = "Web Search";
        public string Tools = "Tools";
        public string Triggers = "Triggers";
        public string Agents = "Agents";
        public string Topics = "Topics";
        public string Entities = "Entities";
        public string Variables = "Variables";
        public string SuggestedPrompts = "Suggested prompts";
        public string SuggestedPromptsText = "Suggest ways of starting conversations for Teams and Microsoft 365 channels.";

        public AgentDocumentationContent(AgentEntity agent, string path, DocumentationContext context = null)
        {
            NotificationHelper.SendNotification("Preparing documentation content for " + agent.Name);
            folderPath = path + CharsetHelper.GetSafeName(@"\AgentDoc " + agent.Name + @"\");
            Directory.CreateDirectory(folderPath);
            filename = CharsetHelper.GetSafeName(agent.Name);
            this.agent = agent;
            this.context = context;
        }

        /// <summary>
        /// Resolves a flow ID to its display name using the DocumentationContext.
        /// </summary>
        public string GetFlowNameForId(string flowId)
        {
            if (string.IsNullOrEmpty(flowId)) return flowId;
            return context?.GetFlowNameById(flowId) ?? flowId;
        }

        /// <summary>
        /// Resolves a Dataverse table schema name to its display name using the DocumentationContext.
        /// </summary>
        public string GetTableDisplayName(string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName)) return schemaName;
            return context?.GetTableDisplayName(schemaName) ?? schemaName;
        }

        /// <summary>
        /// Returns all tool infos with flow names resolved via the DocumentationContext.
        /// </summary>
        public List<AgentToolInfo> GetResolvedToolInfos()
        {
            var tools = agent.GetAllToolInfos();
            if (context != null)
            {
                foreach (var tool in tools)
                {
                    if (!string.IsNullOrEmpty(tool.FlowId) && string.IsNullOrEmpty(tool.AgentFlowName))
                    {
                        tool.AgentFlowName = context.GetFlowNameById(tool.FlowId);
                    }
                }
            }
            return tools;
        }
    }
}