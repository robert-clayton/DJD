using Godot;
using System;

public class Globals : Node{
    [Signal] public delegate float TimeScaleChanged();
    [Signal] public delegate float MusicDbChanged();
    [Signal] public delegate float PlayerSfxDbChanged();
    [Signal] public delegate float EnemySfxDbChanged();
    [Signal] public delegate bool EnemySfxPaused();
    [Signal] public delegate bool PlayerSfxPaused();
    [Signal] public delegate bool MusicPaused();

    public Node CurrentScene { get; set; }
    public float minDb = -15f;
    private float timeScale = 1f;
    private float musicDb = 0f;
    private float playerSfxDb = 0f;
    private float enemySfxDb = 0f;
    
    public float TimeScale { 
        get { return timeScale; }
        set {
            timeScale = value;
            EmitSignal("TimeScaleChanged", value);
        }
    }

    public float MusicDb { 
        get { return musicDb; }
        set {
            musicDb = value;
            EmitSignal("MusicPaused", musicDb == minDb);
            
            EmitSignal("MusicDbChanged", value);
        }
    }

    public float PlayerSfxDb { 
        get { return playerSfxDb; }
        set {
            playerSfxDb = value;
            EmitSignal("PlayerSfxPaused", playerSfxDb == minDb);
            EmitSignal("PlayerSfxDbChanged", value);
        }
    }

    public float EnemySfxDb { 
        get { return enemySfxDb; }
        set {
            enemySfxDb = value;
            EmitSignal("EnemySfxPaused", enemySfxDb == minDb);
            EmitSignal("EnemySfxDbChanged", value);
        }
    }

    public override void _Ready(){
        Viewport root = GetTree().Root;
        CurrentScene = root.GetChild(root.GetChildCount() - 1);
    }
}
