#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using System.Linq;
using System.Threading;
using System.ComponentModel.Design;
using FTOptix.WebUI;
using System.IO;
using System.Timers;
#endregion

public class BuzzwordBingo : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
        LogUser();
        Shuffle();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void Shuffle()
    {
        var buzzwordPath = ResourceUri.FromProjectRelativePath("buzzwords.txt").Uri;
        string[] buzzwordArray = File.ReadAllLines(buzzwordPath);

        if (buzzwordArray.Length < 25)
        {
            Log.Warning("You have less than the minimum of 25 buzzwords...");
            CallDialogBox("UI/Screens/NotEnoughBuzzwords");
        }
        else
        {
            var random = new Random();
            var randomArray = buzzwordArray.OrderBy(x => random.Next()).ToArray();
            var bingoArray = new string[5, 5];
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    bingoArray[i, j] = randomArray[i * 5 + j];
                }
            }
            var node = Owner.Get("VerticalLayout1");
            var row = 0;
            var column = 0;
            foreach (var child in node.Children)
            {
                for (int i = 0; i < node.Children.Count; i++)
                {
                    var checkbox = InformationModel.Get<CheckBox>(child.Children.ElementAt(i).NodeId);
                    checkbox.Text = bingoArray[row, column];
                    column++;
                }
                column = 0;
                row++;
            }
            var middle = Owner.Get<CheckBox>("VerticalLayout1/HorizontalLayout3/Checkbox3");
            middle.Text = "FREE";
        }
    }

    [ExportMethod]
    public void ClearChecks()
    {
        var node = Owner.Get("VerticalLayout1");
        var row = 0;
        var column = 0;
        foreach (var child in node.Children)
        {
            for (int i = 0; i < node.Children.Count; i++)
            {
                var checkbox = InformationModel.Get<CheckBox>(child.Children.ElementAt(i).NodeId);
                checkbox.Checked = false;
                column++;
            }
            column = 0;
            row++;
        }
    }

    [ExportMethod]
    public void CheckScore()
    {
        LogicObject.GetVariable("WinnerFlag").Value = false;
        var node = Owner.Get("VerticalLayout1");
        var row = 0;
        var column = 0;
        var score = new bool[5, 5];
        foreach (var child in node.Children)
        {
            for (int i = 0; i < node.Children.Count; i++)
            {
                var checkbox = InformationModel.Get<CheckBox>(child.Children.ElementAt(i).NodeId);
                score[row, column] = checkbox.Checked;
                column++;
            }
            column = 0;
            row++;
        }
        var checkWinner = CheckWinner(score);
        LogicObject.GetVariable("WinnerFlag").Value = checkWinner.Winner;
        if (checkWinner.Winner) Log.Info(checkWinner.Message);
        if (checkWinner.Winner) CallDialogBox("UI/Screens/YouWin");
    }

    private static (bool Winner, string Message) CheckWinner(bool[,] score)
    {
        var winner = false;

        // check rows
        for (int r = 0; r < score.GetLength(0); r++)  // row
        {
            //winner = false;
            for (int c = 0; c < score.GetLength(1); c++)  // column
            {
                winner = score[r, c];
                if (!winner) break;
            }
            if (winner) return (Winner: winner, Message: $"winner row {r}");
        }

        // check columns
        for (int c = 0; c < score.GetLength(1); c++)  // column
        {
            //winner = false;
            for (int r = 0; r < score.GetLength(0); r++)  // row
            {
                winner = score[r, c];
                if (!winner) break;
            }
            if (winner) return (Winner: winner, Message: $"winner column {c}");
        }

        // check diag1 UpLeft-LowRight
        for (int i = 0; i < score.GetLength(0); i++)
        {
            //winner = false;
            winner = score[i, i];
            if (!winner) break;
        }
        if (winner) return (Winner: winner, Message: $"winner diag1 UpLeft-LowRight");

        // check diag2 UpRight-LowLeft
        for (int i = 0; i < score.GetLength(0); i++)
        {
            //winner = false;
            winner = score[score.GetLength(0) - 1 - i, i];
            if (!winner) break;
        }
        if (winner) return (Winner: winner, Message: $"winner diag2 UpRight-LowLeft");

        return (Winner: false, Message: $"Le-Hoo...Ze-Herr");
    }

    private void CallDialogBox(string path)
    {
        try
        {
            //var myDialog = InformationModel.Get<DialogType>(LogicObject.GetVariable("DialogValue" + e.NewValue.Value.ToString()).Value);
            var myDialog = Project.Current.Get<DialogType>(path);
            UICommands.OpenDialog((Window)Owner, myDialog);
        }
        catch
        {
            Log.Warning($"No corresponding DialogBox found at {path}");
        }
    }

    private void LogUser()
    {
        var ip = Owner.Owner.GetVariable("IpAddress").Value.ToString();
        var isWeb = Owner.Owner.GetVariable("IsWeb").Value.ToString();

        Log.Info($"Buzzword Bingo started by {Environment.UserName} from {ip}");
    }

}
