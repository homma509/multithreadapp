using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.Remoting.Messaging;

namespace WindowsFormsApplication1
{
    public delegate void SetProgressValueDelegate(int percent);

    public partial class Form : System.Windows.Forms.Form
    {
        private DateTime _start;
        private DateTime _stop;

        private int _clickThreadId;
        private int _callbackThreadId;

        private CancellationTokenSource _cts;
        private Task task;

        public Form()
        {
            InitializeComponent();

            SetButtonEnabled();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            button1.Enabled = false;
            button2.Enabled = true;
            textBox1.Text = "実行中";
            Application.DoEvents();

            this._start = DateTime.Now;
            this._clickThreadId = Thread.CurrentThread.ManagedThreadId;

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            var taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            Task task = Task.Factory.StartNew(
                () => TplWorker.DoWork(new SetProgressValueDelegate(SetProgressValue), token)
            )
            .ContinueWith(
                t => Completed(t)
            );
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            textBox1.Text = "中断中…";

            //try
            //{
                _cts.Cancel();
                //task.Wait();

            //}
            //catch (Exception)
            //{
            //    textBox1.Text = "中断";
            //}

            //button1.Enabled = true;
        }

        private void Completed(Task<TplWorker> t)
        {
            this._stop = DateTime.Now;
            this._callbackThreadId = Thread.CurrentThread.ManagedThreadId;

            SetButtonEnabled();

            Exception ex = t.Exception;
            if (ex != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(string.Format("Exception - {0}: {1}", ex.GetType().Name, ex.Message));
                sb.AppendLine(string.Format("InnerException - {0}: {1}", ex.InnerException.GetType().Name, ex.InnerException.Message));
                SetTextbox1Text(sb.ToString());
                SetProgressValue(0);
            }
            else
            {
                ShowResult(t.Result);
                MessageBox.Show("完了しました。");
            }
        }

        private void ShowResult(TplWorker worker)
        {
            string msg = FormatResult(this._start, this._stop,
                worker, this._clickThreadId, this._callbackThreadId);

            SetTextbox1Text(msg);
        }

        private static string FormatResult(DateTime start, DateTime stop,
            TplWorker worker, int threadIdStart, int threadIdCallBack)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("開始:{0:HH:mm:ss.fff} @スレッド]{1}",
                start, threadIdStart));
            for (int i = 0; i < worker.OutputData.Length; i++)
                sb.AppendFormat("{0}: '{1}'\r\n", i, worker.OutputData[i]);
            sb.AppendLine(string.Format("終了:{0:HH:mm:ss.fff} @スレッド]{1}",
                stop, threadIdCallBack));

            return sb.ToString();
        }

        private delegate void SetTextbox1TextDelegate(string msg);
        private delegate void SetButtonEnabledDelegate();

        private void SetTextbox1Text(string msg)
        {
            if (this.InvokeRequired)
                this.Invoke(new SetTextbox1TextDelegate(SetTextbox1Text), msg);
            else
                this.textBox1.Text = msg;
        }

        private void SetButtonEnabled()
        {
            if (this.InvokeRequired)
                this.Invoke(new SetButtonEnabledDelegate(SetButtonEnabled));
            else
            {
                this.button1.Enabled = true;
                this.button2.Enabled = false;
            }
        }

        private void SetProgressValue(int percent)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new SetProgressValueDelegate(SetProgressValue), percent);
            } else {
                this.progressBar1.Value = percent;
                Console.WriteLine("バー更新={0}", percent);
            }
        }
    }
}
