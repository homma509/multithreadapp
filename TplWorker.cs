using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    class TplWorker 
    {
        private const int CountOfNumbers = 10;

        public int[] InputData = new int[CountOfNumbers];
        public string[] OutputData = new string[CountOfNumbers];
        private int _CountOfDone;

        public TplWorker() {
            InitializeData();
        }

        private void InitializeData()
        {
            for (int i = 0; i < CountOfNumbers; i++) {
                InputData[i] = i + 1;
                OutputData[i] = null;
            }
        }

        public static TplWorker DoWork(SetProgressValueDelegate progress, CancellationToken token)
        {
            Console.WriteLine("DoWork先頭 ManagedThreadId={0}",
                               Thread.CurrentThread.ManagedThreadId);

            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = token;
            po.MaxDegreeOfParallelism = System.Environment.ProcessorCount;
            
            TplWorker instance = new TplWorker();

            try
            {
                Parallel.For(0, CountOfNumbers, po, index =>
                {
                    po.CancellationToken.ThrowIfCancellationRequested();
                    instance.ProcessANumber(progress, index);
                });
            }
            catch (OperationCanceledException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw e.InnerException; 
            }

            Console.WriteLine("DoWork末尾 ManagedThreadId={0}",
                               Thread.CurrentThread.ManagedThreadId);

            return instance;

        }

        internal void ProcessANumber(SetProgressValueDelegate progress, int index)
        {
            int input = InputData[index];
            string output = string.Format("{0:000}", input);
            RandomWait();
            OutputData[index] = output;

            Interlocked.Increment(ref this._CountOfDone);

            progress.Invoke((int)((double)this._CountOfDone / CountOfNumbers * 100));

            Console.WriteLine("OutputData[{0}] 終了 = {1}, ManagedThreadId={2}",
                                index, output, Thread.CurrentThread.ManagedThreadId);
        }

        private void RandomWait()
        {
            int waitTime = (new Random()).Next(500, 1000);
            Thread.Sleep(waitTime);
        }

    }
}
