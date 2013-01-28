// ****************************************************************************
// Copyright 2013-2013 VMware, Inc. All rights reserved. -- VMware Confidential
// ****************************************************************************

namespace AutoTSC
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;

	internal class Program
	{
		private static FileSystemWatcher _watcher;
		// default path
		private static string _pathToTypescript = @"C:\repos\vlaunchpro\Dev\src\vLaunchPro.Web\Bundles\TypeScript";

		private static string ClientFilePath()
		{
			return _pathToTypescript + @"\VMware.Go.Client.generated.js";
		}

		private static string UnitTestFilePath()
		{
			return _pathToTypescript + @"\_UnitTests\VMware.Go.ClientWithUnitTests.generated.js";
		}

		private static void Main(string[] args)
		{
			if (args.Length > 0)
			{
				_pathToTypescript = args[0];
			}

			_watcher = new FileSystemWatcher { Path = _pathToTypescript, IncludeSubdirectories = true, Filter = "*.ts" };

			// Add event handlers.
			_watcher.Changed += OnChanged;
			_watcher.Created += OnChanged;
			_watcher.Deleted += OnChanged;
			_watcher.Renamed += OnRenamed;

			// Begin watching.
			_watcher.EnableRaisingEvents = true;

			// Wait for the user to quit the program.
			Console.WriteLine("Watching for Typescript changes at \"{0}\"... press \'q\' to quit.", _pathToTypescript);
			while (Console.Read() != 'q')
			{
				;
			}
		}

		private static void RunClientTscCmd()
		{
			IEnumerable<string> files = Directory.EnumerateFiles(_pathToTypescript, "*", SearchOption.AllDirectories)
			                                     .Where(s => !s.Contains("_UnitTests") && !s.Contains("_G11n") && s.EndsWith(".ts"));
			string clientTscCmd = files.Aggregate("--sourcemap --out \"" + ClientFilePath() + "\" ",
			                                      (current, file) => current + (file + " "));
			//write command to file b/c it may be too long for the cmd line
			File.WriteAllText("ClientTscCmdLine.generated.txt", clientTscCmd);
			ProcessStartInfo cmd = new ProcessStartInfo("CMD.exe", "/C tsc @ClientTscCmdLine.generated.txt");
			cmd.RedirectStandardInput = true;
			cmd.UseShellExecute = false;
			cmd.CreateNoWindow = false;
			cmd.WindowStyle = ProcessWindowStyle.Normal;
			Process.Start(cmd);
		}

		private static void RunUnitTestTscCmd()
		{
			IEnumerable<string> files = Directory.EnumerateFiles(_pathToTypescript, "*", SearchOption.AllDirectories)
			                                     .Where(s => !s.Contains("_G11n") && s.EndsWith(".ts"));
			string unitTestTscCmd = files.Aggregate("--sourcemap --out \"" + UnitTestFilePath() + "\" ",
			                                        (current, file) => current + (file + " "));
			//write command to file b/c it may be too long for the cmd line
			File.WriteAllText("UnitTestTscCmdLine.generated.txt", unitTestTscCmd);
			ProcessStartInfo cmd = new ProcessStartInfo("CMD.exe", "/C tsc @UnitTestTscCmdLine.generated.txt");
			cmd.RedirectStandardInput = true;
			cmd.UseShellExecute = false;
			cmd.CreateNoWindow = false;
			cmd.WindowStyle = ProcessWindowStyle.Normal;
			Process.Start(cmd);
		}

		private static void ProcessChange(string fileFullPath)
		{
			// if unit test change, only compile unit test file. otherwise compile client file
			if (fileFullPath.Contains("_UnitTests"))
			{
				RunUnitTestTscCmd();
			}
			else
			{
				RunClientTscCmd();
			}
		}

		private static void ProcessChangeOnlyOnce(FileSystemEventArgs e, Action<FileSystemEventArgs> processor)
		{
			try
			{
				_watcher.EnableRaisingEvents = false;
				processor(e);
			}
			finally
			{
				_watcher.EnableRaisingEvents = true;
			}
		}

		private static string RelativePath(string fullPath)
		{
			return fullPath.Replace(_pathToTypescript, "");
		}

		// Define the event handlers. 
		private static void OnChanged(object source, FileSystemEventArgs ev)
		{
			ProcessChangeOnlyOnce(ev, e =>
			{
				ConsoleWriteLineSeparator();
				Console.WriteLine("{0}: {1}", e.ChangeType.ToString().ToUpper(), RelativePath(e.FullPath));
				ProcessChange(e.FullPath);
			});
		}

		private static void OnRenamed(object source, RenamedEventArgs ev)
		{
			ProcessChangeOnlyOnce(ev, e =>
			{
				ConsoleWriteLineSeparator();
				Console.WriteLine("RENAMED to: \"{0}\"", RelativePath(e.FullPath));
				ProcessChange(e.FullPath);
			});
		}

		private static void ConsoleWriteLineSeparator()
		{
			Console.WriteLine("===================================================================");
		}
	}
}