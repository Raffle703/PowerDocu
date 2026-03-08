using System;
using System.Collections.Generic;
using System.Linq;
using PowerDocu.Common;

namespace PowerDocu.SolutionDocumenter
{
    public class SolutionDocumentationContent
    {
        public List<FlowEntity> flows = new List<FlowEntity>();
        public List<AppEntity> apps = new List<AppEntity>();
        public List<AppModuleEntity> appModules = new List<AppModuleEntity>();
        public SolutionEntity solution;
        public DocumentationContext context;
        public string folderPath,
            filename;

        public SolutionDocumentationContent(
            DocumentationContext context,
            string path
        )
        {
            this.context = context;
            this.solution = context.Solution;
            this.apps = context.Apps ?? new List<AppEntity>();
            this.flows = context.Flows ?? new List<FlowEntity>();
            this.appModules = context.AppModules ?? new List<AppModuleEntity>();
            filename = CharsetHelper.GetSafeName(solution.UniqueName);
            folderPath = path;
        }

        /// <summary>
        /// Legacy constructor for backward compatibility.
        /// </summary>
        public SolutionDocumentationContent(
            SolutionEntity solution,
            List<AppEntity> apps,
            List<FlowEntity> flows,
            List<AppModuleEntity> appModules,
            string path
        )
        {
            this.solution = solution;
            this.apps = apps ?? new List<AppEntity>();
            this.flows = flows ?? new List<FlowEntity>();
            this.appModules = appModules ?? new List<AppModuleEntity>();
            filename = CharsetHelper.GetSafeName(solution.UniqueName);
            folderPath = path;
        }

        public string GetDisplayNameForComponent(SolutionComponent component)
        {
            if (component.Type == "Workflow")
            {
                // Try to resolve flow by ID using the context first (most reliable)
                if (context != null)
                {
                    string flowName = context.GetFlowNameById(component.ID);
                    if (!string.IsNullOrEmpty(flowName))
                    {
                        FlowEntity flow = context.GetFlowById(component.ID);
                        if (flow?.trigger != null)
                            return flowName + " (" + flow.trigger.Name + ": " + flow.trigger.Type + ")";
                        return flowName;
                    }
                }
                // Fallback: search parsed flows list by ID
                FlowEntity flowEntity = flows?.FirstOrDefault(f =>
                    f.ID != null && f.ID.Trim('{', '}').Equals(component.ID?.Trim('{', '}'), StringComparison.OrdinalIgnoreCase));
                if (flowEntity != null)
                {
                    return flowEntity.Name + " (" + flowEntity.trigger.Name + ": " + flowEntity.trigger.Type + ")";
                }
            }
            if (component.Type == "Model-Driven App")
            {
                AppModuleEntity appModule = appModules?.FirstOrDefault(a => a.UniqueName != null && a.UniqueName.Equals(component.SchemaName, StringComparison.OrdinalIgnoreCase));
                if (appModule != null)
                {
                    return appModule.GetDisplayName();
                }
            }
            return solution.GetDisplayNameForComponent(component);
        }
    }
}
