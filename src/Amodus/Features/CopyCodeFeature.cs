using System;
using InnerNet;
using UnityEngine;

namespace Amodus.Features;

/// <summary>
/// Copies the code of the lobby to the clipboard when the player leaves the lobby or gets disconnected from it
/// (the host left, a kick, a lost connection), so everybody can paste the code and be back in a few seconds.
/// The feature does not hook into any "leave" method of the game. It watches the connection every frame instead,
/// so every way out of a lobby is noticed in the same way.
/// </summary>
internal sealed class CopyCodeFeature : Feature
{
    // The game may say "not in a lobby" for a moment while scenes change. Only a longer absence counts as leaving.
    private const float LeaveDelaySeconds = 0.75f;

    private int _gameId = int.MinValue;
    private string _gameCode;

    private string _lastCode = string.Empty;
    private string _copiedCode = string.Empty;
    private bool _copyFailed;
    private bool _inLobby;
    private float _missingSince = -1f;
    private string _description = "Copies the lobby code when you leave";

    public override string Id => "CopyCode";

    public override string Title => "Copy lobby code";

    public override string Category => "Lobby";

    public override string Description => _description;

    public override bool EnabledByDefault => true;

    public override void OnUpdate()
    {
        var code = ReadLobbyCode();

        if (code != null)
        {
            if (!_inLobby || code != _lastCode)
            {
                _lastCode = code;
                _inLobby = true;
                _description = "Lobby " + code + ": copied when you leave";
            }

            _missingSince = -1f;
            return;
        }

        if (!_inLobby)
            return;

        float now = Time.unscaledTime;
        if (_missingSince < 0f)
        {
            _missingSince = now;
            return;
        }

        if (now - _missingSince < LeaveDelaySeconds)
            return;

        _inLobby = false;
        _missingSince = -1f;

        if (Enabled)
            CopyLastCode();
        else
            _description = "Off. Last lobby was " + _lastCode;
    }

    private void CopyLastCode()
    {
        if (Clipboard.Set(_lastCode))
        {
            _copiedCode = _lastCode;
            _copyFailed = false;
            _description = "Copied " + _lastCode + " to the clipboard";
            AmodusPlugin.LogSource.LogInfo($"Lobby code {_lastCode} copied to the clipboard.");
        }
        else
        {
            _copyFailed = true;
            _description = "Could not copy. Code was " + _lastCode;
            AmodusPlugin.LogSource.LogWarning($"Could not copy the lobby code {_lastCode} to the clipboard.");
        }
    }

    protected override void OnEnabled()
    {
        if (!_inLobby)
            _description = _copyFailed || _copiedCode.Length == 0 ? "Copies the lobby code when you leave" : "Last copied: " + _copiedCode;
    }

    /// <summary>The code of the online lobby the player is in right now, or null when the player is not in one.</summary>
    private string ReadLobbyCode()
    {
        var client = AmongUsClient.Instance;
        if (client == null)
            return null;

        // Free play and local (same network) games have no online lobby code.
        if (client.GameState == InnerNetClient.GameStates.NotJoined || client.NetworkMode != NetworkModes.OnlineGame)
            return null;

        if (client.ClientId == InnerNetClient.NoClientId)
            return null;

        int id = client.GameId;
        if (id != _gameId)
        {
            _gameId = id;
            _gameCode = ToCode(id);
        }

        return _gameCode;
    }

    private static string ToCode(int gameId)
    {
        try
        {
            var code = GameCode.IntToGameName(gameId);
            return IsValidCode(code) ? code : null;
        }
        catch (Exception e)
        {
            ErrorOnce.Log("CopyCode", e);
            return null;
        }
    }

    // Lobby codes are 4 or 6 capital letters.
    private static bool IsValidCode(string code)
    {
        if (string.IsNullOrEmpty(code) || (code.Length != 4 && code.Length != 6))
            return false;

        foreach (char c in code)
        {
            if (c < 'A' || c > 'Z')
                return false;
        }

        return true;
    }
}
