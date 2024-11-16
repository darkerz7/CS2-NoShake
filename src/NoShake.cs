using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.UserMessages;
using ClientPrefsAPI;
using CounterStrikeSharp.API.Core.Translations;
using Microsoft.Extensions.Localization;
using CounterStrikeSharp.API.Core.Capabilities;

namespace CS2_NoShake
{
	public class NoShake : BasePlugin
	{
		static bool[] g_bNoShake = new bool[65];
		static IClientPrefsAPI? _CP_api;
		static IStringLocalizer? Strlocalizer;

		public override string ModuleName => "NoShake";
		public override string ModuleDescription => "Disable env_shake";
		public override string ModuleAuthor => "DarkerZ [RUS]";
		public override string ModuleVersion => "1.DZ.1";
		public override void OnAllPluginsLoaded(bool hotReload)
		{
			try
			{
				PluginCapability<IClientPrefsAPI> CapabilityEW = new("clientprefs:api");
				_CP_api = IClientPrefsAPI.Capability.Get();
			}
			catch (Exception)
			{
				_CP_api = null;
				PrintToConsole("ClientPrefs API Failed!");
			}

			if (hotReload)
			{
				Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false }).ToList().ForEach(player =>
				{
					GetValue(player);
				});
			}
		}
		public override void Load(bool hotReload)
		{
			Strlocalizer = Localizer;
			RegisterEventHandler<EventPlayerConnectFull>(OnEventPlayerConnectFull);
			RegisterEventHandler<EventPlayerDisconnect>(OnEventPlayerDisconnect);
			HookUserMessage(120, OnShake, HookMode.Pre);
		}

		public override void Unload(bool hotReload)
		{
			RemoveCommand("css_noshake", OnCommandNoShake);
			RemoveCommand("css_shake", OnCommandNoShake);
			DeregisterEventHandler<EventPlayerConnectFull>(OnEventPlayerConnectFull);
			DeregisterEventHandler<EventPlayerDisconnect>(OnEventPlayerDisconnect);
			UnhookUserMessage(120, OnShake, HookMode.Pre);
		}

		private HookResult OnShake(UserMessage um)
		{
			Utilities.GetPlayers().ForEach(player =>
			{
				if (player != null && player.IsValid && g_bNoShake[player.Slot])
				{
					um.Recipients.Remove(player);
				}
			});
			return HookResult.Continue;
		}

		private HookResult OnEventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;
			if (player != null && player.IsValid) g_bNoShake[player.Slot] = false;
			return HookResult.Continue;
		}

		private HookResult OnEventPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
		{
			GetValue(@event.Userid);
			return HookResult.Continue;
		}

		[ConsoleCommand("css_noshake", "Disables or enables screen shakes")]
		[ConsoleCommand("css_shake", "Disables or enables screen shakes")]
		[CommandHelper(minArgs: 0, usage: "", whoCanExecute: CommandUsage.CLIENT_ONLY)]
		public void OnCommandNoShake(CCSPlayerController? player, CommandInfo command)
		{
			if (player == null || !player.IsValid) return;
			if (g_bNoShake[player.Slot])
			{
				g_bNoShake[player.Slot] = false;
				SetValue(player);
				ReplyToCommand(player, command.CallingContext == CommandCallingContext.Console, "Message_NoShake_Disabled");
			}
			else
			{
				g_bNoShake[player.Slot] = true;
				SetValue(player);
				ReplyToCommand(player, command.CallingContext == CommandCallingContext.Console, "Message_NoShake_Enabled");
			}
		}

		async void GetValue(CCSPlayerController? player)
		{
			if (player == null || !player.IsValid) return;
			if (_CP_api != null)
			{
				string sValue = await _CP_api.GetClientCookie(player.SteamID.ToString(), "NoShake");
				int iValue;
				if (string.IsNullOrEmpty(sValue) || !Int32.TryParse(sValue, out iValue)) iValue = 0;
				if (iValue == 0) g_bNoShake[player.Slot] = false;
				else g_bNoShake[player.Slot] = true;
			}
		}

		async void SetValue(CCSPlayerController? player)
		{
			if (player == null || !player.IsValid) return;
			if (_CP_api != null)
			{
				if (g_bNoShake[player.Slot]) await _CP_api.SetClientCookie(player.SteamID.ToString(), "NoShake", "1");
				else await _CP_api.SetClientCookie(player.SteamID.ToString(), "NoShake", "0");
			}
		}

		static void ReplyToCommand(CCSPlayerController player, bool bConsole, string sMessage, params object[] arg)
		{
			if (Strlocalizer == null) return;
			Server.NextFrame(() =>
			{
				if (player is { IsValid: true, IsBot: false, IsHLTV: false })
				{
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						if (!bConsole) player.PrintToChat($" \x0B[\x04NoShake\x0B]\x01{Strlocalizer[sMessage, arg]}");
						else player.PrintToConsole($"[NoShake]{Strlocalizer[sMessage, arg]}");
					}
				}
			});
		}
		public static void PrintToConsole(string sMessage, int iColor = 1)
		{
			Console.ForegroundColor = (ConsoleColor)8;
			Console.Write("[");
			Console.ForegroundColor = (ConsoleColor)6;
			Console.Write("NoShake");
			Console.ForegroundColor = (ConsoleColor)8;
			Console.Write("] ");
			Console.ForegroundColor = (ConsoleColor)iColor;
			Console.WriteLine(sMessage, false);
			Console.ResetColor();
			/* Colors:
				* 0 - No color		1 - White		2 - Red-Orange		3 - Orange
				* 4 - Yellow		5 - Dark Green	6 - Green			7 - Light Green
				* 8 - Cyan			9 - Sky			10 - Light Blue		11 - Blue
				* 12 - Violet		13 - Pink		14 - Light Red		15 - Red */
		}
	}
}
