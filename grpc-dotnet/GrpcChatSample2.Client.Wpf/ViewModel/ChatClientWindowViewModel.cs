using Google.Protobuf.WellKnownTypes;
using GrpcChatSample.Common;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Data;

namespace GrpcChatSample2.Client.Wpf
{
    public class ChatClientWindowViewModel : BindableBase, IDisposable
    {
        private ChatServiceClient m_chatServiceClient;

        public ObservableCollection<string> ChatHistory { get; } = new ObservableCollection<string>();
        private readonly object m_chatHistoryLockObject = new object();
        public string Title
        {
            get { return m_title + " - " + m_fromName; }
            set { SetProperty(ref m_title, value); }
        }
        private string m_title = "Chat Client";

        public string FromName
        {
            get { return m_fromName; }
            set
            {
                SetProperty(ref m_fromName, value, () => 
                {
                    RaisePropertyChanged(nameof(Title));
                });
            }
        }
        private string m_fromName = "Richard";

        public string ToName
        {
            get { return m_toName; }
            set { SetProperty(ref m_toName, value); }
        }
        private string m_toName = "Sam";

        private bool disposedValue;

        public DelegateCommand ConnectCommand { get; }
        public DelegateCommand<string> WriteCommand { get; }

        public ChatClientWindowViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(ChatHistory, m_chatHistoryLockObject);

            WriteCommand = new DelegateCommand<string>(WriteCommandExecute);
            ConnectCommand = new DelegateCommand(ConnectCommandExecute);
           
        }

        private void ConnectCommandExecute()
        {
            m_chatServiceClient = new ChatServiceClient(FromName);

            StartReadingChatServer();
        }
        CancellationTokenSource _cts = new CancellationTokenSource();
        private void StartReadingChatServer()
        {
            var res = m_chatServiceClient.ChatLogs()
                .ForEachAsync((x) => ChatHistory.Add($"{x.At.ToDateTime().ToString("HH:mm:ss")} {x.FromName}: {x.Content}"), _cts.Token);

            App.Current.Exit += (_, __) => _cts.Cancel();
        }

        private async void WriteCommandExecute(string content)
        {
            try
            {
                var reply = await m_chatServiceClient.Write(new ChatLog
                {
                    FromName = m_fromName,
                    ToName = m_toName,
                    Content = content,
                    At = Timestamp.FromDateTime(DateTime.Now.ToUniversalTime()),
                });

                if (reply?.Success == true)
                {
                    ChatHistory.Add($"{DateTime.Now.ToString("HH:mm:ss")} {m_fromName}: {content}");
                }
                else if (reply?.Success == false)
                {
                    ChatHistory.Add($"{DateTime.Now.ToString("HH:mm:ss")} Server: {reply.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                ChatHistory.Add($"{DateTime.Now.ToString("HH:mm:ss")} System: {ex.Message}");
            }
         
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_chatServiceClient?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
