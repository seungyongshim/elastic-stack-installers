﻿using System;
using System.IO;
using System.Threading.Tasks;
using ElastiBuild.Extensions;
using Elastic.Installer;
using SimpleExec;

namespace ElastiBuild.BullseyeTargets
{
    public class SignProductBinariesTarget : SignToolTargetBase<SignProductBinariesTarget>
    {
        public static async Task RunAsync(BuildContext ctx)
        {
            var ap = ctx.GetArtifactPackage();

            var SignToolExePath = Path.Combine(
                ctx.ToolsDir,
                MagicStrings.Dirs.Cert,
                MagicStrings.Files.SignToolExe);

            foreach (var binary in ctx.Config.GetProductConfig(ap.TargetName).PublishedBinaries)
            {
                bool signed = false;
                int tryCount = ctx.Config.TimestampUrls.Count;

                for (int tryNr = 0; tryNr < tryCount; ++tryNr)
                {
                    var timestampUrl = ctx.Config.TimestampUrls[tryNr];
                    var (certPass, SignToolArgs) = MakeSignToolArgs(ctx, timestampUrl);

                    var FullSignToolArgs = SignToolArgs + Path
                        .Combine(ctx.InDir, Path.GetFileNameWithoutExtension(ap.FileName), binary)
                        .Quote();

                    await Console.Out.WriteAsync(SignToolExePath + " ");
                    await Console.Out.WriteLineAsync(FullSignToolArgs.Replace(certPass, "[redacted]"));

                    try
                    {
                        await Command.RunAsync(SignToolExePath, FullSignToolArgs, noEcho: true);
                        signed = true;
                        break;
                    }
                    catch (Exception /*ex*/)
                    {
                        await Console.Out.WriteLineAsync(
                            $"Error: timestap server {timestampUrl} is unavailable, " +
                            $"{tryCount - tryNr - 1} server(s) left to try.");
                    }
                }

                if (!signed)
                    throw new Exception("Error: None of the timestamp servers available.");
            }
        }
    }
}
