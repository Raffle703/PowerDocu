using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PowerDocu.Common;

namespace PowerDocu.FlowDocumenter
{
    public static class FlowDocumentationGenerator
    {
        private static readonly ConcurrentDictionary<string, object> _flowOutputLocks = new();

        /// <summary>
        /// Parses flows from the given file without generating documentation output.
        /// Returns the parsed flows and the resolved output path.
        /// </summary>
        public static (List<FlowEntity> Flows, string Path) ParseFlows(string filePath, string outputPath = null)
        {
            if (!File.Exists(filePath))
            {
                NotificationHelper.SendNotification("File not found: " + filePath);
                return (null, null);
            }

            string path = outputPath == null ? Path.GetDirectoryName(filePath) : $"{outputPath}/{Path.GetFileNameWithoutExtension(filePath)}";
            FlowParser flowParserFromZip = new FlowParser(filePath);
            if (outputPath == null && flowParserFromZip.packageType == FlowParser.PackageType.SolutionPackage)
            {
                path += @"\Solution " + CharsetHelper.GetSafeName(Path.GetFileNameWithoutExtension(filePath));
            }
            List<FlowEntity> flows = flowParserFromZip.getFlows();
            NotificationHelper.SendNotification($"FlowParser: Parsed {flows.Count} flow(s) from {filePath}.");
            return (flows, path);
        }

        /// <summary>
        /// Generates documentation output for pre-parsed flows using the DocumentationContext.
        /// </summary>
        public static void GenerateOutput(DocumentationContext context, string path)
        {
            if (context.Flows == null || !context.Config.documentFlows) return;

            // Pre-warm ConnectorHelper static state to avoid a race condition during parallel init
            ConnectorHelper.getConnectorIcon("");

            DateTime startDocGeneration = DateTime.Now;
            Parallel.ForEach(context.Flows,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                flow =>
                {
                    string flowOutputFolder = path + CharsetHelper.GetSafeName(@"\FlowDoc " + flow.Name + @"\");
                    string flowLockKey = Path.GetFullPath(flowOutputFolder).ToLowerInvariant();
                    object flowOutputLock = _flowOutputLocks.GetOrAdd(flowLockKey, _ => new object());

                    lock (flowOutputLock)
                    {
                        if (flow.flowType == FlowEntity.FlowType.CloudFlow || flow.flowType == FlowEntity.FlowType.Unknown)
                        {
                            GraphBuilder gbzip = new GraphBuilder(flow, path);
                            gbzip.buildTopLevelGraph();
                            gbzip.buildDetailedGraph();
                            if (context.FullDocumentation)
                            {
                                FlowActionSortOrder sortOrder = context.Config.flowActionSortOrder switch
                                {
                                    "By order of appearance" => FlowActionSortOrder.SortByOrder,
                                    "By name" => FlowActionSortOrder.SortByName,
                                    _ => FlowActionSortOrder.SortByName
                                };
                                FlowDocumentationContent content = new FlowDocumentationContent(flow, path, sortOrder, context);
                                string wordTemplate = (!String.IsNullOrEmpty(context.Config.wordTemplate) && File.Exists(context.Config.wordTemplate))
                                    ? context.Config.wordTemplate : null;
                                if (context.Config.outputFormat.Equals(OutputFormatHelper.Word) || context.Config.outputFormat.Equals(OutputFormatHelper.All))
                                {
                                    NotificationHelper.SendNotification("Creating Word documentation");
                                    FlowWordDocBuilder wordzip = new FlowWordDocBuilder(content, wordTemplate, context.Config.addTableOfContents);
                                }
                                if (context.Config.outputFormat.Equals(OutputFormatHelper.Markdown) || context.Config.outputFormat.Equals(OutputFormatHelper.All))
                                {
                                    NotificationHelper.SendNotification("Creating Markdown documentation");
                                    FlowMarkdownBuilder markdownFile = new FlowMarkdownBuilder(content);
                                }
                                if (context.Config.outputFormat.Equals(OutputFormatHelper.Html) || context.Config.outputFormat.Equals(OutputFormatHelper.All))
                                {
                                    NotificationHelper.SendNotification("Creating HTML documentation");
                                    FlowHtmlBuilder htmlFile = new FlowHtmlBuilder(content);
                                }
                            }
                        }
                        context.Progress?.Increment("Flows");
                    }
                });
            DateTime endDocGeneration = DateTime.Now;
            NotificationHelper.SendNotification($"FlowDocumenter: Generated documentation for {context.Flows.Count} flow(s) in {(endDocGeneration - startDocGeneration).TotalSeconds} seconds.");
        }

        /// <summary>
        /// Legacy method: parses and generates documentation in one step (used for standalone files).
        /// </summary>
        public static List<FlowEntity> GenerateDocumentation(string filePath, bool fullDocumentation, ConfigHelper config, string outputPath = null)
        {
            var (flows, path) = ParseFlows(filePath, outputPath);
            if (flows == null) return null;

            var context = new DocumentationContext
            {
                Flows = flows,
                Config = config,
                FullDocumentation = fullDocumentation
            };
            GenerateOutput(context, path);
            return flows;
        }
    }
}