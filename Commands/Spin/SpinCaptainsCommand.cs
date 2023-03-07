﻿using System.Text;
using Discord;
using Discord.WebSocket;
using TF2PugBot.Data;
using TF2PugBot.Extensions;
using TF2PugBot.Types;

namespace TF2PugBot.Commands.Spin;

public class SpinCaptainsCommand : BaseSpinCommand, ICommand
{
    /// <inheritdoc />
    public async Task PerformAsync (SocketSlashCommand command, SocketGuildUser caller)
    {
        if (caller.IsConnectedToVoice())
        {
            var connectedUsers = caller.VoiceChannel.ConnectedUsers;
            int playersInVoice = connectedUsers.Count;
            if (playersInVoice < 7)
            {
                await command.RespondAsync("Spin requires atleast 7 players, ignoring.", ephemeral: true);
                return;
            }

            ulong guildId = command.GuildId.GetValueOrDefault();

            EmbedBuilder  embedBuilder = new EmbedBuilder();
            StringBuilder sb           = new StringBuilder();

            embedBuilder.WithTitle("Spinning for Team Captain!");
            embedBuilder.WithColor(Color.Teal);

            try
            {
                if (DataManager.GuildGameHasEnded(guildId))
                {
                    MedicImmunePlayer[]? newImmunities = DataManager.GetTemporaryMedImmunePlayers(guildId);

                    if (newImmunities is not null)
                    {
                        foreach (MedicImmunePlayer player in newImmunities)
                        {
                            if (player is not null)
                            {
                                sb.AppendLine($"{player.DisplayName} will be granted med immunity for next medic spin");
                            }
                        }

                        embedBuilder.WithFooter(sb.ToString());

                        await DataManager.MakePermanentImmunitiesAsync(guildId);
                    }

                    await DataManager.TryEndGuildGame(guildId);
                }


                List<SocketGuildUser>? winners = await Spin(command, caller.VoiceChannel.ConnectedUsers, embedBuilder,
                                                            SpinMode.Duo, DataManager.InstantSpin);
                if (winners is not null)
                {
                    DataManager.StartNewGuildGame(guildId);
                    await DataManager.UpdatePlayerStatsAsync(guildId,
                                                             StatTypes.CaptainSpinsWon,
                                                             winners.Select(w => w.Id).ToArray());

                    if (DataManager.GuildHasPingsEnabled(guildId))
                    {
                        await command.FollowupAsync($"<@!{winners[0].Id}> and <@!{winners[1].Id}> are team captains!");
                    }
                    else
                    {
                        await command.FollowupAsync(
                            $"{winners[0].DisplayName} and {winners[1].DisplayName} are team captains!");
                    }

                    // await command.FollowupAsync($"winner");
                }

                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
        }

        await command.RespondAsync("You are not in a voice channel with other players!", ephemeral: true);
    }
}