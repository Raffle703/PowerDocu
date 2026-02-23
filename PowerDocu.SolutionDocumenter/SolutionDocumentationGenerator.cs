using System;
using System.Collections.Generic;
using System.IO;
using PowerDocu.Common;
using PowerDocu.AppDocumenter;
using PowerDocu.FlowDocumenter;

namespace PowerDocu.SolutionDocumenter
{
    public static class SolutionDocumentationGenerator
    {
        private static List<FlowEntity> flows;
        private static List<AppEntity> apps;

        public static void GenerateDocumentation(string filePath, bool fullDocumentation, ConfigHelper config, string outputPath = null)
        {
            if (File.Exists(filePath))
            {
                DateTime startDocGeneration = DateTime.Now;

                flows = FlowDocumentationGenerator.GenerateDocumentation(
                    filePath,
                    fullDocumentation,
                    config,
                    outputPath
                );

                apps = AppDocumentationGenerator.GenerateDocumentation(
                    filePath,
                    fullDocumentation,
                    config,
                    outputPath
                );

                // Parse and document the solution if enabled
                if (config.documentSolution)
                {
                    SolutionParser solutionParser = new SolutionParser(filePath);
                    if (solutionParser.solution != null)
                    {
                        string path = outputPath == null
                            ? Path.GetDirectoryName(filePath) + @"\Solution " + CharsetHelper.GetSafeName(Path.GetFileNameWithoutExtension(filePath) + @"\")
                            : outputPath + @"\" + CharsetHelper.GetSafeName(Path.GetFileNameWithoutExtension(filePath) + @"\");

                        SolutionDocumentationContent solutionContent = new SolutionDocumentationContent(solutionParser.solution, apps, flows, path);
                        DataverseGraphBuilder dataverseGraphBuilder = new DataverseGraphBuilder(solutionContent);

                        if (fullDocumentation)
                        {
                            if (config.outputFormat.Equals(OutputFormatHelper.Word) || config.outputFormat.Equals(OutputFormatHelper.All))
                            {
                                NotificationHelper.SendNotification("Creating Solution documentation");
                                SolutionWordDocBuilder wordzip = new SolutionWordDocBuilder(solutionContent, config.wordTemplate);
                            }
                            if (config.outputFormat.Equals(OutputFormatHelper.Markdown) || config.outputFormat.Equals(OutputFormatHelper.All))
                            {
                                SolutionMarkdownBuilder mdDoc = new SolutionMarkdownBuilder(solutionContent);
                            }
                            if (config.outputFormat.Equals(OutputFormatHelper.Html) || config.outputFormat.Equals(OutputFormatHelper.All))
                            {
                                NotificationHelper.SendNotification("Creating HTML Solution documentation");
                                SolutionHtmlBuilder htmlDoc = new SolutionHtmlBuilder(solutionContent);
                            }
                        }
                    }
                }

                DateTime endDocGeneration = DateTime.Now;
                NotificationHelper.SendNotification($"SolutionDocumenter: Created documentation for {filePath}. Total solution documentation completed in {(endDocGeneration - startDocGeneration).TotalSeconds} seconds.");
            }
            else
            {
                NotificationHelper.SendNotification($"File not found: {filePath}");
            }
        }
    }
}