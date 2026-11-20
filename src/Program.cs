using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("XButtonMapper")]
[assembly: AssemblyDescription("Map mouse back/forward buttons to Copy and Paste on Windows 10.")]
[assembly: AssemblyCompany("XButtonMapper")]
[assembly: AssemblyProduct("XButtonMapper")]
[assembly: AssemblyCopyright("Copyright (c) 2026")]
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyFileVersion("2.0.0.0")]

namespace XButtonMapper
{
    internal static class Program
    {
        private const string MutexName = @"Local\XButtonMapper_2DA8B262_575B_4F05_BDAA_565664E47349";
        private const string ActivationEventName = @"Local\XButtonMapper_Activate_2DA8B262_575B_4F05_BDAA_565664E47349";
        private const string SmokeTestMutexName = @"Local\XButtonMapper_SmokeTest_2DA8B262_575B_4F05_BDAA_565664E47349";
        private const string SmokeTestActivationEventName = @"Local\XButtonMapper_SmokeTest_Activate_2DA8B262_575B_4F05_BDAA_565664E47349";

        [STAThread]
        private static int Main(string[] args)
        {
            if (HasArgument(args, "--self-test"))
            {
                return SelfTest.Run();
            }

            bool smokeTest = HasArgument(args, "--smoke-test");
            string mutexName = smokeTest ? SmokeTestMutexName : MutexName;
            string activationEventName = smokeTest ? SmokeTestActivationEventName : ActivationEventName;

            Mutex instanceMutex = null;
            EventWaitHandle activationEvent = null;
            bool ownsMutex = false;

            try
            {
                bool createdNew;
                instanceMutex = new Mutex(true, mutexName, out createdNew);
                ownsMutex = createdNew;

                if (!createdNew)
                {
                    SignalExistingInstance(activationEventName);
                    return 0;
                }

                activationEvent = new EventWaitHandle(
                    false,
                    EventResetMode.AutoReset,
                    activationEventName);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApplicationContext(activationEvent, smokeTest));
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "XButtonMapper 无法启动。\r\n\r\n" + ex.Message,
                    "XButtonMapper",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return 1;
            }
            finally
            {
                if (activationEvent != null)
                {
                    activationEvent.Dispose();
                }

                if (instanceMutex != null)
                {
                    if (ownsMutex)
                    {
                        try
                        {
                            instanceMutex.ReleaseMutex();
                        }
                        catch (ApplicationException)
                        {
                            // The process is already shutting down; there is nothing else to release.
                        }
                    }

                    instanceMutex.Dispose();
                }
            }
        }

        private static bool HasArgument(string[] args, string expected)
        {
            if (args == null)
            {
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void SignalExistingInstance(string activationEventName)
        {
            try
            {
                using (EventWaitHandle activationEvent = EventWaitHandle.OpenExisting(activationEventName))
                {
                    activationEvent.Set();
                }
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                MessageBox.Show(
                    "XButtonMapper 已经在运行，请在系统托盘中查看。",
                    "XButtonMapper",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "XButtonMapper 已经由其他权限级别运行。",
                    "XButtonMapper",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
    }
}
