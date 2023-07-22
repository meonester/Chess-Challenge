using ChessChallenge.API;
using System.Diagnostics;

namespace ChessChallenge.Example
{
    public class StockfishBot : IChessBot
    {
        private bool started = false;
        private static readonly int timeBudget = 200; 
        private static Process engineProcess;
        private static Board currentBoard;
        private static Move? bestMove;

        public void CreateUCIProcess(string uciPath = "./stockfish.exe")
        {
            ProcessStartInfo si = new ProcessStartInfo() {
                FileName = uciPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };

            engineProcess = new Process();
            engineProcess.StartInfo = si;
            try {
                // throws an exception on win98
                engineProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
            } catch { }

            engineProcess.OutputDataReceived += new DataReceivedEventHandler(engineProcessOutputDataReceived);
            engineProcess.ErrorDataReceived += new DataReceivedEventHandler(engineProcessErrorDataReceived);

            engineProcess.Start();
            engineProcess.BeginErrorReadLine();
            engineProcess.BeginOutputReadLine();

            engineSendLine("uci");
            engineSendLine("isready");
        }

        private static void engineSendLine(string command) {
            engineProcess.StandardInput.WriteLine(command);
            engineProcess.StandardInput.Flush();
        }

        private static void engineProcessOutputDataReceived(object sender, DataReceivedEventArgs e) {
            string text = e.Data ?? "";
            if(text.StartsWith("bestmove ")){
                var temp = text.Replace("bestmove ", "");
                var ponderIndex = temp.IndexOf(" ponder");
                var uciMove = ponderIndex < 0 ? temp : temp.Remove(ponderIndex);
                bestMove = new Move(uciMove, currentBoard);
            }
            //Debug.WriteLine("[UCI] " + text);
        }

        private static void engineProcessErrorDataReceived(object sender, DataReceivedEventArgs e) {
            string text = e.Data ?? "";
            Debug.WriteLine("[Error] " + text);
        }

        public Move Think(Board board, Timer timer)
        {
            if(engineProcess == null)
                CreateUCIProcess();
            if(!started){
                engineSendLine("ucinewgame");
                started = true;
            }

            bestMove = null;
            currentBoard = board;

            var boardState = board.GetFenString();

            engineSendLine("position fen " + boardState);
            engineSendLine("go movetime " + timeBudget);

            System.Threading.Thread.Sleep(timeBudget);
            while(bestMove == null)
                System.Threading.Thread.Sleep(1);

            return bestMove.Value;
        }
    }
}