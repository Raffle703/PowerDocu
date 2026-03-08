using System;
using System.Collections.Generic;
using System.IO;
using PowerDocu.Common;

namespace PowerDocu.AppModuleDocumenter
{
    public static class AppModuleDocumentationGenerator
    {
        /// <summary>
        /// Generates documentation output for Model-Driven Apps using the DocumentationContext.
        /// AppModules are extracted from Customizations during the orchestrator's parse phase.
        /// </summary>
        public static void GenerateOutput(DocumentationContext context, string path)
        {
            if (context.AppModules == null || context.AppModules.Count == 0 || !context.Config.documentModelDrivenApps) return;

            DateTime startDocGeneration = DateTime.Now;
            NotificationHelper.SendNotification($"Found {context.AppModules.Count} Model-Driven App(s) in the solution.");

            if (context.FullDocumentation)
            {
                foreach (AppModuleEntity appModule in context.AppModules)
                {
                    AppModuleDocumentationContent content = new AppModuleDocumentationContent(appModule, path, context);

                    if (appModule.SiteMap != null)
                    {
                        SiteMapSvgBuilder.GenerateSiteMapSvg(appModule.SiteMap, content.folderPath, content.filename);
                    }

                    string wordTemplate = (!String.IsNullOrEmpty(context.Config.wordTemplate) && File.Exists(context.Config.wordTemplate))
                        ? context.Config.wordTemplate : null;
                    if (context.Config.outputFormat.Equals(OutputFormatHelper.Word) || context.Config.outputFormat.Equals(OutputFormatHelper.All))
                    {
                        NotificationHelper.SendNotification("Creating Word documentation for Model-Driven App: " + appModule.GetDisplayName());
                        AppModuleWordDocBuilder wordDoc = new AppModuleWordDocBuilder(content, wordTemplate);
                    }
                    if (context.Config.outputFormat.Equals(OutputFormatHelper.Markdown) || context.Config.outputFormat.Equals(OutputFormatHelper.All))
                    {
                        NotificationHelper.SendNotification("Creating Markdown documentation for Model-Driven App: " + appModule.GetDisplayName());
                        AppModuleMarkdownBuilder markdownDoc = new AppModuleMarkdownBuilder(content);
                    }
                    if (context.Config.outputFormat.Equals(OutputFormatHelper.Html) || context.Config.outputFormat.Equals(OutputFormatHelper.All))
                    {
                        NotificationHelper.SendNotification("Creating HTML documentation for Model-Driven App: " + appModule.GetDisplayName());
                        AppModuleHtmlBuilder htmlDoc = new AppModuleHtmlBuilder(content);
                    }
                }
            }

            DateTime endDocGeneration = DateTime.Now;
            NotificationHelper.SendNotification(
                $"AppModuleDocumenter: Processed {context.AppModules.Count} Model-Driven App(s) in {(endDocGeneration - startDocGeneration).TotalSeconds} seconds."
            );
        }

        /// <summary>
        /// Legacy method: generates documentation from a SolutionEntity directly.
        /// </summary>
        public static List<AppModuleEntity> GenerateDocumentation(
            SolutionEntity solution,
            bool fullDocumentation,
            ConfigHelper config,
            string path,
            List<AppEntity> apps = null
        )
        {
            if (solution?.Customizations == null)
            {
                NotificationHelper.SendNotification("No customizations found, skipping Model-Driven App documentation.");
                return new List<AppModuleEntity>();
            }

            List<AppModuleEntity> appModules = solution.Customizations.getAppModules();
            if (appModules == null || appModules.Count == 0)
            {
                NotificationHelper.SendNotification("No Model-Driven Apps found in the solution.");
                return new List<AppModuleEntity>();
            }

            var context = new DocumentationContext
            {
                Solution = solution,
                Customizations = solution.Customizations,
                AppModules = appModules,
                Apps = apps ?? new List<AppEntity>(),
                Roles = solution.Customizations.getRoles(),
                Tables = solution.Customizations.getEntities(),
                Config = config,
                FullDocumentation = fullDocumentation
            };
            GenerateOutput(context, path);
            return appModules;
        }
    }
}
