using System;
using System.Diagnostics;
using System.IO;

namespace WireManager.Common
{
    public class wgCmdProcess
	{
		public wgCmdProcess(string pathToCmd, bool noWindow) 
		{
			mainProcess = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					RedirectStandardInput = true,
					RedirectStandardError = true,
					RedirectStandardOutput = true,
					CreateNoWindow = noWindow,
					UseShellExecute = false,
					Arguments = "/k",
					FileName = pathToCmd,
					WorkingDirectory = pathToCmd.Substring(0, pathToCmd.IndexOf('\\'))
				},
				EnableRaisingEvents = true
			};
			mainProcess.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
			{
				Errors += "\n" + e.Data;
			});

			mainProcess.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
			{
				LastMessage = e.Data ?? "";
				MessageHistory += "\n" + LastMessage;
				MessageBuffer += "\n" + LastMessage;
			});

			mainProcess.Exited += new EventHandler((s, e) =>
			{
				IsEnded = true;
				input?.Dispose();
			});

			mainProcess.Start();

			input = mainProcess.StandardInput;
			input.NewLine = "\n";
			mainProcess.BeginErrorReadLine();
			mainProcess.BeginOutputReadLine();
		}

		public bool IsEnded { get; private set; } = false;
		public Process mainProcess { get; private set; }
		public StreamWriter input { get; private set; }

		public string Errors { get; private set; }
		public string MessageHistory { get; private set; } = "";
		public string MessageBuffer { get; set; } = "";
		public string LastMessage { get; set; } = "";
	}
}
