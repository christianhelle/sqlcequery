#addin nuget:?package=Cake.FileHelpers&version=6.1.3

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var solutionName   = "./QueryAnalyzer.sln";
var configurationName = "Release";

var target = Argument("target", "Default");
var configuration = Argument("configuration", configurationName);
var artifactFolder = "./Artifacts/";
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
    // DotNetRestore(solutionName);
});

Task("Build-Release")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() => 
{
    if (IsRunningOnWindows()) 
    {
        MSBuild(solutionName, settings => 
            settings.SetConfiguration(configurationName)
                    .WithProperty("DeployOnBuild", "true")
                    .WithTarget("Build")
                    .SetMaxCpuCount(0));
    }
    else 
    {
        XBuild(solutionName, settings => settings.SetConfiguration(configurationName));
    }
});

Task("CleanUp-Release")
    .IsDependentOn("Build-Release")
    .Does(() => 
{
    var folder = "./Binaries/Release";
    DeleteFiles(folder + "/**/*.pdb");
    DeleteFiles(folder + "/**/*.xml");
    DeleteDirectory(folder + "/amd64", new DeleteDirectorySettings { Recursive = true, Force = true });
    DeleteDirectory(folder + "/x86", new DeleteDirectorySettings { Recursive = true, Force = true });
    DeleteDirectory(folder + "/de", new DeleteDirectorySettings { Recursive = true, Force = true });
    DeleteDirectory(folder + "/es", new DeleteDirectorySettings { Recursive = true, Force = true });
    DeleteDirectory(folder + "/fr", new DeleteDirectorySettings { Recursive = true, Force = true });
    DeleteDirectory(folder + "/hu", new DeleteDirectorySettings { Recursive = true, Force = true });
    DeleteDirectory(folder + "/it", new DeleteDirectorySettings { Recursive = true, Force = true });
    DeleteDirectory(folder + "/pt-BR", new DeleteDirectorySettings { Recursive = true, Force = true });
    DeleteDirectory(folder + "/ro", new DeleteDirectorySettings { Recursive = true, Force = true });
    DeleteDirectory(folder + "/ru", new DeleteDirectorySettings { Recursive = true, Force = true });
    DeleteDirectory(folder + "/sv", new DeleteDirectorySettings { Recursive = true, Force = true });
    DeleteDirectory(folder + "/zh-Hans", new DeleteDirectorySettings { Recursive = true, Force = true });
    DeleteDirectory(folder + "/cs-CZ", new DeleteDirectorySettings { Recursive = true, Force = true });
    DeleteDirectory(folder + "/ja-JP", new DeleteDirectorySettings { Recursive = true, Force = true });
});

Task("Compress-Artifacts")
    .IsDependentOn("CleanUp-Release")
    .Does(() =>
{   
    Zip("./Binaries/Release", artifactFolder + desktopClient + "-Binaries.zip");  
});

Task("Setup-Client-Package")
    .IsDependentOn("Build-Release")
    .Does(() => 
{
    var setupFile = "./Setup.iss";
    var outputFile = artifactFolder + "SQLCEQueryAnalyzer-Setup.exe";
    var exitCodeWithArgument = StartProcess(innoSetup, setupFile);
    Information("Exit code: {0}", exitCodeWithArgument);
    MoveFile("./Artifacts/SQLCEQueryAnalyzer-Setup.exe", outputFile);
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
