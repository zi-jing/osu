// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Graphics.Backgrounds;
using osu.Game.Online.API;
using osu.Game.Online.Multiplayer;
using osu.Game.Screens.OnlinePlay.Components;
using osuTK;

namespace osu.Game.Screens.OnlinePlay.Multiplayer.Match
{
    public class MultiplayerReadyButton : MultiplayerRoomComposite
    {
        public Action OnReadyClick
        {
            set => button.Action = value;
        }

        [Resolved]
        private IAPIProvider api { get; set; }

        [Resolved]
        private OsuColour colours { get; set; }

        [Resolved]
        private OngoingOperationTracker ongoingOperationTracker { get; set; }

        private IBindable<bool> operationInProgress;

        private Sample sampleReadyCount;

        private readonly ButtonWithTrianglesExposed button;

        private int countReady;

        public MultiplayerReadyButton()
        {
            InternalChild = button = new ButtonWithTrianglesExposed
            {
                RelativeSizeAxes = Axes.Both,
                Size = Vector2.One,
                Enabled = { Value = true },
            };
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            sampleReadyCount = audio.Samples.Get(@"SongSelect/select-difficulty");

            operationInProgress = ongoingOperationTracker.InProgress.GetBoundCopy();
            operationInProgress.BindValueChanged(_ => updateState());
        }

        protected override void OnRoomUpdated()
        {
            base.OnRoomUpdated();

            updateState();
        }

        private void updateState()
        {
            var localUser = Client.LocalUser;

            int newCountReady = Room?.Users.Count(u => u.State == MultiplayerUserState.Ready) ?? 0;
            int newCountTotal = Room?.Users.Count(u => u.State != MultiplayerUserState.Spectating) ?? 0;

            switch (localUser?.State)
            {
                default:
                    button.Text = "Ready";
                    updateButtonColour(true);
                    break;

                case MultiplayerUserState.Spectating:
                case MultiplayerUserState.Ready:
                    string countText = $"({newCountReady} / {newCountTotal} ready)";

                    if (Room?.Host?.Equals(localUser) == true)
                    {
                        button.Text = $"Start match {countText}";
                        updateButtonColour(true);
                    }
                    else
                    {
                        button.Text = $"Waiting for host... {countText}";
                        updateButtonColour(false);
                    }

                    break;
            }

            bool enableButton = Room?.State == MultiplayerRoomState.Open && !operationInProgress.Value;

            // When the local user is the host and spectating the match, the "start match" state should be enabled if any users are ready.
            if (localUser?.State == MultiplayerUserState.Spectating)
                enableButton &= Room?.Host?.Equals(localUser) == true && newCountReady > 0;

            button.Enabled.Value = enableButton;

            if (newCountReady != countReady)
            {
                countReady = newCountReady;
                Scheduler.AddOnce(playSound);
            }
        }

        private void playSound()
        {
            if (sampleReadyCount == null)
                return;

            var channel = sampleReadyCount.GetChannel();
            channel.Frequency.Value = 0.77f + countReady * 0.06f;
            channel.Play();
        }

        private void updateButtonColour(bool green)
        {
            if (green)
            {
                button.BackgroundColour = colours.Green;
                button.Triangles.ColourDark = colours.Green;
                button.Triangles.ColourLight = colours.GreenLight;
            }
            else
            {
                button.BackgroundColour = colours.YellowDark;
                button.Triangles.ColourDark = colours.YellowDark;
                button.Triangles.ColourLight = colours.Yellow;
            }
        }

        private class ButtonWithTrianglesExposed : ReadyButton
        {
            public new Triangles Triangles => base.Triangles;
        }
    }
}
