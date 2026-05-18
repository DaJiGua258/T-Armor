using QFramework.Manager;
using QFramework.ViewController.Player;
using QFramework.ViewController.UI;
using UnityEngine;

namespace QFramework.ViewController.Mission
{
    public class GeneratorInteractable : Interactable
    {
        private CommandEntry[] _commands;

        public void SetCommands(CommandEntry cmd1, CommandEntry cmd2, CommandEntry cmd3)
        {
            _commands = new CommandEntry[] { cmd1, cmd2, cmd3 };
        }

        public override void OnInteract(GameObject player)
        {
            if (IsLocked) return;
            base.OnInteract(player);

            if (_commands != null)
            {
                var panel = UIGameManager.Instance.GetComponentInChildren<TerminalPanel>(true);
                panel.ShowWithCommands(_commands[0], _commands[1], _commands[2]);
            }
        }
    }
}
