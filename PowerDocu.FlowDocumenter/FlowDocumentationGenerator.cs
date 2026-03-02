using System;
using System.Collections.Generic;
using System.IO;
using PowerDocu.Common;

namespace PowerDocu.FlowDocumenter
{
    public static class FlowDocumentationGenerator
    {
        public static List<FlowEntity> GenerateDocumentation(string filePath, bool fullDocumentation, ConfigHelper config, string outputPath = null)
        {
            if (File.Exists(filePath))
            {
                string path = outputPath == null ? Path.GetDirectoryName(filePath) : $"{outputPath}/{Path.GetFileNameWithoutExtension(filePath)}";
                DateTime startDocGeneration = DateTime.Now;
                FlowParser flowParserFromZip = new FlowParser(filePath);
                if (outputPath == null && flowParserFromZip.packageType == FlowParser.PackageType.SolutionPackage)
                {
                    path += @"\Solution " + CharsetHelper.GetSafeName(Path.GetFileNameWithoutExtension(filePath));
                }
                List<FlowEntity> flows = flowParserFromZip.getFlows();
                if (config.documentFlows)
                {
                    foreach (FlowEntity flow in flows)
                    {
                        // Only process cloud flows
                        if (flow.flowType == FlowEntity.FlowType.CloudFlow || flow.flowType == FlowEntity.FlowType.Unknown)
                        {
                            GraphBuilder gbzip = new GraphBuilder(flow, path);
                            gbzip.buildTopLevelGraph();
                            gbzip.buildDetailedGraph();
                            if (fullDocumentation)
                            {
                                FlowActionSortOrder sortOrder = config.flowActionSortOrder switch
                                {
                                    "By order of appearance" => FlowActionSortOrder.SortByOrder,
                                    "By name" => FlowActionSortOrder.SortByName,
                                    _ => FlowActionSortOrder.SortByName
                                };
                                FlowDocumentationContent content = new FlowDocumentationContent(flow, path, sortOrder);
                                if (config.outputFormat.Equals(OutputFormatHelper.Word) || config.outputFormat.Equals(OutputFormatHelper.All))
                                {
                                    NotificationHelper.SendNotification("Creating Word documentation");
                                    if (String.IsNullOrEmpty(config.wordTemplate) || !File.Exists(config.wordTemplate))
                                    {
                                        FlowWordDocBuilder wordzip = new FlowWordDocBuilder(content, null, config.addTableOfContents);
                                    }
                                    else
                                    {
                                        FlowWordDocBuilder wordzip = new FlowWordDocBuilder(content, config.wordTemplate, config.addTableOfContents);
                                    }
                                }
                                if (config.outputFormat.Equals(OutputFormatHelper.Markdown) || config.outputFormat.Equals(OutputFormatHelper.All))
                                {
                                    NotificationHelper.SendNotification("Creating Markdown documentation");
                                    FlowMarkdownBuilder markdownFile = new FlowMarkdownBuilder(content);
                                }
                                if (config.outputFormat.Equals(OutputFormatHelper.Html) || config.outputFormat.Equals(OutputFormatHelper.All))
                                {
                                    NotificationHelper.SendNotification("Creating HTML documentation");
                                    FlowHtmlBuilder htmlFile = new FlowHtmlBuilder(content);
                                }
                            }
                        }
                    }
                }
                DateTime endDocGeneration = DateTime.Now;
                NotificationHelper.SendNotification("FlowDocumenter: Created documentation for " + filePath + ". A total of " + flowParserFromZip.getFlows().Count + " files were processed in " + (endDocGeneration - startDocGeneration).TotalSeconds + " seconds.");
                return flows;
            }
            else
            {
                NotificationHelper.SendNotification("File not found: " + filePath);
            }
            return null;
        }
    }
}