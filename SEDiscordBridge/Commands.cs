using NLog;
using System.Net.Http;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.Game.World;
using System.Threading;
using DSharpPlus.Entities;

namespace SEDiscordBridge
{
    [Category("sedb")]
    public class Commands : CommandModule {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public SEDiscordBridgePlugin Plugin => (SEDiscordBridgePlugin)Context.Plugin;

        [Command("reload", "Reload SEDB Service")]
        [Permission(MyPromoteLevel.Admin)]
        public void ReloadBridge()
        {
            if (Plugin.Config.Enabled) {
                Plugin.UnloadSEDB();
                Thread.Sleep(100);
                Plugin.LoadSEDB();
                Context.Respond("SEDB plugin reloaded!");
            }
            else
                Context.Respond("SEDB plugin Disabled!");
        }

        [Command("reloadconfig", "Reload current SEDB configuration")]
        [Permission(MyPromoteLevel.Admin)]
        public void ReloadBridgeConfig() {
            Plugin.InitConfig();
            Plugin.DDBridge?.SendStatus(null, UserStatus.DoNotDisturb);

            if (Plugin.Config.Enabled) {
                if (Plugin.Torch.CurrentSession == null && !Plugin.Config.PreLoad) {
                    Plugin.UnloadSEDB();

                }
                else {
                    Plugin.LoadSEDB();
                }
            }
            else {
                Plugin.UnloadSEDB();
            }
            Context.Respond("SEDB plugin reloaded!");
        }

        [Command("link", "Link you steamID to a discord account")]
        [Permission(MyPromoteLevel.None)]
        public async void Link() {
            IMyPlayer player = Context.Player;
            if (player == null) {
                Context.Respond("Command cannot be ran from console");
                return;
            }

            HttpResponseMessage response;
            using (HttpClient clients = new HttpClient()) {
                List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>
                {
                        new KeyValuePair<string, string>("steamid",Context.Player.SteamUserId.ToString()),
                };
                FormUrlEncodedContent content = new FormUrlEncodedContent(pairs);
                response = await clients.PostAsync("http://sedb.uk/discord/guid-manager.php", content);
            }

            string texts = await response.Content.ReadAsStringAsync();

            Dictionary<string, string> kvp = Utils.ParseQueryString(texts);

            if (kvp["existance"] == "false") {
                MyVisualScriptLogicProvider.OpenSteamOverlay($"https://steamcommunity.com/linkfilter/?url=http://sedb.uk/?guid={kvp["guid"]}&steamid={Context.Player.SteamUserId}", Context.Player.IdentityId);
                Context.Respond("A browser window has been opened... Please continue there.");
            }
            else {
                Context.Respond("Your SteamId has already been linked to discord, if this has not been authenticated by yourself... Please contact your admin");
            }
        }

        [Command("get")]
        [Permission(MyPromoteLevel.Admin)]
        public async void GetUser(string playername) {
            try {
                bool found = false;
                Utils utils = new Utils();
                var player = Utils.GetPlayerByNameOrId(playername);
                string uSteamid = "0";

                if (player != null) {
                    uSteamid = player.Id.SteamId.ToString();
                    found = true;
                }

                else {
                    foreach (var member in MySession.Static.Players.GetAllPlayers()) {
                        if (playername == member.SteamId.ToString()) {
                            uSteamid = member.SteamId.ToString();
                            found = true;
                        }
                    }
                    if (!found)
                        Context.Respond("Player not found or steamID not valid");
                }

                Dictionary<string, string> kvp = Utils.ParseQueryString(await Utils.DataRequest(uSteamid, Context.Plugin.Id.ToString(), "get_discord_name"));
                if (kvp["error_code"] == "0") {
                    Context.Respond($"The user's discord name is {kvp["data"]}");
                }
                if (kvp["error_code"] == "1") {
                    Context.Respond("Unable to find linked account - Please make sure you have linked an account");
                    Log.Warn(kvp["error_message"]);
                }
                if (kvp["error_code"] == "2") {
                    Context.Respond("Unable to get data - see log for more info");
                    Log.Warn("Unauthorised attempt to access data - Contact Bishbash777");
                }
                if (kvp["error_code"] == "3") {
                    Context.Respond("API error... Please see log");
                    Log.Warn(kvp["error_message"]);
                }

            }
            catch (System.Exception e) {
                Log.Warn(e.ToString());
            }
        }

        [Command("verify", "If you have linked your discord account, you can verify the link by entering this command")]
        [Permission(MyPromoteLevel.None)]
        public async void GetMyId() {
            string uSteamid = Context.Player.SteamUserId.ToString();
            Dictionary<string, string> kvp = Utils.ParseQueryString(await Utils.DataRequest(uSteamid, Context.Plugin.Id.ToString(), "get_discord_name"));
            if (kvp["error_code"] == "0") {
                Context.Respond($"Your discord name is {kvp["data"]}");
            }
            if (kvp["error_code"] == "1") {
                Context.Respond("Unable to find linked account - Please make sure you have linked an account");
                Log.Warn(kvp["error_message"]);
            }
            if (kvp["error_code"] == "2") {
                Context.Respond("Unable to get data - see log for more info");
                Log.Warn("Unauthorised attempt to access data - Contact Bishbash777");
            }
            if (kvp["error_code"] == "3") {
                Context.Respond("API error... Please see log");
                Log.Warn(kvp["error_message"]);
            }
        }

        [Command("unlink", "If you have linked your discord account, you can verify the link by entering this command")]
        [Permission(MyPromoteLevel.None)]
        public async void Unlink() {

            IMyPlayer player = Context.Player;
            if (player == null) {
                Context.Respond("Command cannot be ran from console");
                return;
            }
            string uSteamid = Context.Player.SteamUserId.ToString();
            await Utils.DataRequest(uSteamid, Context.Plugin.Id.ToString(), "unlink");
            Context.Respond("Your discord account has been unlinked! You may now link your account again");
        }

        [Command("enable", "To enable SEDB if disabled")]
        [Permission(MyPromoteLevel.Admin)]
        public void EnableBridge()
        {
            Plugin.LoadSEDB();
            Context.Respond("SEDB plugin enabled!");
        }

        [Command("disable", "To disable SEDB if enabled")]
        [Permission(MyPromoteLevel.Admin)]
        public void DisableBridge()
        {
            Plugin.UnloadSEDB();
            Context.Respond("SEDB plugin disabled!");
        }
    }
}
