using System.Collections.Generic;
using System.IO;
using System.Linq;
using PowerDocu.Common;

namespace PowerDocu.AppModuleDocumenter
{
    public class AppModuleDocumentationContent
    {
        public string folderPath, filename;
        public AppModuleEntity appModule;
        public DocumentationContext context;

        // Section headers
        public string headerOverview = "Overview";
        public string headerSecurityRoles = "Security Roles";
        public string headerNavigation = "Navigation (Site Map)";
        public string headerTables = "Tables";
        public string headerViews = "Views";
        public string headerCustomPages = "Custom Pages";
        public string headerAppSettings = "App Settings";
        public string headerDocumentationGenerated = "Documentation generated at";

        // Cross-reference data from the context
        public List<RoleEntity> allRoles;
        public List<TableEntity> allTables;
        public List<AppEntity> allApps;
        private CustomizationsEntity customizations;

        public AppModuleDocumentationContent(AppModuleEntity appModule, string path, DocumentationContext context)
        {
            NotificationHelper.SendNotification("Preparing documentation content for Model-Driven App: " + appModule.GetDisplayName());
            this.appModule = appModule;
            this.context = context;
            folderPath = path + CharsetHelper.GetSafeName(@"\MDADoc " + appModule.GetDisplayName() + @"\");
            Directory.CreateDirectory(folderPath);
            filename = CharsetHelper.GetSafeName(appModule.GetDisplayName());
            allRoles = context.Roles ?? new List<RoleEntity>();
            allTables = context.Tables ?? new List<TableEntity>();
            allApps = context.Apps ?? new List<AppEntity>();
            this.customizations = context.Customizations;
        }

        /// <summary>
        /// Legacy constructor for backward compatibility.
        /// </summary>
        public AppModuleDocumentationContent(AppModuleEntity appModule, string path, List<RoleEntity> roles = null, List<TableEntity> tables = null, CustomizationsEntity customizations = null, List<AppEntity> apps = null)
        {
            NotificationHelper.SendNotification("Preparing documentation content for Model-Driven App: " + appModule.GetDisplayName());
            this.appModule = appModule;
            folderPath = path + CharsetHelper.GetSafeName(@"\MDADoc " + appModule.GetDisplayName() + @"\");
            Directory.CreateDirectory(folderPath);
            filename = CharsetHelper.GetSafeName(appModule.GetDisplayName());
            allRoles = roles ?? new List<RoleEntity>();
            allTables = tables ?? new List<TableEntity>();
            allApps = apps ?? new List<AppEntity>();
            this.customizations = customizations;
        }

        /// <summary>
        /// Resolves a security role GUID to its display name using the roles parsed from the solution.
        /// </summary>
        public string GetRoleNameById(string roleId)
        {
            if (string.IsNullOrEmpty(roleId)) return roleId;
            RoleEntity role = allRoles.FirstOrDefault(r => r.ID != null && r.ID.Trim('{', '}').Equals(roleId.Trim('{', '}'), System.StringComparison.OrdinalIgnoreCase));
            if (role?.Name != null) return role.Name;
            // If not found in the solution roles, try to resolve against known OOTB role templates
            return SecurityRoles.GetDisplayName(roleId) ?? roleId;
        }

        /// <summary>
        /// Resolves a table schema name to its display name using the tables parsed from the solution.
        /// </summary>
        public string GetTableDisplayName(string schemaName)
        {
            if (string.IsNullOrEmpty(schemaName)) return schemaName;
            TableEntity table = allTables.FirstOrDefault(t => t.getName().Equals(schemaName, System.StringComparison.OrdinalIgnoreCase));
            return table?.getLocalizedName() ?? schemaName;
        }

        /// <summary>
        /// Resolves a view (saved query) GUID to its display name, parent table, and query type
        /// by searching across all tables in the solution.
        /// </summary>
        public (string ViewName, string TableName, string QueryType) GetViewDetails(string viewId)
        {
            if (string.IsNullOrEmpty(viewId)) return (viewId, "", "");
            string normalizedId = viewId.Trim('{', '}');
            foreach (var table in allTables)
            {
                foreach (var view in table.GetViews())
                {
                    if (view.GetViewId().Trim('{', '}').Equals(normalizedId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        string viewName = view.GetViewName();
                        string tableName = table.getLocalizedName() ?? table.getName();
                        string queryType = view.GetQueryTypeDisplayName();
                        return (string.IsNullOrEmpty(viewName) ? viewId : viewName, tableName, queryType);
                    }
                }
            }
            return (viewId, "", "");
        }

        /// <summary>
        /// Resolves a custom page's display name. Tries the parsed canvas app's Name (from Properties.json),
        /// falls back to the customizations XML display name, then to the raw CanvasAppName.
        /// </summary>
        public string GetCustomPageDisplayName(AppModuleAppElement page)
        {
            if (customizations != null && !string.IsNullOrEmpty(page.CanvasAppName))
            {
                string resolved = customizations.getAppNameBySchemaName(page.CanvasAppName);
                if (!string.IsNullOrEmpty(resolved))
                {
                    AppEntity app = allApps.FirstOrDefault(a => a.Name.Equals(resolved, System.StringComparison.OrdinalIgnoreCase));
                    return app != null ? app.Name : resolved;
                }
            }
            return !string.IsNullOrEmpty(page.CanvasAppName) ? page.CanvasAppName : page.UniqueName;
        }

        /// <summary>
        /// Finds the parsed AppEntity that corresponds to a custom page's canvas app,
        /// by matching the resolved display name against AppEntity.Name.
        /// </summary>
        public AppEntity GetCanvasAppForPage(AppModuleAppElement page)
        {
            string displayName = GetCustomPageDisplayName(page);
            return allApps.FirstOrDefault(a => a.Name.Equals(displayName, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns the relative path from this MDA doc folder to the canvas app's doc folder.
        /// </summary>
        public string GetCanvasAppDocRelativePath(AppEntity app, string indexFile)
        {
            string appFolder = "AppDoc " + CharsetHelper.GetSafeName(app.Name);
            return "../" + appFolder + "/" + indexFile;
        }
    }
}
