#addin nuget:?package=Cake.FileHelpers&version=6.1.3

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var solutionName   = "./QueryAnalyzer.sln";
var target = Argument("target", "Default");
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
    CleanDirectories("./Artifacts/**");
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
    MSBuild(solutionName, settings => 
        settings.SetConfiguration("Release")
                .SetPlatformTarget("x64")
                .WithProperty("DeployOnBuild", "true")
                .WithTarget("Build")
                .SetMaxCpuCount(0));
    
    MSBuild(solutionName, settings => 
        settings.SetConfiguration("Release")
                .SetPlatformTarget("ARM64")
                .WithProperty("DeployOnBuild", "true")
                .WithTarget("Build")
                .SetMaxCpuCount(0));

    MSBuild(solutionName, settings => 
        settings.SetConfiguration("Release")
                .WithProperty("DeployOnBuild", "true")
                .WithTarget("Build")
                .SetMaxCpuCount(0));
                
    var folders = new[] { "./Binaries/ARM64", "./Binaries/x64", "./Binaries/x86" };
    foreach (var folder in folders)
    {
        CopyFiles("./Binaries/Release/SqlCeDatabase.*", folder);
        CopyDirectory("./Binaries/Release/SqlCe31", folder + "/SqlCe31");
        CopyDirectory("./Binaries/Release/SqlCe35", folder + "/SqlCe35");
        CopyDirectory("./Binaries/Release/SqlCe40", folder + "/SqlCe40");
    }
});

Task("CleanUp-Release")
    .IsDependentOn("Build-Release")
    .Does(() => 
{
    var folders = new[] { "./Binaries/ARM64", "./Binaries/x64", "./Binaries/x86" };
    foreach (var folder in folders)
    {
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
    }

    DeleteDirectory("./Binaries/Release", new DeleteDirectorySettings { Recursive = true, Force = true });
});

Task("Compress-Artifacts")
    .IsDependentOn("CleanUp-Release")
    .Does(() =>
{
     var platforms = new[] { "ARM64", "x64", "x86" };
    foreach (var platform in platforms)
    {   
        var outputFile = artifactFolder + desktopClient + "-Binaries-" + platform + ".zip";
        Zip("./Binaries/" + platform, outputFile);
    }
});

Task("Setup-Client-Package")
    .IsDependentOn("Build-Release")
    .Does(() => 
{    
     var platforms = new[] { "ARM64", "x64", "x86" };
    foreach (var platform in platforms)
    {
        var setupFile = "./Setup" + "-" + platform + ".iss";
        var outputFile = artifactFolder + "SQLCEQueryAnalyzer-Setup" + "-" + platform + ".exe";
        var exitCodeWithArgument = StartProcess(innoSetup, setupFile);
        Information("Exit code: {0}", exitCodeWithArgument);
        MoveFile("./Artifacts/SQLCEQueryAnalyzer-Setup" + "-" + platform + ".exe", outputFile);
    }
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
