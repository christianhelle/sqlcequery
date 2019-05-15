#tool nuget:?package=GitVersion.CommandLine&version=4.0.0
#addin "Cake.FileHelpers&version=3.1.0"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var debugConfiguration = "Debug";
var releaseConfiguration = "Release";
var solutionName   = "./SQL Compact Query Analyzer.sln";
var configurationName = releaseConfiguration;

var target = Argument("target", "Default");
var configuration = Argument("configuration", configurationName);

var commit = GitVersion().Sha.ToString().Substring(0, 7);
var artifactFolder = "./Artifacts/" + DateTime.UtcNow.ToString("yyyy-MM-dd_") + commit + "/";
Information("Output folder is: " + artifactFolder);

var desktopClientApp = "Editor";
var desktopClientSetup = desktopClientApp + ".Setup";
var desktopClient  = "SQLCEQueryAnalyzer";

var innoSetup = "../Dependencies/InnoSetup/ISCC.exe";


//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDir = Directory(artifactFolder);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
    CleanDirectories("./Binaries/**");
    CleanDirectories("./**/obj/**");
    CleanDirectories("./**/bin/**");
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() => 
{
    NuGetRestore(solutionName);
    DotNetCoreRestore(solutionName);
});

Task("Build-Debug")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() => 
{
    if (IsRunningOnWindows()) 
    {
        MSBuild(solutionName, settings => 
            settings.SetConfiguration(debugConfiguration)
                    .WithProperty("DeployOnBuild", "true")
                    .WithTarget("Build")
                    .SetMaxCpuCount(0));
    }
    else 
    {
        XBuild(solutionName, settings => settings.SetConfiguration(debugConfiguration));
    }
});

Task("Build-Release")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() => 
{
    if (IsRunningOnWindows()) 
    {
        MSBuild(solutionName, settings => 
            settings.SetConfiguration(releaseConfiguration)
                    .WithProperty("DeployOnBuild", "true")
                    .WithTarget("Build")
                    .SetMaxCpuCount(0));
    }
    else 
    {
        XBuild(solutionName, settings => settings.SetConfiguration(releaseConfiguration));
    }
});

Task("CleanUp-Release")
    .IsDependentOn("Build-Release")
    .Does(() => 
{
    var folder = "./Binaries/Release";
    DeleteFiles(folder + "/**/*.pdb");
    DeleteFiles(folder + "/**/*.xml");
    DeleteFiles(folder + "/**/*.resources.dll");
    DeleteDirectory(folder + "/amd64", new DeleteDirectorySettings { Recursive = true, Force = true });
    DeleteDirectory(folder + "/x86", new DeleteDirectorySettings { Recursive = true, Force = true });    
});

Task("Compress-Artifacts")
    .IsDependentOn("CleanUp-Release")
    .Does(() =>
{   
    var folder = "./Binaries/Release";
    Zip(folder, artifactFolder + desktopClient + "-Binaries.zip");
    DeleteDirectory(folder, new DeleteDirectorySettings { Recursive = true, Force = true });    
});

Task("Setup-Client-Package")
    .IsDependentOn("Build-Release")
    .Does(() => 
{
    var setupFile = "./Setup.iss";
    var outputFile = artifactFolder + "SQLCEQueryAnalyzer-Setup.exe";
    ReplaceTextInFiles(setupFile, "1.0.0", GitVersion().MajorMinorPatch);
    var exitCodeWithArgument = StartProcess(innoSetup, setupFile);
    Information("Exit code: {0}", exitCodeWithArgument);
    MoveFile("./Artifacts/SQLCEQueryAnalyzer-Setup.exe", outputFile);
    ReplaceTextInFiles(setupFile, GitVersion().MajorMinorPatch, "1.0.0");
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Setup-Client-Package")
    .IsDependentOn("Compress-Artifacts");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
