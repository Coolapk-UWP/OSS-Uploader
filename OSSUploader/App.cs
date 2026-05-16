using CoolapkUWP.OSSUploader.Common;
using CoolapkUWP.OSSUploader.Helpers;
using CoolapkUWP.OSSUploader.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation.Collections;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace CoolapkUWP.OSSUploader
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    internal sealed partial class App : IFrameworkViewSource, IFrameworkView
    {
        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            CoreApplication.Suspending += OnSuspending;
            CoreApplication.BackgroundActivated += OnBackgroundActivated;
        }

        public IFrameworkView CreateView() => this;

        public void Initialize(CoreApplicationView applicationView) => applicationView.Activated += OnApplicationViewActivated;

        public void SetWindow(CoreWindow window) => _window = window;

        public void Run()
        {
            _window.Activate();
            CoreWindow.GetForCurrentThread().Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessUntilQuit);
        }

        void IFrameworkView.Load(string entryPoint) { }
        void IFrameworkView.Uninitialize() { }

        private async void OnApplicationViewActivated(CoreApplicationView sender, IActivatedEventArgs e)
        {
            if (!_isLoad)
            {
                RegisterExceptionHandlingSynchronizationContext();
                _isLoad = true;
            }

            ResourceLoader _loader = ResourceLoader.GetForViewIndependentUse();
            StringBuilder builder = new StringBuilder()
                .AppendLine(_loader.GetString("MessageDialogContent"))
                .AppendLine(string.Format(_loader.GetString("FrameworkFormat"), RuntimeInformation.FrameworkDescription))
                .AppendLine(string.Format(_loader.GetString("DeviceFamilyFormat"), AnalyticsInfo.VersionInfo.DeviceFamily.Replace('.', ' ')))
                .AppendLine(string.Format(_loader.GetString("OSPlatformFormat"), Environment.OSVersion.ToString()))
                .Append(string.Format(_loader.GetString("OSArchitectureFormat"), RuntimeInformation.OSArchitecture));
            MessageDialog dialog = new MessageDialog(builder.ToString(), _loader.GetString("MessageDialogTitle"));
            _ = await dialog.ShowAsync();

            CoreApplication.Exit();
        }

        /// <summary>
        /// 在将要挂起应用程序执行时调用。  在不知道应用程序
        /// 无需知道应用程序会被终止还是会恢复，
        /// 并让内存内容保持不变。
        /// </summary>
        /// <param name="sender">挂起的请求的源。</param>
        /// <param name="e">有关挂起请求的详细信息。</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();
            //TODO: 保存应用程序状态并停止任何后台活动
            deferral.Complete();
        }

        /// <summary>
        /// Should be called from OnActivated and OnLaunched.
        /// </summary>
        private void RegisterExceptionHandlingSynchronizationContext()
        {
            ExceptionHandlingSynchronizationContext
                .Register()
                .UnhandledException += SynchronizationContext_UnhandledException;
        }

        private void SynchronizationContext_UnhandledException(object sender, Common.UnhandledExceptionEventArgs e) => e.Handled = true;

        /// <summary>
        /// Called whenever the app service is activated.
        /// </summary>
        /// <param name="args"></param>
        private void OnBackgroundActivated(object sender, BackgroundActivatedEventArgs args)
        {
            if (_appServiceInitialized == false) // Only need to setup the handlers once
            {
                _appServiceInitialized = true;

                IBackgroundTaskInstance taskInstance = args.TaskInstance;
                taskInstance.Canceled += OnAppServicesCanceled;

                AppServiceTriggerDetails appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
                _appServiceDeferral = taskInstance.GetDeferral();
                _appServiceConnection = appService.AppServiceConnection;
                _appServiceConnection.RequestReceived += OnAppServiceRequestReceived;
                _appServiceConnection.ServiceClosed += AppServiceConnection_ServiceClosed;
            }
        }

        /// <summary>
        /// The handler for app service calls.
        /// This extension provides the exponent function. Extensions can provide more
        /// than one function. You could send a "command" argument in args.Request.Message
        /// to identify the function to carry out.
        /// </summary>
        /// <param name="sender">Contains details about the app connection.</param>
        /// <param name="args">Contains arguments for the app service and the deferral object.</param>
        private async void OnAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // Get a deferral because we use an await-able API below (SendResponseAsync()) to respond to the message
            // and we don't want this call to get cancelled while we are waiting.
            AppServiceDeferral messageDeferral = args.GetDeferral();
            ValueSet message = args.Request.Message;
            ValueSet returnMessage = new ValueSet();

            try
            {
                if (message.TryGetValue("UID", out object uid) && message.TryGetValue("UserName", out object username) && message.TryGetValue("Token", out object token))
                {
                    NetworkHelper.SetLoginCookie(uid.ToString(), username.ToString(), token.ToString());
                }

                if (message.TryGetValue("DeviceCode", out object deviceCode) && message.TryGetValue("AppToken", out object appToken))
                {
                    NetworkHelper.SetRequestHeaders(deviceCode.ToString(), appToken.ToString());
                }

                if (message.TryGetValue("UserAgent", out object userAgent))
                {
                    NetworkHelper.SetRequestHeaders(userAgent.ToString());
                }

                if (message.TryGetValue("Images", out object images))
                {
                    string uploadBucket = message.TryGetValue("UploadBucket", out object UploadBucket) ? UploadBucket.ToString() : "image";
                    string uploadDir = message.TryGetValue("UploadDir", out object UploadDir) ? UploadDir.ToString() : "feed";
                    string toUid = message.TryGetValue("ToUid", out object ToUid) ? ToUid.ToString() : string.Empty;
                    IEnumerable<UploadFileFragment> fragments = JsonConvert.DeserializeObject<IEnumerable<UploadFileFragment>>(images.ToString(), new JsonSerializerSettings { ContractResolver = new IgnoreIgnoredContractResolver() });
                    returnMessage["Result"] = await RequestHelper.UploadImages(fragments, uploadBucket, uploadDir, toUid).ContinueWith(x => x.Result.ToArray());
                }
            }
            catch (Exception ex)
            {
                returnMessage["Error"] = ex.Message;
            }

            await args.Request.SendResponseAsync(returnMessage);
            messageDeferral.Complete();
        }

        /// <summary>
        /// Called if the system is going to cancel the app service because resources it needs to reclaim resources.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="reason"></param>
        private void OnAppServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason) => _appServiceDeferral.Complete();

        /// <summary>
        /// Called when the caller closes the connection to the app service.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args) => _appServiceDeferral.Complete();

        private class IgnoreIgnoredContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                IList<JsonProperty> list = base.CreateProperties(type, memberSerialization);
                if (list != null)
                {
                    foreach (JsonProperty item in list)
                    {
                        if (item.Ignored)
                        {
                            item.Ignored = false;
                        }
                    }
                }
                return list;
            }
        }

        private CoreWindow _window;
        private bool _isLoad = false;
        private bool _appServiceInitialized = false;
        private AppServiceConnection _appServiceConnection;
        private BackgroundTaskDeferral _appServiceDeferral;
    }
}
