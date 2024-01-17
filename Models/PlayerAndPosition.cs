using System;
namespace sm_coding_challenge.Models;

public enum PlayPosition { kicking, passing, receiving, rushing }

public class PlayerAndPosition
{
    public PlayPosition Position { get; set; }

    public PlayerModel Player { get; set; }
}

