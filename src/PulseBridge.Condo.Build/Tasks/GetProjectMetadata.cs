namespace PulseBridge.Condo.Build.Tasks
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Represents a Microsoft Build task used to set additional project metadata for .NET CoreCLR projects using the project.json
    /// format.
    /// </summary>
    public class GetProjectMetadata : Task
    {
        #region Properties
        /// <summary>
        /// Gets or sets the list of projects for which to set additional metadata.
        /// </summary>
        [Required]
        [Output]
        public ITaskItem[] Projects { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Executes the <see cref="GetProjectMetadata"/> task.
        /// </summary>
        /// <returns>
        /// A value indicating whether or not the task was successfully executed.
        /// </returns>
        public override bool Execute()
        {
            // iterate over each project
            foreach (var project in this.Projects)
            {
                // set the metadata on the project
                SetMetadata(project);

                // log a message
                Log.LogMessage(MessageImportance.Low, $"Updated project metadata for project: {project.GetMetadata("ProjectName")}");
            }

            // assume its always true
            return true;
        }

        /// <summary>
        /// Sets additional project metadata on the specified <paramref name="project"/>.
        /// </summary>
        /// <param name="project">
        /// The project item on which to set additional project metadata.
        /// </param>
        private static void SetMetadata(ITaskItem project)
        {
            // get the full path of the project file
            var path = project.GetMetadata("FullPath");

            // parse the file
            var json = JObject.Parse(File.ReadAllText(path));

            // get the frameworks node
            var frameworks = json["frameworks"] as JObject;

            // determine if the frameworks node did not exist
            if (frameworks == null)
            {
                // move on immediately
                return;
            }

            // get the name properties ordered by name
            var names = frameworks.Properties().Select(p => p.Name)
                .OrderByDescending(name => name);

            // get the highest netcore tfm
            var tfm = names.FirstOrDefault(name => name.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase));

            // set the target frameworks property
            project.SetMetadata("TargetFrameworks", string.Join(";", names));
            project.SetMetadata("NetCoreFramework", tfm);

            // get the directory name from the path
            var directory = Path.GetDirectoryName(path);
            var group = Path.GetFileName(Path.GetDirectoryName(directory));

            // set the project directory path
            project.SetMetadata("ProjectDir", directory + Path.DirectorySeparatorChar);

            // set the project group
            project.SetMetadata("ProjectGroup", group);

            // set the name of the project (using the directory name by convention)
            // todo: parse the name from the project.json file
            project.SetMetadata("ProjectName", Path.GetFileName(directory));

            // set the shared sources directory
            // todo: parse this from the project.json file
            project.SetMetadata("SharedSourcesDir", Path.Combine(directory, "shared") + Path.DirectorySeparatorChar);

            // set the condo assembly info path
            project.SetMetadata("CondoAssemblyInfo", Path.Combine(directory, "Properties", "Condo.AssemblyInfo.cs"));
        }
        #endregion
    }
}